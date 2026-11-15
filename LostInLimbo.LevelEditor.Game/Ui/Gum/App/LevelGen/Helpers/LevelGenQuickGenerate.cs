using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Helpers;

/// <summary>
/// Rules for quick generation (F5): one run with the next seed, as one loop iteration would do.
/// </summary>
public static class LevelGenQuickGenerate
{
  /// <summary>
  /// Decides whether quick generation may start a run.
  /// </summary>
  /// <param name="result">Current generation result; null means unknown.</param>
  /// <param name="loopActive">Whether a generation loop is active.</param>
  /// <param name="awaitingResult">Whether an earlier request's outcome is not reported yet.</param>
  /// <returns>
  /// True if no loop is active, no request is pending, and the result is
  /// <see cref="EnGenResult.None"/>, <see cref="EnGenResult.Finished"/> or
  /// <see cref="EnGenResult.Cancelled"/>. A run in progress (running, paused) or a run that ended
  /// with <see cref="EnGenResult.Error"/> or <see cref="EnGenResult.ValidationError"/> blocks it.
  /// </returns>
  public static bool CanStart(EnGenResult? result, bool loopActive, bool awaitingResult) =>
    !loopActive
    && !awaitingResult
    && result is EnGenResult.None or EnGenResult.Finished or EnGenResult.Cancelled;
}
