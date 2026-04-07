using System.IO.Abstractions;
using PhotoManager.Core.Services;
using PhotoManager.Domain;
using NSubstitute;

namespace PhotoManager.Tests;

public class GeocodingTests
{
    private static IFileSystem MakeFileSystem(string dataPath, string[]? lines = null)
    {
        var fileSystem = Substitute.For<IFileSystem>();

        fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            System.IO.Path.Combine(callInfo.ArgAt<string>(0), callInfo.ArgAt<string>(1)));
        fileSystem.Path.GetDirectoryName(Arg.Any<string>()).Returns(callInfo =>
            System.IO.Path.GetDirectoryName(callInfo.Arg<string>()));

        fileSystem.File.Exists(dataPath).Returns(lines != null);
        fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);

        if (lines != null)
            fileSystem.File.ReadLines(dataPath).Returns(lines);

        return fileSystem;
    }

    private static string[] MakeTsvLines(params (string name, double lat, double lon, string country)[] cities)
        => cities.Select(c =>
        {
            // GeoNames TSV: geonameid(0) name(1) asciiname(2) alt(3) lat(4) lon(5) ... country(8)
            var cols = new string[9];
            cols[0] = "1";
            cols[1] = c.name;
            cols[2] = c.name;
            cols[3] = "";
            cols[4] = c.lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            cols[5] = c.lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            cols[6] = "P";
            cols[7] = "PPL";
            cols[8] = c.country;
            return string.Join("\t", cols);
        }).ToArray();

    [Fact]
    public async Task ResolveAsync_ReturnsNearestCity()
    {
        var dataPath = "/home/.photomanager/cities500.tsv";
        var lines = MakeTsvLines(
            ("Sydney", -33.87, 151.21, "AU"),
            ("Melbourne", -37.81, 144.96, "AU"));

        var fileSystem = MakeFileSystem(dataPath, lines);
        var svc = new GeoNamesGeocodingService(fileSystem, "/home/.photomanager");

        var result = await svc.ResolveAsync(new GpsCoordinates(-33.86, 151.20));

        Assert.Equal("Sydney, AU", result);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsCachedResultOnSecondCall()
    {
        var dataPath = "/home/.photomanager/cities500.tsv";
        var lines = MakeTsvLines(("Sydney", -33.87, 151.21, "AU"));
        var fileSystem = MakeFileSystem(dataPath, lines);
        var svc = new GeoNamesGeocodingService(fileSystem, "/home/.photomanager");

        var first = await svc.ResolveAsync(new GpsCoordinates(-33.86, 151.20));
        var second = await svc.ResolveAsync(new GpsCoordinates(-33.86, 151.20));

        Assert.Equal(first, second);
        // ReadLines called once (data loaded once)
        fileSystem.File.Received(1).ReadLines(dataPath);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNull_WhenNoCitiesInBand()
    {
        var dataPath = "/home/.photomanager/cities500.tsv";
        // City is far from the query point
        var lines = MakeTsvLines(("Reykjavik", 64.13, -21.89, "IS"));
        var fileSystem = MakeFileSystem(dataPath, lines);
        var svc = new GeoNamesGeocodingService(fileSystem, "/home/.photomanager");

        // Query near the equator — >1° band away from Reykjavik
        var result = await svc.ResolveAsync(new GpsCoordinates(0.0, 0.0));

        Assert.Null(result);
    }

    [Fact]
    public void PlanOrganization_WithLocationName_ShouldUseCityInPath()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Path.GetFileName(Arg.Any<string>()).Returns(callInfo =>
            System.IO.Path.GetFileName(callInfo.Arg<string>()));
        fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            System.IO.Path.Combine(callInfo.ArgAt<string>(0), callInfo.ArgAt<string>(1), callInfo.ArgAt<string>(2)));
        fileSystem.File.Exists(Arg.Any<string>()).Returns(false);

        var organizer = new PhotoOrganizer(fileSystem);

        var photos = new List<PhotoMetadata>
        {
            new PhotoMetadata
            {
                SourcePath = "/source/photo.jpg",
                Hash = "ABC123",
                FileName = "photo.jpg",
                DateTaken = new DateTime(2024, 6, 1),
                Location = new GpsCoordinates(-33.87, 151.21),
                LocationName = "Sydney, AU"
            }
        };

        var config = new PhotoManagerConfig
        {
            SourceFolder = "/source",
            DestinationFolder = "/dest",
            OrganizationPattern = "{Year}/{Location}"
        };

        var operations = organizer.PlanOrganization(photos, config);

        Assert.Single(operations);
        Assert.Contains("Sydney, AU", operations[0].DestinationPath);
    }

    [Fact]
    public void PlanOrganization_WithLocationButNoName_ShouldFallBackToCoordinates()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Path.GetFileName(Arg.Any<string>()).Returns(callInfo =>
            System.IO.Path.GetFileName(callInfo.Arg<string>()));
        fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            System.IO.Path.Combine(callInfo.ArgAt<string>(0), callInfo.ArgAt<string>(1), callInfo.ArgAt<string>(2)));
        fileSystem.File.Exists(Arg.Any<string>()).Returns(false);

        var organizer = new PhotoOrganizer(fileSystem);

        var photos = new List<PhotoMetadata>
        {
            new PhotoMetadata
            {
                SourcePath = "/source/photo.jpg",
                Hash = "ABC123",
                FileName = "photo.jpg",
                DateTaken = new DateTime(2024, 6, 1),
                Location = new GpsCoordinates(-33.87, 151.21),
                LocationName = null
            }
        };

        var config = new PhotoManagerConfig
        {
            SourceFolder = "/source",
            DestinationFolder = "/dest",
            OrganizationPattern = "{Year}/{Location}"
        };

        var operations = organizer.PlanOrganization(photos, config);

        Assert.Single(operations);
        Assert.Contains("-33.87", operations[0].DestinationPath);
        Assert.Contains("151.21", operations[0].DestinationPath);
    }
}
