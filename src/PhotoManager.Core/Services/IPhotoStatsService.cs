using PhotoManager.Domain;

namespace PhotoManager.Core.Services;

/// <summary>
/// Computes aggregate statistics for a collection of photos
/// </summary>
public interface IPhotoStatsService
{
    PhotoStats ComputeStats(IEnumerable<PhotoMetadata> photos);
}
