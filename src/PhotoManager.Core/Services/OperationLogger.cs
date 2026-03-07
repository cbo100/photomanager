using System.IO.Abstractions;
using System.Text.Json;
using PhotoManager.Domain;

namespace PhotoManager.Core.Services;

public class OperationLogger : IOperationLogger
{
    private readonly IFileSystem _fileSystem;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OperationLogger(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public OperationLog StartSession(PhotoManagerConfig config, bool isDryRun)
    {
        return new OperationLog
        {
            SessionId = Guid.NewGuid().ToString(),
            StartedAt = DateTime.UtcNow,
            SourceFolder = config.SourceFolder,
            DestinationFolder = config.DestinationFolder,
            Pattern = config.OrganizationPattern,
            Mode = config.OperationType.ToString(),
            DryRun = isDryRun
        };
    }

    public OperationLog AddEntry(OperationLog log, OperationLogEntry entry)
    {
        return log with { Entries = new List<OperationLogEntry>(log.Entries) { entry } };
    }

    public async Task<string> SaveAsync(OperationLog log, string logDirectory, CancellationToken ct = default)
    {
        _fileSystem.Directory.CreateDirectory(logDirectory);
        var filePath = _fileSystem.Path.Combine(logDirectory, $"photomanager-{log.SessionId}.json");
        var json = JsonSerializer.Serialize(log, JsonOptions);
        await _fileSystem.File.WriteAllTextAsync(filePath, json, ct);
        return filePath;
    }

    public async Task<OperationLog?> LoadAsync(string logFilePath, CancellationToken ct = default)
    {
        if (!_fileSystem.File.Exists(logFilePath))
            return null;
        var json = await _fileSystem.File.ReadAllTextAsync(logFilePath, ct);
        return JsonSerializer.Deserialize<OperationLog>(json, JsonOptions);
    }
}
