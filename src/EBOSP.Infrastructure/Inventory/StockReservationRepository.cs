using EBOSP.Application.Inventory;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Inventory;

public sealed class StockReservationRepository(AppDbContext context) : IStockReservationRepository
{
    public async Task AddAsync(StockReservation reservation, CancellationToken cancellationToken) =>
        await context.StockReservations.AddAsync(reservation, cancellationToken);

    public Task<StockReservation?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.StockReservations.SingleOrDefaultAsync(r => r.TenantId == tenantId && r.Id == id, cancellationToken);
}
