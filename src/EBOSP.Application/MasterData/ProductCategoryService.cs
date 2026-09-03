using EBOSP.Application.Common;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.MasterData;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.MasterData;

public sealed class ProductCategoryService(
    IProductCategoryRepository categories,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork) : IProductCategoryService
{
    public async Task<ProductCategoryResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateProductCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = ProductCategory.Create(tenantId, request.Name);
        await categories.AddAsync(category, cancellationToken);

        events.Record("ProductCategoryCreated", tenantId, nameof(ProductCategory), category.Id.ToString(), new { category.Name }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(category);
    }

    public async Task<ProductCategoryResponse> UpdateAsync(Guid tenantId, Guid actingUserId, Guid id, UpdateProductCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await GetOwnedAsync(tenantId, id, cancellationToken);
        category.Update(request.Name);

        events.Record("ProductCategoryUpdated", tenantId, nameof(ProductCategory), category.Id.ToString(), new { category.Name }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(category);
    }

    public async Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var category = await GetOwnedAsync(tenantId, id, cancellationToken);
        category.Activate();

        events.Record("ProductCategoryActivated", tenantId, nameof(ProductCategory), category.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var category = await GetOwnedAsync(tenantId, id, cancellationToken);
        category.Deactivate();

        events.Record("ProductCategoryDeactivated", tenantId, nameof(ProductCategory), category.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductCategoryResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await GetOwnedAsync(tenantId, id, cancellationToken));

    public async Task<PagedResult<ProductCategoryResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await categories.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<ProductCategoryResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task<ProductCategory> GetOwnedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        await categories.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Product category not found.");

    private static ProductCategoryResponse ToResponse(ProductCategory category) => new(category.Id, category.Name, category.Status.ToString());
}
