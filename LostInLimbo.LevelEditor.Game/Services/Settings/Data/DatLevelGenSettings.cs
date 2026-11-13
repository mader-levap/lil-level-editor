using LostInLimbo.LevelGen.Data.Prop;

namespace LostInLimbo.LevelEditor.Services.Settings.Data;

/// <summary>
/// Saved state of the level generation overlay's forms.
/// </summary>
public sealed record DatLevelGenSettings
{
  // Defaults.

  /// <summary>Default instance (forms at their defaults).</summary>
  public static DatLevelGenSettings Default { get; } = new();

  // Fields.

  /// <summary>State of the Properties form (it edits exactly the library's properties).</summary>
  public PropMain Properties { get; init; } = PropMain.Default;

  /// <summary>State of the Configuration form.</summary>
  public DatConfigFormSettings Configuration { get; init; } = DatConfigFormSettings.Default;
}
