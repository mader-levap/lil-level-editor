using LostInLimbo.LevelEditor.Services.SystemInfo.Data;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.Log;

namespace LostInLimbo.LevelEditor.Ui.Gum.Theme;

/// <summary>
/// All colors of the editor UI in one place: the colors fed into Gum's V3 style (Forms
/// controls) and the colors our own code uses (panels, text, log severities, build types).
/// </summary>
/// <remarks>
/// Engine-neutral (<see cref="UiRgba"/>), so it can be unit-tested headlessly.
/// <see cref="EditorTheme"/> maps it onto Gum.
/// </remarks>
public sealed record EditorPalette
{
  #region Surfaces

  /// <summary>Background of our panels (app bar, overlays, cards); slightly translucent over 3D.</summary>
  public required UiRgba PanelBackground { get; init; }
  /// <summary>Gum: secondary surfaces (for example scroll viewer and list backgrounds).</summary>
  public required UiRgba SurfaceVariant { get; init; }
  /// <summary>Gum: background of text boxes and combo boxes.</summary>
  public required UiRgba InputBackground { get; init; }

  /// <summary>Gum: base color of buttons and other interactive controls (disabled/checked looks derive from it).</summary>
  public required UiRgba Primary { get; init; }
  /// <summary>Gum: highlight color (selected list items, hover accents); the theme's single accent.</summary>
  public required UiRgba Accent { get; init; }

  #endregion

  #region Text and icons

  /// <summary>Main text color.</summary>
  public required UiRgba TextPrimary { get; init; }
  /// <summary>Secondary text color (placeholders, hints).</summary>
  public required UiRgba TextMuted { get; init; }

  /// <summary>Gum: color of built-in icons (check marks, dropdown arrows).</summary>
  public required UiRgba IconDefault { get; init; }

  #endregion

  #region Other

  /// <summary>Success color.</summary>
  public required UiRgba Success { get; init; }
  /// <summary>Warning color.</summary>
  public required UiRgba Danger { get; init; }

  #endregion

  #region Progress

  /// <summary>Unfilled part of progress bars.</summary>
  public required UiRgba ProgressTrack { get; init; }
  /// <summary>Filled part of progress bars.</summary>
  public required UiRgba ProgressFill { get; init; }
  /// <summary>Translucent moving bands over the filled part of progress bars.</summary>
  public required UiRgba ProgressBand { get; init; }

  #endregion

  #region Semantic colors

  /// <summary>Color for debug log lines.</summary>
  public required UiRgba LogDebug { get; init; }
  /// <summary>Color for info log lines.</summary>
  public required UiRgba LogInfo { get; init; }
  /// <summary>Color for success log lines.</summary>
  public required UiRgba LogSuccess { get; init; }
  /// <summary>Color for warning log lines.</summary>
  public required UiRgba LogWarning { get; init; }
  /// <summary>Color for error log lines.</summary>
  public required UiRgba LogError { get; init; }

  /// <summary>Highlight for development builds in the app bar.</summary>
  public required UiRgba BuildDev { get; init; }
  /// <summary>Highlight for debug builds in the app bar.</summary>
  public required UiRgba BuildDebug { get; init; }
  /// <summary>Highlight for release builds in the app bar.</summary>
  public required UiRgba BuildRelease { get; init; }
  /// <summary>Highlight for unknown builds in the app bar.</summary>
  public required UiRgba BuildUnknown { get; init; }

  #endregion

  /// <summary>
  /// Default dark neutral palette with one accent: green with a slight blue tint.
  /// </summary>
  public static EditorPalette Default { get; } = new()
  {
    PanelBackground = new UiRgba(0x1E, 0x1F, 0x22, 0xE0),
    SurfaceVariant = new UiRgba(0x2B, 0x2D, 0x31),
    InputBackground = new UiRgba(0x16, 0x17, 0x19),

    // Buttons use a darker shade of the accent: Gum derives disabled/checked looks from Primary,
    // and a neutral grey made enabled and disabled buttons indistinguishable.
    Primary = new UiRgba(0x2B, 0x8A, 0x63),
    Accent = new UiRgba(0x3D, 0xB0, 0x82),

    TextPrimary = new UiRgba(0xE6, 0xE6, 0xE6),
    TextMuted = new UiRgba(0x9A, 0xA0, 0xA6),

    IconDefault = new UiRgba(0xF8, 0xF8, 0xF8),

    Success = new UiRgba(0x6C, 0xC0, 0x70),
    Danger = new UiRgba(0xE5, 0x53, 0x4B),

    ProgressTrack = new UiRgba(0x16, 0x17, 0x19),
    ProgressFill = new UiRgba(0x2B, 0x8A, 0x63),
    ProgressBand = new UiRgba(0x3D, 0xB0, 0x82, 0x60),

    LogDebug = new UiRgba(0x99, 0x99, 0x99),
    LogInfo = new UiRgba(0xE6, 0xE6, 0xE6),
    LogSuccess = new UiRgba(0x6C, 0xFF, 0x70),
    LogWarning = new UiRgba(0xE0, 0xA0, 0x40),
    LogError = new UiRgba(0xFF, 0x53, 0x4B),

    BuildDev = new UiRgba(0xE0, 0xA0, 0x40),
    BuildDebug = new UiRgba(0xF8, 0xF8, 0x4B),
    BuildRelease = new UiRgba(0x6C, 0xF8, 0x70),
    BuildUnknown = new UiRgba(0xF8, 0x53, 0x4B),
  };

  /// <summary>
  /// Gets the text color of a status log line.
  /// </summary>
  /// <param name="severity">Line severity.</param>
  /// <returns>Text color; <see cref="TextPrimary"/> for unknown values.</returns>
  public UiRgba ForSeverity(EnStatusSeverity severity) => severity switch
  {
    EnStatusSeverity.Debug => LogDebug,
    EnStatusSeverity.Info => LogInfo,
    EnStatusSeverity.Success => LogSuccess,
    EnStatusSeverity.Warning => LogWarning,
    EnStatusSeverity.Error => LogError,
    _ => TextPrimary,
  };

  /// <summary>
  /// Gets the label color of a build type in the app bar.
  /// </summary>
  /// <param name="buildType">Build type.</param>
  /// <returns>Label color; <see cref="BuildUnknown"/> for unrecognized values.</returns>
  public UiRgba ForBuildType(EnBuildType buildType) => buildType switch
  {
    EnBuildType.Development => BuildDev,
    EnBuildType.Debug => BuildDebug,
    EnBuildType.Release => BuildRelease,
    _ => BuildUnknown,
  };
}
