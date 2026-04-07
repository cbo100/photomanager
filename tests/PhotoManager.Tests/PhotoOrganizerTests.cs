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

    [Fact]
    public void PlanOrganization_WhenDestinationExists_ShouldSkipByDefault()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Path.GetFileName(Arg.Any<string>()).Returns(callInfo => System.IO.Path.GetFileName(callInfo.Arg<string>()));
        fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            System.IO.Path.Combine(callInfo.ArgAt<string>(0), callInfo.ArgAt<string>(1), callInfo.ArgAt<string>(2)));
        fileSystem.File.Exists(Arg.Any<string>()).Returns(true);

        var organizer = new PhotoOrganizer(fileSystem);

        var photos = new List<PhotoMetadata>
        {
            new PhotoMetadata { SourcePath = "/source/photo.jpg", Hash = "ABC123", FileName = "photo.jpg", DateTaken = new DateTime(2024, 6, 1) }
        };

        var config = new PhotoManagerConfig
        {
            SourceFolder = "/source",
            DestinationFolder = "/dest",
            OverwriteExisting = false
        };

        // Act
        var operations = organizer.PlanOrganization(photos, config);

        // Assert
        Assert.Empty(operations);
    }

    [Fact]
    public void PlanOrganization_WhenDestinationExistsAndOverwriteEnabled_ShouldIncludeOperation()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Path.GetFileName(Arg.Any<string>()).Returns(callInfo => System.IO.Path.GetFileName(callInfo.Arg<string>()));
        fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            System.IO.Path.Combine(callInfo.ArgAt<string>(0), callInfo.ArgAt<string>(1), callInfo.ArgAt<string>(2)));
        fileSystem.File.Exists(Arg.Any<string>()).Returns(true);

        var organizer = new PhotoOrganizer(fileSystem);

        var photos = new List<PhotoMetadata>
        {
            new PhotoMetadata { SourcePath = "/source/photo.jpg", Hash = "ABC123", FileName = "photo.jpg", DateTaken = new DateTime(2024, 6, 1) }
        };

        var config = new PhotoManagerConfig
        {
            SourceFolder = "/source",
            DestinationFolder = "/dest",
            OverwriteExisting = true
        };

        // Act
        var operations = organizer.PlanOrganization(photos, config);

        // Assert
        Assert.Single(operations);
    }

    [Fact]
    public void PlanOrganization_WhenDestinationDoesNotExist_ShouldIncludeOperation()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Path.GetFileName(Arg.Any<string>()).Returns(callInfo => System.IO.Path.GetFileName(callInfo.Arg<string>()));
        fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            System.IO.Path.Combine(callInfo.ArgAt<string>(0), callInfo.ArgAt<string>(1), callInfo.ArgAt<string>(2)));
        fileSystem.File.Exists(Arg.Any<string>()).Returns(false);

        var organizer = new PhotoOrganizer(fileSystem);

        var photos = new List<PhotoMetadata>
        {
            new PhotoMetadata { SourcePath = "/source/photo.jpg", Hash = "ABC123", FileName = "photo.jpg", DateTaken = new DateTime(2024, 6, 1) }
        };

        var config = new PhotoManagerConfig
        {
            SourceFolder = "/source",
            DestinationFolder = "/dest",
            OverwriteExisting = false
        };

        // Act
        var operations = organizer.PlanOrganization(photos, config);

        // Assert
        Assert.Single(operations);
    }

    [Fact]
    public void CleanEmptyDirectories_ShouldRemoveEmptySubdirectories()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.EnumerateDirectories("/root", "*", SearchOption.AllDirectories)
            .Returns(["/root/2024/06", "/root/2024"]);
        fileSystem.Directory.EnumerateFileSystemEntries("/root/2024/06").Returns([]);
        fileSystem.Directory.EnumerateFileSystemEntries("/root/2024").Returns([]);

        var organizer = new PhotoOrganizer(fileSystem);

        // Act
        var removed = organizer.CleanEmptyDirectories("/root");

        // Assert
        Assert.Equal(2, removed);
        fileSystem.Directory.Received(1).Delete("/root/2024/06");
        fileSystem.Directory.Received(1).Delete("/root/2024");
    }

    [Fact]
    public void CleanEmptyDirectories_ShouldNotRemoveNonEmptyDirectories()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.EnumerateDirectories("/root", "*", SearchOption.AllDirectories)
            .Returns(["/root/2024/06"]);
        fileSystem.Directory.EnumerateFileSystemEntries("/root/2024/06")
            .Returns(["/root/2024/06/photo.jpg"]);

        var organizer = new PhotoOrganizer(fileSystem);

        // Act
        var removed = organizer.CleanEmptyDirectories("/root");

        // Assert
        Assert.Equal(0, removed);
        fileSystem.Directory.DidNotReceive().Delete(Arg.Any<string>());
    }

    [Fact]
    public void CleanEmptyDirectories_ShouldNotDeleteRootFolder()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.EnumerateDirectories("/root", "*", SearchOption.AllDirectories)
            .Returns([]);

        var organizer = new PhotoOrganizer(fileSystem);

        // Act
        var removed = organizer.CleanEmptyDirectories("/root");

        // Assert
        Assert.Equal(0, removed);
        fileSystem.Directory.DidNotReceive().Delete("/root");
    }
}
