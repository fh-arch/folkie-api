namespace Folkie.Application.Common.Interfaces;

public interface IEmailSender
{
    Task<bool> SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken ct = default);
}
