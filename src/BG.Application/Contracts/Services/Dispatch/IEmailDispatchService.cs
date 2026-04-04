using BG.Application.Common;
using BG.Application.Models.Dispatch;

namespace BG.Application.Contracts.Services;

public interface IEmailDispatchService
{
    Task<OperationResult<EmailDispatchReceiptDto>> SendDispatchEmailAsync(
        string toAddress,
        string guaranteeNumber,
        string referenceNumber,
        byte[]? pdfAttachment,
        string? attachmentFileName,
        CancellationToken cancellationToken = default);
}
