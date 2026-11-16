using System;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Helpers;

/// <summary>
/// Rules for quick generation (F5): one run with the seed in the box, or with the next seed once
/// that one was generated.
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

  /// <summary>
  /// Chooses the seed of a quick generation run: the seed in the box if it was not generated yet
  /// (first press, or the user typed another seed), otherwise the next one. So the box always
  /// shows the seed of the map on screen, and a failed seed stays there to retry with Start.
  /// </summary>
  /// <param name="seedText">Seed text box content.</param>
  /// <param name="lastRunSeed">Text seed of the current or last run; null if there was none.</param>
  /// <returns>
  /// <see cref="LevelGenLoopSeed.FallbackSeed"/> for a blank box (a blank seed would make the
  /// library pick a time-based one); <paramref name="seedText"/> unchanged if it differs from
  /// <paramref name="lastRunSeed"/> (compared as text); otherwise
  /// <see cref="LevelGenLoopSeed.Advance"/> of it.
  /// </returns>
  public static string SeedFor(string? seedText, string? lastRunSeed)
  {
    if (string.IsNullOrWhiteSpace(seedText)) return LevelGenLoopSeed.FallbackSeed;
    return string.Equals(seedText, lastRunSeed, StringComparison.Ordinal) ? LevelGenLoopSeed.Advance(seedText) : seedText;
  }
}
