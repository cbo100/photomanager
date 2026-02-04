using PhotoManager.Domain;

namespace PhotoManager.Core.Services;

/// <summary>
/// Interface for scanning directories for image files
/// </summary>
public interface IPhotoScanner
{
    /// <summary>
    /// Scans a directory recursively for image files and extracts their metadata
    /// </summary>
    Task<List<PhotoMetadata>> ScanDirectoryAsync(
        string directoryPath,
        string[] fileExtensions,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress information for scanning operation
/// </summary>
public record ScanProgress(int FilesProcessed, int TotalFiles, string? CurrentFile = null);
