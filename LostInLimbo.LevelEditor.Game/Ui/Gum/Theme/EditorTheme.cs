using System;
using System.IO;
using Gum.DataTypes.Variables;
using Gum.Forms.DefaultVisuals.V3;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using SkiaGum.Content.Fonts;
using SkiaSharp;
using Stride.Core.Diagnostics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Theme;

/// <summary>
/// Code-only Gum theme of the editor: dark neutral palette, one accent, Inter font, no focus bars.
/// </summary>
/// <remarks>
/// Works by changing Gum's V3 <see cref="Styling.ActiveStyle"/>, which Forms controls read when
/// they are <b>created</b>. Call <see cref="Apply"/> once, right after
/// <c>GumService.Default.Initialize</c> and before any UI is built; controls created earlier keep
/// the stock look. Our own code reads colors from <see cref="Palette"/>.
/// </remarks>
public static class EditorTheme
{
  /// <summary>Logger for this class.</summary>
  private static readonly Logger Log = GlobalLogger.GetLogger(nameof(EditorTheme));

  /// <summary>Gum style slot name for the bold cut of a registered font family.</summary>
  private const string BoldStyleSlot = "Bold";

  /// <summary>Gum variable name of a text state's font.</summary>
  private const string FontVariable = "Font";

  /// <summary>Gum variable type name for strings.</summary>
  private const string StringType = "string";

  /// <summary>
  /// How much Gum darkens <c>Primary</c> for the pressed state, in percent (negative = darker).
  /// Gum's default is -15, which was too subtle on the accent-colored buttons.
  /// </summary>
  public const float PressedDarkenPercent = -40;

  /// <summary>
  /// How much Gum lightens <c>Primary</c> for the hover state, in percent (Gum's default).
  /// </summary>
  public const float HoverLightenPercent = 15;

  /// <summary>Palette in use; <see cref="EditorPalette.Default"/> until <see cref="Apply"/> sets another.</summary>
  public static EditorPalette Palette { get; private set; } = EditorPalette.Default;

  /// <summary>
  /// Applies the theme to Gum's default visuals.
  /// </summary>
  /// <param name="palette">Palette to use.</param>
  /// <exception cref="ArgumentNullException"><paramref name="palette"/> is null.</exception>
  public static void Apply(EditorPalette palette)
  {
    ArgumentNullException.ThrowIfNull(palette);
    Palette = palette;

    Styling style = Styling.ActiveStyle;
    ApplyColors(style.Colors, palette);
    if (RegisterFonts()) ApplyFont(style.Text);
  }

  /// <summary>
  /// Converts an engine-neutral color to a Skia color.
  /// </summary>
  /// <param name="color">Color to convert.</param>
  /// <returns>Skia color with the same channels.</returns>
  public static SKColor ToSkColor(UiRgba color) => new(color.Red, color.Green, color.Blue, color.Alpha);

  /// <summary>
  /// Copies the palette into Gum's V3 color slots.
  /// </summary>
  /// <param name="colors">Gum color slots.</param>
  /// <param name="palette">Palette to copy.</param>
  private static void ApplyColors(Colors colors, EditorPalette palette)
  {
    colors.Primary = ToSkColor(palette.Primary);
    colors.Accent = ToSkColor(palette.Accent);
    colors.SurfaceVariant = ToSkColor(palette.SurfaceVariant);
    colors.InputBackground = ToSkColor(palette.InputBackground);
    colors.TextPrimary = ToSkColor(palette.TextPrimary);
    colors.TextMuted = ToSkColor(palette.TextMuted);
    colors.IconDefault = ToSkColor(palette.IconDefault);
    colors.Success = ToSkColor(palette.Success);
    colors.Danger = ToSkColor(palette.Danger);

    // Hover/pressed colors are derived from the base colors by these percentages when a
    // control is created.
    colors.PercentDarken = PressedDarkenPercent;
    colors.PercentLighten = HoverLightenPercent;

    // Gum draws the keyboard-focus bar under every focused control in Colors.Warning; the editor
    // is mouse-driven, so the bars are hidden. Gum uses no other Warning color in its controls.
    colors.Warning = SKColors.Transparent;
  }

  /// <summary>
  /// Registers the Inter regular and bold cuts with Gum under <see cref="EditorFonts.FamilyName"/>.
  /// </summary>
  /// <returns>True if the regular cut was registered; false (logged) if the theme keeps Gum's font.</returns>
  private static bool RegisterFonts()
  {
    if (!RegisterFont(EditorFonts.RegularFile, styleSlot: null)) return false;
    RegisterFont(EditorFonts.BoldFile, BoldStyleSlot); // without it, bold falls back to regular
    return true;
  }

  /// <summary>
  /// Registers one font file. Problems are logged, never thrown, so a broken install still has a UI.
  /// </summary>
  /// <param name="fileName">Font file name from <see cref="EditorFonts"/>.</param>
  /// <param name="styleSlot">Gum style slot (<c>"Bold"</c>, ...), or null for the regular cut.</param>
  /// <returns>True if registered.</returns>
  private static bool RegisterFont(string fileName, string? styleSlot)
  {
    string path = EditorFonts.ResolvePath(fileName);
    if (!File.Exists(path))
    {
      Log.Error($"Theme font not found: {path}");
      return false;
    }

    if (GumFontMapper.RegisterFont(EditorFonts.FamilyName, File.ReadAllBytes(path), styleSlot) == null)
    {
      Log.Error($"Theme font could not be loaded: {path}");
      return false;
    }

    return true;
  }

  /// <summary>
  /// Points all Gum text styles at the registered font family.
  /// </summary>
  /// <param name="text">Gum text styles.</param>
  private static void ApplyFont(Text text)
  {
    foreach (StateSave state in new[] { text.Normal, text.Strong, text.Emphasis })
    {
      state.SetValue(FontVariable, EditorFonts.FamilyName, StringType);
    }
  }
}
