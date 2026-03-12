using BG.Domain.Guarantees;

namespace BG.Application.ReferenceData;

public static class GuaranteeResourceCatalog
{
    private static readonly IReadOnlyList<GuaranteeCategory> GuaranteeCategories =
    [
        GuaranteeCategory.Contract,
        GuaranteeCategory.PurchaseOrder
    ];

    private static readonly IReadOnlyList<GuaranteeRequestType> RequestTypes =
    [
        GuaranteeRequestType.Extend,
        GuaranteeRequestType.Reduce,
        GuaranteeRequestType.Release,
        GuaranteeRequestType.ReplaceWithReducedGuarantee,
        GuaranteeRequestType.VerifyStatus
    ];

    private static readonly IReadOnlyList<GuaranteeDocumentCaptureChannel> CaptureChannels =
    [
        GuaranteeDocumentCaptureChannel.ManualUpload,
        GuaranteeDocumentCaptureChannel.ScanStation,
        GuaranteeDocumentCaptureChannel.OracleImport
    ];

    private static readonly IReadOnlyList<GuaranteeOutgoingLetterPrintMode> DispatchPrintModes =
    [
        GuaranteeOutgoingLetterPrintMode.WorkstationPrinter,
        GuaranteeOutgoingLetterPrintMode.CentralPrintRoom,
        GuaranteeOutgoingLetterPrintMode.PdfExport
    ];

    private static readonly IReadOnlyList<GuaranteeDispatchChannel> DispatchChannels =
    [
        GuaranteeDispatchChannel.HandDelivery,
        GuaranteeDispatchChannel.Courier,
        GuaranteeDispatchChannel.OfficialEmail,
        GuaranteeDispatchChannel.InternalMail
    ];

    public static IReadOnlyList<GuaranteeCategory> GetSupportedGuaranteeCategories()
    {
        return GuaranteeCategories;
    }

    public static IReadOnlyList<GuaranteeRequestType> GetSupportedRequestTypes()
    {
        return RequestTypes;
    }

    public static IReadOnlyList<GuaranteeDocumentCaptureChannel> GetSupportedCaptureChannels()
    {
        return CaptureChannels;
    }

    public static IReadOnlyList<GuaranteeOutgoingLetterPrintMode> GetSupportedDispatchPrintModes()
    {
        return DispatchPrintModes;
    }

    public static IReadOnlyList<GuaranteeDispatchChannel> GetSupportedDispatchChannels()
    {
        return DispatchChannels;
    }

    public static bool RequiresRequestedAmount(GuaranteeRequestType requestType)
    {
        return requestType is GuaranteeRequestType.Reduce or GuaranteeRequestType.ReplaceWithReducedGuarantee;
    }

    public static bool RequiresRequestedExpiryDate(GuaranteeRequestType requestType)
    {
        return requestType == GuaranteeRequestType.Extend;
    }

    public static string GetGuaranteeCategoryResourceKey(GuaranteeCategory category)
    {
        return category switch
        {
            GuaranteeCategory.Contract => "GuaranteeCategory_Contract",
            GuaranteeCategory.PurchaseOrder => "GuaranteeCategory_PurchaseOrder",
            _ => "GuaranteeCategory_Contract"
        };
    }

    public static string GetRequestTypeResourceKey(GuaranteeRequestType requestType)
    {
        return requestType switch
        {
            GuaranteeRequestType.Extend => "RequestType_Extend",
            GuaranteeRequestType.Reduce => "RequestType_Reduce",
            GuaranteeRequestType.Release => "RequestType_Release",
            GuaranteeRequestType.ReplaceWithReducedGuarantee => "RequestType_ReplaceWithReducedGuarantee",
            GuaranteeRequestType.VerifyStatus => "RequestType_VerifyStatus",
            _ => "RequestType_Extend"
        };
    }

    public static string GetRequestStatusResourceKey(GuaranteeRequestStatus status)
    {
        return status switch
        {
            GuaranteeRequestStatus.Draft => "RequestStatus_Draft",
            GuaranteeRequestStatus.InApproval => "RequestStatus_InApproval",
            GuaranteeRequestStatus.Returned => "RequestStatus_Returned",
            GuaranteeRequestStatus.ApprovedForDispatch => "RequestStatus_ApprovedForDispatch",
            GuaranteeRequestStatus.SubmittedToBank => "RequestStatus_SubmittedToBank",
            GuaranteeRequestStatus.AwaitingBankResponse => "RequestStatus_AwaitingBankResponse",
            GuaranteeRequestStatus.Completed => "RequestStatus_Completed",
            GuaranteeRequestStatus.Rejected => "RequestStatus_Rejected",
            GuaranteeRequestStatus.Cancelled => "RequestStatus_Cancelled",
            _ => "RequestStatus_Draft"
        };
    }

    public static string GetRequestChannelResourceKey(GuaranteeRequestChannel requestChannel)
    {
        return requestChannel switch
        {
            GuaranteeRequestChannel.RequestWorkspace => "RequestChannel_RequestWorkspace",
            GuaranteeRequestChannel.OperationsDesk => "RequestChannel_OperationsDesk",
            GuaranteeRequestChannel.OracleImport => "RequestChannel_OracleImport",
            _ => "RequestChannel_RequestWorkspace"
        };
    }

    public static string GetDocumentTypeResourceKey(GuaranteeDocumentType documentType)
    {
        return documentType switch
        {
            GuaranteeDocumentType.GuaranteeInstrument => "GuaranteeDocumentType_GuaranteeInstrument",
            GuaranteeDocumentType.OutgoingLetter => "GuaranteeDocumentType_OutgoingLetter",
            GuaranteeDocumentType.BankResponse => "GuaranteeDocumentType_BankResponse",
            GuaranteeDocumentType.SupportingDocument => "GuaranteeDocumentType_SupportingDocument",
            _ => "GuaranteeDocumentType_SupportingDocument"
        };
    }

    public static string GetCaptureChannelResourceKey(GuaranteeDocumentCaptureChannel captureChannel)
    {
        return captureChannel switch
        {
            GuaranteeDocumentCaptureChannel.ManualUpload => "IntakeCaptureChannel_ManualUpload",
            GuaranteeDocumentCaptureChannel.ScanStation => "IntakeCaptureChannel_ScanStation",
            GuaranteeDocumentCaptureChannel.OracleImport => "IntakeCaptureChannel_OracleImport",
            _ => "IntakeCaptureChannel_ManualUpload"
        };
    }

    public static string GetCaptureChannelHintResourceKey(GuaranteeDocumentCaptureChannel captureChannel)
    {
        return captureChannel switch
        {
            GuaranteeDocumentCaptureChannel.ScanStation => "IntakeWorkspace_ProvenanceHint_ScanStation",
            GuaranteeDocumentCaptureChannel.OracleImport => "IntakeWorkspace_ProvenanceHint_OracleImport",
            _ => "IntakeWorkspace_ProvenanceHint_ManualUpload"
        };
    }

    public static string GetDispatchPrintModeResourceKey(GuaranteeOutgoingLetterPrintMode printMode)
    {
        return printMode switch
        {
            GuaranteeOutgoingLetterPrintMode.WorkstationPrinter => "DispatchPrintMode_WorkstationPrinter",
            GuaranteeOutgoingLetterPrintMode.CentralPrintRoom => "DispatchPrintMode_CentralPrintRoom",
            GuaranteeOutgoingLetterPrintMode.PdfExport => "DispatchPrintMode_PdfExport",
            _ => "DispatchPrintMode_WorkstationPrinter"
        };
    }

    public static string GetDispatchChannelResourceKey(GuaranteeDispatchChannel dispatchChannel)
    {
        return dispatchChannel switch
        {
            GuaranteeDispatchChannel.HandDelivery => "DispatchChannel_HandDelivery",
            GuaranteeDispatchChannel.Courier => "DispatchChannel_Courier",
            GuaranteeDispatchChannel.OfficialEmail => "DispatchChannel_OfficialEmail",
            GuaranteeDispatchChannel.InternalMail => "DispatchChannel_InternalMail",
            _ => "DispatchChannel_HandDelivery"
        };
    }
}
