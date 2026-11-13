using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Ui.Gum.App.Info;
using LostInLimbo.LevelGen.Data.Config;
using LostInLimbo.LevelGen.Data.Prop;
using LostInLimbo.LevelGen.Data.Result.Stats;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.App.Info;

/// <summary>
/// Headless tests for <see cref="LevelGraphSource"/> (whether the information overlay must
/// rebuild its graph).
/// </summary>
public sealed class LevelGraphSourceTests
{
  /// <summary>Run inputs of a typical run.</summary>
  private static readonly LevelGenRunInputs Inputs =
    new(PropMain.Default with { TextSeed = "mordor" }, CnfLevelGeneration.Default);

  /// <summary>Source of a finished run with a completed world.</summary>
  private static readonly LevelGraphSource Finished =
    new(EnGenResult.Finished, Inputs, 42L, EnPhase.Completed, 10L);

  /// <summary>
  /// Verifies that the same run seen twice (each refresh copies the world) is the same source.
  /// </summary>
  [Fact]
  public void SameRun_IsEqual()
  {
    // Arrange: The same values, with equal but separate run inputs.
    var again = new LevelGraphSource(
      EnGenResult.Finished,
      new LevelGenRunInputs(PropMain.Default with { TextSeed = "mordor" }, CnfLevelGeneration.Default),
      42L,
      EnPhase.Completed,
      10L);

    // Assert: Equal, so the graph is kept.
    Assert.Equal(Finished, again);
  }

  /// <summary>
  /// Verifies that any change of run, progress or result is a different source.
  /// </summary>
  [Fact]
  public void ChangedRun_IsNotEqual()
  {
    // Arrange: Each field changed on its own.
    LevelGraphSource[] changed =
    [
      Finished with { Result = EnGenResult.Paused },
      Finished with { RunInputs = Inputs with { Props = Inputs.Props with { TextSeed = "007" } } },
      Finished with { RunInputs = null },
      Finished with { NumericSeed = 43L },
      Finished with { Phase = EnPhase.Map },
      Finished with { Steps = 11L },
    ];

    // Assert: None equals the original, so the graph is rebuilt.
    Assert.All(changed, source => Assert.NotEqual(Finished, source));
  }

  /// <summary>
  /// Verifies that losing the world (phase and steps unknown) is a different source.
  /// </summary>
  [Fact]
  public void NoWorld_IsNotEqualToWorld()
  {
    // Arrange: Same run, but no world available.
    LevelGraphSource noWorld = Finished with { Phase = null, Steps = null };

    // Assert: Different.
    Assert.NotEqual(Finished, noWorld);
  }
}
