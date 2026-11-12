using System;
using System.Globalization;

namespace LostInLimbo.LevelEditor.Ui.Gum.LevelGen;

/// <summary>
/// Text seed arithmetic for loop generation: the first seed comes from the form, every next
/// seed is the previous one incremented by 1.
/// </summary>
/// <remarks>
/// Seeds stay text seeds (they are passed to <c>LostInLimbo.LevelGen</c> as <c>TextSeed</c>);
/// only numeric texts can be incremented, so a non-numeric start falls back to
/// <see cref="FallbackSeed"/>.
/// </remarks>
public static class LevelGenLoopSeed
{
  /// <summary>Seed used when the form seed is not a number (including empty).</summary>
  public const string FallbackSeed = "0";

  /// <summary>Accepted number format: optional leading sign and digits only (no whitespace or separators).</summary>
  private const NumberStyles SeedNumberStyles = NumberStyles.AllowLeadingSign;

  /// <summary>
  /// Gets the seed of the first loop iteration.
  /// </summary>
  /// <param name="seedText">Seed text box content.</param>
  /// <returns>
  /// <paramref name="seedText"/> unchanged if it is a 64-bit integer (for example <c>"007"</c>);
  /// otherwise <see cref="FallbackSeed"/>.
  /// </returns>
  public static string First(string? seedText) =>
    seedText != null && TryParse(seedText, out _) ? seedText : FallbackSeed;

  /// <summary>
  /// Gets the seed of the next loop iteration.
  /// </summary>
  /// <param name="currentSeed">Seed of the current iteration; must be a 64-bit integer.</param>
  /// <returns>
  /// <paramref name="currentSeed"/> + 1 in invariant format (so <c>"007"</c> becomes <c>"8"</c>).
  /// <see cref="long.MaxValue"/> wraps around to <see cref="long.MinValue"/>.
  /// </returns>
  /// <exception cref="ArgumentNullException"><paramref name="currentSeed"/> is null.</exception>
  /// <exception cref="ArgumentException"><paramref name="currentSeed"/> is not a 64-bit integer.</exception>
  public static string Next(string currentSeed)
  {
    ArgumentNullException.ThrowIfNull(currentSeed);
    if (!TryParse(currentSeed, out long value))
      throw new ArgumentException($"Loop seed \"{currentSeed}\" is not a number.", nameof(currentSeed));

    return unchecked(value + 1).ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// Parses a seed text as a 64-bit integer.
  /// </summary>
  /// <param name="text">Seed text.</param>
  /// <param name="value">Parsed value.</param>
  /// <returns>True if the text is a number.</returns>
  private static bool TryParse(string text, out long value) =>
    long.TryParse(text, SeedNumberStyles, CultureInfo.InvariantCulture, out value);
}
