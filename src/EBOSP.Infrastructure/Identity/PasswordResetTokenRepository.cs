using EBOSP.Application.Identity;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Identity;

public sealed class PasswordResetTokenRepository(AppDbContext context) : IPasswordResetTokenRepository
{
    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken) =>
        await context.PasswordResetTokens.AddAsync(token, cancellationToken);

    public Task<PasswordResetToken?> FindByHashIgnoringTenantAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.PasswordResetTokens.IgnoreQueryFilters().SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
}
