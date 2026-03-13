namespace BG.Domain.Workflow;

public sealed class RequestApprovalProcess
{
    public RequestApprovalProcess(
        Guid guaranteeRequestId,
        Guid workflowDefinitionId,
        DateTimeOffset submittedAtUtc,
        ApprovalDelegationPolicy finalSignatureDelegationPolicy = ApprovalDelegationPolicy.Inherit,
        decimal? delegationAmountThreshold = null)
    {
        Id = Guid.NewGuid();
        GuaranteeRequestId = guaranteeRequestId;
        WorkflowDefinitionId = workflowDefinitionId;
        SubmittedAtUtc = submittedAtUtc;
        FinalSignatureDelegationPolicy = finalSignatureDelegationPolicy;
        DelegationAmountThreshold = NormalizeThreshold(delegationAmountThreshold);
        Status = RequestApprovalProcessStatus.InProgress;
    }

    private RequestApprovalProcess()
    {
    }

    public Guid Id { get; private set; }

    public Guid GuaranteeRequestId { get; private set; }

    public Guid WorkflowDefinitionId { get; private set; }

    public RequestApprovalProcessStatus Status { get; private set; }

    public DateTimeOffset SubmittedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public ApprovalDelegationPolicy FinalSignatureDelegationPolicy { get; private set; }

    public decimal? DelegationAmountThreshold { get; private set; }

    public Guid? LastReturnedByUserId { get; private set; }

    public DateTimeOffset? LastReturnedAtUtc { get; private set; }

    public string? LastReturnedNote { get; private set; }

    public Guid? LastRejectedByUserId { get; private set; }

    public DateTimeOffset? LastRejectedAtUtc { get; private set; }

    public string? LastRejectedNote { get; private set; }

    public BG.Domain.Guarantees.GuaranteeRequest GuaranteeRequest { get; private set; } = default!;

    public RequestWorkflowDefinition WorkflowDefinition { get; private set; } = default!;

    public ICollection<RequestApprovalStage> Stages { get; private set; } = new List<RequestApprovalStage>();

    public RequestApprovalStage AddStage(
        Guid? roleId,
        string? titleResourceKey,
        string? summaryResourceKey,
        string? titleText,
        string? summaryText,
        bool requiresLetterSignature,
        ApprovalDelegationPolicy delegationPolicy = ApprovalDelegationPolicy.Inherit)
    {
        var stage = new RequestApprovalStage(
            Id,
            Stages.Count + 1,
            roleId,
            titleResourceKey,
            summaryResourceKey,
            titleText,
            summaryText,
            requiresLetterSignature,
            delegationPolicy);

        Stages.Add(stage);
        return stage;
    }

    public void Start()
    {
        if (Stages.Count == 0)
        {
            throw new InvalidOperationException("Approval process requires at least one stage.");
        }

        if (Stages.Any(stage => !stage.RoleId.HasValue))
        {
            throw new InvalidOperationException("Approval process requires every stage to have an assigned role.");
        }

        if (Stages.Any(stage => stage.Status != RequestApprovalStageStatus.Pending))
        {
            throw new InvalidOperationException("Approval process was already started.");
        }

        Stages.OrderBy(stage => stage.Sequence).First().Activate();
        Status = RequestApprovalProcessStatus.InProgress;
    }

    public void ResetForResubmission(DateTimeOffset resubmittedAtUtc)
    {
        if (Status != RequestApprovalProcessStatus.Returned)
        {
            throw new InvalidOperationException("Only a returned approval process can be resubmitted.");
        }

        if (Stages.Any(stage => !stage.RoleId.HasValue))
        {
            throw new InvalidOperationException("Approval process requires every stage to have an assigned role.");
        }

        foreach (var stage in Stages)
        {
            stage.Reset();
        }

        Status = RequestApprovalProcessStatus.InProgress;
        SubmittedAtUtc = resubmittedAtUtc;
        CompletedAtUtc = null;
        LastRejectedAtUtc = null;
        LastRejectedByUserId = null;
        LastRejectedNote = null;

        Stages.OrderBy(stage => stage.Sequence).First().Activate();
    }

    public RequestApprovalStage? GetCurrentStage()
    {
        return Stages.SingleOrDefault(stage => stage.Status == RequestApprovalStageStatus.Active);
    }

    public bool IsActionableByAnyOf(IEnumerable<Guid> roleIds)
    {
        ArgumentNullException.ThrowIfNull(roleIds);

        var currentStage = GetCurrentStage();
        if (currentStage?.RoleId is null)
        {
            return false;
        }

        return roleIds.Contains(currentStage.RoleId.Value);
    }

    public bool ApproveCurrentStage(
        Guid actedByUserId,
        DateTimeOffset actedAtUtc,
        string? note,
        Guid? actedOnBehalfOfUserId = null,
        Guid? approvalDelegationId = null)
    {
        var currentStage = GetRequiredCurrentStage();
        currentStage.MarkApproved(actedByUserId, actedAtUtc, note, actedOnBehalfOfUserId, approvalDelegationId);

        var nextStage = Stages
            .Where(stage => stage.Sequence > currentStage.Sequence)
            .OrderBy(stage => stage.Sequence)
            .FirstOrDefault();

        if (nextStage is null)
        {
            Status = RequestApprovalProcessStatus.Approved;
            CompletedAtUtc = actedAtUtc;
            return true;
        }

        nextStage.Activate();
        return false;
    }

    public void ReturnCurrentStage(
        Guid actedByUserId,
        DateTimeOffset actedAtUtc,
        string? note,
        Guid? actedOnBehalfOfUserId = null,
        Guid? approvalDelegationId = null)
    {
        var currentStage = GetRequiredCurrentStage();
        currentStage.MarkReturned(actedByUserId, actedAtUtc, note, actedOnBehalfOfUserId, approvalDelegationId);
        Status = RequestApprovalProcessStatus.Returned;
        LastReturnedByUserId = actedByUserId;
        LastReturnedAtUtc = actedAtUtc;
        LastReturnedNote = NormalizeOptional(note, 512);
    }

    public void RejectCurrentStage(
        Guid actedByUserId,
        DateTimeOffset actedAtUtc,
        string? note,
        Guid? actedOnBehalfOfUserId = null,
        Guid? approvalDelegationId = null)
    {
        var currentStage = GetRequiredCurrentStage();
        currentStage.MarkRejected(actedByUserId, actedAtUtc, note, actedOnBehalfOfUserId, approvalDelegationId);
        Status = RequestApprovalProcessStatus.Rejected;
        CompletedAtUtc = actedAtUtc;
        LastRejectedByUserId = actedByUserId;
        LastRejectedAtUtc = actedAtUtc;
        LastRejectedNote = NormalizeOptional(note, 512);
    }

    public void CancelByOwner(DateTimeOffset cancelledAtUtc)
    {
        if (Status != RequestApprovalProcessStatus.InProgress)
        {
            throw new InvalidOperationException("Only an in-progress approval process can be cancelled.");
        }

        foreach (var stage in Stages.Where(stage =>
                     stage.Status is RequestApprovalStageStatus.Pending or RequestApprovalStageStatus.Active))
        {
            stage.Cancel();
        }

        Status = RequestApprovalProcessStatus.Cancelled;
        CompletedAtUtc = cancelledAtUtc;
    }

    private RequestApprovalStage GetRequiredCurrentStage()
    {
        return GetCurrentStage()
               ?? throw new InvalidOperationException("Approval process does not have an active stage.");
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Maximum length is {maxLength} characters.");
        }

        return normalized;
    }

    private static decimal? NormalizeThreshold(decimal? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (value.Value <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Threshold must be greater than zero.");
        }

        return decimal.Round(value.Value, 2, MidpointRounding.AwayFromZero);
    }
}
