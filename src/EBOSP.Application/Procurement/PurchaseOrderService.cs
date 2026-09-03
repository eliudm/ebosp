using EBOSP.Application.Common;
using EBOSP.Application.MasterData;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Procurement;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Procurement;

/// <summary>
/// Converts an approved PurchaseRequest into a PurchaseOrder (dev guide §14). Deliberately not
/// automatic on approval (spec §8.3: approved requests "can" generate a PO, not "must") - choosing
/// a supplier and final line pricing is a separate decision from approving the request's
/// legitimacy/budget.
/// </summary>
public sealed class PurchaseOrderService(
    IPurchaseOrderRepository purchaseOrders,
    IPurchaseRequestRepository purchaseRequests,
    ISupplierRepository suppliers,
    IProductRepository products,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock) : IPurchaseOrderService
{
    public async Task<PurchaseOrderResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var purchaseRequest = await purchaseRequests.GetByIdAsync(tenantId, request.PurchaseRequestId, cancellationToken)
                               ?? throw new NotFoundException("Purchase request not found.");
        if (purchaseRequest.Status != PurchaseRequestStatus.Approved)
        {
            throw new ConflictException("Only an approved purchase request can be converted to a purchase order.");
        }

        if (await purchaseOrders.ExistsForRequestAsync(tenantId, purchaseRequest.Id, cancellationToken))
        {
            throw new ConflictException("This purchase request has already been converted to a purchase order.");
        }

        _ = await suppliers.GetByIdAsync(tenantId, request.SupplierId, cancellationToken) ?? throw new NotFoundException("Supplier not found.");

        var normalizedPoNumber = request.PoNumber.Trim();
        if (await purchaseOrders.PoNumberExistsAsync(tenantId, normalizedPoNumber, cancellationToken))
        {
            throw new ConflictException("A purchase order with this PO number already exists.");
        }

        foreach (var line in request.Lines)
        {
            _ = await products.GetByIdAsync(tenantId, line.ProductId, cancellationToken) ?? throw new NotFoundException("Product not found.");
        }

        var order = PurchaseOrder.Create(
            tenantId, purchaseRequest.Id, request.SupplierId, normalizedPoNumber, actingUserId, clock.UtcNow,
            request.Lines.Select(l => (l.ProductId, l.Quantity, l.UnitPrice)).ToList());
        await purchaseOrders.AddAsync(order, cancellationToken);

        events.Record("PurchaseOrderCreated", tenantId, nameof(PurchaseOrder), order.Id.ToString(),
            new { order.PurchaseRequestId, order.SupplierId, order.PoNumber, order.Total }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(order);
    }

    public async Task<PurchaseOrderResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await purchaseOrders.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Purchase order not found."));

    public async Task<PagedResult<PurchaseOrderResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await purchaseOrders.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<PurchaseOrderResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private static PurchaseOrderResponse ToResponse(PurchaseOrder order) => new(
        order.Id, order.PurchaseRequestId, order.SupplierId, order.PoNumber, order.Status.ToString(), order.Total, order.CreatedByUserId, order.CreatedAt,
        order.Lines.Select(l => new PurchaseOrderLineResponse(l.ProductId, l.Quantity, l.UnitPrice)).ToList());
}
