namespace PhotoManager.Domain;

public record OperationLogEntry
{
    public required string SourcePath { get; init; }
    public required string DestinationPath { get; init; }
    public required string OperationType { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; }
    public long FileSizeBytes { get; init; }
}
