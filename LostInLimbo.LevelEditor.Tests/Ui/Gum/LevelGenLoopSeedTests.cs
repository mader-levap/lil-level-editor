using LostInLimbo.LevelEditor.Ui.Gum.LevelGen;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

/// <summary>
/// Headless tests for <see cref="LevelGenLoopSeed"/> (loop seed fallback and increment).
/// </summary>
public sealed class LevelGenLoopSeedTests
{
  /// <summary>
  /// Verifies that numeric seeds are used verbatim for the first iteration.
  /// </summary>
  [Theory]
  [InlineData("0")]
  [InlineData("5")]
  [InlineData("007")]
  [InlineData("-3")]
  [InlineData("9223372036854775807")]
  public void First_NumericSeed_IsKeptVerbatim(string seedText)
  {
    // Act: Resolve the first seed.
    string seed = LevelGenLoopSeed.First(seedText);

    // Assert: Unchanged text.
    Assert.Equal(seedText, seed);
  }

  /// <summary>
  /// Verifies that empty, non-numeric, padded and out-of-range seeds fall back to "0".
  /// </summary>
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("mordor")]
  [InlineData(" 5")]
  [InlineData("5 ")]
  [InlineData("1.5")]
  [InlineData("1,000")]
  [InlineData("9223372036854775808")]
  public void First_NonNumericSeed_FallsBackToZero(string? seedText)
  {
    // Act: Resolve the first seed.
    string seed = LevelGenLoopSeed.First(seedText);

    // Assert: Fallback seed "0".
    Assert.Equal("0", seed);
    Assert.Equal(LevelGenLoopSeed.FallbackSeed, seed);
  }

  /// <summary>
  /// Verifies that the next seed is the current one plus 1, in canonical form.
  /// </summary>
  [Theory]
  [InlineData("0", "1")]
  [InlineData("007", "8")]
  [InlineData("-1", "0")]
  [InlineData("-5", "-4")]
  [InlineData("9223372036854775807", "-9223372036854775808")]
  public void Next_NumericSeed_Increments(string currentSeed, string expected)
  {
    // Act: Advance the seed.
    string next = LevelGenLoopSeed.Next(currentSeed);

    // Assert: Incremented (wrapping at the 64-bit limit).
    Assert.Equal(expected, next);
  }

  /// <summary>
  /// Verifies that a non-numeric current seed is rejected.
  /// </summary>
  [Fact]
  public void Next_NonNumericSeed_Throws()
  {
    // Act + Assert: Invalid seed throws.
    Assert.Throws<ArgumentException>(() => LevelGenLoopSeed.Next("mordor"));
  }
}
