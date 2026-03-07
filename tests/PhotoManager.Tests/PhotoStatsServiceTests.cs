using PhotoManager.Core.Services;
using PhotoManager.Domain;

namespace PhotoManager.Tests;

public class PhotoStatsServiceTests
{
    private readonly PhotoStatsService _sut = new();

    [Fact]
    public void ComputeStats_EmptyCollection_ReturnsAllZerosAndEmptyDicts()
    {
        var stats = _sut.ComputeStats([]);

        Assert.Equal(0, stats.TotalPhotos);
        Assert.Equal(0, stats.TotalSizeBytes);
        Assert.Equal(0, stats.PhotosWithDate);
        Assert.Equal(0, stats.PhotosWithLocation);
        Assert.Equal(0, stats.PhotosWithCamera);
        Assert.Equal(0, stats.DuplicateCount);
        Assert.Equal(0, stats.DuplicateSizeBytes);
        Assert.Empty(stats.PhotosByYear);
        Assert.Empty(stats.PhotosByCamera);
    }

    [Fact]
    public void ComputeStats_SinglePhotoWithAllMetadata_CountsCorrectly()
    {
        var photo = new PhotoMetadata
        {
            SourcePath = "test.jpg",
            Hash = "abc123",
            DateTaken = new DateTime(2023, 6, 15),
            Location = new GpsCoordinates(48.8566, 2.3522),
            CameraMake = "Canon",
            CameraModel = "EOS R5",
            FileSize = 5_000_000,
        };

        var stats = _sut.ComputeStats([photo]);

        Assert.Equal(1, stats.TotalPhotos);
        Assert.Equal(5_000_000, stats.TotalSizeBytes);
        Assert.Equal(1, stats.PhotosWithDate);
        Assert.Equal(1, stats.PhotosWithLocation);
        Assert.Equal(1, stats.PhotosWithCamera);
        Assert.Equal(0, stats.DuplicateCount);
        Assert.Single(stats.PhotosByYear);
        Assert.Equal(1, stats.PhotosByYear[2023]);
        Assert.Single(stats.PhotosByCamera);
        Assert.Equal(1, stats.PhotosByCamera["Canon EOS R5"]);
    }

    [Fact]
    public void ComputeStats_PhotosGroupedByYear_CorrectCounts()
    {
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "a.jpg", Hash = "h1", DateTaken = new DateTime(2022, 1, 1) },
            new() { SourcePath = "b.jpg", Hash = "h2", DateTaken = new DateTime(2022, 6, 15) },
            new() { SourcePath = "c.jpg", Hash = "h3", DateTaken = new DateTime(2022, 12, 31) },
            new() { SourcePath = "d.jpg", Hash = "h4", DateTaken = new DateTime(2023, 3, 10) },
            new() { SourcePath = "e.jpg", Hash = "h5", DateTaken = new DateTime(2023, 9, 20) },
        };

        var stats = _sut.ComputeStats(photos);

        Assert.Equal(3, stats.PhotosByYear[2022]);
        Assert.Equal(2, stats.PhotosByYear[2023]);
    }

    [Fact]
    public void ComputeStats_PhotoWithNoDate_GoesIntoYear0()
    {
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "a.jpg", Hash = "h1", DateTaken = new DateTime(2021, 5, 5) },
            new() { SourcePath = "b.jpg", Hash = "h2", DateTaken = null },
        };

        var stats = _sut.ComputeStats(photos);

        Assert.Equal(1, stats.PhotosByYear[2021]);
        Assert.Equal(1, stats.PhotosByYear[0]);
        Assert.Equal(1, stats.PhotosWithDate);
    }

    [Fact]
    public void ComputeStats_DuplicateDetection_CountsAndSizesCorrect()
    {
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "a.jpg", Hash = "same", FileSize = 2_000_000 },
            new() { SourcePath = "b.jpg", Hash = "same", FileSize = 2_000_000 },
            new() { SourcePath = "c.jpg", Hash = "unique", FileSize = 1_000_000 },
        };

        var stats = _sut.ComputeStats(photos);

        Assert.Equal(1, stats.DuplicateCount);
        Assert.Equal(2_000_000, stats.DuplicateSizeBytes);
    }

    [Fact]
    public void ComputeStats_CameraGrouping_GroupsSameCameraAndUnknown()
    {
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "a.jpg", Hash = "h1", CameraMake = "Canon", CameraModel = "EOS R5" },
            new() { SourcePath = "b.jpg", Hash = "h2", CameraMake = "Canon", CameraModel = "EOS R5" },
            new() { SourcePath = "c.jpg", Hash = "h3", CameraMake = null, CameraModel = null },
        };

        var stats = _sut.ComputeStats(photos);

        Assert.Equal(2, stats.PhotosByCamera["Canon EOS R5"]);
        Assert.Equal(1, stats.PhotosByCamera["Unknown"]);
    }

    [Fact]
    public void ComputeStats_TotalSize_SumsAllFileSizes()
    {
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "a.jpg", Hash = "h1", FileSize = 1_000 },
            new() { SourcePath = "b.jpg", Hash = "h2", FileSize = 2_000 },
            new() { SourcePath = "c.jpg", Hash = "h3", FileSize = 3_000 },
        };

        var stats = _sut.ComputeStats(photos);

        Assert.Equal(6_000, stats.TotalSizeBytes);
    }

    [Fact]
    public void ComputeStats_MixedMetadataCoverage_CorrectCounts()
    {
        var photos = new List<PhotoMetadata>
        {
            new() { SourcePath = "a.jpg", Hash = "h1", DateTaken = new DateTime(2020, 1, 1), Location = new GpsCoordinates(1, 2), CameraMake = "Sony" },
            new() { SourcePath = "b.jpg", Hash = "h2", DateTaken = new DateTime(2020, 2, 1), Location = null, CameraMake = null },
            new() { SourcePath = "c.jpg", Hash = "h3", DateTaken = null, Location = null, CameraMake = null },
        };

        var stats = _sut.ComputeStats(photos);

        Assert.Equal(3, stats.TotalPhotos);
        Assert.Equal(2, stats.PhotosWithDate);
        Assert.Equal(1, stats.PhotosWithLocation);
        Assert.Equal(1, stats.PhotosWithCamera);
    }
}
