using EBOSP.Contracts.Identity;

namespace EBOSP.Application.Identity;

public interface ITenantService
{
    /// <summary>Self-service tenant onboarding: creates the tenant and its first tenant-admin user in one transaction (spec §30).</summary>
    Task<TenantResponse> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken);
}
