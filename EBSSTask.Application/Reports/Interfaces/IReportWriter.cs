using EBSSTask.Application.Reports.Models;
using EBSSTask.Domain.Enums;

namespace EBSSTask.Application.Reports.Interfaces;

public interface IReportWriter
{
    ReportFormat Format { get; }

    Task WriteAsync(
        IReportDataSource dataSource,
        ReportFilter filter,
        Stream outputStream,
        CancellationToken cancellationToken);
}
