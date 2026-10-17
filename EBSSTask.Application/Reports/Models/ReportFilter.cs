using System.Text.Json;

namespace EBSSTask.Application.Reports.Models;

/// <summary>
/// Optional filters applied to a report dataset. Persisted as JSON on the FileProcess record.
/// </summary>
public sealed record ReportFilter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static ReportFilter Empty { get; } = new();

    public int? BalanceId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }

    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

    public static ReportFilter FromJson(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? Empty
            : JsonSerializer.Deserialize<ReportFilter>(json, SerializerOptions) ?? Empty;
}
