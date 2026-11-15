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
}
