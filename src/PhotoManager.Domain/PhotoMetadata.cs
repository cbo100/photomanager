namespace PhotoManager.Domain;

/// <summary>
/// Photo metadata extracted from image files
/// </summary>
public record PhotoMetadata
{
    public required string SourcePath { get; init; }
    public DateTime? DateTaken { get; init; }
    public GpsCoordinates? Location { get; init; }
    public string? CameraMake { get; init; }
    public string? CameraModel { get; init; }
    public required string Hash { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public long FileSize { get; init; }
    public string FileName { get; init; } = string.Empty;
}
