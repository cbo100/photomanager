using System.IO.Abstractions;
using PhotoManager.Cli.Parsing;
using PhotoManager.Core.Services;
using PhotoManager.Domain;
using Spectre.Console;
using ValidationResult = PhotoManager.Cli.Parsing.ValidationResult;

namespace PhotoManager.Cli.Commands;

public class OrganizeCommand
{
    private static readonly IReadOnlySet<string> FlagNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "dry-run", "skip-duplicates", "yes", "y", "overwrite",
    };

    public class Settings
    {
        public required string SourcePath { get; init; }
        public string? DestinationPath { get; init; }
        public string Pattern { get; init; } = "{Year}/{Month}";
        public string Mode { get; init; } = "copy";
        public bool DryRun { get; init; }
        public bool SkipDuplicates { get; init; }
        public string Extensions { get; init; } = ".jpg,.jpeg,.png,.heic,.raw,.cr2,.nef";
        public bool Yes { get; init; }
        public bool Overwrite { get; init; }

        public ValidationResult Validate()
        {
            if (DestinationPath == null && !Mode.Equals("move", StringComparison.OrdinalIgnoreCase))
                return ValidationResult.Error("A destination folder must be specified when using copy or symlink mode. Use --mode move for in-place organisation.");

            return ValidationResult.Success();
        }

        public static (Settings? Value, string? Error) TryParse(string[] args)
        {
            var parsed = ArgParser.Parse(args, FlagNames);

            if (parsed.HelpRequested)
                return (null, null);

            if (parsed.Positionals.Count == 0)
                return (null, "Missing required argument: <source>");

            var settings = new Settings
            {
                SourcePath = parsed.Positionals[0],
                DestinationPath = parsed.Positionals.Count > 1 ? parsed.Positionals[1] : null,
                Pattern = parsed.GetOptionOrDefault("pattern", "{Year}/{Month}"),
                Mode = parsed.GetOptionOrDefault("mode", "copy"),
                DryRun = parsed.HasFlag("dry-run"),
                SkipDuplicates = parsed.HasFlag("skip-duplicates"),
                Extensions = parsed.GetOptionOrDefault("extensions", ".jpg,.jpeg,.png,.heic,.raw,.cr2,.nef"),
                Yes = parsed.HasFlag("yes") || parsed.HasFlag("y"),
                Overwrite = parsed.HasFlag("overwrite"),
            };

            var validation = settings.Validate();
            return validation.Successful
                ? (settings, null)
                : (null, validation.ErrorMessage);
        }
    }

    public static void PrintHelp()
    {
        AnsiConsole.MarkupLine("[bold]photomanager organize[/] [grey]<source> [destination][/] [grey][options][/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[underline]Arguments:[/]");
        AnsiConsole.MarkupLine("  [green]<source>[/]              Source directory to scan");
        AnsiConsole.MarkupLine("  [green][destination][/]         Destination directory [grey](defaults to source for in-place organisation)[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[underline]Options:[/]");
        AnsiConsole.MarkupLine("  [green]--pattern[/]             Organisation pattern [grey](default: {Year}/{Month})[/]");
        AnsiConsole.MarkupLine("  [green]--mode[/]                Operation mode: copy, move, or symlink [grey](default: copy)[/]");
        AnsiConsole.MarkupLine("  [green]--extensions[/]          Comma-separated file extensions [grey](default: .jpg,.jpeg,.png,.heic,.raw,.cr2,.nef)[/]");
        AnsiConsole.MarkupLine("  [green]--dry-run[/]             Preview without executing");
        AnsiConsole.MarkupLine("  [green]--skip-duplicates[/]     Skip duplicate files");
        AnsiConsole.MarkupLine("  [green]--overwrite[/]           Overwrite files that already exist at the destination");
        AnsiConsole.MarkupLine("  [green]-y, --yes[/]             Skip confirmation prompt and execute immediately");
        AnsiConsole.MarkupLine("  [green]-h, --help[/]            Show this help");
    }

    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var (settings, error) = Settings.TryParse(args);

        if (settings == null && error == null) // --help requested
        {
            PrintHelp();
            return 0;
        }

        if (error != null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {error}");
            PrintHelp();
            return 1;
        }

        return await new OrganizeCommand().ExecuteAsync(settings!, cancellationToken);
    }

    private async Task<int> ExecuteAsync(Settings settings, CancellationToken cancellationToken)
    {
        var fileSystem = new FileSystem();
        var metadataExtractor = new MetadataExtractorService(fileSystem);
        var scanner = new PhotoScanner(fileSystem, metadataExtractor);
        var organizer = new PhotoOrganizer(fileSystem);
        var geocoder = new GeoNamesGeocodingService(fileSystem);

        var extensions = settings.Extensions.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var operationType = settings.Mode.ToLowerInvariant() switch
        {
            "move" => OperationType.Move,
            "symlink" => OperationType.Symlink,
            _ => OperationType.Copy
        };

        AnsiConsole.MarkupLine($"[green]Scanning directory:[/] {settings.SourcePath}");

        var destination = settings.DestinationPath ?? settings.SourcePath;

        List<PhotoMetadata>? photos = null;

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[yellow]Scanning files[/]");

                var progress = new Progress<ScanProgress>(p =>
                {
                    task.MaxValue = p.TotalFiles;
                    task.Value = p.FilesProcessed;
                    task.Description = $"[yellow]Scanning:[/] {Path.GetFileName(p.CurrentFile)}";
                });

                photos = await scanner.ScanDirectoryAsync(
                    settings.SourcePath,
                    extensions,
                    progress);
            });

        if (photos == null || photos.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No photos found![/]");
            return 1;
        }

        // Check for duplicates
        if (settings.SkipDuplicates)
        {
            var duplicates = organizer.DetectDuplicates(photos);
            if (duplicates.Any())
            {
                var duplicateFiles = duplicates.SelectMany(d => d.Value.Skip(1)).ToList();
                photos = photos.Except(duplicateFiles).ToList();
                AnsiConsole.MarkupLine($"[yellow]Skipping {duplicateFiles.Count} duplicate files[/]");
            }
        }

        // Resolve GPS coordinates to location names when {Location} pattern is used
        if (settings.Pattern.Contains("{Location}", StringComparison.OrdinalIgnoreCase))
        {
            var photosWithLocation = photos.Where(p => p.Location != null).ToList();
            if (photosWithLocation.Count > 0)
            {
                await AnsiConsole.Status()
                    .StartAsync("Resolving locations...", async _ =>
                    {
                        await geocoder.EnsureDataAvailableAsync(cancellationToken);

                        var resolved = new Dictionary<PhotoMetadata, PhotoMetadata>();
                        foreach (var photo in photosWithLocation)
                        {
                            var name = await geocoder.ResolveAsync(photo.Location!, cancellationToken);
                            if (name != null)
                                resolved[photo] = photo with { LocationName = name };
                        }

                        photos = photos
                            .Select(p => resolved.TryGetValue(p, out var r) ? r : p)
                            .ToList();
                    });
            }
        }

        var config = new PhotoManagerConfig
        {
            SourceFolder = settings.SourcePath,
            DestinationFolder = destination,
            OrganizationPattern = settings.Pattern,
            OperationType = operationType,
            OverwriteExisting = settings.Overwrite
        };

        var operations = organizer.PlanOrganization(photos, config);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Planning {operations.Count} operations ({settings.Mode} mode)[/]");

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[yellow]DRY RUN - No files will be modified[/]");
            AnsiConsole.WriteLine();
            PrintPreview(operations, settings.SourcePath, destination);
            return 0;
        }

        if (operations.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Nothing to do — all files already exist at the destination.[/]");
            return 0;
        }

        PrintPreview(operations, settings.SourcePath, destination);

        if (!settings.Yes && !AnsiConsole.Confirm($"Execute {operations.Count} {settings.Mode} operations?"))
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
            return 0;
        }

        AnsiConsole.WriteLine();

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[green]{char.ToUpper(settings.Mode[0]) + settings.Mode[1..]} files[/]");

                var progress = new Progress<OperationProgress>(p =>
                {
                    task.MaxValue = p.TotalOperations;
                    task.Value = p.OperationsCompleted;
                    task.Description = $"[green]Processing:[/] {Path.GetFileName(p.CurrentFile)}";
                });

                await organizer.ExecuteOperationsAsync(operations, progress);
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]✓ Successfully organized {operations.Count} photos![/]");

        if (operationType == OperationType.Move)
        {
            var removed = organizer.CleanEmptyDirectories(settings.SourcePath);
            if (removed > 0)
                AnsiConsole.MarkupLine($"[dim]Removed {removed} empty director{(removed == 1 ? "y" : "ies")} from source.[/]");
        }

        return 0;
    }

    private static void PrintPreview(List<PhotoOperation> operations, string source, string destination)
    {
        const int previewSize = 10;

        var table = new Table();
        table.AddColumn("Source");
        table.AddColumn("Destination");

        foreach (var op in operations.Take(previewSize))
        {
            table.AddRow(
                Path.DirectorySeparatorChar + Path.GetRelativePath(source, op.SourcePath),
                op.DestinationPath.Replace(destination, ""));
        }

        AnsiConsole.Write(table);

        if (operations.Count > previewSize)
            AnsiConsole.MarkupLine($"[dim]... and {operations.Count - previewSize} more operations[/]");

        AnsiConsole.WriteLine();
    }
}
