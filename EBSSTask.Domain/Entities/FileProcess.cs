using EBSSTask.Domain.Enums;

namespace EBSSTask.Domain.Entities;

/// <summary>
/// Tracks a background file-generation request from submission until the file is ready (or failed).
/// State transitions are encapsulated so the entity can never reach an invalid state.
/// </summary>
public class FileProcess
{
    public const int MaxErrorMessageLength = 4000;

    // Required by EF Core.
    private FileProcess() { }

    public Guid Id { get; private set; }

    public FileProcessStatus ProcessStatus { get; private set; }

    public ReportType ReportType { get; private set; }

    public ReportFormat ReportFormat { get; private set; }

    /// <summary>Serialized report filter (JSON). Persisted so a job can be resumed after a restart.</summary>
    public string? Parameters { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? StartedAt { get; private set; }

    /// <summary>UTC time processing finished (successfully or not).</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Path of the generated file, relative to the configured reports folder.</summary>
    public string? FilePath { get; private set; }

    public string? UserId { get; private set; }

    public string? ConnectionId { get; private set; }

    public string? ErrorMessage { get; private set; }

    public bool IsFinished => ProcessStatus is FileProcessStatus.Completed or FileProcessStatus.Failed;

    public static FileProcess Create(
        ReportType reportType,
        ReportFormat reportFormat,
        string? parameters,
        string? connectionId,
        DateTime createdAtUtc)
    {
        return new FileProcess
        {
            Id = Guid.NewGuid(),
            ProcessStatus = FileProcessStatus.Pending,
            ReportType = reportType,
            ReportFormat = reportFormat,
            Parameters = parameters,
            ConnectionId = connectionId,
            CreatedAt = createdAtUtc
        };
    }

    public void MarkProcessing(DateTime utcNow)
    {
        if (IsFinished)
        {
            throw new InvalidOperationException($"File process {Id} is already {ProcessStatus}.");
        }

        ProcessStatus = FileProcessStatus.Processing;
        StartedAt = utcNow;
        ErrorMessage = null;
    }

    public void MarkCompleted(string filePath, DateTime utcNow)
    {
        if (ProcessStatus != FileProcessStatus.Processing)
        {
            throw new InvalidOperationException($"File process {Id} cannot complete from state {ProcessStatus}.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        ProcessStatus = FileProcessStatus.Completed;
        FilePath = filePath;
        CompletedAt = utcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage, DateTime utcNow)
    {
        ProcessStatus = FileProcessStatus.Failed;
        FilePath = null;
        CompletedAt = utcNow;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
            ? "Unknown error."
            : errorMessage.Length > MaxErrorMessageLength
                ? errorMessage[..MaxErrorMessageLength]
                : errorMessage;
    }

    /// <summary>
    /// Returns an interrupted job (e.g. host shutdown) to the queue-able state so it can be resumed.
    /// </summary>
    public void ResetToPending()
    {
        if (ProcessStatus != FileProcessStatus.Processing)
        {
            return;
        }

        ProcessStatus = FileProcessStatus.Pending;
        StartedAt = null;
    }
}
