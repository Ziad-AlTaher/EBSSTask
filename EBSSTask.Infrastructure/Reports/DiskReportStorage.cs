using EBSSTask.Application.Common.Options;
using EBSSTask.Application.Reports.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EBSSTask.Infrastructure.Reports;

public sealed class DiskReportStorage : IReportStorage
{
    private readonly string _baseDirectory;

    public DiskReportStorage(
        IOptions<ReportGenerationOptions> options,
        IHostEnvironment environment)
    {
        var configuredPath = options.Value.OutputFolder;

        _baseDirectory = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);

        if (!Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
        }
    }

    public Task<Stream> OpenWriteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveAbsolutePath(relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        return Task.FromResult<Stream>(stream);
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveAbsolutePath(relativePath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public bool Exists(string relativePath)
    {
        var fullPath = ResolveAbsolutePath(relativePath);
        return File.Exists(fullPath);
    }

    public string ResolveAbsolutePath(string relativePath)
    {
        return Path.Combine(_baseDirectory, relativePath.TrimStart('/', '\\'));
    }
}
