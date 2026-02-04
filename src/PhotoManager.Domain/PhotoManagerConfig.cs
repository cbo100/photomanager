namespace PhotoManager.Domain;

/// <summary>
/// Configuration options for photo organization
/// </summary>
public class PhotoManagerConfig
{
    public required string SourceFolder { get; set; }
    public required string DestinationFolder { get; set; }
    public string OrganizationPattern { get; set; } = "{Year}/{Month}";
    public OperationType OperationType { get; set; } = OperationType.Copy;
    public DuplicateHandling HandleDuplicates { get; set; } = DuplicateHandling.Skip;
    public bool UseLocation { get; set; } = false;
    public string[] FileExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".heic", ".raw", ".cr2", ".nef"];
    public bool PreserveOriginalDate { get; set; } = true;
    public bool ParallelProcessing { get; set; } = true;
    public int MaxDegreeOfParallelism { get; set; } = 4;
}

/// <summary>
/// How to handle duplicate files
/// </summary>
public enum DuplicateHandling
{
    Skip,
    Rename,
    Overwrite
}
