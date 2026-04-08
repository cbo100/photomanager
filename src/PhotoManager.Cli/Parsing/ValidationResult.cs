namespace PhotoManager.Cli.Parsing;

public readonly record struct ValidationResult
{
    public bool Successful { get; init; }
    public string? ErrorMessage { get; init; }

    public static ValidationResult Success() => new() { Successful = true };
    public static ValidationResult Error(string message) => new() { Successful = false, ErrorMessage = message };
}
