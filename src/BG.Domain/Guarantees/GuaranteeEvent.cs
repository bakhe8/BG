using BG.Domain.Identity;

namespace BG.Domain.Guarantees;

public sealed class GuaranteeEvent
{
    private GuaranteeEvent(
        Guid guaranteeId,
        Guid? guaranteeRequestId,
        Guid? guaranteeCorrespondenceId,
        Guid? guaranteeDocumentId,
        GuaranteeEventType eventType,
        DateTimeOffset occurredAtUtc,
        string summary,
        Guid? actorUserId,
        string? actorDisplayName,
        decimal? previousAmount,
        decimal? newAmount,
        DateOnly? previousExpiryDate,
        DateOnly? newExpiryDate,
        GuaranteeStatus? previousStatus,
        GuaranteeStatus? newStatus,
        string? approvalStageLabel = null,
        string? approvalPolicyResourceKey = null,
        string? approvalResponsibleSignerDisplayName = null,
        ApprovalLedgerExecutionMode? approvalExecutionMode = null,
        string? dispatchStageResourceKey = null,
        string? dispatchMethodResourceKey = null,
        string? dispatchPolicyResourceKey = null,
        string? operationsScenarioTitleResourceKey = null,
        string? operationsLaneResourceKey = null,
        string? operationsMatchConfidenceResourceKey = null,
        int? operationsMatchScore = null,
        string? operationsPolicyResourceKey = null)
    {
        GuaranteeId = guaranteeId;
        GuaranteeRequestId = guaranteeRequestId;
        GuaranteeCorrespondenceId = guaranteeCorrespondenceId;
        GuaranteeDocumentId = guaranteeDocumentId;
        EventType = eventType;
        OccurredAtUtc = occurredAtUtc;
        Summary = NormalizeRequired(summary, nameof(summary), 512);
        ActorUserId = actorUserId;
        ActorDisplayName = NormalizeOptional(actorDisplayName, 256);
        PreviousAmount = previousAmount;
        NewAmount = newAmount;
        PreviousExpiryDate = previousExpiryDate;
        NewExpiryDate = newExpiryDate;
        PreviousStatus = previousStatus?.ToString();
        NewStatus = newStatus?.ToString();
        ApprovalStageLabel = NormalizeOptional(approvalStageLabel, 128);
        ApprovalPolicyResourceKey = NormalizeOptional(approvalPolicyResourceKey, 128);
        ApprovalResponsibleSignerDisplayName = NormalizeOptional(approvalResponsibleSignerDisplayName, 256);
        ApprovalExecutionMode = approvalExecutionMode?.ToString();
        DispatchStageResourceKey = NormalizeOptional(dispatchStageResourceKey, 128);
        DispatchMethodResourceKey = NormalizeOptional(dispatchMethodResourceKey, 128);
        DispatchPolicyResourceKey = NormalizeOptional(dispatchPolicyResourceKey, 128);
        OperationsScenarioTitleResourceKey = NormalizeOptional(operationsScenarioTitleResourceKey, 128);
        OperationsLaneResourceKey = NormalizeOptional(operationsLaneResourceKey, 128);
        OperationsMatchConfidenceResourceKey = NormalizeOptional(operationsMatchConfidenceResourceKey, 128);
        OperationsMatchScore = operationsMatchScore;
        OperationsPolicyResourceKey = NormalizeOptional(operationsPolicyResourceKey, 128);
    }

    private GuaranteeEvent()
    {
        Summary = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid GuaranteeId { get; private set; }

    public Guid? GuaranteeRequestId { get; private set; }

    public Guid? GuaranteeCorrespondenceId { get; private set; }

    public Guid? GuaranteeDocumentId { get; private set; }

    public GuaranteeEventType EventType { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string Summary { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string? ActorDisplayName { get; private set; }

    public decimal? PreviousAmount { get; private set; }

    public decimal? NewAmount { get; private set; }

    public DateOnly? PreviousExpiryDate { get; private set; }

    public DateOnly? NewExpiryDate { get; private set; }

    public string? PreviousStatus { get; private set; }

    public string? NewStatus { get; private set; }

    public string? ApprovalStageLabel { get; private set; }

    public string? ApprovalPolicyResourceKey { get; private set; }

    public string? ApprovalResponsibleSignerDisplayName { get; private set; }

    public string? ApprovalExecutionMode { get; private set; }

    public string? DispatchStageResourceKey { get; private set; }

    public string? DispatchMethodResourceKey { get; private set; }

    public string? DispatchPolicyResourceKey { get; private set; }

    public string? OperationsScenarioTitleResourceKey { get; private set; }

    public string? OperationsLaneResourceKey { get; private set; }

    public string? OperationsMatchConfidenceResourceKey { get; private set; }

    public int? OperationsMatchScore { get; private set; }

    public string? OperationsPolicyResourceKey { get; private set; }

    public Guarantee Guarantee { get; internal set; } = default!;

    public GuaranteeRequest? GuaranteeRequest { get; internal set; }

    public GuaranteeCorrespondence? GuaranteeCorrespondence { get; internal set; }

    public GuaranteeDocument? GuaranteeDocument { get; internal set; }

    public User? ActorUser { get; internal set; }

    internal static GuaranteeEvent Registered(Guid guaranteeId, DateTimeOffset occurredAtUtc)
    {
        return new GuaranteeEvent(
            guaranteeId,
            null,
            null,
            null,
            GuaranteeEventType.Registered,
            occurredAtUtc,
            "Guarantee registered in BG.",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            GuaranteeStatus.Active,
            null,
            null,
            null,
            null);
    }

    internal static GuaranteeEvent RequestRecorded(
        Guid guaranteeId,
        Guid requestId,
        GuaranteeRequestType requestType,
        GuaranteeRequestChannel requestChannel,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName)
    {
        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            null,
            null,
            GuaranteeEventType.RequestRecorded,
            occurredAtUtc,
            $"Request recorded: {requestType} via {requestChannel}.",
            actorUserId,
            actorDisplayName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    internal static GuaranteeEvent RequestSubmittedForApproval(
        Guid guaranteeId,
        Guid requestId,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName,
        string? stageLabel)
    {
        var summary = string.IsNullOrWhiteSpace(stageLabel)
            ? "Request submitted for approval."
            : $"Request submitted for approval. Current stage: {stageLabel}.";

        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            null,
            null,
            GuaranteeEventType.RequestSubmittedForApproval,
            occurredAtUtc,
            summary,
            actorUserId,
            actorDisplayName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    internal static GuaranteeEvent RequestUpdated(
        Guid guaranteeId,
        Guid requestId,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName,
        decimal? previousAmount,
        decimal? newAmount,
        DateOnly? previousExpiryDate,
        DateOnly? newExpiryDate)
    {
        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            null,
            null,
            GuaranteeEventType.RequestUpdated,
            occurredAtUtc,
            "Request details revised before approval submission.",
            actorUserId,
            actorDisplayName,
            previousAmount,
            newAmount,
            previousExpiryDate,
            newExpiryDate,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    internal static GuaranteeEvent RequestCancelled(
        Guid guaranteeId,
        Guid requestId,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName)
    {
        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            null,
            null,
            GuaranteeEventType.RequestCancelled,
            occurredAtUtc,
            "Request cancelled by owner before completion.",
            actorUserId,
            actorDisplayName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    internal static GuaranteeEvent RequestWithdrawn(
        Guid guaranteeId,
        Guid requestId,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName,
        string? stageLabel)
    {
        var summary = string.IsNullOrWhiteSpace(stageLabel)
            ? "Request withdrawn by owner while awaiting approval."
            : $"Request withdrawn by owner while awaiting approval. Active stage: {stageLabel}.";

        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            null,
            null,
            GuaranteeEventType.RequestWithdrawn,
            occurredAtUtc,
            summary,
            actorUserId,
            actorDisplayName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    internal static GuaranteeEvent ApprovalDecisionRecorded(
        Guid guaranteeId,
        Guid requestId,
        GuaranteeEventType eventType,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName,
        string? responsibleSignerDisplayName,
        string? stageLabel,
        string? approvalPolicyResourceKey,
        ApprovalLedgerExecutionMode approvalExecutionMode,
        string? note)
    {
        var summaryPrefix = eventType switch
        {
            GuaranteeEventType.ApprovalApproved => "Approval stage approved",
            GuaranteeEventType.ApprovalReturned => "Approval stage returned",
            GuaranteeEventType.ApprovalRejected => "Approval stage rejected",
            _ => throw new InvalidOperationException("Unsupported approval event type.")
        };

        var stageSuffix = string.IsNullOrWhiteSpace(stageLabel) ? "." : $": {stageLabel}.";
        var delegatedSuffix = approvalExecutionMode != ApprovalLedgerExecutionMode.Delegated ||
                              string.IsNullOrWhiteSpace(responsibleSignerDisplayName)
            ? string.Empty
            : $" On behalf of {responsibleSignerDisplayName.Trim()}.";
        var noteSuffix = string.IsNullOrWhiteSpace(note) ? string.Empty : $" Note: {note.Trim()}.";

        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            null,
            null,
            eventType,
            occurredAtUtc,
            $"{summaryPrefix}{stageSuffix}{delegatedSuffix}{noteSuffix}",
            actorUserId,
            actorDisplayName,
            null,
            null,
            null,
            null,
            null,
            null,
            stageLabel,
            approvalPolicyResourceKey,
            responsibleSignerDisplayName,
            approvalExecutionMode);
    }

    internal static GuaranteeEvent CorrespondenceRecorded(
        Guid guaranteeId,
        Guid? requestId,
        Guid correspondenceId,
        GuaranteeCorrespondenceDirection direction,
        GuaranteeCorrespondenceKind kind,
        string referenceNumber,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName)
    {
        var eventType = direction == GuaranteeCorrespondenceDirection.Outgoing
            ? GuaranteeEventType.OutgoingCorrespondenceRecorded
            : GuaranteeEventType.IncomingCorrespondenceRecorded;

        var summary = $"{direction} correspondence recorded: {kind} ({referenceNumber}).";

        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            correspondenceId,
            null,
            eventType,
            occurredAtUtc,
            summary,
            actorUserId,
            actorDisplayName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    internal static GuaranteeEvent OutgoingLetterPrinted(
        Guid guaranteeId,
        Guid requestId,
        Guid correspondenceId,
        GuaranteeOutgoingLetterPrintMode printMode,
        int printCount,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName)
    {
        var action = printCount <= 1 ? "printed" : "reprinted";

        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            correspondenceId,
            null,
            GuaranteeEventType.OutgoingLetterPrinted,
            occurredAtUtc,
            $"Outgoing request letter {action} via {printMode} (copy {printCount}).",
            actorUserId,
            actorDisplayName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            dispatchStageResourceKey: GetDispatchPrintStageResourceKey(printCount),
            dispatchMethodResourceKey: GetDispatchPrintModeResourceKey(printMode),
            dispatchPolicyResourceKey: "DispatchLedgerPolicy_PrintRecordedBeforeExternalDispatch");
    }

    internal static GuaranteeEvent OutgoingLetterDispatched(
        Guid guaranteeId,
        Guid requestId,
        Guid correspondenceId,
        GuaranteeDispatchChannel dispatchChannel,
        string? dispatchReference,
        string? dispatchNote,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName)
    {
        var referenceSuffix = string.IsNullOrWhiteSpace(dispatchReference)
            ? string.Empty
            : $" Tracking: {dispatchReference.Trim()}.";
        var noteSuffix = string.IsNullOrWhiteSpace(dispatchNote)
            ? string.Empty
            : $" Note: {dispatchNote.Trim()}.";

        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            correspondenceId,
            null,
            GuaranteeEventType.OutgoingLetterDispatched,
            occurredAtUtc,
            $"Outgoing request letter dispatched via {dispatchChannel}.{referenceSuffix}{noteSuffix}",
            actorUserId,
            actorDisplayName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            dispatchStageResourceKey: "DispatchLedgerStep_Dispatched",
            dispatchMethodResourceKey: GetDispatchChannelResourceKey(dispatchChannel),
            dispatchPolicyResourceKey: "DispatchLedgerPolicy_BankHandoffRecorded");
    }

    internal static GuaranteeEvent OutgoingLetterDelivered(
        Guid guaranteeId,
        Guid requestId,
        Guid correspondenceId,
        GuaranteeDispatchChannel? dispatchChannel,
        string? deliveryReference,
        string? deliveryNote,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName)
    {
        var referenceSuffix = string.IsNullOrWhiteSpace(deliveryReference)
            ? string.Empty
            : $" Receipt: {deliveryReference.Trim()}.";
        var noteSuffix = string.IsNullOrWhiteSpace(deliveryNote)
            ? string.Empty
            : $" Note: {deliveryNote.Trim()}.";

        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            correspondenceId,
            null,
            GuaranteeEventType.OutgoingLetterDelivered,
            occurredAtUtc,
            $"Outgoing request letter delivery confirmed.{referenceSuffix}{noteSuffix}",
            actorUserId,
            actorDisplayName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            dispatchStageResourceKey: "DispatchLedgerStep_Delivered",
            dispatchMethodResourceKey: GetDispatchChannelResourceKey(dispatchChannel),
            dispatchPolicyResourceKey: "DispatchLedgerPolicy_DeliveryConfirmationRecorded");
    }

    internal static GuaranteeEvent OutgoingLetterDispatchReopened(
        Guid guaranteeId,
        Guid requestId,
        Guid correspondenceId,
        GuaranteeDispatchChannel? dispatchChannel,
        string? dispatchReference,
        string? correctionNote,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName)
    {
        var channelFragment = dispatchChannel.HasValue
            ? $" after {dispatchChannel.Value}"
            : string.Empty;
        var referenceSuffix = string.IsNullOrWhiteSpace(dispatchReference)
            ? string.Empty
            : $" Tracking: {dispatchReference.Trim()}.";
        var noteSuffix = string.IsNullOrWhiteSpace(correctionNote)
            ? string.Empty
            : $" Note: {correctionNote.Trim()}.";

        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            correspondenceId,
            null,
            GuaranteeEventType.OutgoingLetterDispatchReopened,
            occurredAtUtc,
            $"Outgoing request letter dispatch reopened{channelFragment}.{referenceSuffix}{noteSuffix}",
            actorUserId,
            actorDisplayName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            dispatchStageResourceKey: "DispatchLedgerStep_Reopened",
            dispatchMethodResourceKey: GetDispatchChannelResourceKey(dispatchChannel),
            dispatchPolicyResourceKey: "DispatchLedgerPolicy_HandoffCorrectedBeforeDelivery");
    }

    internal static GuaranteeEvent DocumentCaptured(
        Guid guaranteeId,
        Guid documentId,
        Guid? requestId,
        GuaranteeDocumentType documentType,
        GuaranteeDocumentCaptureChannel captureChannel,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName,
        string? sourceSystemName,
        string? sourceReference)
    {
        var sourceFragments = new List<string>();

        if (!string.IsNullOrWhiteSpace(sourceSystemName))
        {
            sourceFragments.Add($"source system: {sourceSystemName.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(sourceReference))
        {
            sourceFragments.Add($"source reference: {sourceReference.Trim()}");
        }

        var provenanceSuffix = sourceFragments.Count == 0
            ? string.Empty
            : $" ({string.Join(", ", sourceFragments)})";

        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            null,
            documentId,
            GuaranteeEventType.DocumentCaptured,
            occurredAtUtc,
            $"Document captured: {documentType} via {captureChannel}.{provenanceSuffix}",
            actorUserId,
            actorDisplayName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    internal static GuaranteeEvent RequestDocumentLinked(
        Guid guaranteeId,
        Guid requestId,
        Guid documentId,
        GuaranteeDocumentType documentType,
        string fileName,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName)
    {
        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            null,
            documentId,
            GuaranteeEventType.RequestDocumentLinked,
            occurredAtUtc,
            $"Document attached to request: {documentType} ({NormalizeRequired(fileName, nameof(fileName), 260)}).",
            actorUserId,
            actorDisplayName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    internal static GuaranteeEvent FromConfirmedChange(
        Guid guaranteeId,
        Guid requestId,
        Guid correspondenceId,
        GuaranteeEventType eventType,
        DateTimeOffset occurredAtUtc,
        string summary,
        Guid? actorUserId,
        string? actorDisplayName,
        decimal? previousAmount,
        decimal? newAmount,
        DateOnly? previousExpiryDate,
        DateOnly? newExpiryDate,
        GuaranteeStatus? previousStatus,
        GuaranteeStatus? newStatus,
        string? operationsScenarioTitleResourceKey,
        string? operationsLaneResourceKey,
        string? operationsMatchConfidenceResourceKey,
        int? operationsMatchScore,
        string? operationsPolicyResourceKey)
    {
        return new GuaranteeEvent(
            guaranteeId,
            requestId,
            correspondenceId,
            null,
            eventType,
            occurredAtUtc,
            summary,
            actorUserId,
            actorDisplayName,
            previousAmount,
            newAmount,
            previousExpiryDate,
            newExpiryDate,
            previousStatus,
            newStatus,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            operationsScenarioTitleResourceKey,
            operationsLaneResourceKey,
            operationsMatchConfidenceResourceKey,
            operationsMatchScore,
            operationsPolicyResourceKey);
    }

    private static string GetDispatchPrintStageResourceKey(int printCount)
    {
        return printCount <= 1
            ? "DispatchLedgerStep_Printed"
            : "DispatchLedgerStep_Reprinted";
    }

    private static string GetDispatchPrintModeResourceKey(GuaranteeOutgoingLetterPrintMode printMode)
    {
        return printMode switch
        {
            GuaranteeOutgoingLetterPrintMode.WorkstationPrinter => "DispatchPrintMode_WorkstationPrinter",
            GuaranteeOutgoingLetterPrintMode.CentralPrintRoom => "DispatchPrintMode_CentralPrintRoom",
            GuaranteeOutgoingLetterPrintMode.PdfExport => "DispatchPrintMode_PdfExport",
            _ => "DispatchPrintMode_WorkstationPrinter"
        };
    }

    private static string? GetDispatchChannelResourceKey(GuaranteeDispatchChannel? dispatchChannel)
    {
        return dispatchChannel switch
        {
            GuaranteeDispatchChannel.HandDelivery => "DispatchChannel_HandDelivery",
            GuaranteeDispatchChannel.Courier => "DispatchChannel_Courier",
            GuaranteeDispatchChannel.OfficialEmail => "DispatchChannel_OfficialEmail",
            GuaranteeDispatchChannel.InternalMail => "DispatchChannel_InternalMail",
            _ => null
        };
    }

    internal void LinkToRequest(GuaranteeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.GuaranteeId != GuaranteeId)
        {
            throw new InvalidOperationException("The request does not belong to the guarantee.");
        }

        if (GuaranteeRequestId.HasValue && GuaranteeRequestId.Value != request.Id)
        {
            throw new InvalidOperationException("The ledger entry is already linked to another request.");
        }

        GuaranteeRequestId = request.Id;
        GuaranteeRequest = request;
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
}
