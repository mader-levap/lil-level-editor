using LostInLimbo.LevelEditor.Services.SystemInfo.Data;
using LostInLimbo.LevelEditor.Ui.Gum.AppBar;
using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

/// <summary>
/// Headless tests for <see cref="BuildTypeColors"/> (app bar build label color).
/// </summary>
public sealed class BuildTypeColorsTests
{
  /// <summary>
  /// Verifies that every known build type maps to its documented color.
  /// </summary>
  [Theory]
  [InlineData(EnBuildType.Development, 255, 165, 0)]
  [InlineData(EnBuildType.Debug, 255, 255, 0)]
  [InlineData(EnBuildType.Release, 0, 128, 0)]
  public void For_KnownBuildType_ReturnsMatchingOpaqueColor(EnBuildType buildType, byte red, byte green, byte blue)
  {
    // Act: Resolve the color for the build type.
    UiRgba color = BuildTypeColors.For(buildType);

    // Assert: Color matches the former UGS app bar and is fully opaque.
    Assert.Equal(new UiRgba(red, green, blue, byte.MaxValue), color);
  }

  /// <summary>
  /// Verifies that an unrecognized build type falls back to the "unknown" color.
  /// </summary>
  [Fact]
  public void For_UnknownBuildType_ReturnsUnknownColor()
  {
    // Arrange: A value outside the enum.
    var unknown = (EnBuildType)999;

    // Act: Resolve the color.
    UiRgba color = BuildTypeColors.For(unknown);

    // Assert: Fallback color is used.
    Assert.Equal(BuildTypeColors.Unknown, color);
  }
}
