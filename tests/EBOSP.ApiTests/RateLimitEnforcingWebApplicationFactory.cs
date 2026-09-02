namespace EBOSP.ApiTests;

/// <summary>Opts back into the real per-IP rate limiter - used only by RateLimitingTests, in its own isolated factory instance.</summary>
public sealed class RateLimitEnforcingWebApplicationFactory : CustomWebApplicationFactory
{
    protected override bool DisableRateLimiting => false;
}
