using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.MasterData;

public interface IBranchRepository
{
    Task AddAsync(Branch branch, CancellationToken cancellationToken);

    Task<Branch?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Branch>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
