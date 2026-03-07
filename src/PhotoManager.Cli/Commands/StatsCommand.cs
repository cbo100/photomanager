using System.ComponentModel;
using System.IO.Abstractions;
using PhotoManager.Core.Services;
using PhotoManager.Domain;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace PhotoManager.Cli.Commands;

public class StatsCommand : AsyncCommand<StatsCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<source>")]
        [Description("Source directory to scan")]
        public required string Source { get; init; }

        [CommandOption("--extensions")]
        [Description("File extensions to scan (comma-separated)")]
        [DefaultValue(".jpg,.jpeg,.png,.heic,.raw,.cr2,.nef")]
        public string Extensions { get; init; } = ".jpg,.jpeg,.png,.heic,.raw,.cr2,.nef";
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var extensions = settings.Extensions.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var fileSystem = new FileSystem();
        var metadataExtractor = new MetadataExtractorService(fileSystem);
        var scanner = new PhotoScanner(fileSystem, metadataExtractor);
        var statsService = new PhotoStatsService();

        AnsiConsole.MarkupLine($"[green]Scanning directory:[/] {settings.Source}");
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
                    task.Description = $"[yellow]Scanning:[/] {Path.GetFileName(p.CurrentFile ?? string.Empty)}";
                });

                photos = await scanner.ScanDirectoryAsync(settings.Source, extensions, progress);
                task.Value = task.MaxValue;
            });

        if (photos == null || photos.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No photos found![/]");
            return 1;
        }

        var stats = statsService.ComputeStats(photos);

        AnsiConsole.WriteLine();

        // Main panel
        var panel = new Panel(BuildStatsRenderable(stats))
        {
            Header = new PanelHeader("📊 Photo Collection Statistics"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0),
        };

        AnsiConsole.Write(panel);

        // Duplicate savings summary
        if (stats.DuplicateCount > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(
                $"[yellow]💾 {stats.DuplicateCount} duplicate(s) found, " +
                $"{FormatBytes(stats.DuplicateSizeBytes)} could be freed[/]");
        }

        return 0;
    }

    private static IRenderable BuildStatsRenderable(PhotoStats stats)
    {
        var rows = new Rows(
            BuildKeyMetricsTable(stats),
            new Text(""),
            BuildYearTable(stats),
            new Text(""),
            BuildCameraTable(stats));

        return rows;
    }

    private static Table BuildKeyMetricsTable(PhotoStats stats)
    {
        var table = new Table();
        table.AddColumn("Metric");
        table.AddColumn("Value");
        table.Border(TableBorder.Simple);

        table.AddRow("Total Photos", stats.TotalPhotos.ToString());
        table.AddRow("Total Size", FormatBytes(stats.TotalSizeBytes));
        table.AddRow("With Date", FormatCoverage(stats.PhotosWithDate, stats.TotalPhotos));
        table.AddRow("With Location", FormatCoverage(stats.PhotosWithLocation, stats.TotalPhotos));
        table.AddRow("With Camera", FormatCoverage(stats.PhotosWithCamera, stats.TotalPhotos));
        table.AddRow("Duplicates", stats.DuplicateCount.ToString());

        return table;
    }

    private static Table BuildYearTable(PhotoStats stats)
    {
        var table = new Table();
        table.Title = new TableTitle("📅 Photos by Year");
        table.AddColumn("Year");
        table.AddColumn("Count");
        table.Border(TableBorder.Simple);

        foreach (var (year, count) in stats.PhotosByYear.OrderByDescending(kvp => kvp.Key).Take(10))
        {
            var label = year == 0 ? "Unknown" : year.ToString();
            table.AddRow(label, count.ToString());
        }

        return table;
    }

    private static Table BuildCameraTable(PhotoStats stats)
    {
        var table = new Table();
        table.Title = new TableTitle("📷 Top Cameras");
        table.AddColumn("Camera");
        table.AddColumn("Count");
        table.Border(TableBorder.Simple);

        foreach (var (camera, count) in stats.PhotosByCamera.OrderByDescending(kvp => kvp.Value).Take(5))
        {
            table.AddRow(camera, count.ToString());
        }

        return table;
    }

    private static string FormatCoverage(int count, int total)
    {
        var pct = total == 0 ? 0 : (double)count / total * 100;
        return $"{count} ({pct:0.#}%)";
    }

    internal static string FormatBytes(long bytes)
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
