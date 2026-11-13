using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Common;

/// <summary>
/// Headless tests for <see cref="UiRgba"/>.
/// </summary>
public sealed class UiRgbaTests
{
  /// <summary>
  /// Verifies that scaling the alpha keeps the color channels and rounds the alpha.
  /// </summary>
  /// <param name="alpha">Original alpha.</param>
  /// <param name="factor">Alpha multiplier.</param>
  /// <param name="expected">Expected alpha.</param>
  [Theory]
  [InlineData(128, 0.2f, 26)] // 25.6 rounds up
  [InlineData(255, 0.5f, 128)] // 127.5 rounds to even: 128
  [InlineData(200, 1f, 200)]
  [InlineData(200, 0f, 0)]
  public void WithAlphaScaled_ScalesAlphaOnly(byte alpha, float factor, byte expected)
  {
    // Arrange: A translucent color.
    var color = new UiRgba(10, 20, 30, alpha);

    // Act: Scale its alpha.
    UiRgba result = color.WithAlphaScaled(factor);

    // Assert: Same color channels, scaled alpha.
    Assert.Equal(new UiRgba(10, 20, 30, expected), result);
  }

  /// <summary>
  /// Verifies that factors outside 0..1 (and NaN) are clamped.
  /// </summary>
  /// <param name="factor">Alpha multiplier.</param>
  /// <param name="expected">Expected alpha.</param>
  [Theory]
  [InlineData(-1f, 0)]
  [InlineData(2f, 100)]
  [InlineData(float.NaN, 0)]
  public void WithAlphaScaled_OutOfRangeFactor_IsClamped(float factor, byte expected)
  {
    // Act: Scale with an out-of-range factor.
    UiRgba result = new UiRgba(1, 2, 3, 100).WithAlphaScaled(factor);

    // Assert: Alpha within the original range.
    Assert.Equal(expected, result.Alpha);
  }
}
