using BG.Domain.Identity;

namespace BG.Domain.Workflow;

public sealed class RequestApprovalStage
{
    public RequestApprovalStage(
        Guid approvalProcessId,
        int sequence,
        Guid? roleId,
        string? titleResourceKey,
        string? summaryResourceKey,
        string? titleText,
        string? summaryText,
        bool requiresLetterSignature,
        ApprovalDelegationPolicy delegationPolicy)
    {
        Id = Guid.NewGuid();
        ApprovalProcessId = approvalProcessId;
        Sequence = sequence;
        RoleId = roleId;
        TitleResourceKey = NormalizeOptional(titleResourceKey, 128);
        SummaryResourceKey = NormalizeOptional(summaryResourceKey, 256);
        TitleText = NormalizeOptional(titleText, 128);
        SummaryText = NormalizeOptional(summaryText, 256);
        RequiresLetterSignature = requiresLetterSignature;
        DelegationPolicy = delegationPolicy;
        Status = RequestApprovalStageStatus.Pending;
    }

    private RequestApprovalStage()
    {
    }

    public Guid Id { get; private set; }

    public Guid ApprovalProcessId { get; private set; }

    public int Sequence { get; private set; }

    public Guid? RoleId { get; private set; }

    public string? TitleResourceKey { get; private set; }

    public string? SummaryResourceKey { get; private set; }

    public string? TitleText { get; private set; }

    public string? SummaryText { get; private set; }

    public bool RequiresLetterSignature { get; private set; }

    public ApprovalDelegationPolicy DelegationPolicy { get; private set; }

    public RequestApprovalStageStatus Status { get; private set; }

    public Guid? ActedByUserId { get; private set; }

    public Guid? ActedOnBehalfOfUserId { get; private set; }

    public Guid? ApprovalDelegationId { get; private set; }

    public DateTimeOffset? ActedAtUtc { get; private set; }

    public string? DecisionNote { get; private set; }

    public DateTimeOffset? SignatureAppliedAtUtc { get; private set; }

    public RequestApprovalProcess ApprovalProcess { get; private set; } = default!;

    public Role? Role { get; private set; }

    public User? ActedByUser { get; private set; }

    public User? ActedOnBehalfOfUser { get; private set; }

    public ApprovalDelegation? ApprovalDelegation { get; private set; }

    internal void Activate()
    {
        if (Status != RequestApprovalStageStatus.Pending)
        {
            throw new InvalidOperationException("Only pending stages can be activated.");
        }

        Status = RequestApprovalStageStatus.Active;
    }

    internal void Reset()
    {
        Status = RequestApprovalStageStatus.Pending;
        ActedByUserId = null;
        ActedOnBehalfOfUserId = null;
        ApprovalDelegationId = null;
        ActedAtUtc = null;
        DecisionNote = null;
        SignatureAppliedAtUtc = null;
    }

    internal void MarkApproved(
        Guid actedByUserId,
        DateTimeOffset actedAtUtc,
        string? note,
        Guid? actedOnBehalfOfUserId = null,
        Guid? approvalDelegationId = null)
    {
        EnsureActive();
        ValidateDelegationContext(actedByUserId, actedOnBehalfOfUserId, approvalDelegationId);
        Status = RequestApprovalStageStatus.Approved;
        ActedByUserId = actedByUserId;
        ActedOnBehalfOfUserId = actedOnBehalfOfUserId;
        ApprovalDelegationId = approvalDelegationId;
        ActedAtUtc = actedAtUtc;
        DecisionNote = NormalizeOptional(note, 512);
        SignatureAppliedAtUtc = RequiresLetterSignature ? actedAtUtc : null;
    }

    internal void MarkReturned(
        Guid actedByUserId,
        DateTimeOffset actedAtUtc,
        string? note,
        Guid? actedOnBehalfOfUserId = null,
        Guid? approvalDelegationId = null)
    {
        EnsureActive();
        ValidateDelegationContext(actedByUserId, actedOnBehalfOfUserId, approvalDelegationId);
        Status = RequestApprovalStageStatus.Returned;
        ActedByUserId = actedByUserId;
        ActedOnBehalfOfUserId = actedOnBehalfOfUserId;
        ApprovalDelegationId = approvalDelegationId;
        ActedAtUtc = actedAtUtc;
        DecisionNote = NormalizeOptional(note, 512);
    }

    internal void MarkRejected(
        Guid actedByUserId,
        DateTimeOffset actedAtUtc,
        string? note,
        Guid? actedOnBehalfOfUserId = null,
        Guid? approvalDelegationId = null)
    {
        EnsureActive();
        ValidateDelegationContext(actedByUserId, actedOnBehalfOfUserId, approvalDelegationId);
        Status = RequestApprovalStageStatus.Rejected;
        ActedByUserId = actedByUserId;
        ActedOnBehalfOfUserId = actedOnBehalfOfUserId;
        ApprovalDelegationId = approvalDelegationId;
        ActedAtUtc = actedAtUtc;
        DecisionNote = NormalizeOptional(note, 512);
    }

    internal void Cancel()
    {
        if (Status is not RequestApprovalStageStatus.Pending and not RequestApprovalStageStatus.Active)
        {
            throw new InvalidOperationException("Only pending or active stages can be cancelled.");
        }

        Status = RequestApprovalStageStatus.Cancelled;
        ActedByUserId = null;
        ActedOnBehalfOfUserId = null;
        ApprovalDelegationId = null;
        ActedAtUtc = null;
        DecisionNote = null;
        SignatureAppliedAtUtc = null;
    }

    private void EnsureActive()
    {
        if (Status != RequestApprovalStageStatus.Active)
        {
            throw new InvalidOperationException("Only the active stage can accept a decision.");
        }
    }

    private static void ValidateDelegationContext(
        Guid actedByUserId,
        Guid? actedOnBehalfOfUserId,
        Guid? approvalDelegationId)
    {
        if (approvalDelegationId.HasValue != actedOnBehalfOfUserId.HasValue)
        {
            throw new InvalidOperationException("Delegated actions must store both the delegation and the delegator.");
        }

        if (actedOnBehalfOfUserId.HasValue && actedOnBehalfOfUserId.Value == actedByUserId)
        {
            throw new InvalidOperationException("A user cannot act on behalf of themselves.");
        }
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
}
