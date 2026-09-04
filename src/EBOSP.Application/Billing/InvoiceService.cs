using EBOSP.Application.Common;
using EBOSP.Application.Sales;
using EBOSP.Contracts.Billing;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Billing;

/// <summary>
/// Generates an Invoice from a Fulfilled SalesOrder (dev guide §15). Deliberately not
/// auto-triggered by Delivery - a separate, explicit action, same "next document is a separate
/// step" precedent as M5's approval->PO and M6's quotation-acceptance->sales-order.
/// </summary>
public sealed class InvoiceService(
    IInvoiceRepository invoices,
    ISalesOrderRepository salesOrders,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock) : IInvoiceService
{
    public async Task<InvoiceResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var order = await salesOrders.GetByIdAsync(tenantId, request.SalesOrderId, cancellationToken)
                    ?? throw new NotFoundException("Sales order not found.");
        if (order.Status != SalesOrderStatus.Fulfilled)
        {
            throw new ConflictException("Only a fulfilled sales order can be invoiced.");
        }

        if (await invoices.ExistsForSalesOrderAsync(tenantId, order.Id, cancellationToken))
        {
            throw new ConflictException("This sales order has already been invoiced.");
        }

        var invoice = Invoice.Create(tenantId, order.Id, order.CustomerId, order.Total, actingUserId, clock.UtcNow);
        await invoices.AddAsync(invoice, cancellationToken);

        events.Record("InvoiceCreated", tenantId, nameof(Invoice), invoice.Id.ToString(), new { invoice.SalesOrderId, invoice.CustomerId, invoice.Total }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(invoice);
    }

    public async Task<InvoiceResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await invoices.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Invoice not found."));

    public async Task<PagedResult<InvoiceResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await invoices.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<InvoiceResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private static InvoiceResponse ToResponse(Invoice invoice) =>
        new(invoice.Id, invoice.SalesOrderId, invoice.CustomerId, invoice.Total, invoice.Status.ToString(), invoice.CreatedByUserId, invoice.CreatedAt);
}
