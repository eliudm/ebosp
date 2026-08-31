using System.Text.Json;
using EBOSP.Application.Common;
using EBOSP.Infrastructure.Persistence;

namespace EBOSP.Infrastructure.Outbox;

public sealed class DomainEventRecorder(AppDbContext context, IClock clock) : IDomainEventRecorder
{
    public void Record(
        string eventType,
        Guid tenantId,
        string aggregateType,
        string aggregateId,
        object payload,
        Guid? actorId = null,
        Guid? correlationId = null,
        Guid? causationId = null)
    {
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            TenantId = tenantId,
            CorrelationId = correlationId,
            CausationId = causationId,
            ActorId = actorId,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            Version = 1,
            Payload = JsonSerializer.Serialize(payload),
            OccurredAt = clock.UtcNow,
        });
    }
}
