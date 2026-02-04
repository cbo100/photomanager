using System.IO.Abstractions;
using System.Text.RegularExpressions;
using PhotoManager.Domain;

namespace PhotoManager.Core.Services;

/// <summary>
/// Organizes photos based on configuration and metadata
/// </summary>
public partial class PhotoOrganizer : IPhotoOrganizer
{
    private readonly IFileSystem _fileSystem;

    public PhotoOrganizer(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public List<PhotoOperation> PlanOrganization(List<PhotoMetadata> photos, PhotoManagerConfig config)
    {
        var operations = new List<PhotoOperation>();

        foreach (var photo in photos)
        {
            var destinationPath = GenerateDestinationPath(photo, config);
            operations.Add(new PhotoOperation
            {
                SourcePath = photo.SourcePath,
                DestinationPath = destinationPath,
                Type = config.OperationType,
                Metadata = photo
            });
        }

        return operations;
    }

    public async Task ExecuteOperationsAsync(
        List<PhotoOperation> operations,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var totalOperations = operations.Count;
        var completedCount = 0;

        foreach (var operation in operations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var destinationDir = _fileSystem.Path.GetDirectoryName(operation.DestinationPath);
            if (!string.IsNullOrEmpty(destinationDir) && !_fileSystem.Directory.Exists(destinationDir))
            {
                _fileSystem.Directory.CreateDirectory(destinationDir);
            }

            switch (operation.Type)
            {
                case OperationType.Copy:
                    await CopyFileAsync(operation.SourcePath, operation.DestinationPath, cancellationToken);
                    break;

                case OperationType.Move:
                    _fileSystem.File.Move(operation.SourcePath, operation.DestinationPath);
                    break;

                case OperationType.Symlink:
                    _fileSystem.File.CreateSymbolicLink(operation.DestinationPath, operation.SourcePath);
                    break;
            }

            var completed = Interlocked.Increment(ref completedCount);
            progress?.Report(new OperationProgress(completed, totalOperations, operation.SourcePath));
        }
    }

    public Dictionary<string, List<PhotoMetadata>> DetectDuplicates(List<PhotoMetadata> photos)
    {
        return photos
            .GroupBy(p => p.Hash)
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        const int bufferSize = 81920; // 80KB buffer
        await using var sourceStream = _fileSystem.File.OpenRead(sourcePath);
        await using var destinationStream = _fileSystem.File.Create(destinationPath);
        await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken);
    }

    private string GenerateDestinationPath(PhotoMetadata photo, PhotoManagerConfig config)
    {
        var dateTaken = photo.DateTaken ?? _fileSystem.File.GetCreationTime(photo.SourcePath);
        var fileName = _fileSystem.Path.GetFileName(photo.SourcePath);

        var pattern = config.OrganizationPattern;

        // Replace tokens in pattern
        pattern = YearRegex().Replace(pattern, dateTaken.Year.ToString());
        pattern = MonthRegex().Replace(pattern, dateTaken.Month.ToString("D2"));
        pattern = MonthNameRegex().Replace(pattern, dateTaken.ToString("MMMM"));
        pattern = DayRegex().Replace(pattern, dateTaken.Day.ToString("D2"));

        if (photo.Location != null)
        {
            // For now, use coordinates as placeholder
            // In future, implement reverse geocoding
            var locationStr = $"{photo.Location.Latitude:F2}_{photo.Location.Longitude:F2}";
            pattern = LocationRegex().Replace(pattern, locationStr);
        }
        else
        {
            pattern = LocationRegex().Replace(pattern, "Unknown");
        }

        if (!string.IsNullOrEmpty(photo.CameraMake))
        {
            pattern = CameraRegex().Replace(pattern, photo.CameraMake);
        }
        else
        {
            pattern = CameraRegex().Replace(pattern, "Unknown");
        }

        var destinationPath = _fileSystem.Path.Combine(config.DestinationFolder, pattern, fileName);
        return destinationPath;
    }

    [GeneratedRegex(@"\{Year\}", RegexOptions.IgnoreCase)]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"\{Month\}", RegexOptions.IgnoreCase)]
    private static partial Regex MonthRegex();

    [GeneratedRegex(@"\{MonthName\}", RegexOptions.IgnoreCase)]
    private static partial Regex MonthNameRegex();

    [GeneratedRegex(@"\{Day\}", RegexOptions.IgnoreCase)]
    private static partial Regex DayRegex();

    [GeneratedRegex(@"\{Location\}", RegexOptions.IgnoreCase)]
    private static partial Regex LocationRegex();

    [GeneratedRegex(@"\{Camera\}", RegexOptions.IgnoreCase)]
    private static partial Regex CameraRegex();
}
