namespace EBOSP.Application.Common;

/// <summary>
/// Records a domain event into the transactional outbox (dev guide §19) in the current
/// <see cref="IUnitOfWork"/> transaction - the actual row is only persisted when the use case
/// calls <see cref="IUnitOfWork.SaveChangesAsync"/>. Also the seed of the identity module's audit
/// trail (dev guide §11: "failed-login rate limiting and audit events").
/// </summary>
public interface IDomainEventRecorder
{
    void Record(
        string eventType,
        Guid tenantId,
        string aggregateType,
        string aggregateId,
        object payload,
        Guid? actorId = null,
        Guid? correlationId = null,
        Guid? causationId = null);
}
