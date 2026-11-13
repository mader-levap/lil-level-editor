using LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Helpers;
using LostInLimbo.LevelGen.Data.Prop;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.App.LevelGen.Helpers;

/// <summary>
/// Headless tests for <see cref="LevelGenPropsBuilder"/>.
/// </summary>
public sealed class LevelGenPropsBuilderTests
{
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
