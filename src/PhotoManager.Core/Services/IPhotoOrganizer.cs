using PhotoManager.Domain;

namespace PhotoManager.Core.Services;

/// <summary>
/// Interface for organizing photos
/// </summary>
public interface IPhotoOrganizer
{
    /// <summary>
    /// Plans organization operations for a list of photos
    /// </summary>
    List<PhotoOperation> PlanOrganization(
        List<PhotoMetadata> photos,
        PhotoManagerConfig config);

    /// <summary>
    /// Executes the planned operations
    /// </summary>
    Task ExecuteOperationsAsync(
        List<PhotoOperation> operations,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects duplicate photos by hash
    /// </summary>
    Dictionary<string, List<PhotoMetadata>> DetectDuplicates(List<PhotoMetadata> photos);
}

/// <summary>
/// Progress information for organization operation
/// </summary>
public record OperationProgress(int OperationsCompleted, int TotalOperations, string? CurrentFile = null);
