using PhotoManager.Cli.Commands;
using Spectre.Console;

if (args.Length == 0 || args[0] is "--help" or "-h" or "-?")
{
    PrintHelp();
    return args.Length == 0 ? 1 : 0;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

return args[0].ToLowerInvariant() switch
{
    "scan" => await ScanCommand.RunAsync(args[1..], cts.Token),
    "organize" or "organise" => await OrganizeCommand.RunAsync(args[1..], cts.Token),
    _ => UnknownCommand(args[0]),
};

static void PrintHelp()
{
    AnsiConsole.MarkupLine("[bold]photomanager[/] [grey]<command> [options][/]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[underline]Commands:[/]");
    AnsiConsole.MarkupLine("  [green]scan[/]                  Scan a directory for photos and display metadata");
    AnsiConsole.MarkupLine("  [green]organize[/] (organise)   Organize photos from source to destination folder");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("Run [green]photomanager <command> --help[/] for command-specific options.");
}

static int UnknownCommand(string name)
{
    AnsiConsole.MarkupLine($"[red]Unknown command:[/] {name}");
    PrintHelp();
    return 1;
}

