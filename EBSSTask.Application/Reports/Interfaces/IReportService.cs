using EBSSTask.Application.Common;
using EBSSTask.Application.Reports.Models;
using EBSSTask.Domain.Entities;

namespace EBSSTask.Application.Reports.Interfaces;

public interface IReportService
{
    Task<ReportSubmissionResult> RequestReportAsync(GenerateReportRequest request, CancellationToken cancellationToken = default);
    Task<FileProcessStatusDto?> GetStatusAsync(Guid processId, CancellationToken cancellationToken = default);
    Task<FileProcess?> GetProcessEntityAsync(Guid processId, CancellationToken cancellationToken = default);
    Task<PagedResult<FileProcessStatusDto>> GetAllFilesAsync(FileProcessQueryParameters parameters, CancellationToken cancellationToken = default);
}
