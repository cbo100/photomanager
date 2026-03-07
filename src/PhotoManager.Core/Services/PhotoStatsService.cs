using PhotoManager.Domain;

namespace PhotoManager.Core.Services;

/// <summary>
/// Computes aggregate statistics for a collection of photos
/// </summary>
public class PhotoStatsService : IPhotoStatsService
{
    public PhotoStats ComputeStats(IEnumerable<PhotoMetadata> photos)
    {
        var photoList = photos.ToList();

        if (photoList.Count == 0)
        {
            return new PhotoStats();
        }

        var photosByYear = photoList
            .GroupBy(p => p.DateTaken.HasValue ? p.DateTaken.Value.Year : 0)
            .ToDictionary(g => g.Key, g => g.Count());

        var photosByCamera = photoList
            .GroupBy(p =>
            {
                if (p.CameraMake == null && p.CameraModel == null)
                    return "Unknown";
                return $"{p.CameraMake} {p.CameraModel}".Trim();
            })
            .ToDictionary(g => g.Key, g => g.Count());

        // Duplicates: group by hash, photos beyond the first in each group are duplicates
        var hashGroups = photoList.GroupBy(p => p.Hash).ToList();
        var duplicatePhotos = hashGroups
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();

        return new PhotoStats
        {
            TotalPhotos = photoList.Count,
            TotalSizeBytes = photoList.Sum(p => p.FileSize),
            PhotosWithDate = photoList.Count(p => p.DateTaken.HasValue),
            PhotosWithLocation = photoList.Count(p => p.Location != null),
            PhotosWithCamera = photoList.Count(p => p.CameraMake != null),
            DuplicateCount = duplicatePhotos.Count,
            DuplicateSizeBytes = duplicatePhotos.Sum(p => p.FileSize),
            PhotosByYear = photosByYear,
            PhotosByCamera = photosByCamera,
        };
    }
}
