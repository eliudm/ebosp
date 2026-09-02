using System.Net;
using System.Net.Http.Json;
using EBOSP.Contracts.Identity;

namespace EBOSP.ApiTests;

/// <summary>
/// Verifies the login rate limiter actually rejects excess requests (spec §14: "Apply rate
/// limiting to authentication and sensitive endpoints") - runs against its own isolated factory
/// instance with the real per-IP limiter enabled, so it can't collide with the rest of the suite
/// (which disables rate limiting - see CustomWebApplicationFactory).
/// </summary>
public class RateLimitingTests(RateLimitEnforcingWebApplicationFactory factory) : IClassFixture<RateLimitEnforcingWebApplicationFactory>
{
    [Fact]
    public async Task Login_ExceedsRateLimit_Returns429()
    {
        var client = factory.CreateClient();
        var request = new LoginRequest { Email = "nobody@example.com", Password = "WrongPassword123!" };

        HttpStatusCode? rejectedStatus = null;
        for (var i = 0; i < 25 && rejectedStatus is null; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rejectedStatus = response.StatusCode;
            }
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedStatus);
    }
}
