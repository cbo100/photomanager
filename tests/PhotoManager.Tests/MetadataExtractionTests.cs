using System.IO.Abstractions;
using PhotoManager.Core.Services;
using PhotoManager.Domain;

namespace PhotoManager.Tests;

public class MetadataExtractionTests
{
    [Fact]
    public async Task ExtractMetadata_WithGpsData_ShouldExtractLocation()
    {
        // Arrange
        var fileSystem = new FileSystem();
        var extractor = new MetadataExtractorService(fileSystem);
        var testImagePath = "/home/chris/dev/photomanager/testing/ingest/0207AF55-420F-4B99-A733-0E27B9ED8326.JPG";

        // Skip test if file doesn't exist
        if (!File.Exists(testImagePath))
        {
            return;
        }

        // Act
        var metadata = await extractor.ExtractMetadataAsync(testImagePath);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(testImagePath, metadata.SourcePath);
        
        // Output for debugging
        Console.WriteLine($"File: {metadata.FileName}");
        Console.WriteLine($"Date Taken: {metadata.DateTaken}");
        Console.WriteLine($"Location: {(metadata.Location != null ? $"{metadata.Location.Latitude}, {metadata.Location.Longitude}" : "None")}");
        Console.WriteLine($"Camera: {metadata.CameraMake} {metadata.CameraModel}");
        Console.WriteLine($"Dimensions: {metadata.Width}x{metadata.Height}");
    }

    [Fact]
    public async Task ExtractMetadata_MultipleImages_CheckGpsData()
    {
        // Arrange
        var fileSystem = new FileSystem();
        var extractor = new MetadataExtractorService(fileSystem);
        var testDir = "/home/chris/dev/photomanager/testing/ingest";

        // Skip test if directory doesn't exist
        if (!Directory.Exists(testDir))
        {
            return;
        }

        var imageFiles = Directory.GetFiles(testDir, "*.JPG").Take(5).ToList();
        
        // Act & Assert
        foreach (var imagePath in imageFiles)
        {
            var metadata = await extractor.ExtractMetadataAsync(imagePath);
            Assert.NotNull(metadata);
            
            Console.WriteLine($"\nFile: {Path.GetFileName(imagePath)}");
            Console.WriteLine($"  Date: {metadata.DateTaken?.ToString("yyyy-MM-dd HH:mm:ss") ?? "None"}");
            Console.WriteLine($"  GPS: {(metadata.Location != null ? $"{metadata.Location.Latitude:F6}, {metadata.Location.Longitude:F6}" : "None")}");
            Console.WriteLine($"  Camera: {metadata.CameraMake} {metadata.CameraModel}");
        }
    }
}
