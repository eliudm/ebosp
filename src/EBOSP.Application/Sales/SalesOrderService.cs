using EBOSP.Application.Common;
using EBOSP.Application.Inventory;
using EBOSP.Application.MasterData;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Inventory;
using EBOSP.Contracts.Sales;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Sales;

/// <summary>
/// Converts an Accepted Quotation into a SalesOrder, reserving stock for every line (dev guide
/// §15: "reserve inventory... enforce the rule transactionally"). Reuses IInventoryService as-is
/// rather than reimplementing its already-hardened concurrency-retry logic against a second unit
/// of work - each ReserveAsync call commits independently, so a failure partway through a
/// multi-line order is handled with explicit compensation (releasing every line already reserved
/// in this request) instead of leaving orphaned reservations behind.
/// </summary>
public sealed class SalesOrderService(
    ISalesOrderRepository salesOrders,
    IQuotationRepository quotations,
    ICustomerRepository customers,
    IWarehouseRepository warehouses,
    IInventoryService inventoryService,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock) : ISalesOrderService
{
    public async Task<SalesOrderResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateSalesOrderRequest request, CancellationToken cancellationToken)
    {
        var quotation = await quotations.GetByIdAsync(tenantId, request.QuotationId, cancellationToken)
                         ?? throw new NotFoundException("Quotation not found.");
        if (quotation.Status != QuotationStatus.Accepted)
        {
            throw new ConflictException("Only an accepted quotation can be converted to a sales order.");
        }

        if (await salesOrders.ExistsForQuotationAsync(tenantId, quotation.Id, cancellationToken))
        {
            throw new ConflictException("This quotation has already been converted to a sales order.");
        }

        var customer = await customers.GetByIdAsync(tenantId, quotation.CustomerId, cancellationToken)
                       ?? throw new NotFoundException("Customer not found.");
        if (customer.Status != CustomerStatus.Active)
        {
            throw new ConflictException("Customer is not active.");
        }

        // Credit exposure is approximated from open sales order totals - there is no Invoice/Payment
        // yet (Billing + payments is a later phase) to know what's actually still owed.
        var committedTotal = await salesOrders.SumOpenTotalsForCustomerAsync(tenantId, customer.Id, cancellationToken);
        if (committedTotal + quotation.Total > customer.CreditLimit)
        {
            throw new ConflictException("This order would exceed the customer's credit limit.");
        }

        _ = await warehouses.GetByIdAsync(tenantId, request.WarehouseId, cancellationToken) ?? throw new NotFoundException("Warehouse not found.");

        var reservedLines = new List<(Guid ProductId, int Quantity, decimal UnitPrice, Guid ReservationId)>();
        try
        {
            foreach (var line in quotation.Lines)
            {
                var reservation = await inventoryService.ReserveAsync(
                    tenantId, actingUserId,
                    new ReserveStockRequest { WarehouseId = request.WarehouseId, ProductId = line.ProductId, Quantity = line.Quantity },
                    cancellationToken);
                reservedLines.Add((line.ProductId, line.Quantity, line.UnitPrice, reservation.Id));
            }
        }
        catch
        {
            // Best-effort compensation: undo every reservation this request already made before the
            // failure, rather than leaving them orphaned against a sales order that never gets created.
            foreach (var (_, _, _, reservationId) in reservedLines)
            {
                await inventoryService.ReleaseReservationAsync(tenantId, reservationId, cancellationToken);
            }

            throw;
        }

        var order = SalesOrder.Create(tenantId, quotation.Id, quotation.CustomerId, quotation.BranchId, request.WarehouseId, actingUserId, clock.UtcNow, reservedLines);
        await salesOrders.AddAsync(order, cancellationToken);

        events.Record("SalesOrderCreated", tenantId, nameof(SalesOrder), order.Id.ToString(), new { order.CustomerId, order.WarehouseId, order.Total }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(order);
    }

    public async Task<SalesOrderResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await salesOrders.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Sales order not found."));

    public async Task<PagedResult<SalesOrderResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await salesOrders.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<SalesOrderResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private static SalesOrderResponse ToResponse(SalesOrder order) => new(
        order.Id, order.QuotationId, order.CustomerId, order.BranchId, order.WarehouseId, order.Status.ToString(), order.Total, order.CreatedByUserId, order.CreatedAt,
        order.Lines.Select(l => new SalesOrderLineResponse(l.ProductId, l.Quantity, l.UnitPrice)).ToList());
}
