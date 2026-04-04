using MailKit.Security;
using MimeKit;

namespace BG.Infrastructure.Email;

public interface ISmtpClientAdapter : IAsyncDisposable
{
    Task ConnectAsync(string host, int port, SecureSocketOptions socketOptions, CancellationToken cancellationToken = default);

    Task AuthenticateAsync(string userName, string? password, CancellationToken cancellationToken = default);

    Task<string> SendAsync(MimeMessage message, CancellationToken cancellationToken = default);

    Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default);
}
