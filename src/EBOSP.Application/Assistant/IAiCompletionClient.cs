namespace EBOSP.Application.Assistant;

public enum AiMessageRole
{
    User,
    Assistant,
    ToolResult,
}

/// <summary>One callable tool offered to the model. InputSchemaJson is a raw JSON Schema string - simple enough for four fixed tools that hand-authoring it is clearer than a schema-building library.</summary>
public sealed record AiToolDefinition(string Name, string Description, string InputSchemaJson);

public sealed record AiToolCall(string Id, string Name, string InputJson);

public sealed record AiToolResult(string ToolCallId, string ResultJson, bool IsError);

/// <summary>
/// One turn in the conversation. A User message carries Text; an Assistant turn that wants to call
/// tools carries ToolCalls (optionally alongside explanatory Text); a ToolResult message carries
/// ToolResults answering the immediately preceding ToolCalls.
/// </summary>
public sealed record AiMessage(AiMessageRole Role, string? Text = null, IReadOnlyList<AiToolCall>? ToolCalls = null, IReadOnlyList<AiToolResult>? ToolResults = null);

/// <summary>What the model produced for one turn - final text, one or more tool calls, or both (a model may explain itself before calling a tool).</summary>
public sealed record AiCompletionTurn(string? Text, IReadOnlyList<AiToolCall> ToolCalls);

/// <summary>
/// Provider-agnostic seam over the LLM (dev guide §43) - the only Anthropic-specific wire format
/// lives behind the Infrastructure implementation of this interface, the same Clean Architecture
/// split already used for IObjectStorage/IPasswordResetNotifier.
/// </summary>
public interface IAiCompletionClient
{
    Task<AiCompletionTurn> CompleteAsync(string systemPrompt, IReadOnlyList<AiMessage> messages, IReadOnlyList<AiToolDefinition> tools, CancellationToken cancellationToken);
}
