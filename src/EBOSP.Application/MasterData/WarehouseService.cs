using EBOSP.Application.Common;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.MasterData;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.MasterData;

public sealed class WarehouseService(
    IWarehouseRepository warehouses,
    IBranchRepository branches,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork) : IWarehouseService
{
    public async Task<WarehouseResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        // The FK constraint is the backstop; this is the friendly 404 instead of a raw DB error,
        // and it's what actually stops a cross-tenant BranchId from being accepted.
        _ = await branches.GetByIdAsync(tenantId, request.BranchId, cancellationToken)
            ?? throw new NotFoundException("Branch not found.");

        var warehouse = Warehouse.Create(tenantId, request.BranchId, request.Name);
        await warehouses.AddAsync(warehouse, cancellationToken);

        events.Record("WarehouseCreated", tenantId, nameof(Warehouse), warehouse.Id.ToString(), new { warehouse.Name, warehouse.BranchId }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(warehouse);
    }

    public async Task<WarehouseResponse> UpdateAsync(Guid tenantId, Guid actingUserId, Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var warehouse = await GetOwnedAsync(tenantId, id, cancellationToken);
        warehouse.Update(request.Name);

        events.Record("WarehouseUpdated", tenantId, nameof(Warehouse), warehouse.Id.ToString(), new { warehouse.Name }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(warehouse);
    }

    public async Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await GetOwnedAsync(tenantId, id, cancellationToken);
        warehouse.Activate();

        events.Record("WarehouseActivated", tenantId, nameof(Warehouse), warehouse.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await GetOwnedAsync(tenantId, id, cancellationToken);
        warehouse.Deactivate();

        events.Record("WarehouseDeactivated", tenantId, nameof(Warehouse), warehouse.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<WarehouseResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await GetOwnedAsync(tenantId, id, cancellationToken));

    public async Task<PagedResult<WarehouseResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await warehouses.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<WarehouseResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task<Warehouse> GetOwnedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        await warehouses.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Warehouse not found.");

    private static WarehouseResponse ToResponse(Warehouse warehouse) => new(warehouse.Id, warehouse.BranchId, warehouse.Name, warehouse.Status.ToString());
}
