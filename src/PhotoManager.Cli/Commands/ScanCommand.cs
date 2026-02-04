using System.ComponentModel;
using System.IO.Abstractions;
using PhotoManager.Core.Services;
using PhotoManager.Domain;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhotoManager.Cli.Commands;

public class ScanCommand : Command<ScanCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<source>")]
        [Description("Source directory to scan")]
        public required string SourcePath { get; init; }

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

        var extensions = settings.Extensions.Split(',', StringSplitOptions.RemoveEmptyEntries);

        AnsiConsole.MarkupLine($"[green]Scanning directory:[/] {settings.SourcePath}");
        AnsiConsole.WriteLine();

        List<PhotoMetadata>? photos = null;

        await AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
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

                task.Value = task.MaxValue;
            });

        if (photos == null || photos.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No photos found![/]");
            return 1;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Found {photos.Count} photos[/]");
        AnsiConsole.WriteLine();

        // Display summary table
        var table = new Table();
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("Total Photos", photos.Count.ToString());
        table.AddRow("Total Size", FormatBytes(photos.Sum(p => p.FileSize)));
        table.AddRow("With Date", photos.Count(p => p.DateTaken.HasValue).ToString());
        table.AddRow("With Location", photos.Count(p => p.Location != null).ToString());
        table.AddRow("With Camera Info", photos.Count(p => !string.IsNullOrEmpty(p.CameraMake)).ToString());

        AnsiConsole.Write(table);

        // Detect duplicates
        var organizer = new PhotoOrganizer(fileSystem);
        var duplicates = organizer.DetectDuplicates(photos);

        if (duplicates.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]Found {duplicates.Count} duplicate groups ({duplicates.Sum(d => d.Value.Count)} total files)[/]");
        }

        return 0;
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
