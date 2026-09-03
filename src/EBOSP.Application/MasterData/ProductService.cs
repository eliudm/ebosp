using EBOSP.Application.Common;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.MasterData;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.MasterData;

public sealed class ProductService(
    IProductRepository products,
    IProductCategoryRepository categories,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork) : IProductService
{
    public async Task<ProductResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateProductRequest request, CancellationToken cancellationToken)
    {
        await EnsureCategoryOwnedAsync(tenantId, request.CategoryId, cancellationToken);

        var normalizedSku = request.Sku.Trim();
        if (await products.SkuExistsAsync(tenantId, normalizedSku, cancellationToken))
        {
            throw new ConflictException("A product with this SKU already exists.");
        }

        var product = Product.Create(
            tenantId,
            request.CategoryId,
            normalizedSku,
            request.Name,
            request.Description,
            request.UnitPrice,
            request.TaxRatePercent,
            request.ReorderLevel);
        await products.AddAsync(product, cancellationToken);

        events.Record("ProductCreated", tenantId, nameof(Product), product.Id.ToString(), new { product.Sku, product.Name }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid tenantId, Guid actingUserId, Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await EnsureCategoryOwnedAsync(tenantId, request.CategoryId, cancellationToken);

        var product = await GetOwnedAsync(tenantId, id, cancellationToken);
        product.Update(request.CategoryId, request.Name, request.Description, request.UnitPrice, request.TaxRatePercent, request.ReorderLevel);

        events.Record("ProductUpdated", tenantId, nameof(Product), product.Id.ToString(), new { product.Name }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(product);
    }

    public async Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var product = await GetOwnedAsync(tenantId, id, cancellationToken);
        product.Activate();

        events.Record("ProductActivated", tenantId, nameof(Product), product.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var product = await GetOwnedAsync(tenantId, id, cancellationToken);
        product.Deactivate();

        events.Record("ProductDeactivated", tenantId, nameof(Product), product.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await GetOwnedAsync(tenantId, id, cancellationToken));

    public async Task<PagedResult<ProductResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await products.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<ProductResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task EnsureCategoryOwnedAsync(Guid tenantId, Guid? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId is { } id && await categories.GetByIdAsync(tenantId, id, cancellationToken) is null)
        {
            throw new NotFoundException("Product category not found.");
        }
    }

    private async Task<Product> GetOwnedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        await products.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Product not found.");

    private static ProductResponse ToResponse(Product product) => new(
        product.Id,
        product.CategoryId,
        product.Sku,
        product.Name,
        product.Description,
        product.UnitPrice,
        product.TaxRatePercent,
        product.ReorderLevel,
        product.Status.ToString());
}
