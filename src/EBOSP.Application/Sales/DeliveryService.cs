using EBOSP.Application.Common;
using EBOSP.Application.Inventory;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Inventory;
using EBOSP.Contracts.Sales;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Sales;

/// <summary>
/// Ships a SalesOrder in full (dev guide §15: "Delivery is a separate auditable state
/// transition") - one delivery per order, no partial shipments this phase. Each line's earlier
/// reservation is released and the same quantity issued, reusing IInventoryService exactly as
/// SalesOrderService does.
/// </summary>
public sealed class DeliveryService(
    IDeliveryRepository deliveries,
    ISalesOrderRepository salesOrders,
    IInventoryService inventoryService,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock) : IDeliveryService
{
    public async Task<DeliveryResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateDeliveryRequest request, CancellationToken cancellationToken)
    {
        var order = await salesOrders.GetByIdAsync(tenantId, request.SalesOrderId, cancellationToken)
                    ?? throw new NotFoundException("Sales order not found.");
        if (order.Status != SalesOrderStatus.Open)
        {
            throw new ConflictException("Only an open sales order can be delivered.");
        }

        foreach (var line in order.Lines)
        {
            await inventoryService.ReleaseReservationAsync(tenantId, line.ReservationId, cancellationToken);
            await inventoryService.IssueAsync(
                tenantId, actingUserId,
                new IssueStockRequest { WarehouseId = order.WarehouseId, ProductId = line.ProductId, Quantity = line.Quantity, Reason = $"Delivery for sales order {order.Id}" },
                cancellationToken);
        }

        var delivery = Delivery.Create(tenantId, order.Id, order.WarehouseId, actingUserId, clock.UtcNow, order.Lines.Select(l => (l.ProductId, l.Quantity)).ToList());
        await deliveries.AddAsync(delivery, cancellationToken);

        order.MarkFulfilled();

        // NotifyUserId/NotificationTitle are read by the M9 outbox background processor
        // (EBOSP.Worker) - the order is already loaded here, so the recipient is free.
        events.Record("DeliveryCreated", tenantId, nameof(Delivery), delivery.Id.ToString(),
            new { delivery.SalesOrderId, delivery.WarehouseId, NotifyUserId = order.CreatedByUserId, NotificationTitle = "Your sales order has shipped" }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(delivery);
    }

    public async Task<DeliveryResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await deliveries.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Delivery not found."));

    public async Task<PagedResult<DeliveryResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await deliveries.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<DeliveryResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private static DeliveryResponse ToResponse(Delivery delivery) => new(
        delivery.Id, delivery.SalesOrderId, delivery.WarehouseId, delivery.DeliveredByUserId, delivery.DeliveredAt,
        delivery.Lines.Select(l => new DeliveryLineResponse(l.ProductId, l.Quantity)).ToList());
}
