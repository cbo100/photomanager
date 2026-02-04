using System.IO.Abstractions;
using PhotoManager.Domain;

namespace PhotoManager.Core.Services;

/// <summary>
/// Scans directories for image files and extracts metadata
/// </summary>
public class PhotoScanner : IPhotoScanner
{
    private readonly IFileSystem _fileSystem;
    private readonly IMetadataExtractor _metadataExtractor;

    public PhotoScanner(IFileSystem fileSystem, IMetadataExtractor metadataExtractor)
    {
        _fileSystem = fileSystem;
        _metadataExtractor = metadataExtractor;
    }

    public async Task<List<PhotoMetadata>> ScanDirectoryAsync(
        string directoryPath,
        string[] fileExtensions,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!_fileSystem.Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var files = _fileSystem.Directory
            .EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => fileExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var totalFiles = files.Count;
        var results = new List<PhotoMetadata>();
        var processedCount = 0;

        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        var metadataList = new List<PhotoMetadata?>(totalFiles);
        for (int i = 0; i < totalFiles; i++)
        {
            metadataList.Add(null);
        }

        await Parallel.ForEachAsync(
            files.Select((file, index) => (file, index)),
            options,
            async (item, ct) =>
            {
                var (file, index) = item;
                var metadata = await _metadataExtractor.ExtractMetadataAsync(file, ct);

                if (metadata != null)
                {
                    metadataList[index] = metadata;
                }

                var processed = Interlocked.Increment(ref processedCount);
                progress?.Report(new ScanProgress(processed, totalFiles, file));
            });

        results.AddRange(metadataList.Where(m => m != null)!);

        return results;
    }
}
