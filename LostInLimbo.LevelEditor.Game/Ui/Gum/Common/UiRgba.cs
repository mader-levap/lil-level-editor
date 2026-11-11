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
}
