namespace Folkie.Application.Common.Interfaces;

/// <summary>
/// Domain event'lerinden bildirim oluşturmak için.
/// Handler'lar bunu kullanır; e-posta/in-app push burada birleşir.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(
        Guid userId,
        string type,
        string title,
        string? body = null,
        object? data = null,
        CancellationToken ct = default);
}
