using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

/// <summary>
/// Headless tests for <see cref="GumIcons"/> path resolution.
/// </summary>
public sealed class GumIconsTests
{
  /// <summary>
  /// Verifies that icon paths are absolute and point into the GumContent/Icons folder.
  /// </summary>
  [Fact]
  public void ResolvePath_ReturnsAbsolutePathInIconFolder()
  {
    // Arrange: A base directory.
    string baseDirectory = Path.Combine(Path.GetTempPath(), "app");

    // Act: Resolve the gears icon.
    string path = GumIcons.ResolvePath(baseDirectory, GumIcons.Gears);

    // Assert: Absolute path ending with GumContent/Icons/<file>.
    Assert.True(Path.IsPathFullyQualified(path));
    Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "GumContent", "Icons", GumIcons.Gears)), path);
  }

  /// <summary>
  /// Verifies that blank arguments are rejected.
  /// </summary>
  [Theory]
  [InlineData("", "a.png")]
  [InlineData("C:\\app", " ")]
  public void ResolvePath_BlankArgument_Throws(string baseDirectory, string iconFile)
  {
    // Act + Assert: Invalid input is rejected.
    Assert.ThrowsAny<ArgumentException>(() => GumIcons.ResolvePath(baseDirectory, iconFile));
  }

  /// <summary>
  /// Verifies that every icon used by the Gum UI is copied to the test output (same csproj rule
  /// as the app), so a renamed or missing PNG fails here instead of at runtime.
  /// </summary>
  [Theory]
  [InlineData(GumIcons.Gears)]
  [InlineData(GumIcons.Info)]
  [InlineData(GumIcons.Start)]
  [InlineData(GumIcons.Step)]
  [InlineData(GumIcons.Pause)]
  [InlineData(GumIcons.Stop)]
  public void ResolvePath_KnownIcon_FileExistsInOutput(string iconFile)
  {
    // Act: Resolve against the test output folder.
    string path = GumIcons.ResolvePath(iconFile);

    // Assert: File was copied.
    Assert.True(File.Exists(path), $"Missing icon file: {path}");
  }
}
