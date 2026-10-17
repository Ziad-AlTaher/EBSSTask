namespace EBSSTask.Application.Reports.Models;

public enum ReportColumnKind
{
    Text,
    Integer,
    Decimal,
    DateTime
}

/// <summary>
/// Format-agnostic column metadata. Writers use <see cref="Kind"/> for number/date formatting
/// and <see cref="RelativeWidth"/> for PDF layout.
/// </summary>
public sealed record ReportColumn(string Header, ReportColumnKind Kind = ReportColumnKind.Text, float RelativeWidth = 1f);

/// <summary>A unit of work pushed through the background queue.</summary>
public sealed record ReportGenerationJob(Guid ProcessId);

/// <summary>Who should receive the real-time notification.</summary>
public sealed record NotificationTarget(string? UserId, string? ConnectionId);

public sealed record FileReadyNotification(Guid ProcessId, string FileName, string DownloadUrl, DateTime CompletedAt);

public sealed record FileFailedNotification(Guid ProcessId, string ErrorMessage, DateTime FailedAt);

/// <summary>
/// Summary and table totals for the report (computed via Stored Procedure for BalanceHistory).
/// </summary>
public sealed record ReportSummaryData
{
    public decimal FirstPreviousBalance { get; init; }
    public decimal TotalDebits { get; init; }
    public decimal TotalCredits { get; init; }
    public decimal FinalBalance { get; init; }
    public decimal TotalPreviousBalances { get; init; }
    public decimal TotalFinalBalances { get; init; }
    public int TotalRecords { get; init; }
    public string? AccountType { get; init; }
}
