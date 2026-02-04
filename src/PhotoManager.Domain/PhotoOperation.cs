namespace PhotoManager.Domain;

/// <summary>
/// Planned file operations
/// </summary>
public record PhotoOperation
{
    public required string SourcePath { get; init; }
    public required string DestinationPath { get; init; }
    public OperationType Type { get; init; }
    public PhotoMetadata? Metadata { get; init; }
}

/// <summary>
/// Type of file operation to perform
/// </summary>
public enum OperationType
{
    Copy,
    Move,
    Symlink
}
