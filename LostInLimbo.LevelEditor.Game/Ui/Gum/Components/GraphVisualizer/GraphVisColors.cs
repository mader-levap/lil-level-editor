using System;
using System.Collections.Generic;
using System.Linq;
using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Color helpers of the graph visualizer: darkening and assigning distinct, stable colors to keys
/// (for example group names).
/// </summary>
public static class GraphVisColors
{
  #region Palette definition

  /// <summary>Number of hues in one lightness tier of the palette.</summary>
  private const int HueCount = 12;

  /// <summary>
  /// Hue distance between two neighbouring palette slots, in degrees. 150 walks all
  /// <see cref="HueCount"/> hues (30 degrees apart) while keeping neighbours far apart, so a key
  /// pushed to the next slot by a collision still looks clearly different.
  /// </summary>
  private const float HueStepDegrees = 150f;

  /// <summary>Full circle, in degrees.</summary>
  private const float FullCircleDegrees = 360f;

  /// <summary>Saturation and lightness (0..1) of each palette tier; never gray, never too dark for black text.</summary>
  private static readonly (float Saturation, float Lightness)[] Tiers = [(0.70f, 0.68f), (0.75f, 0.50f)];

  /// <summary>Weight of red in perceived brightness.</summary>
  private const float RedWeight = 0.299f;

  /// <summary>Weight of green in perceived brightness.</summary>
  private const float GreenWeight = 0.587f;

  /// <summary>Weight of blue in perceived brightness.</summary>
  private const float BlueWeight = 0.114f;

  /// <summary>Perceived brightness (0..255) from which dark text reads better than light text.</summary>
  private const float DarkTextBrightnessThreshold = 140f;

  /// <summary>FNV-1a 32-bit offset basis.</summary>
  private const uint FnvOffsetBasis = 2166136261;

  /// <summary>FNV-1a 32-bit prime.</summary>
  private const uint FnvPrime = 16777619;

  #endregion

  /// <summary>
  /// Fixed palette of clearly distinct colors. Contains no gray, which is reserved for "no key".
  /// </summary>
  public static IReadOnlyList<UiRgba> Palette { get; } = BuildPalette();

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
  /// Hashes a key the same way in every process and on every machine (unlike
  /// <see cref="string.GetHashCode()"/>, which is randomized per process).
  /// </summary>
  /// <param name="key">Key to hash.</param>
  /// <returns>FNV-1a hash of the key's UTF-16 code units.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
  public static uint StableHash(string key)
  {
    ArgumentNullException.ThrowIfNull(key);

    uint hash = FnvOffsetBasis;
    foreach (char character in key)
    {
      hash = (hash ^ (byte)character) * FnvPrime;
      hash = (hash ^ (byte)(character >> 8)) * FnvPrime;
    }

    return hash;
  }

  /// <summary>
  /// Assigns a palette color to every key. A key always starts from the slot given by its hash, so
  /// the same key normally gets the same color in every graph. When two keys of one call hash to
  /// the same slot, the later one in ordinal order takes the next free slot, so colors within one
  /// graph stay distinct as long as there are no more keys than palette colors.
  /// </summary>
  /// <param name="keys">Keys (duplicates are ignored).</param>
  /// <returns>Color of each distinct key.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="keys"/> or one of its items is null.</exception>
  public static IReadOnlyDictionary<string, UiRgba> AssignColors(IEnumerable<string> keys)
  {
    ArgumentNullException.ThrowIfNull(keys);

    var result = new Dictionary<string, UiRgba>();
    var taken = new bool[Palette.Count];
    int takenCount = 0;

    // Ordinal order makes the collision outcome independent of the input order.
    foreach (string key in keys.Distinct().OrderBy(key => key, StringComparer.Ordinal))
    {
      int slot = (int)(StableHash(key) % (uint)Palette.Count);
      if (takenCount < Palette.Count)
      {
        while (taken[slot]) slot = (slot + 1) % Palette.Count;
        taken[slot] = true;
        takenCount++;
      }

      // More keys than colors: the palette is exhausted and colors repeat from here on.
      result[key] = Palette[slot];
    }

    return result;
  }

  /// <summary>
  /// Builds the palette: every tier walks the hue circle in large steps.
  /// </summary>
  /// <returns>Palette colors, slot order.</returns>
  private static UiRgba[] BuildPalette()
  {
    var palette = new UiRgba[HueCount * Tiers.Length];
    for (int tier = 0; tier < Tiers.Length; tier++)
    {
      for (int index = 0; index < HueCount; index++)
      {
        float hue = index * HueStepDegrees % FullCircleDegrees;
        palette[tier * HueCount + index] = FromHsl(hue, Tiers[tier].Saturation, Tiers[tier].Lightness);
      }
    }

    return palette;
  }

  /// <summary>
  /// Converts hue, saturation and lightness to an opaque color.
  /// </summary>
  /// <param name="hueDegrees">Hue, in degrees (0..360).</param>
  /// <param name="saturation">Saturation (0..1).</param>
  /// <param name="lightness">Lightness (0..1).</param>
  /// <returns>Opaque color.</returns>
  private static UiRgba FromHsl(float hueDegrees, float saturation, float lightness)
  {
    const float sextantDegrees = 60f;

    float chroma = (1 - MathF.Abs(2 * lightness - 1)) * saturation;
    float sextant = hueDegrees / sextantDegrees;
    float second = chroma * (1 - MathF.Abs(sextant % 2 - 1));
    float offset = lightness - chroma / 2;

    (float red, float green, float blue) = (int)sextant switch
    {
      0 => (chroma, second, 0f),
      1 => (second, chroma, 0f),
      2 => (0f, chroma, second),
      3 => (0f, second, chroma),
      4 => (second, 0f, chroma),
      _ => (chroma, 0f, second),
    };

    return new UiRgba(ToByte(red + offset), ToByte(green + offset), ToByte(blue + offset));
  }

  /// <summary>
  /// Scales one color channel.
  /// </summary>
  /// <param name="channel">Channel value.</param>
  /// <param name="factor">Factor (0..1).</param>
  /// <returns>Scaled channel.</returns>
  private static byte Scale(byte channel, float factor) => (byte)MathF.Round(channel * factor);

  /// <summary>
  /// Converts a 0..1 channel to a byte.
  /// </summary>
  /// <param name="value">Channel value (0..1).</param>
  /// <returns>Channel as a byte.</returns>
  private static byte ToByte(float value) => (byte)MathF.Round(Math.Clamp(value, 0f, 1f) * byte.MaxValue);
}
