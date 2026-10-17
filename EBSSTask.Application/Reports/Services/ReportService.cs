using EBSSTask.Application.Common;
using EBSSTask.Application.Common.Interfaces;
using EBSSTask.Application.Reports;
using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Application.Reports.Models;
using EBSSTask.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EBSSTask.Application.Reports.Services;

public sealed class ReportService : IReportService
{
    private readonly IApplicationDbContext _context;
    private readonly IReportQueue _reportQueue;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IApplicationDbContext context,
        IReportQueue reportQueue,
        ILogger<ReportService> logger)
    {
        _context = context;
        _reportQueue = reportQueue;
        _logger = logger;
    }

    public async Task<ReportSubmissionResult> RequestReportAsync(
        GenerateReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var filterJson = request.Filter?.ToJson();

        var process = FileProcess.Create(
            request.ReportType,
            request.ReportFormat,
            filterJson,
            request.ConnectionId,
            DateTime.UtcNow);

        await _context.FileProcesses.AddAsync(process, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created FileProcess {ProcessId} for {ReportType} ({ReportFormat})",
            process.Id,
            process.ReportType,
            process.ReportFormat);

        // Queue for background execution
        await _reportQueue.EnqueueAsync(new ReportGenerationJob(process.Id), cancellationToken);

        return new ReportSubmissionResult(
            process.Id,
            process.ProcessStatus,
            process.CreatedAt,
            ReportRoutes.Status(process.Id),
            ReportRoutes.Download(process.Id));
    }

    public async Task<FileProcessStatusDto?> GetStatusAsync(
        Guid processId,
        CancellationToken cancellationToken = default)
    {
        var process = await _context.FileProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == processId, cancellationToken);

        if (process == null)
        {
            return null;
        }

        var downloadUrl = process.ProcessStatus == Domain.Enums.FileProcessStatus.Completed
            ? ReportRoutes.Download(process.Id)
            : null;

        return new FileProcessStatusDto(
            process.Id,
            process.ProcessStatus,
            process.ReportType,
            process.ReportFormat,
            process.CreatedAt,
            process.StartedAt,
            process.CompletedAt,
            process.ErrorMessage,
            downloadUrl);
    }

    public async Task<FileProcess?> GetProcessEntityAsync(
        Guid processId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FileProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == processId, cancellationToken);
    }

    public async Task<PagedResult<FileProcessStatusDto>> GetAllFilesAsync(
        FileProcessQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = parameters.PageNumber < 1 ? 1 : parameters.PageNumber;
        var pageSize = parameters.PageSize is < 1 or > 100 ? 20 : parameters.PageSize;

        var query = _context.FileProcesses.AsNoTracking();

        if (parameters.Status.HasValue)
        {
            query = query.Where(p => p.ProcessStatus == parameters.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(p => new FileProcessStatusDto(
            p.Id,
            p.ProcessStatus,
            p.ReportType,
            p.ReportFormat,
            p.CreatedAt,
            p.StartedAt,
            p.CompletedAt,
            p.ErrorMessage,
            p.ProcessStatus == Domain.Enums.FileProcessStatus.Completed ? ReportRoutes.Download(p.Id) : null));

        return new PagedResult<FileProcessStatusDto>(items, totalCount, pageNumber, pageSize);
    }
}
