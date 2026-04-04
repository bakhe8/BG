using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Dispatch;
using BG.Application.Models.Dispatch;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BG.Infrastructure.Email;

internal sealed class MailKitEmailDispatchService : IEmailDispatchService
{
    private readonly SmtpOptions _options;
    private readonly ISmtpClientAdapterFactory _smtpClientFactory;

    public MailKitEmailDispatchService(
        IOptions<SmtpOptions> options,
        ISmtpClientAdapterFactory smtpClientFactory)
    {
        _options = options.Value;
        _smtpClientFactory = smtpClientFactory;
    }

    public async Task<OperationResult<EmailDispatchReceiptDto>> SendDispatchEmailAsync(
        string toAddress,
        string guaranteeNumber,
        string referenceNumber,
        byte[]? pdfAttachment,
        string? attachmentFileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            return OperationResult<EmailDispatchReceiptDto>.Failure("dispatch.email_send_failed");
        }

        try
        {
            var message = BuildMessage(toAddress, guaranteeNumber, referenceNumber, pdfAttachment, attachmentFileName);
            var socketOptions = _options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            await using var client = _smtpClientFactory.Create();
            await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            var response = await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            var messageId = string.IsNullOrWhiteSpace(response) ? Guid.NewGuid().ToString("N") : response;
            return OperationResult<EmailDispatchReceiptDto>.Success(new EmailDispatchReceiptDto(messageId, DateTimeOffset.UtcNow));
        }
        catch
        {
            return OperationResult<EmailDispatchReceiptDto>.Failure("dispatch.email_send_failed");
        }
    }

    private MimeMessage BuildMessage(
        string toAddress,
        string guaranteeNumber,
        string referenceNumber,
        byte[]? pdfAttachment,
        string? attachmentFileName)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = $"Bank guarantee dispatch {guaranteeNumber} ({referenceNumber})";

        var bodyBuilder = new BodyBuilder
        {
            TextBody =
                "Dear bank team," + Environment.NewLine +
                Environment.NewLine +
                $"Please find the dispatch letter for guarantee {guaranteeNumber} with reference {referenceNumber}." + Environment.NewLine +
                Environment.NewLine +
                "Regards," + Environment.NewLine +
                "BG"
        };

        if (pdfAttachment is { Length: > 0 })
        {
            bodyBuilder.Attachments.Add(attachmentFileName ?? "dispatch-letter.pdf", pdfAttachment, ContentType.Parse("application/pdf"));
        }

        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }
}
