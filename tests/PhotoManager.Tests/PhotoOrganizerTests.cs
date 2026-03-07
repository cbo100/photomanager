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

public class PhotoOrganizerCollisionTests
{
    private static IFileSystem CreateFileSystem()
    {
        var fs = Substitute.For<IFileSystem>();
        fs.Path.GetFileName(Arg.Any<string>()).Returns(ci => System.IO.Path.GetFileName(ci.Arg<string>()));
        fs.Path.GetDirectoryName(Arg.Any<string>()).Returns(ci => System.IO.Path.GetDirectoryName(ci.Arg<string>()));
        fs.Path.GetFileNameWithoutExtension(Arg.Any<string>()).Returns(ci => System.IO.Path.GetFileNameWithoutExtension(ci.Arg<string>()));
        fs.Path.GetExtension(Arg.Any<string>()).Returns(ci => System.IO.Path.GetExtension(ci.Arg<string>()));
        fs.Path.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns(ci =>
            System.IO.Path.Combine(ci.ArgAt<string>(0), ci.ArgAt<string>(1)));
        fs.Path.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(ci =>
            System.IO.Path.Combine(ci.ArgAt<string>(0), ci.ArgAt<string>(1), ci.ArgAt<string>(2)));
        return fs;
    }

    private static PhotoManagerConfig RenameConfig() => new()
    {
        SourceFolder = "/src",
        DestinationFolder = "/dest",
        OrganizationPattern = "{Year}/{Month}",
        HandleDuplicates = DuplicateHandling.Rename
    };

    // 1. No collision — different filenames in the same folder → no suffix
    [Fact]
    public void PlanOrganization_NoCollision_NoSuffixAdded()
    {
        var organizer = new PhotoOrganizer(CreateFileSystem());
        var date = new DateTime(2024, 6, 15);
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "/src/a.jpg", Hash = "H1", FileName = "a.jpg", DateTaken = date },
            new() { SourcePath = "/src/b.jpg", Hash = "H2", FileName = "b.jpg", DateTaken = date },
        };

        var ops = organizer.PlanOrganization(photos, RenameConfig());

        Assert.Equal(2, ops.Count);
        Assert.Contains(ops, op => op.DestinationPath.EndsWith("a.jpg"));
        Assert.Contains(ops, op => op.DestinationPath.EndsWith("b.jpg"));
        Assert.DoesNotContain(ops, op => op.DestinationPath.Contains("_1"));
    }

    // 2. Two photos with same filename → first keeps name, second gets _1 suffix
    [Fact]
    public void PlanOrganization_TwoCollisions_Rename_SecondGetsSuffix()
    {
        var organizer = new PhotoOrganizer(CreateFileSystem());
        var date = new DateTime(2024, 6, 15);
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "/src/cam1/photo.jpg", Hash = "H1", FileName = "photo.jpg", DateTaken = date },
            new() { SourcePath = "/src/cam2/photo.jpg", Hash = "H2", FileName = "photo.jpg", DateTaken = date },
        };

        var ops = organizer.PlanOrganization(photos, RenameConfig());

        Assert.Equal(2, ops.Count);
        Assert.Contains(ops, op => op.DestinationPath.EndsWith("photo.jpg") && !op.DestinationPath.Contains("_1"));
        Assert.Contains(ops, op => op.DestinationPath.EndsWith("photo_1.jpg"));
    }

    // 3. Three photos with same filename → original, _1, _2 (sorted by source path)
    [Fact]
    public void PlanOrganization_TripleCollision_Rename_CorrectSuffixes()
    {
        var organizer = new PhotoOrganizer(CreateFileSystem());
        var date = new DateTime(2024, 6, 15);
        // Provide in non-alphabetical order to verify sort-by-source determinism
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "/src/c/photo.jpg", Hash = "H3", FileName = "photo.jpg", DateTaken = date },
            new() { SourcePath = "/src/a/photo.jpg", Hash = "H1", FileName = "photo.jpg", DateTaken = date },
            new() { SourcePath = "/src/b/photo.jpg", Hash = "H2", FileName = "photo.jpg", DateTaken = date },
        };

        var ops = organizer.PlanOrganization(photos, RenameConfig());

        Assert.Equal(3, ops.Count);
        Assert.Contains(ops, op => op.SourcePath == "/src/a/photo.jpg" && op.DestinationPath.EndsWith("photo.jpg") && !op.DestinationPath.Contains("_"));
        Assert.Contains(ops, op => op.SourcePath == "/src/b/photo.jpg" && op.DestinationPath.EndsWith("photo_1.jpg"));
        Assert.Contains(ops, op => op.SourcePath == "/src/c/photo.jpg" && op.DestinationPath.EndsWith("photo_2.jpg"));
    }

    // 4. Overwrite mode → both map to the same destination path, no suffix
    [Fact]
    public void PlanOrganization_Collision_Overwrite_SameDestinationKept()
    {
        var organizer = new PhotoOrganizer(CreateFileSystem());
        var date = new DateTime(2024, 6, 15);
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "/src/cam1/photo.jpg", Hash = "H1", FileName = "photo.jpg", DateTaken = date },
            new() { SourcePath = "/src/cam2/photo.jpg", Hash = "H2", FileName = "photo.jpg", DateTaken = date },
        };
        var config = new PhotoManagerConfig
        {
            SourceFolder = "/src",
            DestinationFolder = "/dest",
            OrganizationPattern = "{Year}/{Month}",
            HandleDuplicates = DuplicateHandling.Overwrite
        };

        var ops = organizer.PlanOrganization(photos, config);

        Assert.Equal(2, ops.Count);
        Assert.Equal(ops[0].DestinationPath, ops[1].DestinationPath);
        Assert.DoesNotContain(ops, op => op.DestinationPath.Contains("_1"));
    }

    // 5. True duplicates (same hash) with Skip mode → only one operation generated
    [Fact]
    public void PlanOrganization_Duplicate_Skip_OnlyOneOperationGenerated()
    {
        var organizer = new PhotoOrganizer(CreateFileSystem());
        var date = new DateTime(2024, 6, 15);
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "/src/copy1/photo.jpg", Hash = "SAMEHASH", FileName = "photo.jpg", DateTaken = date },
            new() { SourcePath = "/src/copy2/photo.jpg", Hash = "SAMEHASH", FileName = "photo.jpg", DateTaken = date },
        };
        var config = new PhotoManagerConfig
        {
            SourceFolder = "/src",
            DestinationFolder = "/dest",
            OrganizationPattern = "{Year}/{Month}",
            HandleDuplicates = DuplicateHandling.Skip
        };

        var ops = organizer.PlanOrganization(photos, config);

        Assert.Single(ops);
    }

    // 6. Same filename but different EXIF dates → different year folders → no collision, no suffix
    [Fact]
    public void PlanOrganization_SameFilenameButDifferentDates_DifferentFolders_NoSuffix()
    {
        var organizer = new PhotoOrganizer(CreateFileSystem());
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "/src/a/photo.jpg", Hash = "H1", FileName = "photo.jpg", DateTaken = new DateTime(2023, 1, 1) },
            new() { SourcePath = "/src/b/photo.jpg", Hash = "H2", FileName = "photo.jpg", DateTaken = new DateTime(2024, 1, 1) },
        };

        var ops = organizer.PlanOrganization(photos, RenameConfig());

        Assert.Equal(2, ops.Count);
        Assert.DoesNotContain(ops, op => op.DestinationPath.Contains("_1"));
        Assert.Contains(ops, op => op.DestinationPath.Contains("2023"));
        Assert.Contains(ops, op => op.DestinationPath.Contains("2024"));
    }

    // 7. Secondary collision: photo.jpg (x2) + photo_1.jpg exists → should not create duplicate destinations
    [Fact]
    public void PlanOrganization_SecondaryCollision_NoDuplicateDestinations()
    {
        var organizer = new PhotoOrganizer(CreateFileSystem());
        var date = new DateTime(2024, 6, 15);
        // Three photos: two photo.jpg that would collide, plus one photo_1.jpg
        // After renaming first collision: photo.jpg, photo_1.jpg, photo_1.jpg (COLLISION!)
        // Correct behavior: photo.jpg, photo_2.jpg, photo_1.jpg (or similar safe assignment)
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "/src/a/photo.jpg", Hash = "H1", FileName = "photo.jpg", DateTaken = date },
            new() { SourcePath = "/src/b/photo.jpg", Hash = "H2", FileName = "photo.jpg", DateTaken = date },
            new() { SourcePath = "/src/c/photo_1.jpg", Hash = "H3", FileName = "photo_1.jpg", DateTaken = date },
        };

        var ops = organizer.PlanOrganization(photos, RenameConfig());

        Assert.Equal(3, ops.Count);
        
        // Verify no duplicate destinations
        var destinations = ops.Select(o => o.DestinationPath).ToList();
        Assert.Equal(destinations.Count, destinations.Distinct().Count());
    }

    // 8. Case-insensitive collision: Photo.jpg and photo.jpg should be treated as collision
    [Fact]
    public void PlanOrganization_CaseInsensitiveCollision_Rename_SecondGetsSuffix()
    {
        var organizer = new PhotoOrganizer(CreateFileSystem());
        var date = new DateTime(2024, 6, 15);
        // On Windows, Photo.jpg and photo.jpg would collide, so we must detect this
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "/src/a/Photo.jpg", Hash = "H1", FileName = "Photo.jpg", DateTaken = date },
            new() { SourcePath = "/src/b/photo.jpg", Hash = "H2", FileName = "photo.jpg", DateTaken = date },
        };

        var ops = organizer.PlanOrganization(photos, RenameConfig());

        Assert.Equal(2, ops.Count);
        
        // Verify destinations differ (case-insensitively, one must have a suffix)
        var destinations = ops.Select(o => o.DestinationPath).ToList();
        Assert.Equal(2, destinations.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        
        // One should keep name, other should get suffix
        var hasSuffix = ops.Count(op => op.DestinationPath.Contains("_1"));
        Assert.Equal(1, hasSuffix);
    }
}
