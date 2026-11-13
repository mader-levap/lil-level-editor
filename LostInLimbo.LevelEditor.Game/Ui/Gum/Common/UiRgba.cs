using System;

namespace LostInLimbo.LevelEditor.Ui.Gum.Common;

/// <summary>
/// Engine-neutral RGBA color used by pure UI logic, so decisions such as "which color
/// does this build type get" can be unit-tested without Gum or Stride color types.
/// </summary>
/// <param name="Red">Red channel (0-255).</param>
/// <param name="Green">Green channel (0-255).</param>
/// <param name="Blue">Blue channel (0-255).</param>
/// <param name="Alpha">Alpha channel (0-255); 255 is fully opaque.</param>
public readonly record struct UiRgba(byte Red, byte Green, byte Blue, byte Alpha = byte.MaxValue)
{
  /// <summary>Opaque white.</summary>
  public static UiRgba White { get; } = new(255, 255, 255);

  /// <summary>Opaque black.</summary>
  public static UiRgba Black { get; } = new(0, 0, 0);

  /// <summary>
  /// Returns this color with its alpha multiplied by <paramref name="factor"/> (more translucent
  /// for a factor below 1).
  /// </summary>
  /// <param name="factor">Alpha multiplier, clamped to 0..1.</param>
  /// <returns>Same color with the scaled alpha, rounded to the nearest whole value.</returns>
  public UiRgba WithAlphaScaled(float factor)
  {
    float clamped = float.IsNaN(factor) ? 0f : Math.Clamp(factor, 0f, 1f);
    return this with { Alpha = (byte)MathF.Round(Alpha * clamped) };
  }
}
