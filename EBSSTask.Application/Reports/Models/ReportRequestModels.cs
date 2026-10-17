using EBSSTask.Domain.Enums;

namespace EBSSTask.Application.Reports.Models;

public sealed record GenerateReportRequest(
    ReportType ReportType,
    ReportFormat ReportFormat,
    ReportFilter? Filter = null,
    string? ConnectionId = null);

public sealed record ReportSubmissionResult(
    Guid ProcessId,
    FileProcessStatus Status,
    DateTime CreatedAt,
    string StatusUrl,
    string DownloadUrl);

public sealed record FileProcessStatusDto(
    Guid Id,
    FileProcessStatus ProcessStatus,
    ReportType ReportType,
    ReportFormat ReportFormat,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    string? DownloadUrl);

public class FileProcessQueryParameters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public FileProcessStatus? Status { get; set; }
}
