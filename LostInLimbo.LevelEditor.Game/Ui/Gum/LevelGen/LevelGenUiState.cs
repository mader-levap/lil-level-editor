using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Ui.Gum.LevelGen;

/// <summary>
/// What the level generation overlay enables and shows for a given generation state.
/// </summary>
/// <param name="Start">Whether the Start/Resume button is enabled.</param>
/// <param name="Step">Whether the Step button is enabled.</param>
/// <param name="Pause">Whether the Pause button is enabled.</param>
/// <param name="Stop">Whether the Stop button is enabled.</param>
/// <param name="PropsVisible">
/// Whether the properties form is shown. It is hidden while a run is in progress (running or
/// paused), because a resumed run keeps the configuration it was started with. While hidden,
/// the panel shows the generation progress bar in its place.
/// </param>
public sealed record LevelGenUiState(bool Start, bool Step, bool Pause, bool Stop, bool PropsVisible)
{
  /// <summary>Everything disabled and the form hidden (unknown state).</summary>
  public static LevelGenUiState AllDisabled { get; } = new(false, false, false, false, false);

  /// <summary>
  /// Gets the UI state for a generation result.
  /// </summary>
  /// <param name="result">Current generation result; null disables everything.</param>
  /// <returns>UI state.</returns>
  public static LevelGenUiState For(EnGenResult? result) => result switch
  {
    null => AllDisabled,
    EnGenResult.None => new(Start: true, Step: true, Pause: false, Stop: false, PropsVisible: true),
    EnGenResult.Running => new(Start: false, Step: false, Pause: true, Stop: true, PropsVisible: false),
    EnGenResult.Paused => new(Start: true, Step: true, Pause: false, Stop: false, PropsVisible: false),
    EnGenResult.Finished or EnGenResult.Cancelled or EnGenResult.Error or EnGenResult.ValidationError
      => new(Start: true, Step: true, Pause: false, Stop: false, PropsVisible: true),
    _ => AllDisabled,
  };
}
