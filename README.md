# Photo Manager CLI/TUI

A command-line photo organizer that automatically organizes your photos based on EXIF metadata, dates, and customizable patterns. Built with .NET 10 and optimized for performance.

## Requirements

- **.NET 10 SDK** or later
- Linux, macOS, or Windows

## Features (Phase 1 - MVP)

✅ **Scan source folder** recursively for image files  
✅ **Extract EXIF metadata** (date taken, location, camera info)  
✅ **Detect duplicates** via SHA256 hash comparison  
✅ **Preview organization plan** (dry-run mode)  
✅ **Execute organization** (copy/move/symlink files)  
✅ **Progress visualization** with Spectre.Console  

## Quick Start

### Build

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Run Commands

```bash
# Scan a directory and show photo metadata
dotnet run --project src/PhotoManager.Cli scan <source-directory>

# Preview organization plan (dry-run)
dotnet run --project src/PhotoManager.Cli preview <source> <destination> --pattern "{Year}/{Month}"

# Organize photos (copy mode)
dotnet run --project src/PhotoManager.Cli organize <source> <destination> --pattern "{Year}/{Month}"

# Organize photos (move mode)
dotnet run --project src/PhotoManager.Cli organize <source> <destination> --mode move

# Dry-run with duplicate detection
dotnet run --project src/PhotoManager.Cli organize <source> <destination> --dry-run --skip-duplicates
```

## Available Commands

### `scan`
Scans a directory for photos and displays metadata summary.

**Usage:** `photomanager scan <source> [options]`

**Options:**
- `--extensions` - File extensions to scan (default: .jpg,.jpeg,.png,.heic,.raw,.cr2,.nef)

### `preview`
Previews the organization plan without executing it.

**Usage:** `photomanager preview <source> <destination> [options]`

**Options:**
- `--pattern` - Organization pattern (default: {Year}/{Month})
- `--extensions` - File extensions to scan

### `organize`
Organizes photos from source to destination folder.

**Usage:** `photomanager organize <source> <destination> [options]`

**Options:**
- `--pattern` - Organization pattern (default: {Year}/{Month})
- `--mode` - Operation mode: copy, move, or symlink (default: copy)
- `--dry-run` - Preview without executing
- `--skip-duplicates` - Skip duplicate files
- `--extensions` - File extensions to scan

## Organization Patterns

Available tokens for the `--pattern` option:
- `{Year}` - Four-digit year (e.g., 2024)
- `{Month}` - Two-digit month (e.g., 01)
- `{MonthName}` - Month name (e.g., January)
- `{Day}` - Two-digit day (e.g., 15)
- `{Location}` - GPS coordinates (future: location names)
- `{Camera}` - Camera make

**Examples:**
- `{Year}/{Month}` → 2024/01/
- `{Year}/{MonthName}` → 2024/January/
- `{Year}/{Month}/{Day}` → 2024/01/15/
- `{Camera}/{Year}` → Canon/2024/

## Project Structure

```text
PhotoManager/
├── src/
│   ├── PhotoManager.Cli/          # CLI application
│   ├── PhotoManager.Core/         # Business logic
│   └── PhotoManager.Domain/       # Domain models
├── tests/
│   └── PhotoManager.Tests/        # Unit tests
└── plans/
    └── architecture-plan.md       # Architecture documentation
```

## Technologies

- **.NET 10.0** - Latest .NET release
- **C# Latest** - Using latest C# language features
- **Spectre.Console.Cli** - Modern CLI framework
- **MetadataExtractor** - EXIF/IPTC/XMP metadata reading
- **System.IO.Abstractions** - Testable file system operations
- **xUnit + NSubstitute** - Testing
- **AOT Ready** - Prepared for native AOT compilation

## Coming Soon (Phase 2 & 3)

- Interactive event tagging via TUI
- Reverse geocoding for GPS coordinates
- Smart event detection using date/time clustering
- Undo capability via operation log
- Watch mode for auto-organizing new files
- Statistics and reports

## License

MIT
