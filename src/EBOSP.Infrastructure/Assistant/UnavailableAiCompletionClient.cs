using EBOSP.Application.Assistant;
using EBOSP.Application.Common;

namespace EBOSP.Infrastructure.Assistant;

/// <summary>
/// Resolved instead of AnthropicCompletionClient when Ai:ApiKey is unset - the AI assistant is
/// spec §26's own "Optional Differentiator", so an absent key disables just this one feature at
/// request time (503) rather than failing the whole app at startup.
/// </summary>
public sealed class UnavailableAiCompletionClient : IAiCompletionClient
{
    public Task<AiCompletionTurn> CompleteAsync(string systemPrompt, IReadOnlyList<AiMessage> messages, IReadOnlyList<AiToolDefinition> tools, CancellationToken cancellationToken) =>
        throw new ServiceUnavailableException("The AI assistant is not configured.");
}
