using BG.Domain.Workflow;

namespace BG.UnitTests.Domain;

public sealed class RequestApprovalProcessTests
{
    [Fact]
    public void Approving_last_stage_completes_the_process()
    {
        var actedAtUtc = DateTimeOffset.UtcNow;
        var process = new RequestApprovalProcess(Guid.NewGuid(), Guid.NewGuid(), actedAtUtc.AddMinutes(-5));
        process.AddStage(
            Guid.NewGuid(),
            "WorkflowStage_GuaranteesSupervisor_Title",
            "WorkflowStage_GuaranteesSupervisor_Summary",
            "Guarantees Supervisor",
            "First approval stage",
            requiresLetterSignature: true);

        process.Start();

        var completed = process.ApproveCurrentStage(Guid.NewGuid(), actedAtUtc, "Approved");

        Assert.True(completed);
        Assert.Equal(RequestApprovalProcessStatus.Approved, process.Status);
        Assert.Equal(actedAtUtc, process.CompletedAtUtc);
        Assert.Equal(RequestApprovalStageStatus.Approved, Assert.Single(process.Stages).Status);
        Assert.Equal(actedAtUtc, Assert.Single(process.Stages).SignatureAppliedAtUtc);
    }

    [Fact]
    public void Returned_process_can_be_reset_for_resubmission()
    {
        var returnedAtUtc = DateTimeOffset.UtcNow;
        var resubmittedAtUtc = returnedAtUtc.AddHours(2);
        var process = new RequestApprovalProcess(Guid.NewGuid(), Guid.NewGuid(), returnedAtUtc.AddMinutes(-5));
        process.AddStage(
            Guid.NewGuid(),
            "WorkflowStage_GuaranteesSupervisor_Title",
            "WorkflowStage_GuaranteesSupervisor_Summary",
            "Guarantees Supervisor",
            "First approval stage",
            requiresLetterSignature: true);
        process.AddStage(
            Guid.NewGuid(),
            "WorkflowStage_DepartmentManager_Title",
            "WorkflowStage_DepartmentManager_Summary",
            "Department Manager",
            "Second approval stage",
            requiresLetterSignature: true);

        process.Start();
        process.ReturnCurrentStage(Guid.NewGuid(), returnedAtUtc, "Need correction");

        Assert.Equal(RequestApprovalProcessStatus.Returned, process.Status);
        Assert.Equal("Need correction", process.LastReturnedNote);

        process.ResetForResubmission(resubmittedAtUtc);

        Assert.Equal(RequestApprovalProcessStatus.InProgress, process.Status);
        Assert.Equal(resubmittedAtUtc, process.SubmittedAtUtc);
        Assert.Equal(RequestApprovalStageStatus.Active, process.Stages.Single(stage => stage.Sequence == 1).Status);
        Assert.Equal(RequestApprovalStageStatus.Pending, process.Stages.Single(stage => stage.Sequence == 2).Status);
        Assert.Null(process.Stages.Single(stage => stage.Sequence == 1).DecisionNote);
    }

    [Fact]
    public void Delegated_approval_records_on_behalf_of_context_on_the_stage()
    {
        var actedAtUtc = DateTimeOffset.UtcNow;
        var process = new RequestApprovalProcess(Guid.NewGuid(), Guid.NewGuid(), actedAtUtc.AddMinutes(-5));
        var delegatorUserId = Guid.NewGuid();
        var delegationId = Guid.NewGuid();
        process.AddStage(
            Guid.NewGuid(),
            "WorkflowStage_GuaranteesSupervisor_Title",
            "WorkflowStage_GuaranteesSupervisor_Summary",
            "Guarantees Supervisor",
            "First approval stage",
            requiresLetterSignature: true);

        process.Start();

        process.ApproveCurrentStage(Guid.NewGuid(), actedAtUtc, "Approved", delegatorUserId, delegationId);

        var stage = Assert.Single(process.Stages);
        Assert.Equal(delegatorUserId, stage.ActedOnBehalfOfUserId);
        Assert.Equal(delegationId, stage.ApprovalDelegationId);
    }

    [Fact]
    public void Starting_process_with_unassigned_stage_is_not_allowed()
    {
        var process = new RequestApprovalProcess(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        process.AddStage(
            roleId: null,
            titleResourceKey: "WorkflowStage_GuaranteesSupervisor_Title",
            summaryResourceKey: "WorkflowStage_GuaranteesSupervisor_Summary",
            titleText: null,
            summaryText: null,
            requiresLetterSignature: true);

        var exception = Assert.Throws<InvalidOperationException>(() => process.Start());

        Assert.Contains("assigned role", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
