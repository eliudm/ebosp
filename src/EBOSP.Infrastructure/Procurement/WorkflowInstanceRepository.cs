using EBOSP.Application.Procurement;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Procurement;

public sealed class WorkflowInstanceRepository(AppDbContext context) : IWorkflowInstanceRepository
{
    public async Task AddAsync(WorkflowInstance workflow, CancellationToken cancellationToken) =>
        await context.WorkflowInstances.AddAsync(workflow, cancellationToken);

    public Task<WorkflowInstance?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.WorkflowInstances.SingleOrDefaultAsync(w => w.TenantId == tenantId && w.Id == id, cancellationToken);

    public Task<WorkflowInstance?> FindForEntityAsync(Guid tenantId, string entityType, Guid entityId, CancellationToken cancellationToken) =>
        context.WorkflowInstances.SingleOrDefaultAsync(w => w.TenantId == tenantId && w.EntityType == entityType && w.EntityId == entityId, cancellationToken);
}
