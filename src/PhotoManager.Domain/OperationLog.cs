namespace PhotoManager.Domain;

public record OperationLog
{
    public required string SessionId { get; init; }
    public required DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public required string SourceFolder { get; init; }
    public required string DestinationFolder { get; init; }
    public required string Pattern { get; init; }
    public required string Mode { get; init; }
    public bool DryRun { get; init; }
    public List<OperationLogEntry> Entries { get; init; } = new();
    public int TotalOperations => Entries.Count;
    public int SuccessCount => Entries.Count(e => e.Success);
    public int FailureCount => Entries.Count(e => !e.Success);
    public long TotalBytesProcessed => Entries.Where(e => e.Success).Sum(e => e.FileSizeBytes);
}
