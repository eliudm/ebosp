using EBOSP.Application.Common;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Procurement;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Procurement;

public sealed class SupplierService(ISupplierRepository suppliers, IDomainEventRecorder events, IUnitOfWork unitOfWork) : ISupplierService
{
    public async Task<SupplierResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = Supplier.Create(tenantId, request.Name, request.TaxId, request.Contact);
        await suppliers.AddAsync(supplier, cancellationToken);

        events.Record("SupplierCreated", tenantId, nameof(Supplier), supplier.Id.ToString(), new { supplier.Name }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(supplier);
    }

    public async Task<SupplierResponse> UpdateAsync(Guid tenantId, Guid actingUserId, Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await GetOwnedAsync(tenantId, id, cancellationToken);
        supplier.Update(request.Name, request.TaxId, request.Contact);

        events.Record("SupplierUpdated", tenantId, nameof(Supplier), supplier.Id.ToString(), new { supplier.Name }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(supplier);
    }

    public async Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var supplier = await GetOwnedAsync(tenantId, id, cancellationToken);
        supplier.Activate();

        events.Record("SupplierActivated", tenantId, nameof(Supplier), supplier.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var supplier = await GetOwnedAsync(tenantId, id, cancellationToken);
        supplier.Deactivate();

        events.Record("SupplierDeactivated", tenantId, nameof(Supplier), supplier.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<SupplierResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await GetOwnedAsync(tenantId, id, cancellationToken));

    public async Task<PagedResult<SupplierResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await suppliers.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<SupplierResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task<Supplier> GetOwnedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        await suppliers.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Supplier not found.");

    private static SupplierResponse ToResponse(Supplier supplier) => new(supplier.Id, supplier.Name, supplier.TaxId, supplier.Contact, supplier.Status.ToString());
}
