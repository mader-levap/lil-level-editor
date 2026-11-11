using LostInLimbo.LevelEditor.Ui.Gum.LevelGen;
using LostInLimbo.LevelGen.Data.Prop;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

/// <summary>
/// Headless tests for <see cref="DifficultyPresets"/> and <see cref="LevelGenPropsBuilder"/>.
/// </summary>
public sealed class LevelGenPropsTests
{
  /// <summary>
  /// Verifies that the placeholder presets are Easy 0 / Medium 50 / Hard 100, in that order.
  /// </summary>
  [Fact]
  public void Presets_MatchPlaceholderTable()
  {
    // Act: Read the table.
    IReadOnlyList<DifficultyPreset> presets = DifficultyPresets.All;

    // Assert: Names and values in display order.
    Assert.Equal(
      [new DifficultyPreset("Easy", 0), new DifficultyPreset("Medium", 50), new DifficultyPreset("Hard", 100)],
      presets);
  }

  /// <summary>
  /// Verifies that preset values are unique and within the library's allowed range.
  /// </summary>
  [Fact]
  public void Presets_ValuesAreUniqueAndInRange()
  {
    // Act: Collect values.
    int[] values = DifficultyPresets.All.Select(preset => preset.Value).ToArray();

    // Assert: Unique, and each passes PropMain validation range.
    Assert.Equal(values.Length, values.Distinct().Count());
    Assert.All(values, value => Assert.InRange(value, 0, PropMain.MaxDifficulty));
  }

  /// <summary>
  /// Verifies that the default selection matches the library default difficulty.
  /// </summary>
  [Fact]
  public void DefaultIndex_MatchesLibraryDefaultDifficulty()
  {
    // Act: Read the default preset.
    DifficultyPreset preset = DifficultyPresets.All[DifficultyPresets.DefaultIndex];

    // Assert: Same as PropMain.Default.
    Assert.Equal(PropMain.Default.Difficulty, preset.Value);
  }

  /// <summary>
  /// Verifies that an unknown value maps to the first preset.
  /// </summary>
  [Fact]
  public void FindIndex_UnknownValue_ReturnsZero()
  {
    // Act + Assert: A value with no preset falls back to index 0.
    Assert.Equal(0, DifficultyPresets.FindIndex(37));
  }

  /// <summary>
  /// Verifies that a blank seed becomes null (library uses current time).
  /// </summary>
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Build_BlankSeed_ProducesNullSeed(string? seedText)
  {
    // Act: Build from a blank seed.
    PropMain props = LevelGenPropsBuilder.Build(seedText, 0, false);

    // Assert: Seed left to the library.
    Assert.Null(props.TextSeed);
  }

  /// <summary>
  /// Verifies that form values are copied unchanged and the result is valid.
  /// </summary>
  [Fact]
  public void Build_Values_AreCopiedAndValid()
  {
    // Act: Build from typical form values.
    PropMain props = LevelGenPropsBuilder.Build(" my seed ", 50, true);

    // Assert: Values copied as entered (no trimming), other fields default, validation passes.
    Assert.Equal(" my seed ", props.TextSeed);
    Assert.Equal(50, props.Difficulty);
    Assert.True(props.ManyThemes);
    Assert.Equal(PropMain.Default.ConfigVersion, props.ConfigVersion);
    props.Validate();
  }
}
