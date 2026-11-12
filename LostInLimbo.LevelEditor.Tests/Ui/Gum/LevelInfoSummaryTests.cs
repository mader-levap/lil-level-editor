using System.Collections.Generic;
using LostInLimbo.LevelEditor.Ui.Gum.Info;
using LostInLimbo.LevelGen.Data.Result.Stats;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

/// <summary>
/// Headless tests for <see cref="LevelInfoSummary"/> (rows of the level information card).
/// </summary>
public sealed class LevelInfoSummaryTests
{
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
  /// Verifies the world facts rows for a finished run.
  /// </summary>
  [Fact]
  public void Build_WithWorld_ShowsFacts()
  {
    // Arrange: Facts of a completed world.
    var facts = new LevelWorldFacts(EnPhase.Completed, "mordor", -8341167147440548823L, 12, 15, 1234L);

    // Act: Build rows.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Build(EnGenResult.Finished, null, facts);

    // Assert: Status, phase, seeds and counts in display order.
    Assert.Equal(
      [
        new LevelInfoRow("Status:", "Finished"),
        new LevelInfoRow("Phase:", "Completed"),
        new LevelInfoRow("Text seed:", "\"mordor\""),
        new LevelInfoRow("Numeric seed:", "-8341167147440548823"),
        new LevelInfoRow("Graph nodes:", "12"),
        new LevelInfoRow("Graph edges:", "15"),
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
    var facts = new LevelWorldFacts(EnPhase.Map, "007", 7L, 3, 2, 10L);

    // Act: Build rows with a reason.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Build(EnGenResult.ValidationError, "Too few rooms.", facts);

    // Assert: Reason row follows the status row.
    Assert.Equal(new LevelInfoRow("Reason:", "Too few rooms."), rows[1]);
    Assert.Equal(8, rows.Count);
  }

  /// <summary>
  /// Verifies the text seed row: quoted, and marked as time-based when empty.
  /// </summary>
  [Theory]
  [InlineData("007", "\"007\"")]
  [InlineData("", "\"\" (current time)")]
  [InlineData(null, "\"\" (current time)")]
  public void Build_WithWorld_FormatsTextSeed(string? textSeed, string expected)
  {
    // Arrange: Facts with the given text seed.
    var facts = new LevelWorldFacts(EnPhase.Completed, textSeed, 42L, 1, 0, 1L);

    // Act: Build rows.
    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Build(EnGenResult.Finished, null, facts);

    // Assert: Text seed row directly under the phase row, numeric seed after it.
    Assert.Equal(new LevelInfoRow("Phase:", "Completed"), rows[1]);
    Assert.Equal(new LevelInfoRow("Text seed:", expected), rows[2]);
    Assert.Equal(new LevelInfoRow("Numeric seed:", "42"), rows[3]);
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
}
