using System;
using System.Globalization;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.ProgressBar;

/// <summary>
/// Engine-neutral math of <see cref="GumProgressBar"/>: clamping, percentage text, eased fill
/// and band scrolling. Kept separate so it can be unit-tested headlessly.
/// </summary>
public static class ProgressBarMath
{
  /// <summary>Distance below which <see cref="Approach"/> snaps to the target.</summary>
  public const float SnapDistance = 0.001f;

  /// <summary>Completion multiplier giving whole percent.</summary>
  private const float PercentFactor = 100f;

  /// <summary>
  /// Clamps a completion value to the range 0..1.
  /// </summary>
  /// <param name="value">Raw value.</param>
  /// <returns>Value in 0..1; NaN gives 0.</returns>
  public static float Clamp01(float value)
  {
    if (float.IsNaN(value)) return 0f;
    return Math.Clamp(value, 0f, 1f);
  }

  /// <summary>
  /// Formats completion as whole percent, for example <c>"37 %"</c>.
  /// </summary>
  /// <param name="completion">Completion (clamped to 0..1).</param>
  /// <returns>Percentage text.</returns>
  /// <remarks>
  /// Rounds down, so an unfinished run (for example 0.997) never shows "100 %".
  /// </remarks>
  public static string FormatPercent(float completion)
  {
    // Small epsilon guards against float error (0.29f * 100 = 28.999998).
    const float floorEpsilon = 0.0001f;
    int percent = (int)MathF.Floor(Clamp01(completion) * PercentFactor + floorEpsilon);
    return string.Create(CultureInfo.InvariantCulture, $"{percent} %");
  }

  /// <summary>
  /// Moves the shown value toward the target with exponential easing.
  /// </summary>
  /// <param name="shown">Currently shown value.</param>
  /// <param name="target">Target value.</param>
  /// <param name="deltaSeconds">Elapsed time in seconds; non-positive keeps the value.</param>
  /// <param name="ratePerSecond">Easing rate; higher is faster. Non-positive snaps.</param>
  /// <returns>New shown value; never overshoots the target.</returns>
  /// <remarks>
  /// A target below the shown value snaps immediately: completion never decreases within a run,
  /// so a lower target means a new run started.
  /// </remarks>
  public static float Approach(float shown, float target, float deltaSeconds, float ratePerSecond)
  {
    if (target <= shown || ratePerSecond <= 0f) return target;
    if (deltaSeconds <= 0f) return shown;

    float factor = 1f - MathF.Exp(-ratePerSecond * deltaSeconds);
    float next = shown + (target - shown) * factor;
    return target - next < SnapDistance ? target : next;
  }

  /// <summary>
  /// Wraps a scroll offset into the range [0, <paramref name="period"/>).
  /// </summary>
  /// <param name="offset">Offset (any value).</param>
  /// <param name="period">Repeat period; must be positive.</param>
  /// <returns>Wrapped offset.</returns>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> is not positive.</exception>
  public static float WrapOffset(float offset, float period)
  {
    if (!(period > 0f)) throw new ArgumentOutOfRangeException(nameof(period), period, "Period must be positive.");

    float wrapped = offset % period;
    if (wrapped < 0f) wrapped += period;
    return wrapped >= period ? 0f : wrapped; // guards float rounding of tiny negatives
  }
}
