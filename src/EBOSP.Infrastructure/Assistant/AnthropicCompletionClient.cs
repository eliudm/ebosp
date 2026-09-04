using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EBOSP.Application.Assistant;

namespace EBOSP.Infrastructure.Assistant;

/// <summary>
/// Real implementation of <see cref="IAiCompletionClient"/> against Anthropic's Messages API
/// (https://api.anthropic.com/v1/messages, protocol version "2023-06-01"). All Anthropic-specific
/// wire format (content blocks, tool_use/tool_result shapes) is translated here and never leaks
/// into the Application layer's provider-agnostic AiMessage/AiToolCall/AiCompletionTurn vocabulary.
/// </summary>
public sealed class AnthropicCompletionClient(HttpClient httpClient, AiOptions options) : IAiCompletionClient
{
    private const string ApiVersion = "2023-06-01";
    private const int MaxOutputTokens = 1024;

    public async Task<AiCompletionTurn> CompleteAsync(string systemPrompt, IReadOnlyList<AiMessage> messages, IReadOnlyList<AiToolDefinition> tools, CancellationToken cancellationToken)
    {
        var requestBody = BuildRequestBody(systemPrompt, messages, tools);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Add("x-api-key", options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", ApiVersion);

        using var httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Anthropic API request failed ({(int)httpResponse.StatusCode}): {responseBody}");
        }

        return ParseResponse(responseBody);
    }

    private JsonObject BuildRequestBody(string systemPrompt, IReadOnlyList<AiMessage> messages, IReadOnlyList<AiToolDefinition> tools)
    {
        var toolsArray = new JsonArray();
        foreach (var tool in tools)
        {
            toolsArray.Add(new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["input_schema"] = JsonNode.Parse(tool.InputSchemaJson),
            });
        }

        var messagesArray = new JsonArray();
        foreach (var message in messages)
        {
            messagesArray.Add(BuildWireMessage(message));
        }

        return new JsonObject
        {
            ["model"] = options.Model,
            ["max_tokens"] = MaxOutputTokens,
            ["system"] = systemPrompt,
            ["tools"] = toolsArray,
            ["messages"] = messagesArray,
        };
    }

    private static JsonObject BuildWireMessage(AiMessage message) => message.Role switch
    {
        AiMessageRole.User => new JsonObject { ["role"] = "user", ["content"] = message.Text ?? string.Empty },
        AiMessageRole.Assistant => new JsonObject { ["role"] = "assistant", ["content"] = BuildAssistantContent(message) },
        // Anthropic sends tool results back as a "user" role message containing tool_result blocks.
        AiMessageRole.ToolResult => new JsonObject { ["role"] = "user", ["content"] = BuildToolResultContent(message) },
        _ => throw new InvalidOperationException($"Unhandled message role '{message.Role}'."),
    };

    private static JsonArray BuildAssistantContent(AiMessage message)
    {
        var content = new JsonArray();
        if (!string.IsNullOrEmpty(message.Text))
        {
            content.Add(new JsonObject { ["type"] = "text", ["text"] = message.Text });
        }

        foreach (var call in message.ToolCalls ?? [])
        {
            content.Add(new JsonObject
            {
                ["type"] = "tool_use",
                ["id"] = call.Id,
                ["name"] = call.Name,
                ["input"] = string.IsNullOrWhiteSpace(call.InputJson) ? new JsonObject() : JsonNode.Parse(call.InputJson),
            });
        }

        return content;
    }

    private static JsonArray BuildToolResultContent(AiMessage message)
    {
        var content = new JsonArray();
        foreach (var result in message.ToolResults ?? [])
        {
            content.Add(new JsonObject
            {
                ["type"] = "tool_result",
                ["tool_use_id"] = result.ToolCallId,
                ["content"] = result.ResultJson,
                ["is_error"] = result.IsError,
            });
        }

        return content;
    }

    private static AiCompletionTurn ParseResponse(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var content = document.RootElement.GetProperty("content");

        string? text = null;
        var toolCalls = new List<AiToolCall>();
        foreach (var block in content.EnumerateArray())
        {
            var type = block.GetProperty("type").GetString();
            if (type == "text")
            {
                text = (text ?? string.Empty) + block.GetProperty("text").GetString();
            }
            else if (type == "tool_use")
            {
                var id = block.GetProperty("id").GetString()!;
                var name = block.GetProperty("name").GetString()!;
                var input = block.GetProperty("input").GetRawText();
                toolCalls.Add(new AiToolCall(id, name, input));
            }
        }

        return new AiCompletionTurn(text, toolCalls);
    }
}
