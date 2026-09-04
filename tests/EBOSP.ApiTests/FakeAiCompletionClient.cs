using System.Collections.Concurrent;
using EBOSP.Application.Assistant;

namespace EBOSP.ApiTests;

/// <summary>
/// Scriptable stand-in for <see cref="IAiCompletionClient"/> (same DI-swap pattern as
/// <see cref="CapturingPasswordResetNotifier"/>) - tests never call the real Anthropic API.
/// Scripts are keyed by the original user question text, so a multi-turn tool-use exchange (a
/// tool_use turn followed by a final text turn) is expressed as a queue of turns dequeued on each
/// successive <see cref="CompleteAsync"/> call for that same conversation. Also records, per
/// question, exactly which tools were offered in the catalogue - needed to prove a tool the caller
/// isn't authorized for was never even shown to the model, not just never called.
/// </summary>
public sealed class FakeAiCompletionClient : IAiCompletionClient
{
    private readonly ConcurrentDictionary<string, Queue<AiCompletionTurn>> _scripts = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _offeredTools = new();
    private readonly ConcurrentDictionary<string, List<IReadOnlyList<AiMessage>>> _callHistory = new();

    public void Script(string question, params AiCompletionTurn[] turns) => _scripts[question] = new Queue<AiCompletionTurn>(turns);

    public IReadOnlyList<string>? OfferedToolsFor(string question) => _offeredTools.GetValueOrDefault(question);

    /// <summary>Every CompleteAsync call's full message history for this question, in order - inspect a later call's ToolResult messages to see what a tool actually returned.</summary>
    public IReadOnlyList<IReadOnlyList<AiMessage>> CallHistoryFor(string question) => _callHistory.GetValueOrDefault(question) ?? [];

    public Task<AiCompletionTurn> CompleteAsync(string systemPrompt, IReadOnlyList<AiMessage> messages, IReadOnlyList<AiToolDefinition> tools, CancellationToken cancellationToken)
    {
        var question = messages[0].Text ?? string.Empty;
        _offeredTools[question] = tools.Select(t => t.Name).ToList();
        _callHistory.GetOrAdd(question, _ => []).Add(messages);

        if (_scripts.TryGetValue(question, out var queue) && queue.Count > 0)
        {
            return Task.FromResult(queue.Dequeue());
        }

        // Default: a plain-text answer, no tool call - a safe fallback for any unscripted question
        // (covers the "incorrect/ambiguous question" eval case with no setup needed).
        return Task.FromResult(new AiCompletionTurn("I don't have information about that.", []));
    }
}
