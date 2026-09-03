using EBOSP.Contracts.Procurement;

namespace EBOSP.Application.Procurement;

public interface IWorkflowService
{
    Task<WorkflowInstanceResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
}
