using LostInLimbo.LevelEditor.Ui.Gum.LevelGen;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

/// <summary>
/// Headless tests for <see cref="LevelGenUiState"/> (button enable matrix and form visibility).
/// </summary>
public sealed class LevelGenUiStateTests
{
  /// <summary>
  /// Verifies the state for every generation result (matrix carried over from the UGS overlay,
  /// plus form visibility: hidden while running or paused).
  /// </summary>
  [Theory]
  [InlineData(EnGenResult.None, true, true, false, false, true)]
  [InlineData(EnGenResult.Running, false, false, true, true, false)]
  [InlineData(EnGenResult.Paused, true, true, false, false, false)]
  [InlineData(EnGenResult.Finished, true, true, false, false, true)]
  [InlineData(EnGenResult.Cancelled, true, true, false, false, true)]
  [InlineData(EnGenResult.Error, true, true, false, false, true)]
  [InlineData(EnGenResult.ValidationError, true, true, false, false, true)]
  public void For_Result_ReturnsExpectedState(EnGenResult result, bool start, bool step, bool pause, bool stop, bool propsVisible)
  {
    // Act: Resolve the UI state.
    LevelGenUiState state = LevelGenUiState.For(result);

    // Assert: Buttons and form visibility match.
    Assert.Equal(new LevelGenUiState(start, step, pause, stop, propsVisible), state);
  }

  /// <summary>
  /// Verifies that every enum value is covered (a new result must be added to the matrix).
  /// </summary>
  [Fact]
  public void For_EveryDefinedResult_EnablesSomething()
  {
    foreach (EnGenResult result in Enum.GetValues<EnGenResult>())
    {
      // Act: Resolve the UI state.
      LevelGenUiState state = LevelGenUiState.For(result);

      // Assert: Not the all-disabled fallback.
      Assert.NotEqual(LevelGenUiState.AllDisabled, state);
    }
  }

  /// <summary>
  /// Verifies that a null result disables everything.
  /// </summary>
  [Fact]
  public void For_Null_DisablesEverything()
  {
    // Act: Resolve the UI state for an unknown status.
    LevelGenUiState state = LevelGenUiState.For(null);

    // Assert: All disabled, form hidden.
    Assert.Equal(LevelGenUiState.AllDisabled, state);
  }
}
