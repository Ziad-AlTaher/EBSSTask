namespace EBSSTask.Application.Common.Options;

/// <summary>
/// Bound from the "ReportGeneration" section of appsettings.json.
/// </summary>
public sealed class ReportGenerationOptions
{
    public const string SectionName = "ReportGeneration";

    /// <summary>Output folder. Relative paths are resolved against the content root (e.g. "wwwroot/reports").</summary>
    public string OutputFolder { get; set; } = "wwwroot/reports";

    /// <summary>Number of rows fetched from the database per round-trip.</summary>
    public int ChunkSize { get; set; } = 3000;

    /// <summary>Maximum number of reports generated in parallel by the background worker.</summary>
    public int MaxConcurrentJobs { get; set; } = 2;

    /// <summary>Safety cap to protect the process from OOM when rendering PDFs. Larger exports should use Excel.</summary>
    public int MaxPdfRows { get; set; } = 50_000;
}
