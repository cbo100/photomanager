using PhotoManager.Domain;

namespace PhotoManager.Core.Services;

public interface IGeocodingService
{
    /// <summary>
    /// Resolves GPS coordinates to a human-readable location name (e.g. "Sydney, AU").
    /// Returns null if the data is unavailable or no nearby city is found.
    /// </summary>
    Task<string?> ResolveAsync(GpsCoordinates coordinates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the GeoNames city data is downloaded and ready.
    /// Safe to call multiple times; only downloads once.
    /// </summary>
    Task EnsureDataAvailableAsync(CancellationToken cancellationToken = default);
}
