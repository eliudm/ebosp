using EBOSP.Application.Common;
using EBOSP.Application.MasterData;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Procurement;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Procurement;

/// <summary>Implements dev guide §14's submit -> approve/reject workflow, backed by a generic WorkflowInstance (ERD).</summary>
public sealed class PurchaseRequestService(
    IPurchaseRequestRepository purchaseRequests,
    IWorkflowInstanceRepository workflows,
    IBranchRepository branches,
    IProductRepository products,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock) : IPurchaseRequestService
{
    private const string EntityType = nameof(PurchaseRequest);

    public async Task<PurchaseRequestResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreatePurchaseRequestRequest request, CancellationToken cancellationToken)
    {
        _ = await branches.GetByIdAsync(tenantId, request.BranchId, cancellationToken) ?? throw new NotFoundException("Branch not found.");
        foreach (var line in request.Lines)
        {
            _ = await products.GetByIdAsync(tenantId, line.ProductId, cancellationToken) ?? throw new NotFoundException("Product not found.");
        }

        var now = clock.UtcNow;
        var purchaseRequest = PurchaseRequest.Create(
            tenantId, actingUserId, request.BranchId, request.Justification, request.RequiredDate,
            request.Lines.Select(l => (l.ProductId, l.Quantity, l.EstimatedUnitPrice)).ToList());
        await purchaseRequests.AddAsync(purchaseRequest, cancellationToken);

        var workflow = WorkflowInstance.Create(tenantId, EntityType, purchaseRequest.Id, now);
        await workflows.AddAsync(workflow, cancellationToken);

        events.Record("PurchaseRequestSubmitted", tenantId, EntityType, purchaseRequest.Id.ToString(),
            new { purchaseRequest.BranchId, purchaseRequest.EstimatedValue }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(purchaseRequest);
    }

    public async Task<PurchaseRequestResponse> ApproveAsync(Guid tenantId, Guid actingUserId, Guid id, ApprovePurchaseRequestRequest request, CancellationToken cancellationToken)
    {
        var purchaseRequest = await GetOwnedAsync(tenantId, id, cancellationToken);
        if (purchaseRequest.RequestedByUserId == actingUserId)
        {
            throw new ForbiddenOperationException("You cannot approve your own purchase request.");
        }

        // Checked explicitly (client error) rather than letting WorkflowInstance.Approve's own
        // guard throw InvalidOperationException, which GlobalExceptionHandler would otherwise map
        // to an unhandled 500 - rejection is terminal (dev guide §14), so re-approving an
        // already-decided request must surface as a clean 409, not a server fault.
        if (purchaseRequest.Status != PurchaseRequestStatus.Pending)
        {
            throw new ConflictException("Only a pending purchase request can be approved.");
        }

        var workflow = await workflows.FindForEntityAsync(tenantId, EntityType, purchaseRequest.Id, cancellationToken)
                        ?? throw new NotFoundException("Workflow not found.");

        var now = clock.UtcNow;
        workflow.Approve(actingUserId, now, request.Notes);
        purchaseRequest.MarkApproved();

        events.Record("PurchaseApproved", tenantId, EntityType, purchaseRequest.Id.ToString(), new { request.Notes }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(purchaseRequest);
    }

    public async Task<PurchaseRequestResponse> RejectAsync(Guid tenantId, Guid actingUserId, Guid id, RejectPurchaseRequestRequest request, CancellationToken cancellationToken)
    {
        var purchaseRequest = await GetOwnedAsync(tenantId, id, cancellationToken);
        if (purchaseRequest.RequestedByUserId == actingUserId)
        {
            throw new ForbiddenOperationException("You cannot reject your own purchase request.");
        }

        if (purchaseRequest.Status != PurchaseRequestStatus.Pending)
        {
            throw new ConflictException("Only a pending purchase request can be rejected.");
        }

        var workflow = await workflows.FindForEntityAsync(tenantId, EntityType, purchaseRequest.Id, cancellationToken)
                        ?? throw new NotFoundException("Workflow not found.");

        var now = clock.UtcNow;
        workflow.Reject(actingUserId, now, request.Notes);
        purchaseRequest.MarkRejected();

        events.Record("PurchaseRequestRejected", tenantId, EntityType, purchaseRequest.Id.ToString(), new { request.Notes }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(purchaseRequest);
    }

    public async Task<PurchaseRequestResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await GetOwnedAsync(tenantId, id, cancellationToken));

    public async Task<PagedResult<PurchaseRequestResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await purchaseRequests.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<PurchaseRequestResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task<PurchaseRequest> GetOwnedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        await purchaseRequests.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Purchase request not found.");

    private static PurchaseRequestResponse ToResponse(PurchaseRequest request) => new(
        request.Id, request.RequestedByUserId, request.BranchId, request.Justification, request.EstimatedValue, request.RequiredDate, request.Status.ToString(),
        request.Lines.Select(l => new PurchaseRequestLineResponse(l.ProductId, l.Quantity, l.EstimatedUnitPrice)).ToList());
}
