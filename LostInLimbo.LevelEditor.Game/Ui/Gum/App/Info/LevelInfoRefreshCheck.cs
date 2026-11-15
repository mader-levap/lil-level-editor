using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.Info;

/// <summary>
/// Decides when the level information overlay shows stale data and must refresh.
/// </summary>
public static class LevelInfoRefreshCheck
{
  /// <summary>
  /// Checks whether the shown data is stale.
  /// </summary>
  /// <param name="shownResult">Generation result at the last refresh; null if never refreshed.</param>
  /// <param name="shownRunInputs">Run inputs at the last refresh; null if there was no run.</param>
  /// <param name="currentResult">Current generation result.</param>
  /// <param name="currentRunInputs">Current run inputs; null if there is no run.</param>
  /// <returns>True if the result changed or a different run is current.</returns>
  /// <remarks>
  /// A result check alone misses a run that started and ended between two checks (for example
  /// F5 with near-instant generation: <c>Finished</c> before and after). Run inputs are compared
  /// by reference: the service creates a new object for every run started from scratch and keeps
  /// it on pause, resume and step, while value equality would treat a re-run with the same inputs
  /// as unchanged.
  /// </remarks>
  public static bool IsStale(EnGenResult? shownResult, LevelGenRunInputs? shownRunInputs,
                             EnGenResult currentResult, LevelGenRunInputs? currentRunInputs)
    => currentResult != shownResult || !ReferenceEquals(currentRunInputs, shownRunInputs);
}
