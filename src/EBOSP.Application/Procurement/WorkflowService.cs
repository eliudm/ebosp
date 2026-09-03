using EBOSP.Application.Common;
using EBOSP.Contracts.Procurement;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Procurement;

public sealed class WorkflowService(IWorkflowInstanceRepository workflows) : IWorkflowService
{
    public async Task<WorkflowInstanceResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Workflow not found.");
        return ToResponse(workflow);
    }

    private static WorkflowInstanceResponse ToResponse(WorkflowInstance workflow) =>
        new(workflow.Id, workflow.EntityType, workflow.EntityId, workflow.Status.ToString(), workflow.CreatedAt, workflow.DecidedByUserId, workflow.DecidedAt, workflow.DecisionNotes);
}
