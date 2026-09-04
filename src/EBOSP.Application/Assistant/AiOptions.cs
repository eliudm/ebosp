namespace EBOSP.Application.Assistant;

/// <summary>
/// Bound from the "Ai" configuration section. Unlike Jwt/ConnectionStrings, ApiKey has no default
/// and is never startup-validated - spec §26 labels the assistant an "Optional Differentiator", so
/// an absent key disables just this one feature at request time (503) instead of making the whole
/// app unstartable.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string? ApiKey { get; init; }

    public string Model { get; init; } = "claude-sonnet-5";

    public int MaxToolCallsPerRequest { get; init; } = 3;

    public int TimeoutSeconds { get; init; } = 30;
}
