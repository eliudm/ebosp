using EBOSP.Contracts.Common;
using EBOSP.Contracts.MasterData;

namespace EBOSP.Application.MasterData;

public interface IBranchService
{
    Task<BranchResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateBranchRequest request, CancellationToken cancellationToken);

    Task<BranchResponse> UpdateAsync(Guid tenantId, Guid actingUserId, Guid id, UpdateBranchRequest request, CancellationToken cancellationToken);

    Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task DeactivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task<BranchResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<BranchResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
