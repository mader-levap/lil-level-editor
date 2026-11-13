using LostInLimbo.LevelEditor.Services.SystemInfo.Data;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.Log;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;
using SkiaSharp;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Theme;

/// <summary>
/// Headless tests for <see cref="EditorPalette"/> and the pure parts of <see cref="EditorTheme"/>.
/// </summary>
public sealed class EditorPaletteTests
{
  /// <summary>
  /// Verifies that text colors of the default palette are fully opaque (readable over 3D).
  /// </summary>
  [Fact]
  public void Default_TextColors_AreOpaque()
  {
    // Arrange: Default palette.
    EditorPalette palette = EditorPalette.Default;

    // Assert: Text and icon colors have full alpha.
    Assert.Equal(byte.MaxValue, palette.TextPrimary.Alpha);
    Assert.Equal(byte.MaxValue, palette.TextMuted.Alpha);
    Assert.Equal(byte.MaxValue, palette.IconDefault.Alpha);
  }

  /// <summary>
  /// Verifies that each log severity maps to its semantic palette color.
  /// </summary>
  [Fact]
  public void ForSeverity_EachSeverity_ReturnsSemanticColor()
  {
    // Arrange: Default palette.
    EditorPalette palette = EditorPalette.Default;

    // Act + Assert: all severities use semantic colors.
    Assert.Equal(palette.LogDebug, palette.ForSeverity(EnStatusSeverity.Debug));
    Assert.Equal(palette.LogInfo, palette.ForSeverity(EnStatusSeverity.Info));
    Assert.Equal(palette.LogSuccess, palette.ForSeverity(EnStatusSeverity.Success));
    Assert.Equal(palette.LogWarning, palette.ForSeverity(EnStatusSeverity.Warning));
    Assert.Equal(palette.LogError, palette.ForSeverity(EnStatusSeverity.Error));
  }

  /// <summary>
  /// Verifies that all log severities are visually distinct.
  /// </summary>
  [Fact]
  public void ForSeverity_AllSeverities_AreDistinct()
  {
    // Act: Collect the color of every severity.
    UiRgba[] colors = Enum.GetValues<EnStatusSeverity>().Select(EditorPalette.Default.ForSeverity).ToArray();

    // Assert: No two severities share a color.
    Assert.Equal(colors.Length, colors.Distinct().Count());
  }

  /// <summary>
  /// Verifies the app bar color of each known build type.
  /// </summary>
  [Fact]
  public void ForBuildType_KnownTypes_ReturnExpectedColors()
  {
    // Arrange: Default palette.
    EditorPalette palette = EditorPalette.Default;

    // Act + Assert: Development = warning, Debug = debug highlight, Release = success.
    Assert.Equal(palette.BuildDev, palette.ForBuildType(EnBuildType.Development));
    Assert.Equal(palette.BuildDebug, palette.ForBuildType(EnBuildType.Debug));
    Assert.Equal(palette.BuildRelease, palette.ForBuildType(EnBuildType.Release));
  }

  /// <summary>
  /// Verifies that an unrecognized build type falls back to the danger color.
  /// </summary>
  [Fact]
  public void ForBuildType_UnknownValue_ReturnsDanger()
  {
    // Arrange: A value outside the enum.
    var unknown = (EnBuildType)int.MaxValue;

    // Act: Get its color.
    UiRgba color = EditorPalette.Default.ForBuildType(unknown);

    // Assert: Danger color.
    Assert.Equal(EditorPalette.Default.BuildUnknown, color);
  }

  /// <summary>
  /// Verifies the engine-neutral to Skia color conversion keeps all channels.
  /// </summary>
  [Fact]
  public void ToSkColor_KeepsAllChannels()
  {
    // Arrange: A color with distinct channel values.
    var color = new UiRgba(1, 2, 3, 4);

    // Act: Convert.
    SKColor converted = EditorTheme.ToSkColor(color);

    // Assert: Same channels.
    Assert.Equal(new SKColor(1, 2, 3, 4), converted);
  }
}
