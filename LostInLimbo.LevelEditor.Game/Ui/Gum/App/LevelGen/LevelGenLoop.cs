using System;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen;

/// <summary>
/// State of continuous (loop) generation: whether a loop is active and which seed its current
/// iteration uses. Engine- and UI-independent; the panel only wires the UI to it.
/// </summary>
/// <remarks>
/// <para>
/// The first iteration uses the form seed (see <see cref="LevelGenLoopSeed.First"/>), every next
/// one increments it by 1, so any failing iteration can be reproduced from its seed alone, provided
/// other prop form settings are same.
/// </para>
/// <para>
/// The loop ends on the first result other than <see cref="EnGenResult.Finished"/>, or on <see cref="End"/>.
/// </para>
/// </remarks>
public sealed class LevelGenLoop
{
  /// <summary>Whether a loop is in progress.</summary>
  public bool IsActive { get; private set; }

  /// <summary>
  /// Seed of the current iteration while active; after a failure, the seed of the failed
  /// iteration.
  /// </summary>
  public string CurrentSeed { get; private set; } = LevelGenLoopSeed.FallbackSeed;

  /// <summary>
  /// Starts a loop.
  /// </summary>
  /// <param name="seedText">Seed text box content; non-numeric text starts from <see cref="LevelGenLoopSeed.FallbackSeed"/>.</param>
  /// <exception cref="InvalidOperationException">A loop is already active.</exception>
  public void Begin(string? seedText)
  {
    if (IsActive) throw new InvalidOperationException("Generation loop is already active.");

    CurrentSeed = LevelGenLoopSeed.First(seedText);
    IsActive = true;
  }

  /// <summary>
  /// Reports that the current iteration's run ended and decides what happens next.
  /// </summary>
  /// <param name="result">Result the run reached.</param>
  /// <returns>
  /// <see cref="EnLoopDecision.Continue"/> for <see cref="EnGenResult.Finished"/>
  /// (<see cref="CurrentSeed"/> is advanced to the next seed); otherwise
  /// <see cref="EnLoopDecision.Failed"/> (the loop ends and <see cref="CurrentSeed"/> keeps the
  /// failed seed).
  /// </returns>
  /// <exception cref="InvalidOperationException">No loop is active.</exception>
  /// <exception cref="ArgumentException"><paramref name="result"/> is <see cref="EnGenResult.Running"/>.</exception>
  public EnLoopDecision OnRunEnded(EnGenResult result)
  {
    if (!IsActive) throw new InvalidOperationException("Generation loop is not active.");
    if (result == EnGenResult.Running)
      throw new ArgumentException("A running generation has not ended.", nameof(result));

    if (result == EnGenResult.Finished)
    {
      CurrentSeed = LevelGenLoopSeed.Next(CurrentSeed);
      return EnLoopDecision.Continue;
    }

    IsActive = false;
    return EnLoopDecision.Failed;
  }

  /// <summary>
  /// Ends the loop (stopped by the user, or the next run could not be started). Does nothing
  /// when no loop is active.
  /// </summary>
  public void End() => IsActive = false;
}
