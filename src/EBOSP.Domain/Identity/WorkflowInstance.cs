using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum WorkflowStatus
{
    Pending,
    Approved,
    Rejected,
}

/// <summary>
/// Generic approval workflow (ERD: WorkflowId, EntityType, EntityId, Status) - reuses the same
/// EntityType/EntityId idiom OutboxMessage already established, rather than folding approval state
/// into each approvable entity. Not a full WorkflowDefinition engine (no stages/conditions) - no
/// module today needs configurable multi-stage approval.
/// </summary>
public sealed class WorkflowInstance : Entity, ITenantOwned
{
    private WorkflowInstance()
    {
    }

    public static WorkflowInstance Create(Guid tenantId, string entityType, Guid entityId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type is required.", nameof(entityType));
        }

        return new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = entityType,
            EntityId = entityId,
            Status = WorkflowStatus.Pending,
            CreatedAt = now,
        };
    }

    public Guid TenantId { get; private init; }

    public string EntityType { get; private init; } = null!;

    public Guid EntityId { get; private init; }

    public WorkflowStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public string? DecisionNotes { get; private set; }

    public void Approve(Guid decidedByUserId, DateTimeOffset now, string? notes)
    {
        if (Status != WorkflowStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending workflow can be approved.");
        }

        Status = WorkflowStatus.Approved;
        DecidedByUserId = decidedByUserId;
        DecidedAt = now;
        DecisionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void Reject(Guid decidedByUserId, DateTimeOffset now, string? notes)
    {
        if (Status != WorkflowStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending workflow can be rejected.");
        }

        Status = WorkflowStatus.Rejected;
        DecidedByUserId = decidedByUserId;
        DecidedAt = now;
        DecisionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}
