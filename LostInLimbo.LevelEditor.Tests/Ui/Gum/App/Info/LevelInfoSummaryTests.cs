using System.Collections.Generic;
using System.Reflection;
using LostInLimbo.LevelEditor.Ui.Gum.App.Info;
using LostInLimbo.LevelGen.Data.Config;
using LostInLimbo.LevelGen.Data.Config.Graph;
using LostInLimbo.LevelGen.Data.Config.Graph.Builder;
using LostInLimbo.LevelGen.Data.Prop;
using LostInLimbo.LevelGen.Data.Result.Stats;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.App.Info;

/// <summary>
/// Headless tests for <see cref="LevelInfoSummary"/> (rows of the level information card).
/// </summary>
public sealed class LevelInfoSummaryTests
{
  #region Status column

  /// <summary>
  /// Verifies the explanation shown when no world is available, per generation result.
  /// </summary>
  [Theory]
  [InlineData(EnGenResult.None, LevelInfoSummary.NoLevelYet)]
  [InlineData(EnGenResult.Running, LevelInfoSummary.InProgress)]
  [InlineData(EnGenResult.Cancelled, LevelInfoSummary.NoLevelAvailable)]
  [InlineData(EnGenResult.Error, LevelInfoSummary.NoLevelAvailable)]
  public void Build_WithoutWorld_ShowsStatusAndExplanation(EnGenResult result, string explanation)
  {
    // Act: Build rows without a world.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Build(result, null, null);

    // Assert: Status row followed by the explanation.
    Assert.Equal(
      [new LevelInfoRow("Status:", result.ToString()), new LevelInfoRow("Level:", explanation)],
      rows);
  }

  /// <summary>
  /// Verifies the world facts rows for a finished run (the text seed is in the properties column).
  /// </summary>
  [Fact]
  public void Build_WithWorld_ShowsFacts()
  {
    // Arrange: Facts of a completed world.
    var facts = new LevelWorldFacts(EnPhase.Completed, -8341167147440548823L, 1234L);

    // Act: Build rows.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Build(EnGenResult.Finished, null, facts);

    // Assert: Status, phase, numeric seed and steps in display order.
    Assert.Equal(
      [
        new LevelInfoRow("Status:", "Finished"),
        new LevelInfoRow("Phase:", "Completed"),
        new LevelInfoRow("Numeric seed:", "-8341167147440548823"),
        new LevelInfoRow("Map steps:", "1234"),
      ],
      rows);
  }

  /// <summary>
  /// Verifies the failure reason row is added after the status when present.
  /// </summary>
  [Fact]
  public void Build_WithFailureReason_AddsReasonRow()
  {
    // Arrange: Facts of a world that failed validation.
    var facts = new LevelWorldFacts(EnPhase.Map, 7L, 10L);

    // Act: Build rows with a reason.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Build(EnGenResult.ValidationError, "Too few rooms.", facts);

    // Assert: Reason row follows the status row.
    Assert.Equal(new LevelInfoRow("Reason:", "Too few rooms."), rows[1]);
    Assert.Equal(5, rows.Count);
  }

  /// <summary>
  /// Verifies a blank failure reason does not add a row.
  /// </summary>
  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void Build_WithBlankFailureReason_OmitsReasonRow(string reason)
  {
    // Act: Build rows with a blank reason.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Build(EnGenResult.Cancelled, reason, null);

    // Assert: No reason row.
    Assert.DoesNotContain(rows, row => row.Label == "Reason:");
  }

  #endregion

  #region Properties column

  /// <summary>
  /// Verifies every property field is shown, in declaration order.
  /// </summary>
  [Fact]
  public void Properties_ShowsAllFieldsInOrder()
  {
    // Arrange: Non-default properties.
    var props = PropMain.Default with { TextSeed = "mordor", Difficulty = 100, ManyThemes = true };

    // Act: Build rows.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Properties(props);

    // Assert: Config version, text seed, difficulty and themes flag in display order.
    Assert.Equal(
      [
        new LevelInfoRow("Config version:", props.ConfigVersion.ToString()),
        new LevelInfoRow("Text seed:", "\"mordor\""),
        new LevelInfoRow("Difficulty:", "100"),
        new LevelInfoRow("Many themes:", LevelInfoSummary.Yes),
      ],
      rows);
  }

  /// <summary>
  /// Verifies the text seed row: quoted, and marked as time-based when empty.
  /// </summary>
  /// <param name="textSeed">Text seed in the properties.</param>
  /// <param name="expected">Expected row value.</param>
  [Theory]
  [InlineData("007", "\"007\"")]
  [InlineData("", "\"\" (current time)")]
  [InlineData(null, "\"\" (current time)")]
  public void Properties_FormatsTextSeed(string? textSeed, string expected)
  {
    // Act: Build rows for the given text seed.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Properties(PropMain.Default with { TextSeed = textSeed });

    // Assert: Text seed row formatted.
    Assert.Contains(new LevelInfoRow("Text seed:", expected), rows);
  }

  /// <summary>
  /// Verifies a false themes flag shows as "No".
  /// </summary>
  [Fact]
  public void Properties_ManyThemesOff_ShowsNo()
  {
    // Act: Build rows for default properties (themes flag off).
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Properties(PropMain.Default with { ManyThemes = false });

    // Assert: Flag shown as No.
    Assert.Contains(new LevelInfoRow("Many themes:", LevelInfoSummary.No), rows);
  }

  /// <summary>
  /// Verifies there is one row per public property field, so a field added to the library fails
  /// this test until the column shows it.
  /// </summary>
  [Fact]
  public void Properties_CoversEveryPropMainField()
  {
    // Arrange: Public instance properties of the properties record (static Default excluded).
    int fieldCount = typeof(PropMain).GetProperties(BindingFlags.Public | BindingFlags.Instance).Length;

    // Act: Build rows.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Properties(PropMain.Default);

    // Assert: One row per field.
    Assert.Equal(fieldCount, rows.Count);
  }

  /// <summary>
  /// Verifies unknown properties give no rows (the column shows "not available" instead).
  /// </summary>
  [Fact]
  public void Properties_Null_IsEmpty()
  {
    // Act: Build rows without properties.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Properties(null);

    // Assert: No rows.
    Assert.Empty(rows);
  }

  #endregion

  #region Configuration column

  /// <summary>
  /// Verifies the configuration column shows the graph builder used.
  /// </summary>
  [Fact]
  public void Configuration_ShowsBuilder()
  {
    // Arrange: Configuration using the Linear builder.
    var config = CnfLevelGeneration.Default with
    {
      Graph = CnfGraph.Default with
      {
        GraphBuilder = new CnfGraphBuilder { Builder = EnGraphBuilder.Linear, CnfLinear = CnfGraphBuilderLinear.Default },
      },
    };

    // Act: Build rows.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Configuration(config);

    // Assert: Builder row.
    Assert.Equal([new LevelInfoRow("Builder:", "Linear")], rows);
  }

  /// <summary>
  /// Verifies an unknown configuration gives no rows (the column shows "not available" instead).
  /// </summary>
  [Fact]
  public void Configuration_Null_IsEmpty()
  {
    // Act: Build rows without configuration.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Configuration(null);

    // Assert: No rows.
    Assert.Empty(rows);
  }

  #endregion
}
