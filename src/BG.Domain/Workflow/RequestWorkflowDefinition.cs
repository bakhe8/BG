using BG.Domain.Guarantees;

namespace BG.Domain.Workflow;

public sealed class RequestWorkflowDefinition
{
    public RequestWorkflowDefinition(
        string key,
        GuaranteeRequestType requestType,
        GuaranteeCategory? guaranteeCategory,
        string? guaranteeCategoryResourceKey,
        string titleResourceKey,
        string summaryResourceKey,
        DateTimeOffset createdAtUtc,
        ApprovalDelegationPolicy finalSignatureDelegationPolicy = ApprovalDelegationPolicy.Inherit,
        decimal? delegationAmountThreshold = null)
    {
        Id = Guid.NewGuid();
        Key = NormalizeRequired(key, nameof(key), 96);
        RequestType = requestType;
        GuaranteeCategory = guaranteeCategory;
        GuaranteeCategoryResourceKey = NormalizeOptional(guaranteeCategoryResourceKey, 128);
        TitleResourceKey = NormalizeRequired(titleResourceKey, nameof(titleResourceKey), 128);
        SummaryResourceKey = NormalizeRequired(summaryResourceKey, nameof(summaryResourceKey), 256);
        FinalSignatureDelegationPolicy = finalSignatureDelegationPolicy;
        DelegationAmountThreshold = NormalizeThreshold(delegationAmountThreshold);
        CreatedAtUtc = createdAtUtc;
        IsActive = false;
    }

    private RequestWorkflowDefinition()
    {
        Key = string.Empty;
        TitleResourceKey = string.Empty;
        SummaryResourceKey = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; }

    public GuaranteeRequestType RequestType { get; private set; }

    public GuaranteeCategory? GuaranteeCategory { get; private set; }

    public string? GuaranteeCategoryResourceKey { get; private set; }

    public string TitleResourceKey { get; private set; }

    public string SummaryResourceKey { get; private set; }

    public bool IsActive { get; private set; }

    public ApprovalDelegationPolicy FinalSignatureDelegationPolicy { get; private set; }

    public decimal? DelegationAmountThreshold { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? LastModifiedAtUtc { get; private set; }

    public ICollection<RequestWorkflowStageDefinition> Stages { get; private set; } = new List<RequestWorkflowStageDefinition>();

    public bool IsOperationallyReady =>
        Stages.Count > 0 &&
        Stages.All(stage => stage.RoleId.HasValue);

    public IReadOnlyList<string> GetIntegrityIssueResourceKeys()
    {
        var issues = new List<string>();

        if (Stages.Count == 0)
        {
            issues.Add("workflow.definition_requires_stage");
        }

        if (Stages.Any(stage => !stage.RoleId.HasValue))
        {
            issues.Add("workflow.role_required");
        }

        return issues;
    }

    public RequestWorkflowStageDefinition AddStage(
        Guid? roleId,
        string? titleResourceKey,
        string? summaryResourceKey,
        string? customTitle,
        string? customSummary,
        bool requiresLetterSignature,
        DateTimeOffset modifiedAtUtc,
        ApprovalDelegationPolicy delegationPolicy = ApprovalDelegationPolicy.Inherit)
    {
        var stage = new RequestWorkflowStageDefinition(
            Id,
            Stages.Count + 1,
            roleId,
            titleResourceKey,
            summaryResourceKey,
            customTitle,
            customSummary,
            requiresLetterSignature,
            delegationPolicy);

        Stages.Add(stage);
        Touch(modifiedAtUtc);
        return stage;
    }

    public void UpdateStage(
        Guid stageId,
        Guid? roleId,
        string? customTitle,
        string? customSummary,
        DateTimeOffset modifiedAtUtc,
        bool? requiresLetterSignature = null,
        ApprovalDelegationPolicy delegationPolicy = ApprovalDelegationPolicy.Inherit)
    {
        var stage = FindStage(stageId);
        stage.Update(roleId, customTitle, customSummary, requiresLetterSignature, delegationPolicy);
        Touch(modifiedAtUtc);
    }

    public void UpdateGovernance(
        ApprovalDelegationPolicy finalSignatureDelegationPolicy,
        decimal? delegationAmountThreshold,
        DateTimeOffset modifiedAtUtc)
    {
        FinalSignatureDelegationPolicy = finalSignatureDelegationPolicy;
        DelegationAmountThreshold = NormalizeThreshold(delegationAmountThreshold);
        Touch(modifiedAtUtc);
    }

    public void RemoveStage(Guid stageId, DateTimeOffset modifiedAtUtc)
    {
        if (Stages.Count <= 1)
        {
            throw new InvalidOperationException("Workflow definition must keep at least one stage.");
        }

        var stage = FindStage(stageId);
        Stages.Remove(stage);
        ReindexStages();
        Touch(modifiedAtUtc);
    }

    public void MoveStageUp(Guid stageId, DateTimeOffset modifiedAtUtc)
    {
        var orderedStages = Stages
            .OrderBy(stage => stage.Sequence)
            .ToList();

        var currentIndex = orderedStages.FindIndex(stage => stage.Id == stageId);

        if (currentIndex < 0)
        {
            throw new InvalidOperationException("Workflow stage not found.");
        }

        if (currentIndex == 0)
        {
            throw new InvalidOperationException("Workflow stage is already the first stage.");
        }

        (orderedStages[currentIndex - 1], orderedStages[currentIndex]) = (orderedStages[currentIndex], orderedStages[currentIndex - 1]);

        ReindexStages(orderedStages);
        Touch(modifiedAtUtc);
    }

    public void MoveStageDown(Guid stageId, DateTimeOffset modifiedAtUtc)
    {
        var orderedStages = Stages
            .OrderBy(stage => stage.Sequence)
            .ToList();

        var currentIndex = orderedStages.FindIndex(stage => stage.Id == stageId);

        if (currentIndex < 0)
        {
            throw new InvalidOperationException("Workflow stage not found.");
        }

        if (currentIndex == orderedStages.Count - 1)
        {
            throw new InvalidOperationException("Workflow stage is already the last stage.");
        }

        (orderedStages[currentIndex + 1], orderedStages[currentIndex]) = (orderedStages[currentIndex], orderedStages[currentIndex + 1]);

        ReindexStages(orderedStages);
        Touch(modifiedAtUtc);
    }

    private RequestWorkflowStageDefinition FindStage(Guid stageId)
    {
        return Stages.SingleOrDefault(stage => stage.Id == stageId)
               ?? throw new InvalidOperationException("Workflow stage not found.");
    }

    private void ReindexStages()
    {
        ReindexStages(Stages.OrderBy(stage => stage.Sequence));
    }

    private static void ReindexStages(IEnumerable<RequestWorkflowStageDefinition> stages)
    {
        var index = 1;

        foreach (var stage in stages)
        {
            stage.Reorder(index++);
        }
    }

    private void Touch(DateTimeOffset modifiedAtUtc)
    {
        AlignOperationalStatus();
        LastModifiedAtUtc = modifiedAtUtc;
    }

    public bool AlignOperationalStatus()
    {
        var nextIsActive = IsOperationallyReady;
        if (IsActive == nextIsActive)
        {
            return false;
        }

        IsActive = nextIsActive;
        return true;
    }

    private static string NormalizeRequired(string value, string paramName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Maximum length is {maxLength} characters.");
        }

        return normalized;
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
