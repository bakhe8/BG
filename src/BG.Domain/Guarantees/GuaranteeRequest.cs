using BG.Domain.Identity;
using BG.Domain.Workflow;

namespace BG.Domain.Guarantees;

public sealed class GuaranteeRequest
{
    public GuaranteeRequest(
        Guid guaranteeId,
        Guid requestedByUserId,
        GuaranteeRequestType requestType,
        decimal? requestedAmount,
        DateOnly? requestedExpiryDate,
        string? notes,
        DateTimeOffset createdAtUtc,
        GuaranteeRequestChannel requestChannel = GuaranteeRequestChannel.RequestWorkspace)
    {
        Id = Guid.NewGuid();
        GuaranteeId = guaranteeId;
        RequestedByUserId = requestedByUserId;
        RequestType = requestType;
        RequestedAmount = requestedAmount;
        RequestedExpiryDate = requestedExpiryDate;
        Notes = NormalizeOptional(notes, 1000);
        CreatedAtUtc = createdAtUtc;
        RequestChannel = requestChannel;
        Status = GuaranteeRequestStatus.Draft;
    }

    private GuaranteeRequest()
    {
    }

    public Guid Id { get; private set; }

    public Guid GuaranteeId { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    public GuaranteeRequestType RequestType { get; private set; }

    public GuaranteeRequestStatus Status { get; private set; }

    public decimal? RequestedAmount { get; private set; }

    public DateOnly? RequestedExpiryDate { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public GuaranteeRequestChannel RequestChannel { get; private set; }

    public DateTimeOffset? SubmittedToBankAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public Guid? CompletionCorrespondenceId { get; private set; }

    public Guarantee Guarantee { get; internal set; } = default!;

    public User RequestedByUser { get; internal set; } = default!;

    public RequestApprovalProcess? ApprovalProcess { get; internal set; }

    public ICollection<GuaranteeCorrespondence> Correspondence { get; private set; } = new List<GuaranteeCorrespondence>();

    public ICollection<GuaranteeRequestDocumentLink> RequestDocuments { get; private set; } = new List<GuaranteeRequestDocumentLink>();

    public bool IsOwnedBy(Guid userId)
    {
        return RequestedByUserId == userId;
    }

    internal void MarkSubmittedToBank(DateTimeOffset submittedAtUtc)
    {
        if (Status == GuaranteeRequestStatus.Completed)
        {
            throw new InvalidOperationException("A completed request cannot be re-submitted.");
        }

        if (Status is not GuaranteeRequestStatus.ApprovedForDispatch and not GuaranteeRequestStatus.AwaitingBankResponse)
        {
            throw new InvalidOperationException("Only approved requests can be submitted to the bank.");
        }

        SubmittedToBankAtUtc ??= submittedAtUtc;
        Status = GuaranteeRequestStatus.SubmittedToBank;
    }

    internal void ReopenForDispatch()
    {
        if (Status != GuaranteeRequestStatus.AwaitingBankResponse)
        {
            throw new InvalidOperationException("Only requests waiting for bank response can be reopened for dispatch.");
        }

        if (CompletedAtUtc.HasValue)
        {
            throw new InvalidOperationException("A completed request cannot be reopened for dispatch.");
        }

        SubmittedToBankAtUtc = null;
        Status = GuaranteeRequestStatus.ApprovedForDispatch;
    }

    internal void SubmitForApproval(RequestApprovalProcess approvalProcess)
    {
        ArgumentNullException.ThrowIfNull(approvalProcess);

        if (Status is not GuaranteeRequestStatus.Draft and not GuaranteeRequestStatus.Returned)
        {
            throw new InvalidOperationException("Only draft or returned requests can enter approval.");
        }

        if (approvalProcess.GuaranteeRequestId != Id)
        {
            throw new InvalidOperationException("Approval process does not belong to this request.");
        }

        ApprovalProcess = approvalProcess;
        Status = GuaranteeRequestStatus.InApproval;
    }

    internal void Revise(decimal? requestedAmount, DateOnly? requestedExpiryDate, string? notes)
    {
        if (Status is not GuaranteeRequestStatus.Draft and not GuaranteeRequestStatus.Returned)
        {
            throw new InvalidOperationException("Only draft or returned requests can be revised.");
        }

        RequestedAmount = requestedAmount;
        RequestedExpiryDate = requestedExpiryDate;
        Notes = NormalizeOptional(notes, 1000);
    }

    internal void Cancel()
    {
        if (Status is not GuaranteeRequestStatus.Draft and not GuaranteeRequestStatus.Returned)
        {
            throw new InvalidOperationException("Only draft or returned requests can be cancelled.");
        }

        Status = GuaranteeRequestStatus.Cancelled;
    }

    internal void WithdrawFromApproval()
    {
        if (Status != GuaranteeRequestStatus.InApproval)
        {
            throw new InvalidOperationException("Only in-approval requests can be withdrawn.");
        }

        Status = GuaranteeRequestStatus.Cancelled;
    }

    internal void MarkReturnedFromApproval()
    {
        if (Status != GuaranteeRequestStatus.InApproval)
        {
            throw new InvalidOperationException("Only in-approval requests can be returned.");
        }

        Status = GuaranteeRequestStatus.Returned;
    }

    internal void MarkRejectedByApproval()
    {
        if (Status != GuaranteeRequestStatus.InApproval)
        {
            throw new InvalidOperationException("Only in-approval requests can be rejected.");
        }

        Status = GuaranteeRequestStatus.Rejected;
    }

    internal void MarkApprovedForDispatch()
    {
        if (Status != GuaranteeRequestStatus.InApproval)
        {
            throw new InvalidOperationException("Only in-approval requests can be approved for dispatch.");
        }

        Status = GuaranteeRequestStatus.ApprovedForDispatch;
    }

    internal void MarkCompleted(Guid correspondenceId, DateTimeOffset completedAtUtc)
    {
        if (Status == GuaranteeRequestStatus.Completed)
        {
            throw new InvalidOperationException("The request is already completed.");
        }

        CompletionCorrespondenceId = correspondenceId;
        CompletedAtUtc = completedAtUtc;
        Status = GuaranteeRequestStatus.Completed;
    }

    internal void ReopenAppliedBankConfirmation(Guid correspondenceId)
    {
        if (Status != GuaranteeRequestStatus.Completed)
        {
            throw new InvalidOperationException("Only completed requests can reopen an applied bank confirmation.");
        }

        if (!CompletionCorrespondenceId.HasValue || CompletionCorrespondenceId.Value != correspondenceId)
        {
            throw new InvalidOperationException("The applied correspondence does not match the request completion record.");
        }

        CompletionCorrespondenceId = null;
        CompletedAtUtc = null;
        Status = GuaranteeRequestStatus.AwaitingBankResponse;
    }

    internal void AttachCorrespondence(GuaranteeCorrespondence correspondence)
    {
        if (correspondence.GuaranteeRequestId != Id)
        {
            throw new InvalidOperationException("Correspondence does not belong to this request.");
        }

        Correspondence.Add(correspondence);
    }

    internal void AttachDocument(GuaranteeRequestDocumentLink requestDocument)
    {
        ArgumentNullException.ThrowIfNull(requestDocument);

        if (requestDocument.GuaranteeRequestId != Id)
        {
            throw new InvalidOperationException("Document link does not belong to this request.");
        }

        if (RequestDocuments.Any(existing => existing.GuaranteeDocumentId == requestDocument.GuaranteeDocumentId))
        {
            return;
        }

        RequestDocuments.Add(requestDocument);
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
