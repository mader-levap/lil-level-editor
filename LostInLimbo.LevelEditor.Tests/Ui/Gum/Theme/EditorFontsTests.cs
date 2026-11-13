using System;
using System.IO;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Theme;

/// <summary>
/// Headless tests for <see cref="EditorFonts"/> (theme font locations).
/// </summary>
public sealed class EditorFontsTests
{
  /// <summary>
  /// Verifies that a font file resolves into the Inter folder under the base directory.
  /// </summary>
  [Fact]
  public void ResolvePath_ReturnsAbsolutePathInFontFolder()
  {
    // Arrange: A base directory.
    string baseDirectory = Path.GetFullPath("app");

    // Act: Resolve the regular cut.
    string path = EditorFonts.ResolvePath(baseDirectory, EditorFonts.RegularFile);

    // Assert: Path is absolute and inside GumContent/Fonts/Inter.
    Assert.Equal(Path.Combine(baseDirectory, "GumContent", "Fonts", "Inter", EditorFonts.RegularFile), path);
  }

  /// <summary>
  /// Verifies that blank arguments are rejected.
  /// </summary>
  [Theory]
  [InlineData("", EditorFonts.RegularFile)]
  [InlineData("app", " ")]
  public void ResolvePath_BlankArgument_Throws(string baseDirectory, string fileName)
  {
    // Act + Assert: Invalid input throws.
    Assert.Throws<ArgumentException>(() => EditorFonts.ResolvePath(baseDirectory, fileName));
  }

  /// <summary>
  /// Verifies that the build copies the font files and their license to the output folder.
  /// </summary>
  [Theory]
  [InlineData(EditorFonts.RegularFile)]
  [InlineData(EditorFonts.BoldFile)]
  [InlineData(EditorFonts.LicenseFile)]
  public void ResolvePath_KnownFile_ExistsInOutput(string fileName)
  {
    // Act: Resolve against the test output folder (Game project content flows into it).
    string path = EditorFonts.ResolvePath(fileName);

    // Assert: File was copied.
    Assert.True(File.Exists(path), $"Missing font file: {path}");
  }
}
