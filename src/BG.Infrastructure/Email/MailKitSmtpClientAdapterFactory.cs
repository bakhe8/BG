namespace BG.Infrastructure.Email;

internal sealed class MailKitSmtpClientAdapterFactory : ISmtpClientAdapterFactory
{
    public ISmtpClientAdapter Create()
    {
        return new MailKitSmtpClientAdapter();
    }
}
