using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Ui.Gum.App.Info;
using LostInLimbo.LevelGen.Data.Config;
using LostInLimbo.LevelGen.Data.Prop;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.App.Info;

/// <summary>
/// Headless tests for <see cref="LevelInfoRefreshCheck"/> (when the level info overlay must refresh).
/// </summary>
public sealed class LevelInfoRefreshCheckTests
{
  /// <summary>
  /// Creates run inputs as the service does for every run started from scratch.
  /// </summary>
  /// <returns>New run inputs object.</returns>
  private static LevelGenRunInputs NewRunInputs() =>
    new(PropMain.Default with { TextSeed = "mordor" }, CnfLevelGeneration.Default);

  /// <summary>
  /// Verifies that nothing is stale while the result and the run are unchanged.
  /// </summary>
  [Fact]
  public void IsStale_SameResultAndRun_IsFalse()
  {
    // Arrange: The shown run is the current run.
    LevelGenRunInputs inputs = NewRunInputs();

    // Act: Check.
    bool stale = LevelInfoRefreshCheck.IsStale(EnGenResult.Finished, inputs, EnGenResult.Finished, inputs);

    // Assert: Not stale.
    Assert.False(stale);
  }

  /// <summary>
  /// Verifies that a new run that started and finished between two checks is noticed, even though
  /// the result is the same and its inputs are equal by value (the F5 bug).
  /// </summary>
  [Fact]
  public void IsStale_NewRunWithSameResult_IsTrue()
  {
    // Arrange: Equal but separate run inputs (a re-run with the same values).
    LevelGenRunInputs shown = NewRunInputs();
    LevelGenRunInputs current = NewRunInputs();
    Assert.Equal(shown, current);

    // Act: Check Finished -> Finished.
    bool stale = LevelInfoRefreshCheck.IsStale(EnGenResult.Finished, shown, EnGenResult.Finished, current);

    // Assert: Stale.
    Assert.True(stale);
  }

  /// <summary>
  /// Verifies that a result change of the same run is noticed.
  /// </summary>
  [Fact]
  public void IsStale_ResultChanged_IsTrue()
  {
    // Arrange: Same run.
    LevelGenRunInputs inputs = NewRunInputs();

    // Act: Check Running -> Finished.
    bool stale = LevelInfoRefreshCheck.IsStale(EnGenResult.Running, inputs, EnGenResult.Finished, inputs);

    // Assert: Stale.
    Assert.True(stale);
  }

  /// <summary>
  /// Verifies that a cleared session (run inputs dropped) is noticed.
  /// </summary>
  [Fact]
  public void IsStale_RunInputsCleared_IsTrue()
  {
    // Act: Check with the inputs gone and the same result.
    bool stale = LevelInfoRefreshCheck.IsStale(EnGenResult.None, NewRunInputs(), EnGenResult.None, null);

    // Assert: Stale.
    Assert.True(stale);
  }

  /// <summary>
  /// Verifies that no run before and after, with the same result, is not stale.
  /// </summary>
  [Fact]
  public void IsStale_NoRunInputsBothTimes_IsFalse()
  {
    // Act: Check without any run.
    bool stale = LevelInfoRefreshCheck.IsStale(EnGenResult.None, null, EnGenResult.None, null);

    // Assert: Not stale.
    Assert.False(stale);
  }

  /// <summary>
  /// Verifies that a panel that was never refreshed is stale.
  /// </summary>
  [Fact]
  public void IsStale_NeverRefreshed_IsTrue()
  {
    // Act: Check with no shown result.
    bool stale = LevelInfoRefreshCheck.IsStale(null, null, EnGenResult.None, null);

    // Assert: Stale.
    Assert.True(stale);
  }
}
