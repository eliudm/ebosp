using EBOSP.Application.Common;
using EBOSP.Application.MasterData;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Inventory;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Inventory;

public sealed class ReorderRuleService(
    IReorderRuleRepository reorderRules,
    IWarehouseRepository warehouses,
    IProductRepository products,
    IUnitOfWork unitOfWork) : IReorderRuleService
{
    public async Task<ReorderRuleResponse> CreateAsync(Guid tenantId, CreateReorderRuleRequest request, CancellationToken cancellationToken)
    {
        _ = await warehouses.GetByIdAsync(tenantId, request.WarehouseId, cancellationToken) ?? throw new NotFoundException("Warehouse not found.");
        _ = await products.GetByIdAsync(tenantId, request.ProductId, cancellationToken) ?? throw new NotFoundException("Product not found.");

        var rule = ReorderRule.Create(tenantId, request.WarehouseId, request.ProductId, request.ReorderLevel);
        await reorderRules.AddAsync(rule, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(rule);
    }

    public async Task<ReorderRuleResponse> UpdateAsync(Guid tenantId, Guid id, UpdateReorderRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await GetOwnedAsync(tenantId, id, cancellationToken);
        rule.Update(request.ReorderLevel);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(rule);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var rule = await GetOwnedAsync(tenantId, id, cancellationToken);
        reorderRules.Remove(rule);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<ReorderRuleResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await reorderRules.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<ReorderRuleResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task<ReorderRule> GetOwnedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        await reorderRules.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Reorder rule not found.");

    private static ReorderRuleResponse ToResponse(ReorderRule rule) => new(rule.Id, rule.WarehouseId, rule.ProductId, rule.ReorderLevel);
}
