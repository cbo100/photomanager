using System.IO.Abstractions;
using System.Text.Json;
using PhotoManager.Core.Services;
using PhotoManager.Domain;
using NSubstitute;

namespace PhotoManager.Tests;

public class OperationLoggerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static PhotoManagerConfig MakeConfig(string mode = "Copy") => new()
    {
        SourceFolder = "/photos/source",
        DestinationFolder = "/photos/dest",
        OrganizationPattern = "{Year}/{Month}",
        OperationType = Enum.Parse<OperationType>(mode)
    };

    [Fact]
    public void StartSession_CreatesLogWithCorrectFields()
    {
        var fs = Substitute.For<IFileSystem>();
        var logger = new OperationLogger(fs);
        var config = MakeConfig("Move");
        var before = DateTime.UtcNow;

        var log = logger.StartSession(config, isDryRun: true);

        var after = DateTime.UtcNow;
        Assert.True(Guid.TryParse(log.SessionId, out _));
        Assert.InRange(log.StartedAt, before, after);
        Assert.Equal("/photos/source", log.SourceFolder);
        Assert.Equal("/photos/dest", log.DestinationFolder);
        Assert.Equal("{Year}/{Month}", log.Pattern);
        Assert.Equal("Move", log.Mode);
        Assert.True(log.DryRun);
        Assert.Empty(log.Entries);
    }

    [Fact]
    public void AddEntry_ReturnsNewLogWithEntryAppended_OriginalUnchanged()
    {
        var fs = Substitute.For<IFileSystem>();
        var logger = new OperationLogger(fs);
        var log = logger.StartSession(MakeConfig(), isDryRun: false);

        var entry = new OperationLogEntry
        {
            SourcePath = "/src/a.jpg",
            DestinationPath = "/dst/a.jpg",
            OperationType = "Copy",
            Success = true,
            Timestamp = DateTime.UtcNow,
            FileSizeBytes = 1024
        };

        var newLog = logger.AddEntry(log, entry);

        Assert.Empty(log.Entries);
        Assert.Single(newLog.Entries);
        Assert.Equal(entry, newLog.Entries[0]);
    }

    [Fact]
    public void AddEntry_MultipleEntries_AllPresent()
    {
        var fs = Substitute.For<IFileSystem>();
        var logger = new OperationLogger(fs);
        var log = logger.StartSession(MakeConfig(), isDryRun: false);

        for (var i = 1; i <= 3; i++)
        {
            log = logger.AddEntry(log, new OperationLogEntry
            {
                SourcePath = $"/src/{i}.jpg",
                DestinationPath = $"/dst/{i}.jpg",
                OperationType = "Copy",
                Success = true,
                Timestamp = DateTime.UtcNow,
                FileSizeBytes = i * 100
            });
        }

        Assert.Equal(3, log.Entries.Count);
    }

    [Fact]
    public async Task SaveAsync_WritesJsonToCorrectPath_ReturnsFilePath()
    {
        var fs = Substitute.For<IFileSystem>();
        var dir = Substitute.For<IDirectory>();
        var file = Substitute.For<IFile>();
        var path = Substitute.For<IPath>();

        fs.Directory.Returns(dir);
        fs.File.Returns(file);
        fs.Path.Returns(path);

        var capturedJson = string.Empty;
        path.Combine(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => System.IO.Path.Combine(ci.ArgAt<string>(0), ci.ArgAt<string>(1)));
        file.When(x => x.WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(ci => capturedJson = ci.ArgAt<string>(1));

        var logger = new OperationLogger(fs);
        var config = MakeConfig();
        var log = logger.StartSession(config, isDryRun: false);
        log = log with { SessionId = "test-session-id-1234" };

        var returnedPath = await logger.SaveAsync(log, "/logs");

        Assert.Equal("/logs/photomanager-test-session-id-1234.json", returnedPath);
        dir.Received(1).CreateDirectory("/logs");
        Assert.False(string.IsNullOrEmpty(capturedJson));
        var deserialized = JsonSerializer.Deserialize<OperationLog>(capturedJson, JsonOptions);
        Assert.NotNull(deserialized);
        Assert.Equal("test-session-id-1234", deserialized!.SessionId);
        Assert.Equal("/photos/source", deserialized.SourceFolder);
    }

    [Fact]
    public async Task LoadAsync_ReturnsDeserializedLog()
    {
        var fs = Substitute.For<IFileSystem>();
        var file = Substitute.For<IFile>();
        fs.File.Returns(file);

        var originalLog = new OperationLog
        {
            SessionId = "abc-123",
            StartedAt = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            SourceFolder = "/source",
            DestinationFolder = "/dest",
            Pattern = "{Year}/{Month}",
            Mode = "Copy",
            DryRun = false
        };
        var json = JsonSerializer.Serialize(originalLog, JsonOptions);

        file.Exists("/logs/photomanager-abc-123.json").Returns(true);
        file.ReadAllTextAsync("/logs/photomanager-abc-123.json", Arg.Any<CancellationToken>())
            .Returns(json);

        var logger = new OperationLogger(fs);
        var loaded = await logger.LoadAsync("/logs/photomanager-abc-123.json");

        Assert.NotNull(loaded);
        Assert.Equal("abc-123", loaded!.SessionId);
        Assert.Equal("/source", loaded.SourceFolder);
        Assert.Equal("/dest", loaded.DestinationFolder);
        Assert.Equal("Copy", loaded.Mode);
    }

    [Fact]
    public async Task LoadAsync_ReturnsNull_WhenFileNotFound()
    {
        var fs = Substitute.For<IFileSystem>();
        var file = Substitute.For<IFile>();
        fs.File.Returns(file);
        file.Exists(Arg.Any<string>()).Returns(false);

        var logger = new OperationLogger(fs);
        var result = await logger.LoadAsync("/nonexistent/path.json");

        Assert.Null(result);
    }

    [Fact]
    public void SuccessCount_FailureCount_AreCorrect()
    {
        var fs = Substitute.For<IFileSystem>();
        var logger = new OperationLogger(fs);
        var log = logger.StartSession(MakeConfig(), isDryRun: false);

        log = logger.AddEntry(log, new OperationLogEntry { SourcePath = "/1.jpg", DestinationPath = "/d/1.jpg", OperationType = "Copy", Success = true, Timestamp = DateTime.UtcNow });
        log = logger.AddEntry(log, new OperationLogEntry { SourcePath = "/2.jpg", DestinationPath = "/d/2.jpg", OperationType = "Copy", Success = true, Timestamp = DateTime.UtcNow });
        log = logger.AddEntry(log, new OperationLogEntry { SourcePath = "/3.jpg", DestinationPath = "/d/3.jpg", OperationType = "Copy", Success = false, ErrorMessage = "Access denied", Timestamp = DateTime.UtcNow });

        Assert.Equal(3, log.TotalOperations);
        Assert.Equal(2, log.SuccessCount);
        Assert.Equal(1, log.FailureCount);
    }

    [Fact]
    public void TotalBytesProcessed_SumsOnlySuccessful()
    {
        var fs = Substitute.For<IFileSystem>();
        var logger = new OperationLogger(fs);
        var log = logger.StartSession(MakeConfig(), isDryRun: false);

        log = logger.AddEntry(log, new OperationLogEntry { SourcePath = "/1.jpg", DestinationPath = "/d/1.jpg", OperationType = "Copy", Success = true, Timestamp = DateTime.UtcNow, FileSizeBytes = 1000 });
        log = logger.AddEntry(log, new OperationLogEntry { SourcePath = "/2.jpg", DestinationPath = "/d/2.jpg", OperationType = "Copy", Success = true, Timestamp = DateTime.UtcNow, FileSizeBytes = 2000 });
        log = logger.AddEntry(log, new OperationLogEntry { SourcePath = "/3.jpg", DestinationPath = "/d/3.jpg", OperationType = "Copy", Success = false, Timestamp = DateTime.UtcNow, FileSizeBytes = 5000 });

        Assert.Equal(3000, log.TotalBytesProcessed);
    }
}
