using EBSSTask.Application.Reports.Models;

namespace EBSSTask.Application.Reports.Interfaces;

public interface IReportQueue
{
    ValueTask EnqueueAsync(ReportGenerationJob job, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ReportGenerationJob> ReadAllAsync(CancellationToken cancellationToken = default);
}
