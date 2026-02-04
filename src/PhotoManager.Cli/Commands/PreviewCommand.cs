using System.ComponentModel;
using System.IO.Abstractions;
using PhotoManager.Core.Services;
using PhotoManager.Domain;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhotoManager.Cli.Commands;

public class PreviewCommand : Command<PreviewCommand.Settings>
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

        var config = new PhotoManagerConfig
        {
            SourceFolder = settings.SourcePath,
            DestinationFolder = settings.DestinationPath,
            OrganizationPattern = settings.Pattern,
            OperationType = OperationType.Copy
        };

        var operations = organizer.PlanOrganization(photos, config);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Preview of {operations.Count} operations:[/]");
        AnsiConsole.WriteLine();

        // Show sample operations
        var sampleSize = Math.Min(10, operations.Count);
        var table = new Table();
        table.AddColumn("Source");
        table.AddColumn("Destination");

        foreach (var op in operations.Take(sampleSize))
        {
            table.AddRow(
                Path.GetFileName(op.SourcePath),
                op.DestinationPath.Replace(settings.DestinationPath, ""));
        }

        AnsiConsole.Write(table);

        if (operations.Count > sampleSize)
        {
            AnsiConsole.MarkupLine($"[dim]... and {operations.Count - sampleSize} more operations[/]");
        }

        return 0;
    }
}
