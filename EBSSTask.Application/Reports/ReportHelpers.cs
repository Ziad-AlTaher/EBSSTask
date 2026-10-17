using EBSSTask.Domain.Enums;

namespace EBSSTask.Application.Reports;

/// <summary>
/// An expected, user-facing failure (e.g. a limit was exceeded). Its message is safe to show to clients.
/// </summary>
public sealed class ReportGenerationException(string message) : Exception(message);

public static class ReportFormatExtensions
{
    public static string GetFileExtension(this ReportFormat format) => format switch
    {
        ReportFormat.Excel => ".xlsx",
        ReportFormat.Pdf => ".pdf",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    public static string GetContentType(this ReportFormat format) => format switch
    {
        ReportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ReportFormat.Pdf => "application/pdf",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };
}

public static class ReportRoutes
{
    public static string Status(Guid processId) => $"/api/files/{processId}";

    public static string Download(Guid processId) => $"/api/files/{processId}/download";
}
