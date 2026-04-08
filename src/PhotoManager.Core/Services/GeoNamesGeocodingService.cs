using System.IO.Compression;
using System.IO.Abstractions;
using PhotoManager.Domain;

namespace PhotoManager.Core.Services;

public class GeoNamesGeocodingService : IGeocodingService
{
    private const string DownloadUrl = "https://download.geonames.org/export/dump/cities500.zip";
    private const string ZipEntryName = "cities500.txt";

    private readonly IFileSystem _fileSystem;
    private readonly string _dataPath;
    private readonly Dictionary<(double Lat, double Lon), string> _cache = new();

    private List<CityEntry>? _cities;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public GeoNamesGeocodingService(IFileSystem fileSystem, string? dataDirectory = null)
    {
        _fileSystem = fileSystem;
        var dir = dataDirectory ?? _fileSystem.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".photomanager");
        _dataPath = _fileSystem.Path.Combine(dir, "cities500.tsv");
    }

    public async Task EnsureDataAvailableAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cities != null) return;

            var dir = _fileSystem.Path.GetDirectoryName(_dataPath)!;
            if (!_fileSystem.Directory.Exists(dir))
                _fileSystem.Directory.CreateDirectory(dir);

            if (!_fileSystem.File.Exists(_dataPath))
                await DownloadAndExtractAsync(cancellationToken);

            _cities = LoadCities(_dataPath);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string?> ResolveAsync(GpsCoordinates coordinates, CancellationToken cancellationToken = default)
    {
        await EnsureDataAvailableAsync(cancellationToken);

        if (_cities == null || _cities.Count == 0)
            return null;

        // Round to ~1km grid for cache key
        var cacheKey = (Math.Round(coordinates.Latitude, 2), Math.Round(coordinates.Longitude, 2));
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        var nearest = FindBestCity(coordinates.Latitude, coordinates.Longitude);
        if (nearest == null) return null;

        var result = $"{nearest.Name}, {nearest.CountryCode}";
        _cache[cacheKey] = result;
        return result;
    }

    private CityEntry? FindBestCity(double lat, double lon)
    {
        const double radiusKm = 15.0;
        const double bandDegrees = 1.0; // ~110km pre-filter

        var latMin = lat - bandDegrees;
        var latMax = lat + bandDegrees;

        // Binary search for the start of the latitude band
        int lo = 0, hi = _cities!.Count - 1;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (_cities[mid].Latitude < latMin) lo = mid + 1;
            else hi = mid;
        }

        CityEntry? bestWithinRadius = null;
        CityEntry? nearest = null;
        double minDist = double.MaxValue;

        for (var i = lo; i < _cities.Count && _cities[i].Latitude <= latMax; i++)
        {
            var dist = Haversine(lat, lon, _cities[i].Latitude, _cities[i].Longitude);

            if (dist <= radiusKm && (bestWithinRadius == null || _cities[i].Population > bestWithinRadius.Population))
                bestWithinRadius = _cities[i];

            if (dist < minDist)
            {
                minDist = dist;
                nearest = _cities[i];
            }
        }

        return bestWithinRadius ?? nearest;
    }

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;

    private List<CityEntry> LoadCities(string path)
    {
        var cities = new List<CityEntry>(200_000);

        foreach (var line in _fileSystem.File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split('\t');
            if (parts.Length < 15) continue;
            if (!double.TryParse(parts[4], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lat)) continue;
            if (!double.TryParse(parts[5], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lon)) continue;
            long.TryParse(parts[14], out var population);

            cities.Add(new CityEntry(parts[1], lat, lon, parts[8], population));
        }

        cities.Sort((a, b) => a.Latitude.CompareTo(b.Latitude));
        return cities;
    }

    private async Task DownloadAndExtractAsync(CancellationToken cancellationToken)
    {
        using var http = new HttpClient();
        using var response = await http.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

        var entry = zip.GetEntry(ZipEntryName)
            ?? throw new InvalidOperationException($"Expected '{ZipEntryName}' in GeoNames zip.");

        await using var entryStream = entry.Open();
        await using var fileStream = _fileSystem.File.Create(_dataPath);
        await entryStream.CopyToAsync(fileStream, cancellationToken);
    }

    private record CityEntry(string Name, double Latitude, double Longitude, string CountryCode, long Population);
}
