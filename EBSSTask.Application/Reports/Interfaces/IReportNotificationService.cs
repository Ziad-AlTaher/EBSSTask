using EBSSTask.Application.Reports.Models;

namespace EBSSTask.Application.Reports.Interfaces;

public interface IReportNotificationService
{
    Task NotifyFileReadyAsync(NotificationTarget target, FileReadyNotification notification, CancellationToken cancellationToken = default);
    Task NotifyFileFailedAsync(NotificationTarget target, FileFailedNotification notification, CancellationToken cancellationToken = default);
}
