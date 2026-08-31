using EBOSP.Contracts.Identity;

namespace EBOSP.Application.Identity;

public interface IAuthService
{
    Task<TokenResponse> LoginAsync(LoginRequest request, string? clientIp, CancellationToken cancellationToken);

    Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, string? clientIp, CancellationToken cancellationToken);

    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
}
