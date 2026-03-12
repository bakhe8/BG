using BG.Domain.Identity;

namespace BG.Domain.Workflow;

public sealed class RequestWorkflowStageDefinition
{
    public RequestWorkflowStageDefinition(
        Guid workflowDefinitionId,
        int sequence,
        Guid? roleId,
        string? titleResourceKey,
        string? summaryResourceKey,
        string? customTitle,
        string? customSummary,
        bool requiresLetterSignature,
        ApprovalDelegationPolicy delegationPolicy)
    {
        Id = Guid.NewGuid();
        WorkflowDefinitionId = workflowDefinitionId;
        Sequence = sequence;
        RoleId = roleId;
        TitleResourceKey = NormalizeOptional(titleResourceKey, 128);
        SummaryResourceKey = NormalizeOptional(summaryResourceKey, 256);
        CustomTitle = NormalizeOptional(customTitle, 128);
        CustomSummary = NormalizeOptional(customSummary, 256);
        RequiresLetterSignature = requiresLetterSignature;
        DelegationPolicy = delegationPolicy;
    }

    private RequestWorkflowStageDefinition()
    {
    }

    public Guid Id { get; private set; }

    public Guid WorkflowDefinitionId { get; private set; }

    public int Sequence { get; private set; }

    public Guid? RoleId { get; private set; }

    public string? TitleResourceKey { get; private set; }

    public string? SummaryResourceKey { get; private set; }

    public string? CustomTitle { get; private set; }

    public string? CustomSummary { get; private set; }

    public bool RequiresLetterSignature { get; private set; }

    public ApprovalDelegationPolicy DelegationPolicy { get; private set; }

    public RequestWorkflowDefinition WorkflowDefinition { get; private set; } = default!;

    public Role? Role { get; private set; }

    internal void Reorder(int sequence)
    {
        Sequence = sequence;
    }

    internal void Update(
        Guid? roleId,
        string? customTitle,
        string? customSummary,
        bool? requiresLetterSignature,
        ApprovalDelegationPolicy delegationPolicy)
    {
        RoleId = roleId;
        CustomTitle = NormalizeOptional(customTitle, 128);
        CustomSummary = NormalizeOptional(customSummary, 256);
        RequiresLetterSignature = requiresLetterSignature ?? RequiresLetterSignature;
        DelegationPolicy = delegationPolicy;
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
