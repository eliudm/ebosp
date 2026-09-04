using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Security;

public interface ISecurityAlertRepository
{
    Task AddAsync(SecurityAlert alert, CancellationToken cancellationToken);

    Task<SecurityAlert?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<SecurityAlert>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
