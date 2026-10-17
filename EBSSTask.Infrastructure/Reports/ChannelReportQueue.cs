using System.Threading.Channels;
using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Application.Reports.Models;

namespace EBSSTask.Infrastructure.Reports;

public sealed class ChannelReportQueue : IReportQueue
{
    private readonly Channel<ReportGenerationJob> _channel;

    public ChannelReportQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<ReportGenerationJob>(options);
    }

    public async ValueTask EnqueueAsync(ReportGenerationJob job, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public IAsyncEnumerable<ReportGenerationJob> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
