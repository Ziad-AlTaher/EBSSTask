namespace EBSSTask.Application.Reports.Interfaces;

public interface IReportProcessor
{
    Task ProcessAsync(Guid processId, CancellationToken cancellationToken);
}
