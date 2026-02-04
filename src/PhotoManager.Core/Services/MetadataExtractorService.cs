using System.Security.Cryptography;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using PhotoManager.Domain;
using System.IO.Abstractions;

namespace PhotoManager.Core.Services;

/// <summary>
/// Extracts metadata from image files using MetadataExtractor library
/// </summary>
public class MetadataExtractorService : IMetadataExtractor
{
    private readonly IFileSystem _fileSystem;

    public MetadataExtractorService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<PhotoMetadata?> ExtractMetadataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_fileSystem.File.Exists(filePath))
                return null;

            var fileInfo = _fileSystem.FileInfo.New(filePath);
            var hash = await ComputeHashAsync(filePath, cancellationToken);

            IEnumerable<MetadataExtractor.Directory> directories;
            try
            {
                directories = ImageMetadataReader.ReadMetadata(filePath);
            }
            catch
            {
                // If metadata extraction fails, return basic info
                return new PhotoMetadata
                {
                    SourcePath = filePath,
                    Hash = hash,
                    FileSize = fileInfo.Length,
                    FileName = fileInfo.Name
                };
            }

            DateTime? dateTaken = null;
            GpsCoordinates? location = null;
            string? cameraMake = null;
            string? cameraModel = null;
            int width = 0;
            int height = 0;

            // Extract EXIF data
            var exifSubIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (exifSubIfd != null)
            {
                if (exifSubIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTime))
                {
                    dateTaken = dateTime;
                }

                width = exifSubIfd.GetInt32(ExifDirectoryBase.TagExifImageWidth);
                height = exifSubIfd.GetInt32(ExifDirectoryBase.TagExifImageHeight);
            }

            // Extract camera info
            var exifIfd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (exifIfd0 != null)
            {
                cameraMake = exifIfd0.GetString(ExifDirectoryBase.TagMake);
                cameraModel = exifIfd0.GetString(ExifDirectoryBase.TagModel);
            }

            // Extract GPS coordinates
            var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
            if (gpsDirectory != null && gpsDirectory.TryGetGeoLocation(out var gpsLocation))
            {
                if (gpsLocation is { IsZero: false })
                {
                    location = new GpsCoordinates(gpsLocation.Latitude, gpsLocation.Longitude);
                }
            }

            return new PhotoMetadata
            {
                SourcePath = filePath,
                DateTaken = dateTaken,
                Location = location,
                CameraMake = cameraMake,
                CameraModel = cameraModel,
                Hash = hash,
                Width = width,
                Height = height,
                FileSize = fileInfo.Length,
                FileName = fileInfo.Name
            };
        }
        catch (Exception ex)
        {
            // Log error and return null
            Console.WriteLine($"Error extracting metadata from {filePath}: {ex.Message}");
            return null;
        }
    }

    private async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = _fileSystem.File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes);
    }
}
