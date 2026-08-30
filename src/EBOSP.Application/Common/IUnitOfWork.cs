namespace EBOSP.Application.Common;

/// <summary>
/// Database transaction abstraction (dev guide §9) so Application-layer use cases commit their
/// changes without depending on EF Core's DbContext directly.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
