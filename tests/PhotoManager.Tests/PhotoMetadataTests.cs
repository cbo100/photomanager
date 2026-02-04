using FluentAssertions;
using PhotoManager.Domain;

namespace PhotoManager.Tests;

public class PhotoMetadataTests
{
    [Fact]
    public void PhotoMetadata_ShouldCreateWithRequiredProperties()
    {
        // Arrange & Act
        var metadata = new PhotoMetadata
        {
            SourcePath = "/path/to/photo.jpg",
            Hash = "ABC123",
            FileName = "photo.jpg"
        };

        // Assert
        metadata.SourcePath.Should().Be("/path/to/photo.jpg");
        metadata.Hash.Should().Be("ABC123");
        metadata.FileName.Should().Be("photo.jpg");
    }

    [Fact]
    public void GpsCoordinates_ShouldStoreLatitudeAndLongitude()
    {
        // Arrange & Act
        var coordinates = new GpsCoordinates(40.7128, -74.0060);

        // Assert
        coordinates.Latitude.Should().Be(40.7128);
        coordinates.Longitude.Should().Be(-74.0060);
    }
}
