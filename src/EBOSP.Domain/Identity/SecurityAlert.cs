using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum SecurityAlertSeverity
{
    Low,
    Medium,
    High,
    Critical,
}

public enum SecurityAlertStatus
{
    Open,
    Acknowledged,
    Investigating,
    Resolved,
    FalsePositive,
}

/// <summary>
/// Actionable security finding raised by a detection rule (dev guide §18.1's anomaly pipeline;
/// ERD: AlertId, Severity, Rule, Status). Unlike an audit event (immutable history, queried from
/// the outbox), this is a stateful record a Security Officer works through the exact 5 lifecycle
/// states dev guide §18.1 names: Open, Acknowledged, Investigating, Resolved, False Positive.
/// Exclusively system-raised - there is no public create action, so a fabricated alert can never
/// enter through the API.
/// </summary>
public sealed class SecurityAlert : Entity, ITenantOwned
{
    private SecurityAlert()
    {
    }

    public static SecurityAlert Raise(
        Guid tenantId,
        string rule,
        SecurityAlertSeverity severity,
        string description,
        DateTimeOffset createdAt,
        Guid? relatedActorId = null,
        string? relatedAggregateType = null,
        string? relatedAggregateId = null)
    {
        if (string.IsNullOrWhiteSpace(rule))
        {
            throw new ArgumentException("Rule is required.", nameof(rule));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        return new SecurityAlert
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Rule = rule,
            Severity = severity,
            Description = description,
            RelatedActorId = relatedActorId,
            RelatedAggregateType = relatedAggregateType,
            RelatedAggregateId = relatedAggregateId,
            Status = SecurityAlertStatus.Open,
            CreatedAt = createdAt,
        };
    }

    public Guid TenantId { get; private init; }

    public string Rule { get; private init; } = null!;

    public SecurityAlertSeverity Severity { get; private init; }

    public string Description { get; private init; } = null!;

    public Guid? RelatedActorId { get; private init; }

    public string? RelatedAggregateType { get; private init; }

    public string? RelatedAggregateId { get; private init; }

    public SecurityAlertStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public Guid? AcknowledgedByUserId { get; private set; }

    public DateTimeOffset? AcknowledgedAt { get; private set; }

    public Guid? ResolvedByUserId { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public string? ResolutionNotes { get; private set; }

    public void Acknowledge(Guid userId, DateTimeOffset now)
    {
        if (Status != SecurityAlertStatus.Open)
        {
            throw new InvalidOperationException("Only an open alert can be acknowledged.");
        }

        Status = SecurityAlertStatus.Acknowledged;
        AcknowledgedByUserId = userId;
        AcknowledgedAt = now;
    }

    public void Investigate()
    {
        if (Status != SecurityAlertStatus.Acknowledged)
        {
            throw new InvalidOperationException("Only an acknowledged alert can move to investigating.");
        }

        Status = SecurityAlertStatus.Investigating;
    }

    public void Resolve(Guid userId, DateTimeOffset now, string notes)
    {
        RequireNonTerminal();
        if (string.IsNullOrWhiteSpace(notes))
        {
            throw new ArgumentException("Resolution notes are required.", nameof(notes));
        }

        Status = SecurityAlertStatus.Resolved;
        ResolvedByUserId = userId;
        ResolvedAt = now;
        ResolutionNotes = notes.Trim();
    }

    public void MarkFalsePositive(Guid userId, DateTimeOffset now, string notes)
    {
        RequireNonTerminal();
        if (string.IsNullOrWhiteSpace(notes))
        {
            throw new ArgumentException("Resolution notes are required.", nameof(notes));
        }

        Status = SecurityAlertStatus.FalsePositive;
        ResolvedByUserId = userId;
        ResolvedAt = now;
        ResolutionNotes = notes.Trim();
    }

    private void RequireNonTerminal()
    {
        if (Status is SecurityAlertStatus.Resolved or SecurityAlertStatus.FalsePositive)
        {
            throw new InvalidOperationException("This alert has already been closed.");
        }
    }
}
