namespace PhotoManager.Domain;

/// <summary>
/// Aggregated statistics for a photo collection
/// </summary>
public record PhotoStats
{
    public int TotalPhotos { get; init; }
    public long TotalSizeBytes { get; init; }
    public int PhotosWithDate { get; init; }
    public int PhotosWithLocation { get; init; }
    public int PhotosWithCamera { get; init; }
    public int DuplicateCount { get; init; }
    public long DuplicateSizeBytes { get; init; }
    public Dictionary<int, int> PhotosByYear { get; init; } = new();
    public Dictionary<string, int> PhotosByCamera { get; init; } = new();
}
