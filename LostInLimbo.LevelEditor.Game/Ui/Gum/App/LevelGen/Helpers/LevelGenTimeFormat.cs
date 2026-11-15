using System;
using System.Globalization;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Helpers;

/// <summary>
/// Display format of generation times, shared by the status log and the level information card.
/// </summary>
public static class LevelGenTimeFormat
{
  /// <summary>
  /// Formats a duration as whole milliseconds (rounded).
  /// </summary>
  /// <param name="duration">Duration to format.</param>
  /// <returns>For example <c>1234 ms</c>.</returns>
  public static string Milliseconds(TimeSpan duration) =>
    duration.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture) + " ms";
}
