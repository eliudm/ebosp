using EBOSP.Application.Identity;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Identity;

public sealed class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken) =>
        await context.RefreshTokens.AddAsync(token, cancellationToken);

    public Task<RefreshToken?> FindByHashIgnoringTenantAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.RefreshTokens.IgnoreQueryFilters().SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
}
