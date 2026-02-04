namespace PhotoManager.Domain;

/// <summary>
/// Organization rules and patterns
/// </summary>
public record OrganizationRule
{
    public required string Pattern { get; init; }
    public bool UseLocation { get; init; }
    public bool UseEvent { get; init; }
}
