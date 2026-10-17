namespace EBSSTask.Application.Reports.Interfaces;

public interface IReportStorage
{
    Task<Stream> OpenWriteAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    bool Exists(string relativePath);
    string ResolveAbsolutePath(string relativePath);
}
