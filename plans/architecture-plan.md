# Photo Manager CLI/TUI - Architecture Plan

## Overview
A CLI/TUI-based photo manager for organizing image files into a structured layout based on date, location, and events. Built with C# / .NET 10, compiled as self-contained and AOT-compatible.

## Core Libraries

### CLI/TUI Framework
- **Spectre.Console** - Modern, feature-rich CLI/TUI with progress bars, tables, prompts, and interactive menus
- **System.CommandLine** - Microsoft's official CLI parsing library (or Spectre.Console.Cli)

### Image Metadata Extraction
- **MetadataExtractor** - Robust library for reading EXIF, IPTC, XMP metadata from images
- **SixLabors.ImageSharp** - Image processing, verification, and thumbnail generation if needed

### Geolocation (Optional)
- **ReverseGeocoding.NET** or custom offline solution for GPS coordinates → location names
- Consider embedded lightweight database for offline geocoding

### File Operations
- Built-in `System.IO` with `System.IO.Abstractions` for testability

## Project Architecture

```
PhotoManager/
├── PhotoManager.Cli/          # Entry point, CLI commands
├── PhotoManager.Core/         # Business logic, services
├── PhotoManager.Domain/       # Domain models, interfaces
└── PhotoManager.Tests/        # Unit tests
```

## Data Model

### Core Domain Entities

```csharp
// Photo metadata extracted from files
public record PhotoMetadata
{
    public string SourcePath { get; init; }
    public DateTime? DateTaken { get; init; }      // EXIF DateTimeOriginal
    public GpsCoordinates? Location { get; init; }
    public string? CameraMake { get; init; }
    public string? CameraModel { get; init; }
    public string Hash { get; init; }              // For duplicate detection
    public int Width { get; init; }
    public int Height { get; init; }
}

// GPS coordinates from EXIF
public record GpsCoordinates(double Latitude, double Longitude);

// Organization rules and patterns
public record OrganizationRule
{
    public string Pattern { get; init; }           // e.g., "{Year}/{Month}/{Year}-{Month}-{Day}"
    public bool UseLocation { get; init; }
    public bool UseEvent { get; init; }
}

// Planned file operations
public record PhotoOperation
{
    public string SourcePath { get; init; }
    public string DestinationPath { get; init; }
    public OperationType Type { get; init; }       // Move, Copy, Symlink
}

public enum OperationType
{
    Copy,
    Move,
    Symlink
}
```

## Organization Strategies

### 1. Date-Based Organization
```
/organized/
  2024/
    01-January/
      2024-01-15_IMG001.jpg
      2024-01-15_IMG002.jpg
    02-February/
      2024-02-10_IMG003.jpg
```

### 2. Location-Based
```
/organized/
  Europe/
    Italy/
      Rome/
        2024-01-15_IMG001.jpg
```

### 3. Hybrid (Date + Location)
```
/organized/
  2024/
    2024-01-Italy-Rome/
      2024-01-15_IMG001.jpg
    2024-02-USA-NewYork/
      2024-02-10_IMG002.jpg
```

### 4. Event-Based
- Manual tagging via interactive TUI
- Auto-clustering by date proximity (photos within 3 hours = same event)
```
/organized/
  2024-01-Rome-Trip/
    2024-01-15_IMG001.jpg
  2024-02-Birthday-Party/
    2024-02-10_IMG002.jpg
```

## Core Features

### Phase 1 - MVP
1. **Scan source folder** recursively for image files
2. **Extract EXIF metadata** (date taken, location, camera info)
3. **Detect duplicates** via hash comparison
4. **Preview organization plan** (dry-run mode)
5. **Execute organization** (copy/move files)
6. **Progress visualization** with Spectre.Console

### Phase 2 - Advanced
1. **Interactive event tagging** via TUI
2. **Reverse geocoding** for GPS coordinates → location names
3. **Conflict resolution** for filename collisions
4. **Undo capability** via operation log
5. **Watch mode** for auto-organizing new files
6. **Statistics and reports** (total photos, by date, by camera, etc.)

### Phase 3 - Polish
1. **Smart event detection** using date/time clustering
2. **Perceptual hashing** for finding similar images
3. **Batch renaming** with custom patterns
4. **Export functionality** (create albums, web gallery metadata)

## Configuration

### Configuration File (appsettings.json or CLI params)

```json
{
  "sourceFolder": "/path/to/photos",
  "destinationFolder": "/path/to/organized",
  "organizationPattern": "{Year}/{Month}/{Day}",
  "operationType": "Copy",
  "handleDuplicates": "Skip",
  "fileExtensions": [".jpg", ".jpeg", ".png", ".heic", ".raw", ".cr2", ".nef"],
  "preserveOriginalDate": true,
  "hashAlgorithm": "SHA256",
  "parallelProcessing": true,
  "maxDegreeOfParallelism": 4
}
```

### Configuration Options

- **sourceFolder**: Directory to scan for photos
- **destinationFolder**: Where organized photos will be placed
- **organizationPattern**: Template for folder structure
  - Available tokens: `{Year}`, `{Month}`, `{MonthName}`, `{Day}`, `{Location}`, `{Event}`, `{Camera}`
- **operationType**: `Copy`, `Move`, or `Symlink`
- **handleDuplicates**: `Skip`, `Rename`, `Overwrite`, `Interactive`
- **fileExtensions**: List of supported image file extensions
- **preserveOriginalDate**: Maintain file creation/modification dates

## Technical Considerations

### AOT Compatibility
- ✅ Avoid heavy reflection (use source generators where possible)
- ✅ Test with `PublishAot` and `EnableTrimming` flags
- ⚠️ MetadataExtractor and ImageSharp have some AOT limitations
  - May need trimming warnings configuration
  - Test thoroughly with AOT compilation

### Performance Optimizations
- **Parallel processing** for metadata extraction (use `Parallel.ForEachAsync`)
- **Incremental hashing** for large files (streaming)
- **Memory-efficient** file operations (avoid loading entire files into memory)
- **Progress reporting** without blocking main thread

### Data Integrity
- ✅ No embedded databases in photo directories
- ✅ All metadata processed in-memory during operation
- ✅ Optional operation log in separate `.photomanager` folder
- ✅ No modification of original files (metadata stored externally if needed)

### Duplicate Detection
- **SHA256 hashing** for exact duplicates
- **Optional perceptual hashing** (pHash) for similar images
- **Comparison strategy**: Hash → file size → byte-by-byte if needed

## CLI Commands Structure

### Basic Commands

```bash
# Scan and preview organization
photomanager scan <source> [options]

# Organize photos
photomanager organize <source> <dest> [options]
  --pattern "{Year}/{Month}"
  --mode copy|move|symlink
  --dry-run
  --skip-duplicates

# Preview without executing
photomanager preview <source> <dest> [options]

# Interactive event tagging
photomanager events <dest>

# Show statistics
photomanager stats <dest>

# Verify organized structure
photomanager verify <dest>
```

### Example Usage

```bash
# Preview organization by year/month
photomanager preview ~/Pictures/Unsorted ~/Pictures/Organized --pattern "{Year}/{Month}"

# Execute organization (copy mode)
photomanager organize ~/Pictures/Unsorted ~/Pictures/Organized --mode copy

# Organize with custom pattern
photomanager organize ~/Pictures/Unsorted ~/Pictures/Organized \
  --pattern "{Year}/{Location}/{Event}" \
  --mode move

# Interactive mode for event tagging
photomanager events ~/Pictures/Organized
```

## Implementation Roadmap

### Milestone 1: Core Infrastructure (Week 1)
- [ ] Set up .NET 10 project structure
- [ ] Configure NuGet packages
- [ ] Implement basic CLI with System.CommandLine
- [ ] Set up unit testing framework

### Milestone 2: Metadata Extraction (Week 2)
- [ ] Implement EXIF reader using MetadataExtractor
- [ ] Extract date, location, camera info
- [ ] Handle missing/corrupt metadata gracefully
- [ ] Implement file hashing for duplicates

### Milestone 3: Organization Engine (Week 3)
- [ ] Design organization pattern parser
- [ ] Implement file operation planner
- [ ] Create dry-run preview functionality
- [ ] Implement copy/move/symlink operations

### Milestone 4: CLI/TUI (Week 4)
- [ ] Integrate Spectre.Console for rich UI
- [ ] Add progress bars and status updates
- [ ] Implement interactive prompts
- [ ] Create statistics dashboard

### Milestone 5: Polish & Testing (Week 5)
- [ ] Comprehensive unit tests
- [ ] Integration tests with sample photos
- [ ] AOT compilation testing
- [ ] Performance benchmarking
- [ ] Documentation

## Success Criteria

1. ✅ **Zero runtime dependencies** (self-contained executable)
2. ✅ **No artifacts in photo directories** (except organized photos)
3. ✅ **Fast performance** (process 1000+ photos efficiently)
4. ✅ **Robust metadata handling** (graceful degradation)
5. ✅ **User-friendly** CLI/TUI interface
6. ✅ **Safe operations** (preview before execution, no data loss)

## Open Questions

1. Should we support video files in addition to photos?
2. How to handle RAW + JPG pairs from cameras?
3. Should we support custom Lua/C# scripts for organization patterns?
4. Cloud storage integration (Google Photos, iCloud)?
5. Face detection for person-based organization?

## Notes

- Keep the tool focused on local file organization
- Avoid feature creep - start with solid MVP
- Prioritize data safety and user trust
- Consider cross-platform compatibility (Windows, macOS, Linux)
