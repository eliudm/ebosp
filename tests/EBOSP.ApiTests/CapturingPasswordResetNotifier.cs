using System.Collections.Concurrent;
using EBOSP.Application.Identity;

namespace EBOSP.ApiTests;

public sealed class CapturingPasswordResetNotifier : IPasswordResetNotifier
{
    private readonly ConcurrentDictionary<string, string> _tokensByEmail = new();

    public Task NotifyAsync(string email, string resetToken, CancellationToken cancellationToken)
    {
        _tokensByEmail[email] = resetToken;
        return Task.CompletedTask;
    }

    public string? GetTokenFor(string email) => _tokensByEmail.GetValueOrDefault(email);
}
