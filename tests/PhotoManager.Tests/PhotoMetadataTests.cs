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
        Assert.Equal("/path/to/photo.jpg", metadata.SourcePath);
        Assert.Equal("ABC123", metadata.Hash);
        Assert.Equal("photo.jpg", metadata.FileName);
    }

    [Fact]
    public void GpsCoordinates_ShouldStoreLatitudeAndLongitude()
    {
        // Arrange & Act
        var coordinates = new GpsCoordinates(40.7128, -74.0060);

        // Assert
        Assert.Equal(40.7128, coordinates.Latitude);
        Assert.Equal(-74.0060, coordinates.Longitude);
    }
}
