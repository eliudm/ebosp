namespace EBOSP.Application.Common;

/// <summary>
/// Database transaction abstraction (dev guide §9) so Application-layer use cases commit their
/// changes without depending on EF Core's DbContext directly.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detaches every tracked entity without touching the database. A failed SaveChangesAsync
    /// leaves its entities tracked in their pending (e.g. Added) state - any later SaveChangesAsync
    /// on the same scoped context/request would otherwise retry persisting them and fail the same
    /// way again. Needed by compensating actions that must still save something else (e.g. releasing
    /// a reservation) after an earlier save in the same request failed.
    /// </summary>
    void DiscardChanges();
}
