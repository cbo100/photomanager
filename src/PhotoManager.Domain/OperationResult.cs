namespace PhotoManager.Domain;

public record OperationResult
{
    public required PhotoOperation Operation { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
