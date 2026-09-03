using EBOSP.Contracts.Common;
using EBOSP.Contracts.MasterData;

namespace EBOSP.Application.MasterData;

public interface IWarehouseService
{
    Task<WarehouseResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateWarehouseRequest request, CancellationToken cancellationToken);

    Task<WarehouseResponse> UpdateAsync(Guid tenantId, Guid actingUserId, Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken);

    Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task DeactivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task<WarehouseResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<WarehouseResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
