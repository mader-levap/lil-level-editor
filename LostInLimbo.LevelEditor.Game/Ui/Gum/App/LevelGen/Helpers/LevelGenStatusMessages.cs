using LostInLimbo.LevelEditor.Ui.Gum.Components.Log;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Helpers;

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
  /// <param name="seedText">
  /// Text seed of a fresh start, shown in quotes; empty or null means a time-based seed. Not
  /// shown on resume (a resumed run keeps the seed it was started with).
  /// </param>
  /// <returns>Status entry.</returns>
  public static StatusEntry ForStart(bool wasPaused, bool succeeded, string? seedText) => (wasPaused, succeeded) switch
  {
    (true, true) => new("Resumed generation.", EnStatusSeverity.Info),
    (false, true) => new($"Started generation with {SeedLabel(seedText)}.", EnStatusSeverity.Info),
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

  /// <summary>Message after Stop discarded a paused run.</summary>
  public static StatusEntry Cleared { get; } = new("Discarded paused generation.", EnStatusSeverity.Info);

  /// <summary>Message when Stop was refused.</summary>
  public static StatusEntry StopFailed { get; } = new("Failed to stop generation.", EnStatusSeverity.Error);

  /// <summary>
  /// Message when a run reached a non-running state.
  /// </summary>
  /// <param name="result">Reached state.</param>
  /// <param name="seedText">Text seed the run was started with; empty or null means a time-based seed.</param>
  /// <param name="nodes">Graph node count (0 if no world).</param>
  /// <param name="edges">Graph edge count (0 if no world).</param>
  /// <param name="steps">Map generation steps (0 if no world).</param>
  /// <param name="error">Error description for <see cref="EnGenResult.Error"/>; may be null.</param>
  /// <returns>Status entry.</returns>
  public static StatusEntry ForOutcome(EnGenResult result, string? seedText, int nodes, int edges, long steps, string? error)
  {
    string counts = Counts(nodes, edges, steps);
    string prefix = $"LevelGen: {SeedLabel(seedText)}";
    return result switch
    {
      EnGenResult.Finished => new($"{prefix} Finished. {counts}", EnStatusSeverity.Success),
      EnGenResult.ValidationError => new($"{prefix} ValidationError. {counts}", EnStatusSeverity.Warning),
      EnGenResult.Error => new($"{prefix} Error - {error}", EnStatusSeverity.Error),
      _ => new($"{prefix} {result}.", EnStatusSeverity.Debug),
    };
  }

  #region Loop

  /// <summary>
  /// Message when a generation loop started.
  /// </summary>
  /// <param name="seed">Seed of the first iteration.</param>
  /// <returns>Status entry.</returns>
  public static StatusEntry ForLoopStart(string seed) =>
    new($"Loop: started with seed {Quote(seed)}.", EnStatusSeverity.Info);

  /// <summary>
  /// Message when a loop iteration finished.
  /// </summary>
  /// <param name="seed">Seed of the finished iteration.</param>
  /// <param name="nodes">Graph node count (0 if no world).</param>
  /// <param name="edges">Graph edge count (0 if no world).</param>
  /// <param name="steps">Map generation steps (0 if no world).</param>
  /// <returns>Status entry.</returns>
  public static StatusEntry ForLoopIteration(string seed, int nodes, int edges, long steps) =>
    new($"Loop: seed {Quote(seed)} Finished. {Counts(nodes, edges, steps)}", EnStatusSeverity.Success);

  /// <summary>
  /// Message when a loop iteration ended with anything other than success; the loop stops.
  /// </summary>
  /// <param name="seed">Seed of the failed iteration.</param>
  /// <param name="result">Reached state.</param>
  /// <param name="error">Error description; may be null.</param>
  /// <returns>Status entry.</returns>
  public static StatusEntry ForLoopFailure(string seed, EnGenResult result, string? error)
  {
    string detail = string.IsNullOrEmpty(error) ? string.Empty : $" - {error}";
    return new($"Loop: seed {Quote(seed)} ended with {result}{detail}. Loop stopped.", EnStatusSeverity.Error);
  }

  /// <summary>
  /// Message when the service refused to start a loop iteration; the loop stops.
  /// </summary>
  /// <param name="seed">Seed of the iteration that could not start.</param>
  /// <returns>Status entry.</returns>
  public static StatusEntry ForLoopStartFailed(string seed) =>
    new($"Loop: failed to start generation with seed {Quote(seed)}. Loop stopped.", EnStatusSeverity.Error);

  /// <summary>Message when the user stopped the loop.</summary>
  public static StatusEntry LoopStopped { get; } = new("Loop: stopped by user.", EnStatusSeverity.Info);

  #endregion

  #region Helpers

  /// <summary>
  /// Formats world counts.
  /// </summary>
  /// <param name="nodes">Graph node count.</param>
  /// <param name="edges">Graph edge count.</param>
  /// <param name="steps">Map generation steps.</param>
  /// <returns>Counts text.</returns>
  private static string Counts(int nodes, int edges, long steps) =>
    $"Graph: nodes={nodes}, edges={edges}. Map: steps={steps}.";

  /// <summary>
  /// Puts a loop seed in quotes (see <see cref="LevelGenSeedFormat.Quote"/>).
  /// </summary>
  /// <param name="seed">Seed.</param>
  /// <returns>Quoted seed.</returns>
  private static string Quote(string? seed) => LevelGenSeedFormat.Quote(seed);

  /// <summary>
  /// Formats the seed of a single (non-loop) run; an empty seed is marked as time-based.
  /// </summary>
  /// <param name="seedText">Text seed; empty or null means a time-based seed.</param>
  /// <returns>For example <c>seed "abc"</c> or <c>seed "" (current time)</c>.</returns>
  private static string SeedLabel(string? seedText) => $"seed {LevelGenSeedFormat.TextSeed(seedText)}";

  #endregion
}
