using BG.Infrastructure.Email;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BG.UnitTests.Infrastructure;

public sealed class MailKitEmailDispatchServiceTests
{
    [Fact]
    public async Task SendDispatchEmailAsync_returns_failure_when_smtp_is_not_configured()
    {
        var service = CreateService(new SmtpOptions(), new FakeSmtpClientAdapterFactory(new FakeSmtpClientAdapter()));

        var result = await service.SendDispatchEmailAsync(
            "bank@example.com",
            "BG-2026-1001",
            "LTR-1001",
            null,
            null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("dispatch.email_send_failed", result.ErrorCode);
    }

    [Fact]
    public async Task SendDispatchEmailAsync_returns_failure_when_smtp_send_throws()
    {
        var adapter = new FakeSmtpClientAdapter { ThrowOnSend = true };
        var service = CreateService(CreateConfiguredOptions(), new FakeSmtpClientAdapterFactory(adapter));

        var result = await service.SendDispatchEmailAsync(
            "bank@example.com",
            "BG-2026-1001",
            "LTR-1001",
            "PDF"u8.ToArray(),
            "dispatch-letter.pdf",
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("dispatch.email_send_failed", result.ErrorCode);
    }

    [Fact]
    public async Task SendDispatchEmailAsync_returns_success_and_message_id_when_send_succeeds()
    {
        var adapter = new FakeSmtpClientAdapter { SendResponse = "message-123" };
        var service = CreateService(CreateConfiguredOptions(), new FakeSmtpClientAdapterFactory(adapter));

        var result = await service.SendDispatchEmailAsync(
            "bank@example.com",
            "BG-2026-1001",
            "LTR-1001",
            "PDF"u8.ToArray(),
            "dispatch-letter.pdf",
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("message-123", result.Value!.MessageId);
        Assert.True(adapter.ConnectCalled);
        Assert.True(adapter.SendCalled);
        Assert.True(adapter.DisconnectCalled);
        Assert.NotNull(adapter.LastMessage);
        Assert.Equal("Bank guarantee dispatch BG-2026-1001 (LTR-1001)", adapter.LastMessage!.Subject);
    }

    private static MailKitEmailDispatchService CreateService(SmtpOptions smtpOptions, ISmtpClientAdapterFactory factory)
    {
        return new MailKitEmailDispatchService(Options.Create(smtpOptions), factory);
    }

    private static SmtpOptions CreateConfiguredOptions()
    {
        return new SmtpOptions
        {
            Host = "smtp.example.com",
            Port = 587,
            FromAddress = "no-reply@example.com",
            EnableSsl = true
        };
    }

    private sealed class FakeSmtpClientAdapterFactory : ISmtpClientAdapterFactory
    {
        private readonly ISmtpClientAdapter _adapter;

        public FakeSmtpClientAdapterFactory(ISmtpClientAdapter adapter)
        {
            _adapter = adapter;
        }

        public ISmtpClientAdapter Create()
        {
            return _adapter;
        }
    }

    private sealed class FakeSmtpClientAdapter : ISmtpClientAdapter
    {
        public bool ThrowOnSend { get; set; }

        public string SendResponse { get; set; } = string.Empty;

        public bool ConnectCalled { get; private set; }

        public bool SendCalled { get; private set; }

        public bool DisconnectCalled { get; private set; }

        public MimeMessage? LastMessage { get; private set; }

        public Task ConnectAsync(string host, int port, SecureSocketOptions socketOptions, CancellationToken cancellationToken = default)
        {
            ConnectCalled = true;
            return Task.CompletedTask;
        }

        public Task AuthenticateAsync(string userName, string? password, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<string> SendAsync(MimeMessage message, CancellationToken cancellationToken = default)
        {
            SendCalled = true;
            LastMessage = message;

            if (ThrowOnSend)
            {
                throw new InvalidOperationException("Simulated SMTP failure");
            }

            return Task.FromResult(SendResponse);
        }

        public Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default)
        {
            DisconnectCalled = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
