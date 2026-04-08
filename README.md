# Photo Manager CLI

A command-line photo organiser that automatically organises your photos based on EXIF metadata, dates, GPS location, and customisable patterns. Built with .NET 10.

[![CI](https://github.com/cbo100/photomanager/actions/workflows/ci.yml/badge.svg)](https://github.com/cbo100/photomanager/actions/workflows/ci.yml)

## Requirements

- **.NET 10 SDK** or later
- Linux, macOS, or Windows

## Features

- **Scan** a source folder recursively for image files
- **Extract EXIF metadata** — date taken, GPS location, camera info
- **Reverse geocoding** — resolves GPS coordinates to city names offline using the GeoNames dataset (downloaded on first use to `~/.photomanager/`)
- **Detect duplicates** via SHA256 hash comparison
- **Organise** photos by copy, move, or symlink with customisable folder patterns
- **In-place organisation** — omit the destination to organise a folder in place (move mode only)
- **Skip existing files** by default; opt in to overwrite with `--overwrite`
- **Empty folder cleanup** — automatically removes empty directories after a move
- **Dry-run preview** — shows up to 10 planned operations before committing
- **Headless/CI support** via `-y`/`--yes` to skip confirmation
- **Progress visualisation** with Spectre.Console

## Quick Start

### Build

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Run

```bash
# Scan a directory and show photo metadata
dotnet run --project src/PhotoManager.Cli -- scan <source>

# Preview organisation plan (no files changed)
dotnet run --project src/PhotoManager.Cli -- organise <source> <destination> --dry-run

# Organise by year/month (copy mode)
dotnet run --project src/PhotoManager.Cli -- organise <source> <destination>

# Organise in-place by move (no destination needed)
dotnet run --project src/PhotoManager.Cli -- organise <source> --mode move

# Organise by city name from GPS
dotnet run --project src/PhotoManager.Cli -- organise <source> <destination> --pattern "{Location}/{Year}"

# Skip confirmation (headless/CI)
dotnet run --project src/PhotoManager.Cli -- organise <source> <destination> --yes
```

## Commands

### `scan`

Scans a directory for photos and displays a metadata summary.

```
photomanager scan <source> [options]
```

| Option | Default | Description |
|---|---|---|
| `--extensions` | `.jpg,.jpeg,.png,.heic,.raw,.cr2,.nef` | File extensions to scan |

---

### `organise` (alias: `organize`)

Organises photos from source to destination.

```
photomanager organise <source> [destination] [options]
```

Destination is optional — omit it to organise in place (requires `--mode move`).

| Option | Default | Description |
|---|---|---|
| `--pattern` | `{Year}/{Month}` | Folder pattern (see tokens below) |
| `--mode` | `copy` | `copy`, `move`, or `symlink` |
| `--dry-run` | | Preview without executing |
| `--skip-duplicates` | | Skip files with duplicate SHA256 hash |
| `--overwrite` | | Overwrite files already at destination |
| `--extensions` | `.jpg,.jpeg,.png,.heic,.raw,.cr2,.nef` | File extensions to scan |
| `-y`, `--yes` | | Skip confirmation prompt |

## Organisation Patterns

| Token | Example output | Description |
|---|---|---|
| `{Year}` | `2024` | Four-digit year |
| `{Month}` | `06` | Two-digit month |
| `{MonthName}` | `June` | Full month name |
| `{Day}` | `15` | Two-digit day |
| `{Location}` | `Sydney, AU` | City name from GPS (falls back to coordinates) |
| `{Camera}` | `Apple` | Camera make from EXIF |

**Examples:**

```
{Year}/{Month}            → 2024/06/photo.jpg
{Year}/{MonthName}        → 2024/June/photo.jpg
{Year}/{Month}/{Day}      → 2024/06/15/photo.jpg
{Location}/{Year}         → Sydney, AU/2024/photo.jpg
{Camera}/{Year}/{Month}   → Apple/2024/06/photo.jpg
```

### Location resolution

When `{Location}` is used, GPS coordinates are resolved to a city name using the [GeoNames cities500 dataset](https://www.geonames.org/). The dataset is downloaded once to `~/.photomanager/cities500.tsv` (~50 MB). No API key or internet connection is required after the first run.

Resolution strategy: returns the **highest-population city within 15 km**; falls back to the nearest city if none is found within that radius. If no GPS data is present, falls back to `Unknown`.

## Project Structure

```
photomanager/
├── src/
│   ├── PhotoManager.Cli/       # CLI entry point and commands
│   ├── PhotoManager.Core/      # Business logic and services
│   └── PhotoManager.Domain/    # Domain models
├── tests/
│   └── PhotoManager.Tests/     # Unit tests
└── .github/workflows/ci.yml    # CI: build, test, format check
```

## Technologies

- **.NET 10** with C# latest features
- **Spectre.Console.Cli** — CLI framework
- **MetadataExtractor** — EXIF/IPTC/XMP metadata reading
- **System.IO.Abstractions** — testable file system
- **xUnit + NSubstitute** — testing
- **Roslynator + AsyncFixer** — static analysis

## License

MIT

