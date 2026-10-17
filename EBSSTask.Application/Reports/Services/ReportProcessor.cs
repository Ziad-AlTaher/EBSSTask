using EBSSTask.Application.Common.Interfaces;
using EBSSTask.Application.Reports;
using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Application.Reports.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EBSSTask.Application.Reports.Services;

public sealed class ReportProcessor : IReportProcessor
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IReportDataSource> _dataSources;
    private readonly IEnumerable<IReportWriter> _writers;
    private readonly IReportStorage _storage;
    private readonly IReportNotificationService _notificationService;
    private readonly ILogger<ReportProcessor> _logger;

    public ReportProcessor(
        IApplicationDbContext context,
        IEnumerable<IReportDataSource> dataSources,
        IEnumerable<IReportWriter> writers,
        IReportStorage storage,
        IReportNotificationService notificationService,
        ILogger<ReportProcessor> logger)
    {
        _context = context;
        _dataSources = dataSources;
        _writers = writers;
        _storage = storage;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid processId, CancellationToken cancellationToken)
    {
        var process = await _context.FileProcesses
            .FirstOrDefaultAsync(p => p.Id == processId, cancellationToken);

        if (process == null)
        {
            _logger.LogWarning("FileProcess {ProcessId} was not found in the database.", processId);
            return;
        }

        if (process.IsFinished)
        {
            _logger.LogInformation("FileProcess {ProcessId} is already in state {Status}. Skipping.", processId, process.ProcessStatus);
            return;
        }

        var notificationTarget = new NotificationTarget(process.UserId, process.ConnectionId);

        try
        {
            process.MarkProcessing(DateTime.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);

            var dataSource = _dataSources.FirstOrDefault(s => s.Type == process.ReportType)
                ?? throw new InvalidOperationException($"No data source registered for report type {process.ReportType}.");

            var writer = _writers.FirstOrDefault(w => w.Format == process.ReportFormat)
                ?? throw new InvalidOperationException($"No writer registered for report format {process.ReportFormat}.");

            var filter = ReportFilter.FromJson(process.Parameters);

            var extension = process.ReportFormat.GetFileExtension();
            var fileName = $"{process.ReportType}_{process.Id:N}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}";

            _logger.LogInformation("Generating {ReportType} ({Format}) for Process {ProcessId} into {FileName}",
                process.ReportType, process.ReportFormat, process.Id, fileName);

            await using (var stream = await _storage.OpenWriteAsync(fileName, cancellationToken))
            {
                await writer.WriteAsync(dataSource, filter, stream, cancellationToken);
            }

            process.MarkCompleted(fileName, DateTime.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully completed FileProcess {ProcessId}", process.Id);

            var downloadUrl = ReportRoutes.Download(process.Id);
            await _notificationService.NotifyFileReadyAsync(
                notificationTarget,
                new FileReadyNotification(process.Id, fileName, downloadUrl, process.CompletedAt!.Value),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("FileProcess {ProcessId} execution was cancelled.", processId);
            process.ResetToPending();
            await _context.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate report for FileProcess {ProcessId}: {Message}", processId, ex.Message);

            process.MarkFailed(ex.Message, DateTime.UtcNow);
            try
            {
                await _context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to persist Failed status for FileProcess {ProcessId}", processId);
            }

            await _notificationService.NotifyFileFailedAsync(
                notificationTarget,
                new FileFailedNotification(process.Id, ex.Message, process.CompletedAt!.Value),
                CancellationToken.None);
        }
    }
}
