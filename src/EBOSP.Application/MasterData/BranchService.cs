using EBOSP.Application.Common;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.MasterData;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.MasterData;

public sealed class BranchService(IBranchRepository branches, IDomainEventRecorder events, IUnitOfWork unitOfWork) : IBranchService
{
    public async Task<BranchResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var branch = Branch.Create(tenantId, request.Name);
        await branches.AddAsync(branch, cancellationToken);

        events.Record("BranchCreated", tenantId, nameof(Branch), branch.Id.ToString(), new { branch.Name }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(branch);
    }

    public async Task<BranchResponse> UpdateAsync(Guid tenantId, Guid actingUserId, Guid id, UpdateBranchRequest request, CancellationToken cancellationToken)
    {
        var branch = await GetOwnedAsync(tenantId, id, cancellationToken);
        branch.Update(request.Name);

        events.Record("BranchUpdated", tenantId, nameof(Branch), branch.Id.ToString(), new { branch.Name }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(branch);
    }

    public async Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var branch = await GetOwnedAsync(tenantId, id, cancellationToken);
        branch.Activate();

        events.Record("BranchActivated", tenantId, nameof(Branch), branch.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var branch = await GetOwnedAsync(tenantId, id, cancellationToken);
        branch.Deactivate();

        events.Record("BranchDeactivated", tenantId, nameof(Branch), branch.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<BranchResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await GetOwnedAsync(tenantId, id, cancellationToken));

    public async Task<PagedResult<BranchResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await branches.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<BranchResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task<Branch> GetOwnedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        await branches.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Branch not found.");

    private static BranchResponse ToResponse(Branch branch) => new(branch.Id, branch.Name, branch.Status.ToString());
}
