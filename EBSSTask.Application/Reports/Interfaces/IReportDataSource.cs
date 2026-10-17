using EBSSTask.Application.Reports.Models;
using EBSSTask.Domain.Enums;

namespace EBSSTask.Application.Reports.Interfaces;

public interface IReportDataSource
{
    ReportType Type { get; } // The type of report this data source provides to be able to generate more than a single type and register different data sources for each type.
    string Title { get; }
    IReadOnlyList<ReportColumn> Columns { get; }

    Task<int> GetTotalCountAsync(ReportFilter filter, CancellationToken cancellationToken);
    IAsyncEnumerable<IReadOnlyList<object?>> FetchRowsAsync(ReportFilter filter, int chunkSize, CancellationToken cancellationToken);
    Task<ReportSummaryData?> GetSummaryAsync(ReportFilter filter, CancellationToken cancellationToken);
}
