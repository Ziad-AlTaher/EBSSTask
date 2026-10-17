using EBSSTask.API.Hubs;
using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Application.Reports.Models;
using Microsoft.AspNetCore.SignalR;

namespace EBSSTask.API.Services;

public sealed class SignalRReportNotificationService : IReportNotificationService
{
    private readonly IHubContext<ReportHub> _hubContext;
    private readonly ILogger<SignalRReportNotificationService> _logger;

    public SignalRReportNotificationService(
        IHubContext<ReportHub> hubContext,
        ILogger<SignalRReportNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyFileReadyAsync(
        NotificationTarget target,
        FileReadyNotification notification,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Emitting OnFileReady for Process {ProcessId}", notification.ProcessId);

        // 1. Notify specific connection if provided
        if (!string.IsNullOrWhiteSpace(target.ConnectionId))
        {
            await _hubContext.Clients.Client(target.ConnectionId)
                .SendAsync("OnFileReady", notification, cancellationToken);
        }

        // 2. Notify specific user if provided
        if (!string.IsNullOrWhiteSpace(target.UserId))
        {
            await _hubContext.Clients.User(target.UserId)
                .SendAsync("OnFileReady", notification, cancellationToken);
        }

        // 3. Notify any clients subscribed to this process group
        await _hubContext.Clients.Group(notification.ProcessId.ToString())
            .SendAsync("OnFileReady", notification, cancellationToken);
    }

    public async Task NotifyFileFailedAsync(
        NotificationTarget target,
        FileFailedNotification notification,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Emitting OnFileFailed for Process {ProcessId}: {Error}", notification.ProcessId, notification.ErrorMessage);

        if (!string.IsNullOrWhiteSpace(target.ConnectionId))
        {
            await _hubContext.Clients.Client(target.ConnectionId)
                .SendAsync("OnFileFailed", notification, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(target.UserId))
        {
            await _hubContext.Clients.User(target.UserId)
                .SendAsync("OnFileFailed", notification, cancellationToken);
        }

        await _hubContext.Clients.Group(notification.ProcessId.ToString())
            .SendAsync("OnFileFailed", notification, cancellationToken);
    }
}
