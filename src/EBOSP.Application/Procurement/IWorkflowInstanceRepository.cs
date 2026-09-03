using EBOSP.Domain.Identity;

namespace EBOSP.Application.Procurement;

public interface IWorkflowInstanceRepository
{
    Task AddAsync(WorkflowInstance workflow, CancellationToken cancellationToken);

    Task<WorkflowInstance?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<WorkflowInstance?> FindForEntityAsync(Guid tenantId, string entityType, Guid entityId, CancellationToken cancellationToken);
}
