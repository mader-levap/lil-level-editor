using System;
using LostInLimbo.LevelEditor.Ui.Gum.Components.ProgressBar;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.ProgressBar;

/// <summary>
/// Headless tests for <see cref="ProgressBarMath"/>.
/// </summary>
public sealed class ProgressBarMathTests
{
  #region Clamp01

  /// <summary>
  /// Verifies that out-of-range and NaN values are clamped to 0..1.
  /// </summary>
  [Theory]
  [InlineData(float.NaN, 0f)]
  [InlineData(-0.5f, 0f)]
  [InlineData(0.25f, 0.25f)]
  [InlineData(1.5f, 1f)]
  public void Clamp01_Value_IsInRange(float value, float expected)
  {
    // Act: Clamp the value.
    float clamped = ProgressBarMath.Clamp01(value);

    // Assert: Value is correct.
    Assert.Equal(expected, clamped);
  }

  #endregion

  #region FormatPercent

  /// <summary>
  /// Verifies the percentage text, rounded down so an unfinished run never shows 100 %.
  /// </summary>
  [Theory]
  [InlineData(0f, "0 %")]
  [InlineData(0.29f, "29 %")]
  [InlineData(0.37f, "37 %")]
  [InlineData(0.999f, "99 %")]
  [InlineData(1f, "100 %")]
  [InlineData(2f, "100 %")]
  public void FormatPercent_Completion_ReturnsExpectedText(float completion, string expected)
  {
    // Act: Format the completion.
    string text = ProgressBarMath.FormatPercent(completion);

    // Assert: Text is correct.
    Assert.Equal(expected, text);
  }

  #endregion

  #region Approach

  /// <summary>
  /// Verifies that the shown value moves toward a higher target without reaching it in one short frame.
  /// </summary>
  [Fact]
  public void Approach_HigherTarget_MovesTowardWithoutOvershoot()
  {
    // Act: One 1/60 s frame from 0 toward 1.
    float next = ProgressBarMath.Approach(shown: 0f, target: 1f, deltaSeconds: 1f / 60f, ratePerSecond: 8f);

    // Assert: Value moved, but is still below the target.
    Assert.InRange(next, 0.01f, 0.99f);
  }

  /// <summary>
  /// Verifies that easing ends exactly on the target after enough time.
  /// </summary>
  [Fact]
  public void Approach_LongTime_ReachesTargetExactly()
  {
    // Arrange: Start at 0.
    float shown = 0f;

    // Act: Ten seconds of 1/60 s frames.
    for (int i = 0; i < 600; i++) shown = ProgressBarMath.Approach(shown, 0.5f, 1f / 60f, 8f);

    // Assert: Snapped to the target.
    Assert.Equal(0.5f, shown);
  }

  /// <summary>
  /// Verifies that a lower target (new run) snaps immediately.
  /// </summary>
  [Fact]
  public void Approach_LowerTarget_Snaps()
  {
    // Act: Shown 1.0, new run reports 0.
    float next = ProgressBarMath.Approach(shown: 1f, target: 0f, deltaSeconds: 1f / 60f, ratePerSecond: 8f);

    // Assert: Value is the target.
    Assert.Equal(0f, next);
  }

  /// <summary>
  /// Verifies that zero elapsed time keeps the shown value.
  /// </summary>
  [Fact]
  public void Approach_ZeroDelta_KeepsValue()
  {
    // Act: No time passed.
    float next = ProgressBarMath.Approach(shown: 0.2f, target: 0.8f, deltaSeconds: 0f, ratePerSecond: 8f);

    // Assert: Value unchanged.
    Assert.Equal(0.2f, next);
  }

  #endregion

  #region WrapOffset

  /// <summary>
  /// Verifies that offsets wrap into [0, period).
  /// </summary>
  [Theory]
  [InlineData(5f, 24f, 5f)]
  [InlineData(24f, 24f, 0f)]
  [InlineData(30f, 24f, 6f)]
  [InlineData(-6f, 24f, 18f)]
  public void WrapOffset_Offset_IsWrapped(float offset, float period, float expected)
  {
    // Act: Wrap the offset.
    float wrapped = ProgressBarMath.WrapOffset(offset, period);

    // Assert: Value is correct.
    Assert.Equal(expected, wrapped, precision: 4);
  }

  /// <summary>
  /// Verifies that a non-positive period is rejected.
  /// </summary>
  [Fact]
  public void WrapOffset_NonPositivePeriod_Throws()
  {
    // Act + Assert: Zero period throws.
    Assert.Throws<ArgumentOutOfRangeException>(() => ProgressBarMath.WrapOffset(1f, 0f));
  }

  #endregion
}
