using LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Helpers;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.App.LevelGen.Helpers;

/// <summary>
/// Headless tests for <see cref="LevelGenQuickGenerate"/> (when F5 may start a run).
/// </summary>
public sealed class LevelGenQuickGenerateTests
{
  /// <summary>
  /// Verifies which generation results allow quick generation when no loop or request is pending.
  /// </summary>
  [Theory]
  [InlineData(null, false)]
  [InlineData(EnGenResult.None, true)]
  [InlineData(EnGenResult.Finished, true)]
  [InlineData(EnGenResult.Cancelled, true)]
  [InlineData(EnGenResult.Running, false)]
  [InlineData(EnGenResult.Paused, false)]
  [InlineData(EnGenResult.Error, false)]
  [InlineData(EnGenResult.ValidationError, false)]
  public void CanStart_DependsOnResult(EnGenResult? result, bool expected)
  {
    // Act: Decide with no loop and no pending request.
    bool canStart = LevelGenQuickGenerate.CanStart(result, loopActive: false, awaitingResult: false);

    // Assert: Only idle or cleanly ended states allow it.
    Assert.Equal(expected, canStart);
  }

  /// <summary>
  /// Verifies that every result value is covered by <see cref="CanStart_DependsOnResult"/>, so a
  /// new result value is noticed.
  /// </summary>
  [Fact]
  public void CanStart_AllResultsCovered()
  {
    // Arrange: Values tested above.
    EnGenResult[] tested =
    [
      EnGenResult.None, EnGenResult.Finished, EnGenResult.Cancelled, EnGenResult.Running,
      EnGenResult.Paused, EnGenResult.Error, EnGenResult.ValidationError,
    ];

    // Assert: No value is missing.
    Assert.Empty(Enum.GetValues<EnGenResult>().Except(tested));
  }

  /// <summary>
  /// Verifies that an active loop blocks quick generation.
  /// </summary>
  [Fact]
  public void CanStart_LoopActive_IsFalse()
  {
    // Act: Decide while a loop is active.
    bool canStart = LevelGenQuickGenerate.CanStart(EnGenResult.Finished, loopActive: true, awaitingResult: false);

    // Assert: Blocked.
    Assert.False(canStart);
  }

  /// <summary>
  /// Verifies that a pending request (for example a requested pause or stop) blocks quick generation.
  /// </summary>
  [Fact]
  public void CanStart_AwaitingResult_IsFalse()
  {
    // Act: Decide while a request's outcome is pending.
    bool canStart = LevelGenQuickGenerate.CanStart(EnGenResult.Finished, loopActive: false, awaitingResult: true);

    // Assert: Blocked.
    Assert.False(canStart);
  }

  /// <summary>
  /// Verifies a seed that was not generated yet is used as entered: the first F5 after start-up,
  /// or after the user typed a different seed than the last run's.
  /// </summary>
  /// <param name="seedText">Seed text box content.</param>
  /// <param name="lastRunSeed">Seed of the last run, or null if there was none.</param>
  [Theory]
  [InlineData("41", null)]
  [InlineData("41", "40")]
  [InlineData("mordor", "40")]
  [InlineData("41", "041")] // compared as text: a re-typed seed counts as new
  public void SeedFor_SeedNotGeneratedYet_IsUsedAsIs(string seedText, string? lastRunSeed)
  {
    // Act: Choose the seed.
    string seed = LevelGenQuickGenerate.SeedFor(seedText, lastRunSeed);

    // Assert: Unchanged.
    Assert.Equal(seedText, seed);
  }

  /// <summary>
  /// Verifies the seed advances when the box still holds the seed of the last run.
  /// </summary>
  /// <param name="seedText">Seed text box content, equal to the last run's seed.</param>
  /// <param name="expected">Expected seed.</param>
  [Theory]
  [InlineData("41", "42")]
  [InlineData("mordor", LevelGenLoopSeed.FallbackSeed)]
  public void SeedFor_SeedOfLastRun_Advances(string seedText, string expected)
  {
    // Act: Choose the seed with the box still holding the last run's seed.
    string seed = LevelGenQuickGenerate.SeedFor(seedText, seedText);

    // Assert: The next seed.
    Assert.Equal(expected, seed);
  }

  /// <summary>
  /// Verifies a blank seed box gives the fallback seed, so quick generation stays reproducible
  /// (a blank seed would let the library pick a time-based one).
  /// </summary>
  /// <param name="seedText">Blank seed text box content.</param>
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void SeedFor_BlankSeed_GivesFallback(string? seedText)
  {
    // Act: Choose the seed before any run.
    string seed = LevelGenQuickGenerate.SeedFor(seedText, lastRunSeed: null);

    // Assert: Fallback seed.
    Assert.Equal(LevelGenLoopSeed.FallbackSeed, seed);
  }
}
