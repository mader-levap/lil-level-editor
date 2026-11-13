namespace LostInLimbo.LevelEditor.Services.Settings.Data;

/// <summary>
/// Root of the editor settings file: one section per editor area.
/// </summary>
/// <remarks>
/// New areas are added as new sections; a section missing from the file keeps its defaults.
/// </remarks>
public sealed record DatEditorSettings
{
  /// <summary>Settings file format version written by this editor.</summary>
  public const int CurrentVersion = 1;

  // Defaults.

  /// <summary>Default instance (used when no file exists or it cannot be read).</summary>
  public static DatEditorSettings Default { get; } = new();

  // Fields.

  /// <summary>
  /// Format version of the file. Increment when the format changes incompatibly, so older
  /// files can be detected.
  /// </summary>
  public int SettingsVersion { get; init; } = CurrentVersion;

  /// <summary>Level generation overlay settings.</summary>
  public DatLevelGenSettings LevelGen { get; init; } = DatLevelGenSettings.Default;
}
