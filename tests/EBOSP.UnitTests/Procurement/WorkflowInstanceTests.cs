using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Procurement;

public class WorkflowInstanceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_StartsPending()
    {
        var workflow = WorkflowInstance.Create(Guid.NewGuid(), nameof(PurchaseRequest), Guid.NewGuid(), Now);

        Assert.Equal(WorkflowStatus.Pending, workflow.Status);
        Assert.Null(workflow.DecidedByUserId);
        Assert.Null(workflow.DecidedAt);
    }

    [Fact]
    public void Approve_PendingWorkflow_TransitionsToApproved()
    {
        var workflow = WorkflowInstance.Create(Guid.NewGuid(), nameof(PurchaseRequest), Guid.NewGuid(), Now);
        var approver = Guid.NewGuid();

        workflow.Approve(approver, Now, "Looks good");

        Assert.Equal(WorkflowStatus.Approved, workflow.Status);
        Assert.Equal(approver, workflow.DecidedByUserId);
        Assert.Equal(Now, workflow.DecidedAt);
        Assert.Equal("Looks good", workflow.DecisionNotes);
    }

    [Fact]
    public void Reject_PendingWorkflow_TransitionsToRejected()
    {
        var workflow = WorkflowInstance.Create(Guid.NewGuid(), nameof(PurchaseRequest), Guid.NewGuid(), Now);

        workflow.Reject(Guid.NewGuid(), Now, "Over budget");

        Assert.Equal(WorkflowStatus.Rejected, workflow.Status);
        Assert.Equal("Over budget", workflow.DecisionNotes);
    }

    [Fact]
    public void Approve_AlreadyApproved_Throws()
    {
        var workflow = WorkflowInstance.Create(Guid.NewGuid(), nameof(PurchaseRequest), Guid.NewGuid(), Now);
        workflow.Approve(Guid.NewGuid(), Now, null);

        Assert.Throws<InvalidOperationException>(() => workflow.Approve(Guid.NewGuid(), Now, null));
    }

    [Fact]
    public void Reject_AlreadyRejected_Throws()
    {
        var workflow = WorkflowInstance.Create(Guid.NewGuid(), nameof(PurchaseRequest), Guid.NewGuid(), Now);
        workflow.Reject(Guid.NewGuid(), Now, null);

        Assert.Throws<InvalidOperationException>(() => workflow.Reject(Guid.NewGuid(), Now, null));
    }

    [Fact]
    public void Approve_AlreadyRejected_Throws()
    {
        // Rejection is terminal (dev guide §14) - no resubmission flow, so an already-rejected
        // workflow must not be approvable after the fact either.
        var workflow = WorkflowInstance.Create(Guid.NewGuid(), nameof(PurchaseRequest), Guid.NewGuid(), Now);
        workflow.Reject(Guid.NewGuid(), Now, null);

        Assert.Throws<InvalidOperationException>(() => workflow.Approve(Guid.NewGuid(), Now, null));
    }
}
