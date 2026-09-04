using EBOSP.Application.Common;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Security;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Security;

/// <summary>Lifecycle actions for security alerts (dev guide §18.1). "Administrative response actions must be audited" - every transition is also recorded to the outbox.</summary>
public sealed class SecurityAlertService(
    ISecurityAlertRepository alerts,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock) : ISecurityAlertService
{
    public async Task<SecurityAlertResponse> AcknowledgeAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var alert = await GetOwnedAsync(tenantId, id, cancellationToken);
        if (alert.Status != SecurityAlertStatus.Open)
        {
            throw new ConflictException("Only an open alert can be acknowledged.");
        }

        alert.Acknowledge(actingUserId, clock.UtcNow);

        events.Record("SecurityAlertAcknowledged", tenantId, nameof(SecurityAlert), alert.Id.ToString(), new { alert.Rule }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(alert);
    }

    public async Task<SecurityAlertResponse> InvestigateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var alert = await GetOwnedAsync(tenantId, id, cancellationToken);
        if (alert.Status != SecurityAlertStatus.Acknowledged)
        {
            throw new ConflictException("Only an acknowledged alert can move to investigating.");
        }

        alert.Investigate();

        events.Record("SecurityAlertInvestigating", tenantId, nameof(SecurityAlert), alert.Id.ToString(), new { alert.Rule }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(alert);
    }

    public async Task<SecurityAlertResponse> ResolveAsync(Guid tenantId, Guid actingUserId, Guid id, ResolveSecurityAlertRequest request, CancellationToken cancellationToken)
    {
        var alert = await GetOwnedAsync(tenantId, id, cancellationToken);
        RequireNonTerminal(alert);
        alert.Resolve(actingUserId, clock.UtcNow, request.Notes);

        events.Record("SecurityAlertResolved", tenantId, nameof(SecurityAlert), alert.Id.ToString(), new { alert.Rule, request.Notes }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(alert);
    }

    public async Task<SecurityAlertResponse> MarkFalsePositiveAsync(Guid tenantId, Guid actingUserId, Guid id, ResolveSecurityAlertRequest request, CancellationToken cancellationToken)
    {
        var alert = await GetOwnedAsync(tenantId, id, cancellationToken);
        RequireNonTerminal(alert);
        alert.MarkFalsePositive(actingUserId, clock.UtcNow, request.Notes);

        events.Record("SecurityAlertMarkedFalsePositive", tenantId, nameof(SecurityAlert), alert.Id.ToString(), new { alert.Rule, request.Notes }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(alert);
    }

    public async Task<SecurityAlertResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await GetOwnedAsync(tenantId, id, cancellationToken));

    public async Task<PagedResult<SecurityAlertResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await alerts.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<SecurityAlertResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task<SecurityAlert> GetOwnedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        await alerts.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Security alert not found.");

    private static void RequireNonTerminal(SecurityAlert alert)
    {
        if (alert.Status is SecurityAlertStatus.Resolved or SecurityAlertStatus.FalsePositive)
        {
            throw new ConflictException("This alert has already been closed.");
        }
    }

    private static SecurityAlertResponse ToResponse(SecurityAlert alert) => new(
        alert.Id, alert.Rule, alert.Severity.ToString(), alert.Description, alert.RelatedActorId, alert.RelatedAggregateType, alert.RelatedAggregateId,
        alert.Status.ToString(), alert.CreatedAt, alert.AcknowledgedByUserId, alert.AcknowledgedAt, alert.ResolvedByUserId, alert.ResolvedAt, alert.ResolutionNotes);
}
