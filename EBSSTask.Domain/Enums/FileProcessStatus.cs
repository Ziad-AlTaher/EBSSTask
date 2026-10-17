namespace EBSSTask.Domain.Enums;

/// <summary>
/// Lifecycle of a background file-generation process.
/// </summary>
public enum FileProcessStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}
