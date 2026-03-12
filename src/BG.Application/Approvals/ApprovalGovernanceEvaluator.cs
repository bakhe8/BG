using BG.Application.Models.Approvals;
using BG.Domain.Guarantees;
using BG.Domain.Workflow;

namespace BG.Application.Approvals;

internal static class ApprovalGovernanceEvaluator
{
    public static ApprovalGovernanceEvaluation Evaluate(
        ApprovalQueueItemReadModel request,
        Guid actorUserId,
        Guid responsibleSignerUserId,
        ApprovalGovernanceOptions options)
    {
        if (!request.CurrentStageSequence.HasValue)
        {
            return ApprovalGovernanceEvaluation.Allowed;
        }

        return EvaluateCore(
            request.RequestType,
            request.TotalStageCount,
            request.CurrentStageSequence.Value,
            request.RequiresLetterSignature,
            request.FinalSignatureDelegationPolicy,
            request.DelegationAmountThreshold,
            request.RequestedAmount ?? request.CurrentGuaranteeAmount,
            request.CurrentStageDelegationPolicy,
            request.PriorSignatures.Select(signature => new ApprovalPriorSignatureSnapshot(
                signature.StageId,
                signature.Sequence,
                signature.StageTitleResourceKey,
                signature.StageTitle,
                signature.StageRoleName,
                signature.ActorUserId,
                signature.ActorDisplayName,
                signature.ResponsibleSignerUserId,
                signature.ResponsibleSignerDisplayName)),
            actorUserId,
            responsibleSignerUserId,
            options);
    }

    public static ApprovalGovernanceEvaluation Evaluate(
        RequestApprovalProcess process,
        GuaranteeRequestType requestType,
        decimal effectiveApprovalAmount,
        Guid actorUserId,
        Guid responsibleSignerUserId,
        ApprovalGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(process);

        var currentStage = process.GetCurrentStage();
        if (currentStage is null)
        {
            return ApprovalGovernanceEvaluation.Allowed;
        }

        return EvaluateCore(
            requestType,
            process.Stages.Count,
            currentStage.Sequence,
            currentStage.RequiresLetterSignature,
            process.FinalSignatureDelegationPolicy,
            process.DelegationAmountThreshold,
            effectiveApprovalAmount,
            currentStage.DelegationPolicy,
            process.Stages
                .Where(stage =>
                    stage.Sequence < currentStage.Sequence &&
                    stage.Status == RequestApprovalStageStatus.Approved &&
                    stage.ActedAtUtc.HasValue)
                .Select(stage => new ApprovalPriorSignatureSnapshot(
                    stage.Id,
                    stage.Sequence,
                    stage.TitleResourceKey,
                    stage.TitleText,
                    stage.Role?.Name,
                    stage.ActedByUserId,
                    stage.ActedByUser?.DisplayName,
                    stage.ActedOnBehalfOfUserId ?? stage.ActedByUserId,
                    stage.ActedOnBehalfOfUser?.DisplayName ?? stage.ActedByUser?.DisplayName)),
            actorUserId,
            responsibleSignerUserId,
            options);
    }

    private static ApprovalGovernanceEvaluation EvaluateCore(
        GuaranteeRequestType requestType,
        int totalStageCount,
        int currentStageSequence,
        bool requiresLetterSignature,
        ApprovalDelegationPolicy finalSignatureDelegationPolicy,
        decimal? delegationAmountThreshold,
        decimal effectiveApprovalAmount,
        ApprovalDelegationPolicy currentStageDelegationPolicy,
        IEnumerable<ApprovalPriorSignatureSnapshot> priorSignatures,
        Guid actorUserId,
        Guid responsibleSignerUserId,
        ApprovalGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var orderedPriorSignatures = priorSignatures
            .Where(signature => signature.Sequence < currentStageSequence)
            .OrderBy(signature => signature.Sequence)
            .ToArray();

        if (options.RequireSignerSeparation)
        {
            var actorConflict = orderedPriorSignatures.FirstOrDefault(signature => signature.ActorUserId == actorUserId);
            if (actorConflict is not null)
            {
                return ApprovalGovernanceEvaluation.Blocked(
                    "ApprovalGovernancePolicy_SignerSeparation",
                    "ApprovalQueue_GovernanceConflictSameActor",
                    actorConflict);
            }

            var responsibleSignerConflict = orderedPriorSignatures.FirstOrDefault(
                signature => signature.ResponsibleSignerUserId == responsibleSignerUserId);
            if (responsibleSignerConflict is not null)
            {
                return ApprovalGovernanceEvaluation.Blocked(
                    "ApprovalGovernancePolicy_SignerSeparation",
                    "ApprovalQueue_GovernanceConflictSameResponsibleSigner",
                    responsibleSignerConflict);
            }
        }

        var isDelegated = actorUserId != responsibleSignerUserId;
        if (!isDelegated)
        {
            return ApprovalGovernanceEvaluation.AllowedWithPolicy("ApprovalGovernancePolicy_DirectActor");
        }

        if (currentStageDelegationPolicy == ApprovalDelegationPolicy.DirectSignerRequired)
        {
            return ApprovalGovernanceEvaluation.Blocked(
                "ApprovalGovernancePolicy_StageDirectSignerOnly",
                "ApprovalQueue_GovernanceConflictStageDirectSignerOnly");
        }

        if (delegationAmountThreshold.HasValue && effectiveApprovalAmount >= delegationAmountThreshold.Value)
        {
            return ApprovalGovernanceEvaluation.Blocked(
                "ApprovalGovernancePolicy_AmountThresholdDirectOnly",
                "ApprovalQueue_GovernanceConflictAmountThresholdDirectOnly");
        }

        var stageAllowsDelegation = currentStageDelegationPolicy == ApprovalDelegationPolicy.AllowDelegation;
        var finalSignatureStage = requiresLetterSignature && currentStageSequence >= totalStageCount;

        if (finalSignatureStage && finalSignatureDelegationPolicy == ApprovalDelegationPolicy.DirectSignerRequired)
        {
            return ApprovalGovernanceEvaluation.Blocked(
                "ApprovalGovernancePolicy_FinalSignatureDirectOnly",
                "ApprovalQueue_GovernanceConflictFinalSignatureDirectOnly");
        }

        if (currentStageDelegationPolicy == ApprovalDelegationPolicy.AllowDelegation)
        {
            return ApprovalGovernanceEvaluation.AllowedWithPolicy("ApprovalGovernancePolicy_StageDelegationAllowed");
        }

        if (finalSignatureStage &&
            finalSignatureDelegationPolicy == ApprovalDelegationPolicy.AllowDelegation)
        {
            return ApprovalGovernanceEvaluation.AllowedWithPolicy("ApprovalGovernancePolicy_FinalSignatureDelegationAllowed");
        }

        if (!stageAllowsDelegation &&
            finalSignatureStage &&
            options.RequireDirectSignerForFinalSignatureStage &&
            finalSignatureDelegationPolicy == ApprovalDelegationPolicy.Inherit)
        {
            return ApprovalGovernanceEvaluation.Blocked(
                "ApprovalGovernancePolicy_FinalSignatureDirectOnly",
                "ApprovalQueue_GovernanceConflictFinalSignatureDirectOnly");
        }

        if (!stageAllowsDelegation &&
            requiresLetterSignature &&
            (options.DirectSignerOnlyRequestTypes?.Contains(requestType) ?? false))
        {
            return ApprovalGovernanceEvaluation.Blocked(
                "ApprovalGovernancePolicy_RequestTypeDirectOnly",
                "ApprovalQueue_GovernanceConflictRequestTypeDirectOnly");
        }

        return ApprovalGovernanceEvaluation.AllowedWithPolicy("ApprovalGovernancePolicy_DefaultDelegationAllowed");
    }
}

internal sealed record ApprovalPriorSignatureSnapshot(
    Guid StageId,
    int Sequence,
    string? StageTitleResourceKey,
    string? StageTitle,
    string? StageRoleName,
    Guid? ActorUserId,
    string? ActorDisplayName,
    Guid? ResponsibleSignerUserId,
    string? ResponsibleSignerDisplayName);

internal sealed record ApprovalGovernanceEvaluation(
    bool IsDecisionBlocked,
    string? ReasonResourceKey,
    string? PolicyResourceKey,
    string? AppliedPolicyResourceKey,
    Guid? ConflictingStageId,
    int? ConflictingStageSequence,
    string? ConflictingStageTitleResourceKey,
    string? ConflictingStageTitle,
    string? ConflictingStageRoleName,
    string? ConflictingActorDisplayName,
    string? ConflictingResponsibleSignerDisplayName)
{
    public static ApprovalGovernanceEvaluation Allowed { get; } =
        new(false, null, null, null, null, null, null, null, null, null, null);

    public static ApprovalGovernanceEvaluation AllowedWithPolicy(string appliedPolicyResourceKey)
    {
        return new ApprovalGovernanceEvaluation(
            false,
            null,
            null,
            appliedPolicyResourceKey,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    public static ApprovalGovernanceEvaluation Blocked(
        string policyResourceKey,
        string reasonResourceKey)
    {
        return new ApprovalGovernanceEvaluation(
            true,
            reasonResourceKey,
            policyResourceKey,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    public static ApprovalGovernanceEvaluation Blocked(
        string policyResourceKey,
        string reasonResourceKey,
        ApprovalPriorSignatureSnapshot snapshot)
    {
        return new ApprovalGovernanceEvaluation(
            true,
            reasonResourceKey,
            policyResourceKey,
            null,
            snapshot.StageId,
            snapshot.Sequence,
            snapshot.StageTitleResourceKey,
            snapshot.StageTitle,
            snapshot.StageRoleName,
            snapshot.ActorDisplayName,
            snapshot.ResponsibleSignerDisplayName);
    }

    public ApprovalGovernanceStatusDto ToDto()
    {
        return new ApprovalGovernanceStatusDto(
            IsDecisionBlocked,
            ReasonResourceKey,
            PolicyResourceKey,
            ConflictingStageTitleResourceKey,
            ConflictingStageTitle,
            ConflictingStageRoleName,
            ConflictingActorDisplayName,
            ConflictingResponsibleSignerDisplayName);
    }
}
