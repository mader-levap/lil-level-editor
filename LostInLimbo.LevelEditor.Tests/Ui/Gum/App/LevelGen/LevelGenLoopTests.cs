using LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen;
using LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Data;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.App.LevelGen;

/// <summary>
/// Headless tests for <see cref="LevelGenLoop"/> (loop state and seed progression).
/// </summary>
public sealed class LevelGenLoopTests
{
  /// <summary>
  /// Verifies that a new loop is inactive.
  /// </summary>
  [Fact]
  public void New_IsInactive()
  {
    // Arrange + Act: Create the loop.
    var loop = new LevelGenLoop();

    // Assert: Not active.
    Assert.False(loop.IsActive);
  }

  /// <summary>
  /// Verifies that Begin activates the loop with the resolved first seed.
  /// </summary>
  [Theory]
  [InlineData("5", "5")]
  [InlineData("007", "007")]
  [InlineData("mordor", "0")]
  [InlineData("", "0")]
  [InlineData(null, "0")]
  public void Begin_ActivatesWithFirstSeed(string? seedText, string expectedSeed)
  {
    // Arrange: Inactive loop.
    var loop = new LevelGenLoop();

    // Act: Begin the loop.
    loop.Begin(seedText);

    // Assert: Active with the expected seed.
    Assert.True(loop.IsActive);
    Assert.Equal(expectedSeed, loop.CurrentSeed);
  }

  /// <summary>
  /// Verifies that beginning an active loop throws.
  /// </summary>
  [Fact]
  public void Begin_WhileActive_Throws()
  {
    // Arrange: Active loop.
    var loop = new LevelGenLoop();
    loop.Begin("1");

    // Act + Assert: Second begin throws.
    Assert.Throws<InvalidOperationException>(() => loop.Begin("2"));
  }

  /// <summary>
  /// Verifies that finished runs continue the loop with seeds incremented by 1.
  /// </summary>
  [Fact]
  public void OnRunEnded_Finished_ContinuesWithNextSeed()
  {
    // Arrange: Loop started at "5".
    var loop = new LevelGenLoop();
    loop.Begin("5");

    // Act: Two finished runs.
    EnLoopDecision first = loop.OnRunEnded(EnGenResult.Finished);
    string secondSeed = loop.CurrentSeed;
    EnLoopDecision second = loop.OnRunEnded(EnGenResult.Finished);

    // Assert: Continues with "6", then "7".
    Assert.Equal(EnLoopDecision.Continue, first);
    Assert.Equal("6", secondSeed);
    Assert.Equal(EnLoopDecision.Continue, second);
    Assert.Equal("7", loop.CurrentSeed);
    Assert.True(loop.IsActive);
  }

  /// <summary>
  /// Verifies that any non-finished result ends the loop and keeps the failing seed.
  /// </summary>
  [Theory]
  [InlineData(EnGenResult.ValidationError)]
  [InlineData(EnGenResult.Error)]
  [InlineData(EnGenResult.Cancelled)]
  [InlineData(EnGenResult.Paused)]
  [InlineData(EnGenResult.None)]
  public void OnRunEnded_NotFinished_Fails(EnGenResult result)
  {
    // Arrange: Loop at "5" after one finished run ("6" is current).
    var loop = new LevelGenLoop();
    loop.Begin("5");
    loop.OnRunEnded(EnGenResult.Finished);

    // Act: The run with seed "6" fails.
    EnLoopDecision decision = loop.OnRunEnded(result);

    // Assert: Loop ended, failing seed kept.
    Assert.Equal(EnLoopDecision.Failed, decision);
    Assert.False(loop.IsActive);
    Assert.Equal("6", loop.CurrentSeed);
  }

  /// <summary>
  /// Verifies that reporting a running result or reporting while inactive throws.
  /// </summary>
  [Fact]
  public void OnRunEnded_InvalidCalls_Throw()
  {
    // Arrange: Inactive and active loops.
    var inactive = new LevelGenLoop();
    var active = new LevelGenLoop();
    active.Begin("1");

    // Act + Assert: Invalid calls throw; the active loop stays active.
    Assert.Throws<InvalidOperationException>(() => inactive.OnRunEnded(EnGenResult.Finished));
    Assert.Throws<ArgumentException>(() => active.OnRunEnded(EnGenResult.Running));
    Assert.True(active.IsActive);
  }

  /// <summary>
  /// Verifies that End deactivates the loop, can be repeated, and allows a new Begin.
  /// </summary>
  [Fact]
  public void End_DeactivatesAndIsIdempotent()
  {
    // Arrange: Active loop.
    var loop = new LevelGenLoop();
    loop.Begin("1");

    // Act: End twice, then begin again.
    loop.End();
    loop.End();
    bool activeAfterEnd = loop.IsActive;
    loop.Begin("10");

    // Assert: Ended, then restarted with the new seed.
    Assert.False(activeAfterEnd);
    Assert.True(loop.IsActive);
    Assert.Equal("10", loop.CurrentSeed);
  }
}
