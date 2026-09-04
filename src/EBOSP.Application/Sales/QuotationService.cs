using EBOSP.Application.Common;
using EBOSP.Application.MasterData;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Sales;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Sales;

public sealed class QuotationService(
    IQuotationRepository quotations,
    ICustomerRepository customers,
    IBranchRepository branches,
    IProductRepository products,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork) : IQuotationService
{
    public async Task<QuotationResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateQuotationRequest request, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(tenantId, request.CustomerId, cancellationToken) ?? throw new NotFoundException("Customer not found.");
        if (customer.Status != CustomerStatus.Active)
        {
            throw new ConflictException("Customer is not active.");
        }

        _ = await branches.GetByIdAsync(tenantId, request.BranchId, cancellationToken) ?? throw new NotFoundException("Branch not found.");
        foreach (var line in request.Lines)
        {
            _ = await products.GetByIdAsync(tenantId, line.ProductId, cancellationToken) ?? throw new NotFoundException("Product not found.");
        }

        var quotation = Quotation.Create(
            tenantId, request.CustomerId, request.BranchId,
            request.Lines.Select(l => (l.ProductId, l.Quantity, l.UnitPrice)).ToList());
        await quotations.AddAsync(quotation, cancellationToken);

        events.Record("QuotationCreated", tenantId, nameof(Quotation), quotation.Id.ToString(), new { quotation.CustomerId, quotation.Total }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(quotation);
    }

    public async Task<QuotationResponse> AcceptAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var quotation = await GetOwnedAsync(tenantId, id, cancellationToken);
        if (quotation.Status != QuotationStatus.Pending)
        {
            throw new ConflictException("Only a pending quotation can be accepted.");
        }

        quotation.Accept();

        events.Record("QuotationAccepted", tenantId, nameof(Quotation), quotation.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(quotation);
    }

    public async Task<QuotationResponse> RejectAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var quotation = await GetOwnedAsync(tenantId, id, cancellationToken);
        if (quotation.Status != QuotationStatus.Pending)
        {
            throw new ConflictException("Only a pending quotation can be rejected.");
        }

        quotation.Reject();

        events.Record("QuotationRejected", tenantId, nameof(Quotation), quotation.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(quotation);
    }

    public async Task<QuotationResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await GetOwnedAsync(tenantId, id, cancellationToken));

    public async Task<PagedResult<QuotationResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await quotations.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<QuotationResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task<Quotation> GetOwnedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        await quotations.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Quotation not found.");

    private static QuotationResponse ToResponse(Quotation quotation) => new(
        quotation.Id, quotation.CustomerId, quotation.BranchId, quotation.Total, quotation.Status.ToString(),
        quotation.Lines.Select(l => new QuotationLineResponse(l.ProductId, l.Quantity, l.UnitPrice)).ToList());
}
