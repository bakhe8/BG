namespace BG.Infrastructure.Email;

public interface ISmtpClientAdapterFactory
{
    ISmtpClientAdapter Create();
}
