namespace EBOSP.Contracts.Assistant;

public sealed record AskAssistantResponse(string Answer, IReadOnlyList<string> ToolsUsed);
