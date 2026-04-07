using System.IO.Abstractions;
using PhotoManager.Cli.Commands;
using PhotoManager.Core.Services;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("photomanager");

    config.AddCommand<ScanCommand>("scan")
        .WithDescription("Scan a directory for photos and display metadata");

    config.AddCommand<OrganizeCommand>("organize")
        .WithAlias("organise")
        .WithDescription("Organize photos from source to destination folder");

});

return app.Run(args);
