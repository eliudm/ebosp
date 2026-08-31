namespace EBOSP.Domain.Common;

/// <summary>Base type for every domain entity - a stable surrogate identity (spec §13: UUID primary keys).</summary>
public abstract class Entity
{
    public Guid Id { get; protected init; }
}
