using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelGen.Data.Config.Graph;

namespace LostInLimbo.LevelEditor.Services.Settings.Data;

/// <summary>
/// State of the Configuration form: whether its overrides are active and the values picked.
/// </summary>
/// <remarks>
/// Values are kept while the form is inactive, so the user's last picks come back.
/// </remarks>
public sealed record DatConfigFormSettings
{
  // Defaults.

  /// <summary>Default instance (inactive, first builder).</summary>
  public static DatConfigFormSettings Default { get; } = new();

  // Fields.

  /// <summary>Whether the overrides are applied.</summary>
  public bool Active { get; init; } = false;

  /// <summary>Graph builder picked in the form.</summary>
  public EnGraphBuilder GraphBuilder { get; init; } = EnGraphBuilder.OneNode;

  // Helpers.

  /// <summary>
  /// Overrides these settings stand for.
  /// </summary>
  /// <returns>The picked values while active; <see cref="LevelGenConfigOverrides.None"/> otherwise.</returns>
  public LevelGenConfigOverrides ToOverrides() =>
    Active ? new LevelGenConfigOverrides(GraphBuilder: GraphBuilder) : LevelGenConfigOverrides.None;
}
