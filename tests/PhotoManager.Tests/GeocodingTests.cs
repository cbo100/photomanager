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

    private static string[] MakeTsvLines(params (string name, double lat, double lon, string country, long population)[] cities)
        => cities.Select(c =>
        {
            // GeoNames TSV: id(0) name(1) ascii(2) alt(3) lat(4) lon(5) fclass(6) fcode(7) country(8)
            //               cc2(9) admin1(10) admin2(11) admin3(12) admin4(13) population(14)
            var cols = new string[15];
            cols[0] = "1";
            cols[1] = c.name;
            cols[2] = c.name;
            cols[3] = "";
            cols[4] = c.lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            cols[5] = c.lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            cols[6] = "P";
            cols[7] = "PPL";
            cols[8] = c.country;
            cols[9] = "";
            cols[10] = "";
            cols[11] = "";
            cols[12] = "";
            cols[13] = "";
            cols[14] = c.population.ToString();
            return string.Join("\t", cols);
        }).ToArray();

    [Fact]
    public async Task ResolveAsync_ReturnsLargestCityWithin15km()
    {
        var dataPath = "/home/.photomanager/cities500.tsv";
        var lines = MakeTsvLines(
            ("Parramatta", -33.82, 151.00, "AU", 100_000),
            ("Sydney",     -33.87, 151.03, "AU", 5_000_000)); // ~5km from query, larger population

        var fileSystem = MakeFileSystem(dataPath, lines);
        var svc = new GeoNamesGeocodingService(fileSystem, "/home/.photomanager");

        // Query near Parramatta — both cities are within 15km, Sydney has larger population
        var result = await svc.ResolveAsync(new GpsCoordinates(-33.83, 151.02));

        Assert.Equal("Sydney, AU", result);
    }

    [Fact]
    public async Task ResolveAsync_FallsBackToNearestCity_WhenNoneWithin15km()
    {
        var dataPath = "/home/.photomanager/cities500.tsv";
        // City is ~65km from the query point — outside 15km radius
        var lines = MakeTsvLines(("FarAway", -33.87, 151.21, "AU", 1_000_000));
        var fileSystem = MakeFileSystem(dataPath, lines);
        var svc = new GeoNamesGeocodingService(fileSystem, "/home/.photomanager");

        var result = await svc.ResolveAsync(new GpsCoordinates(-33.42, 150.76));

        // Falls back to nearest city even though it's outside the 15km radius
        Assert.Equal("FarAway, AU", result);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsCachedResultOnSecondCall()
    {
        var dataPath = "/home/.photomanager/cities500.tsv";
        var lines = MakeTsvLines(("Sydney", -33.87, 151.21, "AU", 5_000_000));
        var fileSystem = MakeFileSystem(dataPath, lines);
        var svc = new GeoNamesGeocodingService(fileSystem, "/home/.photomanager");

        var first = await svc.ResolveAsync(new GpsCoordinates(-33.86, 151.20));
        var second = await svc.ResolveAsync(new GpsCoordinates(-33.86, 151.20));

        Assert.Equal(first, second);
        // ReadLines called once (data loaded once)
        fileSystem.File.Received(1).ReadLines(dataPath);
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
