using EBOSP.Contracts.Common;
using EBOSP.Contracts.Inventory;

namespace EBOSP.Application.Inventory;

public interface IReorderRuleService
{
    Task<ReorderRuleResponse> CreateAsync(Guid tenantId, CreateReorderRuleRequest request, CancellationToken cancellationToken);

    Task<ReorderRuleResponse> UpdateAsync(Guid tenantId, Guid id, UpdateReorderRuleRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ReorderRuleResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
