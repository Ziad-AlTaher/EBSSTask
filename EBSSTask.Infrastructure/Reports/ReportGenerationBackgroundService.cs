using EBSSTask.Application.Common.Interfaces;
using EBSSTask.Application.Common.Options;
using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Application.Reports.Models;
using EBSSTask.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EBSSTask.Infrastructure.Reports;

public sealed class ReportGenerationBackgroundService : BackgroundService
{
    private readonly IReportQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReportGenerationOptions _options;
    private readonly ILogger<ReportGenerationBackgroundService> _logger;

    public ReportGenerationBackgroundService(
        IReportQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<ReportGenerationOptions> options,
        ILogger<ReportGenerationBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ReportGenerationBackgroundService started. Max concurrent jobs: {MaxJobs}",
            _options.MaxConcurrentJobs);

        // 1. Crash recovery on startup: re-enqueue pending or interrupted jobs
        await RecoverOrphanedJobsAsync(stoppingToken);

        // 2. Consume jobs with bounded concurrency
        var semaphore = new SemaphoreSlim(_options.MaxConcurrentJobs, _options.MaxConcurrentJobs);

        await foreach (var job in _queue.ReadAllAsync(stoppingToken))
        {
            await semaphore.WaitAsync(stoppingToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(ReportGenerationJob job, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IReportProcessor>();

        try
        {
            _logger.LogInformation("Background processing started for Job {ProcessId}", job.ProcessId);
            await processor.ProcessAsync(job.ProcessId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Background processing cancelled for Job {ProcessId} due to host shutdown.", job.ProcessId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in background worker for Job {ProcessId}", job.ProcessId);
        }
    }

    private async Task RecoverOrphanedJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var orphanedJobs = await context.FileProcesses
                .Where(p => p.ProcessStatus == FileProcessStatus.Pending || p.ProcessStatus == FileProcessStatus.Processing)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync(stoppingToken);

            if (orphanedJobs.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Found {Count} unfinished report jobs on startup. Re-enqueuing...", orphanedJobs.Count);

            foreach (var process in orphanedJobs)
            {
                process.ResetToPending();
                await _queue.EnqueueAsync(new ReportGenerationJob(process.Id), stoppingToken);
            }

            await context.SaveChangesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while recovering orphaned jobs on startup.");
        }
    }
}
