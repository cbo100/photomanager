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

        [CommandArgument(1, "<destination>")]
        [Description("Destination directory for organized photos")]
        public required string DestinationPath { get; init; }

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
            DestinationFolder = settings.DestinationPath,
            OrganizationPattern = settings.Pattern,
            OperationType = operationType
        };

        var operations = organizer.PlanOrganization(photos, config);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Planning {operations.Count} operations ({settings.Mode} mode)[/]");

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[yellow]DRY RUN - No files will be modified[/]");
            AnsiConsole.WriteLine();

            var table = new Table();
            table.AddColumn("Source");
            table.AddColumn("Destination");

            foreach (var op in operations.Take(10))
            {
                table.AddRow(
                    Path.GetFileName(op.SourcePath),
                    op.DestinationPath.Replace(settings.DestinationPath, ""));
            }

            AnsiConsole.Write(table);

            if (operations.Count > 10)
            {
                AnsiConsole.MarkupLine($"[dim]... and {operations.Count - 10} more operations[/]");
            }

            return 0;
        }

        if (!AnsiConsole.Confirm($"Execute {operations.Count} {settings.Mode} operations?"))
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

        return 0;
    }
}
