using PhotoManager.Domain;

namespace PhotoManager.Core.Services;

/// <summary>
/// Interface for extracting metadata from image files
/// </summary>
public interface IMetadataExtractor
{
    /// <summary>
    /// Extracts metadata from an image file
    /// </summary>
    Task<PhotoMetadata?> ExtractMetadataAsync(string filePath, CancellationToken cancellationToken = default);
}
