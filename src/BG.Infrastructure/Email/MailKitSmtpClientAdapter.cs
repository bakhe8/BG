using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BG.Infrastructure.Email;

internal sealed class MailKitSmtpClientAdapter : ISmtpClientAdapter
{
    private readonly SmtpClient _client = new();

    public Task ConnectAsync(string host, int port, SecureSocketOptions socketOptions, CancellationToken cancellationToken = default)
    {
        return _client.ConnectAsync(host, port, socketOptions, cancellationToken);
    }

    public Task AuthenticateAsync(string userName, string? password, CancellationToken cancellationToken = default)
    {
        return _client.AuthenticateAsync(userName, password, cancellationToken);
    }

    public Task<string> SendAsync(MimeMessage message, CancellationToken cancellationToken = default)
    {
        return _client.SendAsync(message, cancellationToken);
    }

    public Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default)
    {
        return _client.DisconnectAsync(quit, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
