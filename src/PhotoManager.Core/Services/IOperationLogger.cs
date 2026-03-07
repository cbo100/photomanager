using PhotoManager.Domain;

namespace PhotoManager.Core.Services;

public interface IOperationLogger
{
    OperationLog StartSession(PhotoManagerConfig config, bool isDryRun);
    OperationLog AddEntry(OperationLog log, OperationLogEntry entry);
    Task<string> SaveAsync(OperationLog log, string logDirectory, CancellationToken ct = default);
    Task<OperationLog?> LoadAsync(string logFilePath, CancellationToken ct = default);
}
