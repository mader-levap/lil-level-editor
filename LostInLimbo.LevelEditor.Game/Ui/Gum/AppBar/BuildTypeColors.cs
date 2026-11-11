using LostInLimbo.LevelEditor.Services.SystemInfo.Data;
using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Ui.Gum.AppBar;

/// <summary>
/// Maps a build type to the color used for its label in the app bar.
/// </summary>
public static class BuildTypeColors
{
  /// <summary>Development builds (orange; same as Stride <c>Color.Orange</c>).</summary>
  public static UiRgba Development { get; } = new(255, 165, 0);

  /// <summary>Debug builds (yellow; same as Stride <c>Color.Yellow</c>).</summary>
  public static UiRgba Debug { get; } = new(255, 255, 0);

  /// <summary>Release builds (green; same as Stride <c>Color.Green</c>).</summary>
  public static UiRgba Release { get; } = new(0, 128, 0);

  /// <summary>Unknown build types (red; same as Stride <c>Color.Red</c>).</summary>
  public static UiRgba Unknown { get; } = new(255, 0, 0);

  /// <summary>
  /// Gets the label color for a build type.
  /// </summary>
  /// <param name="buildType">Build type.</param>
  /// <returns>Color for the build type; <see cref="Unknown"/> for unrecognized values.</returns>
  public static UiRgba For(EnBuildType buildType) => buildType switch
  {
    EnBuildType.Development => Development,
    EnBuildType.Debug => Debug,
    EnBuildType.Release => Release,
    _ => Unknown,
  };
}
