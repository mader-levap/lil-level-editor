using System;
using System.Collections.Generic;
using System.Linq;
using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Render;

/// <summary>
/// Color helpers of the graph visualizer: darkening and assigning distinct colors to keys (for
/// example group names).
/// </summary>
public static class GraphVisColors
{
  #region Brightness constants

  /// <summary>Weight of red in perceived brightness.</summary>
  private const float RedWeight = 0.299f;

  /// <summary>Weight of green in perceived brightness.</summary>
  private const float GreenWeight = 0.587f;

  /// <summary>Weight of blue in perceived brightness.</summary>
  private const float BlueWeight = 0.114f;

  /// <summary>Perceived brightness (0..255) from which dark text reads better than light text.</summary>
  private const float DarkTextBrightnessThreshold = 140f;

  #endregion

  /// <summary>
  /// Fixed palette of distinct, moderately saturated colors. Picked by
  /// <see cref="PerceptualDifference"/>: each color is the one farthest from all colors before it
  /// (and from the view's background, the gray of ungrouped nodes and the selection highlight),
  /// so every prefix is as spread out as possible. The first 7 colors are very distinct from each
  /// other (difference at least 20), all 12 still distinct (at least 15). More colors could not
  /// stay that far apart, so <see cref="AssignColors"/> repeats them beyond 12 keys. Contains no
  /// gray, which is reserved for "no key".
  /// </summary>
  public static IReadOnlyList<UiRgba> Palette { get; } =
  [
    new(210, 65, 10),   // orange-red
    new(0, 125, 235),   // blue
    new(55, 180, 65),   // green
    new(255, 145, 255), // pink
    new(15, 245, 225),  // cyan
    new(175, 75, 180),  // purple
    new(255, 155, 105), // peach
    new(105, 185, 255), // light blue
    new(25, 140, 135),  // teal
    new(170, 225, 70),  // lime
    new(150, 125, 0),   // olive
    new(245, 95, 150),  // rose
  ];

  /// <summary>
  /// Darkens (or keeps) a color by scaling its red, green and blue channels.
  /// </summary>
  /// <param name="color">Color to darken.</param>
  /// <param name="brightness">Remaining brightness: 0 gives black, 1 the same color.</param>
  /// <returns>Scaled color with the original alpha.</returns>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="brightness"/> is outside 0..1.</exception>
  public static UiRgba Darken(UiRgba color, float brightness)
  {
    if (!(brightness >= 0 && brightness <= 1))
      throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "Brightness must be between 0 and 1.");

    return new UiRgba(Scale(color.Red, brightness), Scale(color.Green, brightness), Scale(color.Blue, brightness), color.Alpha);
  }

  /// <summary>
  /// Brightens (or keeps) a color by mixing it with white, so black becomes gray and colors
  /// become paler but keep their hue.
  /// </summary>
  /// <param name="color">Color to brighten.</param>
  /// <param name="amount">Share of white: 0 gives the same color, 1 white.</param>
  /// <returns>Brightened color with the original alpha.</returns>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is outside 0..1.</exception>
  public static UiRgba Brighten(UiRgba color, float amount)
  {
    if (!(amount >= 0 && amount <= 1))
      throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be between 0 and 1.");

    return new UiRgba(TowardsWhite(color.Red, amount), TowardsWhite(color.Green, amount), TowardsWhite(color.Blue, amount), color.Alpha);
  }

  /// <summary>
  /// Picks the text color that reads better on a background: the dark one on bright
  /// backgrounds, the light one on dark backgrounds.
  /// </summary>
  /// <param name="background">Background color.</param>
  /// <param name="dark">Text color for bright backgrounds.</param>
  /// <param name="light">Text color for dark backgrounds.</param>
  /// <returns><paramref name="dark"/> or <paramref name="light"/>.</returns>
  public static UiRgba ReadableOn(UiRgba background, UiRgba dark, UiRgba light)
  {
    // Perceived brightness (ITU-R BT.601 weights): green counts most, blue least.
    float brightness = RedWeight * background.Red + GreenWeight * background.Green + BlueWeight * background.Blue;
    return brightness >= DarkTextBrightnessThreshold ? dark : light;
  }

  /// <summary>
  /// Measures how different two colors look, as the distance between them in the OKLab color
  /// space (built so that equal distances look about equally different), times 100. About 20 or
  /// more reads as very distinct, below about 10 as look-alike; black and white are 100 apart.
  /// Alpha is ignored.
  /// </summary>
  /// <param name="first">First color.</param>
  /// <param name="second">Second color.</param>
  /// <returns>Perceptual difference (0 for equal colors).</returns>
  public static float PerceptualDifference(UiRgba first, UiRgba second)
  {
    (float firstL, float firstA, float firstB) = ToOkLab(first);
    (float secondL, float secondA, float secondB) = ToOkLab(second);

    float deltaL = firstL - secondL;
    float deltaA = firstA - secondA;
    float deltaB = firstB - secondB;
    return MathF.Sqrt(deltaL * deltaL + deltaA * deltaA + deltaB * deltaB) * PerceptualDifferenceScale;
  }

  /// <summary>
  /// Assigns a palette color to every key: the keys, sorted in ordinal order, take the palette
  /// slots in turn. The first keys thus get the most different colors, the result does not
  /// depend on the input order, and colors within one graph stay distinct as long as there are
  /// no more keys than palette colors (12); beyond that, colors repeat. A key keeps its color
  /// across graphs with the same keys (always true for the generator's numbered group names
  /// "g1", "g2", ...).
  /// </summary>
  /// <param name="keys">Keys (duplicates are ignored).</param>
  /// <returns>Color of each distinct key.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="keys"/> or one of its items is null.</exception>
  public static IReadOnlyDictionary<string, UiRgba> AssignColors(IEnumerable<string> keys)
  {
    ArgumentNullException.ThrowIfNull(keys);

    var result = new Dictionary<string, UiRgba>();
    int slot = 0;
    foreach (string key in keys.Distinct().OrderBy(key => key, StringComparer.Ordinal))
    {
      // More keys than colors: colors repeat from the start of the palette.
      result[key] = Palette[slot % Palette.Count];
      slot++;
    }

    return result;
  }

  /// <summary>
  /// Scales one color channel.
  /// </summary>
  /// <param name="channel">Channel value.</param>
  /// <param name="factor">Factor (0..1).</param>
  /// <returns>Scaled channel.</returns>
  private static byte Scale(byte channel, float factor) => (byte)MathF.Round(channel * factor);

  /// <summary>
  /// Moves one color channel towards full brightness.
  /// </summary>
  /// <param name="channel">Channel value.</param>
  /// <param name="amount">Share of the way to 255 (0..1).</param>
  /// <returns>Brightened channel.</returns>
  private static byte TowardsWhite(byte channel, float amount) => (byte)MathF.Round(channel + (byte.MaxValue - channel) * amount);

  #region OKLab

  /// <summary>Factor turning an OKLab distance (0..1 between black and white) into <see cref="PerceptualDifference"/> units.</summary>
  private const float PerceptualDifferenceScale = 100f;

  /// <summary>sRGB channel value (0..1) up to which the transfer curve is linear.</summary>
  private const float SrgbLinearThreshold = 0.04045f;

  /// <summary>Slope of the linear part of the sRGB transfer curve.</summary>
  private const float SrgbLinearSlope = 12.92f;

  /// <summary>Offset of the power part of the sRGB transfer curve.</summary>
  private const float SrgbPowerOffset = 0.055f;

  /// <summary>Exponent of the power part of the sRGB transfer curve.</summary>
  private const float SrgbPowerExponent = 2.4f;

  /// <summary>
  /// Linear sRGB to LMS cone response matrix of OKLab, row by row (Björn Ottosson, 2020).
  /// </summary>
  private static readonly float[,] LinearRgbToLms =
  {
    { 0.4122214708f, 0.5363325363f, 0.0514459929f },
    { 0.2119034982f, 0.6806995451f, 0.1073969566f },
    { 0.0883024619f, 0.2817188376f, 0.6299787005f },
  };

  /// <summary>Cube-rooted LMS to OKLab (L, a, b) matrix, row by row.</summary>
  private static readonly float[,] LmsToOkLab =
  {
    { 0.2104542553f, 0.7936177850f, -0.0040720453f },
    { 1.9779984951f, -2.4285922050f, 0.4505937099f },
    { 0.0259040371f, 0.7827717662f, -0.8086757660f },
  };

  /// <summary>
  /// Converts a color to OKLab.
  /// </summary>
  /// <param name="color">Color (alpha is ignored).</param>
  /// <returns>Lightness (0 black .. 1 white) and the two color axes.</returns>
  private static (float L, float A, float B) ToOkLab(UiRgba color)
  {
    float red = ToLinear(color.Red);
    float green = ToLinear(color.Green);
    float blue = ToLinear(color.Blue);

    // Cone responses (long, medium, short wavelengths), then their cube roots.
    (float coneLong, float coneMedium, float coneShort) = Multiply(LinearRgbToLms, red, green, blue);
    return Multiply(LmsToOkLab, MathF.Cbrt(coneLong), MathF.Cbrt(coneMedium), MathF.Cbrt(coneShort));
  }

  /// <summary>
  /// Undoes the sRGB transfer curve of one channel.
  /// </summary>
  /// <param name="channel">Channel value (0..255).</param>
  /// <returns>Linear light (0..1).</returns>
  private static float ToLinear(byte channel)
  {
    float value = channel / (float)byte.MaxValue;
    return value <= SrgbLinearThreshold
      ? value / SrgbLinearSlope
      : MathF.Pow((value + SrgbPowerOffset) / (1 + SrgbPowerOffset), SrgbPowerExponent);
  }

  /// <summary>
  /// Multiplies a 3×3 matrix with a vector.
  /// </summary>
  /// <param name="matrix">Matrix, row by row.</param>
  /// <param name="x">First vector component.</param>
  /// <param name="y">Second vector component.</param>
  /// <param name="z">Third vector component.</param>
  /// <returns>Product.</returns>
  private static (float, float, float) Multiply(float[,] matrix, float x, float y, float z)
  {
    return (
      matrix[0, 0] * x + matrix[0, 1] * y + matrix[0, 2] * z,
      matrix[1, 0] * x + matrix[1, 1] * y + matrix[1, 2] * z,
      matrix[2, 0] * x + matrix[2, 1] * y + matrix[2, 2] * z);
  }

  #endregion
}
