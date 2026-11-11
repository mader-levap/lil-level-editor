using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Ui.Gum.LevelGen;

/// <summary>
/// Status log texts for level generation actions and outcomes. Inputs are plain values so the
/// texts are testable without a generated world.
/// </summary>
public static class LevelGenStatusMessages
{
  /// <summary>
  /// Message after Start was clicked.
  /// </summary>
  /// <param name="wasPaused">Whether the run was paused before (so Start meant resume).</param>
  /// <param name="succeeded">Whether the service accepted the request.</param>
  /// <returns>Status entry.</returns>
  public static StatusEntry ForStart(bool wasPaused, bool succeeded) => (wasPaused, succeeded) switch
  {
    (true, true) => new("Resumed generation.", EnStatusSeverity.Info),
    (false, true) => new("Started generation.", EnStatusSeverity.Info),
    (true, false) => new("Failed to resume generation.", EnStatusSeverity.Error),
    (false, false) => new("Failed to start generation.", EnStatusSeverity.Error),
  };

  /// <summary>
  /// Message after Step was clicked.
  /// </summary>
  /// <param name="succeeded">Whether the service accepted the request.</param>
  /// <returns>Status entry.</returns>
  public static StatusEntry ForStep(bool succeeded) => succeeded
    ? new("Made one generation step.", EnStatusSeverity.Info)
    : new("Failed to make one generation step.", EnStatusSeverity.Error);

  /// <summary>Message when Pause was refused.</summary>
  public static StatusEntry PauseFailed { get; } = new("Failed to pause generation.", EnStatusSeverity.Error);

  /// <summary>Message when Stop was refused.</summary>
  public static StatusEntry StopFailed { get; } = new("Failed to stop generation.", EnStatusSeverity.Error);

  /// <summary>
  /// Message when a run reached a non-running state.
  /// </summary>
  /// <param name="result">Reached state.</param>
  /// <param name="nodes">Graph node count (0 if no world).</param>
  /// <param name="edges">Graph edge count (0 if no world).</param>
  /// <param name="steps">Map generation steps (0 if no world).</param>
  /// <param name="error">Error description for <see cref="EnGenResult.Error"/>; may be null.</param>
  /// <returns>Status entry.</returns>
  public static StatusEntry ForOutcome(EnGenResult result, int nodes, int edges, long steps, string? error)
  {
    string counts = $"Graph: nodes={nodes}, edges={edges}. Map: steps={steps}.";
    return result switch
    {
      EnGenResult.Finished => new($"LevelGen: Finished. {counts}", EnStatusSeverity.Success),
      EnGenResult.ValidationError => new($"LevelGen: ValidationError. {counts}", EnStatusSeverity.Warning),
      EnGenResult.Error => new($"LevelGen: Error - {error}", EnStatusSeverity.Error),
      _ => new($"LevelGen: {result}.", EnStatusSeverity.Info),
    };
  }
}
