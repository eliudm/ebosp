using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Inventory;

public interface IReorderRuleRepository
{
    Task AddAsync(ReorderRule rule, CancellationToken cancellationToken);

    Task<ReorderRule?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    /// <summary>The rule for a specific (warehouse, product) pair, if one has been configured.</summary>
    Task<ReorderRule?> FindForAsync(Guid tenantId, Guid warehouseId, Guid productId, CancellationToken cancellationToken);

    Task<PagedResult<ReorderRule>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);

    void Remove(ReorderRule rule);
}
