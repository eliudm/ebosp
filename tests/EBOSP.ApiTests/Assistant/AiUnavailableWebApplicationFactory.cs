namespace EBOSP.ApiTests.Assistant;

/// <summary>Leaves Program.cs's real IAiCompletionClient resolution in place (no Ai:ApiKey configured -> UnavailableAiCompletionClient) - used only to prove the 503 path itself works.</summary>
public sealed class AiUnavailableWebApplicationFactory : CustomWebApplicationFactory
{
    protected override bool RegisterFakeAiClient => false;
}
