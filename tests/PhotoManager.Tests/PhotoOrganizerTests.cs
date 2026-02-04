using System.IO.Abstractions;
using PhotoManager.Core.Services;
using PhotoManager.Domain;
using NSubstitute;

namespace PhotoManager.Tests;

public class PhotoOrganizerTests
{
    [Fact]
    public void PlanOrganization_WithGpsLocation_ShouldUseCoordinatesInPattern()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        
        // Mock file system path operations
        fileSystem.Path.GetFileName(Arg.Any<string>()).Returns(callInfo => System.IO.Path.GetFileName(callInfo.Arg<string>()));
        fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo => 
            System.IO.Path.Combine(callInfo.ArgAt<string>(0), callInfo.ArgAt<string>(1), callInfo.ArgAt<string>(2)));
        
        var organizer = new PhotoOrganizer(fileSystem);

        var photos = new List<PhotoMetadata>
        {
            new PhotoMetadata
            {
                SourcePath = "/source/photo.jpg",
                Hash = "ABC123",
                FileName = "photo.jpg",
                DateTaken = new DateTime(2024, 10, 15, 14, 30, 0),
                Location = new GpsCoordinates(-33.832108, 150.997711)
            }
        };

        var config = new PhotoManagerConfig
        {
            SourceFolder = "/source",
            DestinationFolder = "/dest",
            OrganizationPattern = "{Year}/{Month}/{Location}"
        };

        // Act
        var operations = organizer.PlanOrganization(photos, config);

        // Assert
        Assert.Single(operations);
        var operation = operations[0];
        
        // Should contain coordinates formatted as "lat_lon"
        Assert.Contains("-33.83", operation.DestinationPath);
        Assert.Contains("151.00", operation.DestinationPath);
        Assert.Contains("2024", operation.DestinationPath);
        Assert.Contains("10", operation.DestinationPath);
    }

    [Fact]
    public void PlanOrganization_WithoutGpsLocation_ShouldUseUnknown()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        
        // Mock file system path operations
        fileSystem.Path.GetFileName(Arg.Any<string>()).Returns(callInfo => System.IO.Path.GetFileName(callInfo.Arg<string>()));
        fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo => 
            System.IO.Path.Combine(callInfo.ArgAt<string>(0), callInfo.ArgAt<string>(1), callInfo.ArgAt<string>(2)));
        
        var organizer = new PhotoOrganizer(fileSystem);

        var photos = new List<PhotoMetadata>
        {
            new PhotoMetadata
            {
                SourcePath = "/source/photo.jpg",
                Hash = "ABC123",
                FileName = "photo.jpg",
                DateTaken = new DateTime(2024, 10, 15, 14, 30, 0),
                Location = null
            }
        };

        var config = new PhotoManagerConfig
        {
            SourceFolder = "/source",
            DestinationFolder = "/dest",
            OrganizationPattern = "{Year}/{Location}"
        };

        // Act
        var operations = organizer.PlanOrganization(photos, config);

        // Assert
        Assert.Single(operations);
        var operation = operations[0];
        
        // Should contain "Unknown" for missing location
        Assert.Contains("Unknown", operation.DestinationPath);
        Assert.Contains("2024", operation.DestinationPath);
    }

    [Fact]
    public void DetectDuplicates_WithSameHash_ShouldGroupTogether()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var organizer = new PhotoOrganizer(fileSystem);

        var photos = new List<PhotoMetadata>
        {
            new PhotoMetadata { SourcePath = "/photo1.jpg", Hash = "ABC123", FileName = "photo1.jpg" },
            new PhotoMetadata { SourcePath = "/photo2.jpg", Hash = "ABC123", FileName = "photo2.jpg" },
            new PhotoMetadata { SourcePath = "/photo3.jpg", Hash = "DEF456", FileName = "photo3.jpg" }
        };

        // Act
        var duplicates = organizer.DetectDuplicates(photos);

        // Assert
        Assert.Single(duplicates);
        Assert.Equal("ABC123", duplicates.Keys.First());
        Assert.Equal(2, duplicates["ABC123"].Count);
    }
}
