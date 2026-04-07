using PhotoManager.Cli.Commands;

namespace PhotoManager.Tests;

public class OrganizeCommandSettingsTests
{
    [Fact]
    public void Settings_PatternDefault_ShouldBeYearMonth()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src" };

        Assert.Equal("{Year}/{Month}", settings.Pattern);
    }

    [Fact]
    public void Settings_ModeDefault_ShouldBeCopy()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src" };

        Assert.Equal("copy", settings.Mode);
    }

    [Fact]
    public void Settings_ExtensionsDefault_ShouldIncludeCommonImageFormats()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src" };

        Assert.Equal(".jpg,.jpeg,.png,.heic,.raw,.cr2,.nef", settings.Extensions);
    }

    [Fact]
    public void Settings_DryRunDefault_ShouldBeFalse()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src" };

        Assert.False(settings.DryRun);
    }

    [Fact]
    public void Settings_SkipDuplicatesDefault_ShouldBeFalse()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src" };

        Assert.False(settings.SkipDuplicates);
    }

    [Fact]
    public void Settings_YesDefault_ShouldBeFalse()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src" };

        Assert.False(settings.Yes);
    }

    [Fact]
    public void Settings_YesSet_ShouldBeTrue()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", Yes = true };

        Assert.True(settings.Yes);
    }

    [Fact]
    public void Settings_OverwriteDefault_ShouldBeFalse()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src" };

        Assert.False(settings.Overwrite);
    }

    [Fact]
    public void Settings_OverwriteSet_ShouldBeTrue()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", Overwrite = true };

        Assert.True(settings.Overwrite);
    }

    [Fact]
    public void Settings_DestinationPath_ShouldBeNullByDefault()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src" };

        Assert.Null(settings.DestinationPath);
    }

    [Fact]
    public void Settings_Validate_InPlaceWithCopyMode_ShouldFail()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", Mode = "copy" };

        var result = settings.Validate();

        Assert.False(result.Successful);
    }

    [Fact]
    public void Settings_Validate_InPlaceWithSymlinkMode_ShouldFail()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", Mode = "symlink" };

        var result = settings.Validate();

        Assert.False(result.Successful);
    }

    [Fact]
    public void Settings_Validate_InPlaceWithMoveMode_ShouldSucceed()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", Mode = "move" };

        var result = settings.Validate();

        Assert.True(result.Successful);
    }

    [Fact]
    public void Settings_Validate_CopyModeWithDestination_ShouldSucceed()
    {
        var settings = new OrganizeCommand.Settings { SourcePath = "/src", DestinationPath = "/dst", Mode = "copy" };

        var result = settings.Validate();

        Assert.True(result.Successful);
    }
}
