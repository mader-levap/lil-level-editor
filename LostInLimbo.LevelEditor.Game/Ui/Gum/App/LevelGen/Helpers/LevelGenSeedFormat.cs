namespace LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Helpers;

/// <summary>
/// Display format of text seeds, shared by the status log and the level information card.
/// </summary>
public static class LevelGenSeedFormat
{
  /// <summary>Note appended to an empty text seed (the library then seeds from the clock).</summary>
  public const string TimeBasedNote = "(current time)";

  /// <summary>
  /// Puts a text seed in quotes (it is text, even if it contains only digits).
  /// </summary>
  /// <param name="seed">Seed; null is shown as empty.</param>
  /// <returns>For example <c>"abc"</c>.</returns>
  public static string Quote(string? seed) => $"\"{seed}\"";

  /// <summary>
  /// Formats the text seed of a single (non-loop) run; an empty seed is marked as time-based.
  /// </summary>
  /// <param name="seedText">Text seed; empty or null means a time-based seed.</param>
  /// <returns>For example <c>"abc"</c> or <c>"" (current time)</c>.</returns>
  public static string TextSeed(string? seedText) => string.IsNullOrEmpty(seedText)
    ? $"{Quote(seedText)} {TimeBasedNote}"
    : Quote(seedText);
}
