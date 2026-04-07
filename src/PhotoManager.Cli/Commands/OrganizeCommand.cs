using System.ComponentModel;
using System.IO.Abstractions;
using PhotoManager.Core.Services;
using PhotoManager.Domain;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhotoManager.Cli.Commands;

public class OrganizeCommand : Command<OrganizeCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<source>")]
        [Description("Source directory to scan")]
        public required string SourcePath { get; init; }

        [CommandArgument(1, "[destination]")]
        [Description("Destination directory for organized photos (defaults to source for in-place organisation)")]
        public string? DestinationPath { get; init; }

        [CommandOption("--pattern")]
        [Description("Organization pattern")]
        [DefaultValue("{Year}/{Month}")]
        public string Pattern { get; init; } = "{Year}/{Month}";

        [CommandOption("--mode")]
        [Description("Operation mode: copy, move, or symlink")]
        [DefaultValue("copy")]
        public string Mode { get; init; } = "copy";

        [CommandOption("--dry-run")]
        [Description("Preview without executing")]
        public bool DryRun { get; init; }

        [CommandOption("--skip-duplicates")]
        [Description("Skip duplicate files")]
        public bool SkipDuplicates { get; init; }

        [CommandOption("--extensions")]
        [Description("File extensions to scan (comma-separated)")]
        [DefaultValue(".jpg,.jpeg,.png,.heic,.raw,.cr2,.nef")]
        public string Extensions { get; init; } = ".jpg,.jpeg,.png,.heic,.raw,.cr2,.nef";

        [CommandOption("-y|--yes")]
        [Description("Skip confirmation prompt and execute immediately")]
        public bool Yes { get; init; }

        [CommandOption("--overwrite")]
        [Description("Overwrite files that already exist at the destination")]
        public bool Overwrite { get; init; }

        public override ValidationResult Validate()
        {
            if (DestinationPath == null && Mode.ToLowerInvariant() != "move")
                return ValidationResult.Error("A destination folder must be specified when using copy or symlink mode. Use --mode move for in-place organisation.");

            return ValidationResult.Success();
        }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var fileSystem = new FileSystem();
        var metadataExtractor = new MetadataExtractorService(fileSystem);
        var scanner = new PhotoScanner(fileSystem, metadataExtractor);
        var organizer = new PhotoOrganizer(fileSystem);

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
