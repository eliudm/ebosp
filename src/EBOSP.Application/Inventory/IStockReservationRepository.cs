using EBOSP.Domain.Identity;

namespace EBOSP.Application.Inventory;

public interface IStockReservationRepository
{
    Task AddAsync(StockReservation reservation, CancellationToken cancellationToken);

    Task<StockReservation?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
}
