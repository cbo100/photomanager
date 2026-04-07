using PhotoManager.Cli.Commands;

namespace PhotoManager.Tests;

public class OrganizeCommandSettingsTests
{
    [Fact]
    public void Settings_PatternDefault_ShouldBeYearMonth()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", DestinationPath = "/dst" };

        Assert.Equal("{Year}/{Month}", settings.Pattern);
    }

    [Fact]
    public void Settings_ModeDefault_ShouldBeCopy()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", DestinationPath = "/dst" };

        Assert.Equal("copy", settings.Mode);
    }

    [Fact]
    public void Settings_ExtensionsDefault_ShouldIncludeCommonImageFormats()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", DestinationPath = "/dst" };

        Assert.Equal(".jpg,.jpeg,.png,.heic,.raw,.cr2,.nef", settings.Extensions);
    }

    [Fact]
    public void Settings_DryRunDefault_ShouldBeFalse()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", DestinationPath = "/dst" };

        Assert.False(settings.DryRun);
    }

    [Fact]
    public void Settings_SkipDuplicatesDefault_ShouldBeFalse()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", DestinationPath = "/dst" };

        Assert.False(settings.SkipDuplicates);
    }

    [Fact]
    public void Settings_YesDefault_ShouldBeFalse()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", DestinationPath = "/dst" };

        Assert.False(settings.Yes);
    }

    [Fact]
    public void Settings_YesSet_ShouldBeTrue()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", DestinationPath = "/dst", Yes = true };

        Assert.True(settings.Yes);
    }
}
