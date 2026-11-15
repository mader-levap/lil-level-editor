using System;
using LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Helpers;
using LostInLimbo.LevelEditor.Ui.Gum.Components.Log;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.App.LevelGen.Helpers;

/// <summary>
/// Headless tests for <see cref="LevelGenStatusMessages"/> (level generation log texts).
/// </summary>
public sealed class LevelGenStatusMessagesTests
{
  /// <summary>
  /// Verifies start/resume messages for success and failure; a fresh start shows the seed in quotes.
  /// </summary>
  [Theory]
  [InlineData(false, true, "mordor", "Started generation with seed \"mordor\".", EnStatusSeverity.Info)]
  [InlineData(false, true, "007", "Started generation with seed \"007\".", EnStatusSeverity.Info)]
  [InlineData(false, true, "", "Started generation with seed \"\" (current time).", EnStatusSeverity.Info)]
  [InlineData(false, true, null, "Started generation with seed \"\" (current time).", EnStatusSeverity.Info)]
  [InlineData(true, true, "mordor", "Resumed generation.", EnStatusSeverity.Info)]
  [InlineData(false, false, "mordor", "Failed to start generation.", EnStatusSeverity.Error)]
  [InlineData(true, false, "mordor", "Failed to resume generation.", EnStatusSeverity.Error)]
  public void ForStart_ReturnsExpectedMessage(bool wasPaused, bool succeeded, string? seedText, string text, EnStatusSeverity severity)
  {
    // Act: Build the message.
    StatusEntry entry = LevelGenStatusMessages.ForStart(wasPaused, succeeded, seedText);

    // Assert: Text and severity.
    Assert.Equal(new StatusEntry(text, severity), entry);
  }

  /// <summary>
  /// Verifies outcome messages: quoted seed (time-based when empty), generation time in whole
  /// milliseconds, and counts for finished and validation-error runs.
  /// </summary>
  [Fact]
  public void ForOutcome_ReturnsExpectedMessages()
  {
    // Arrange: Generation time that rounds up.
    TimeSpan elapsed = TimeSpan.FromMilliseconds(1520.6);

    // Act: Build messages for each kind of outcome.
    StatusEntry finished = LevelGenStatusMessages.ForOutcome(EnGenResult.Finished, "mordor", 5, 4, 12, null, elapsed);
    StatusEntry invalid = LevelGenStatusMessages.ForOutcome(EnGenResult.ValidationError, "007", 1, 0, 3, null, elapsed);
    StatusEntry error = LevelGenStatusMessages.ForOutcome(EnGenResult.Error, "mordor", 0, 0, 0, "boom", elapsed);
    StatusEntry cancelled = LevelGenStatusMessages.ForOutcome(EnGenResult.Cancelled, "mordor", 0, 0, 0, null, elapsed);
    StatusEntry timeSeeded = LevelGenStatusMessages.ForOutcome(EnGenResult.Finished, "", 5, 4, 12, null, elapsed);
    StatusEntry nullSeed = LevelGenStatusMessages.ForOutcome(EnGenResult.Paused, null, 0, 0, 0, null, elapsed);

    // Assert: Texts (with quoted seed and time) and severities.
    Assert.Equal(new StatusEntry("LevelGen: seed \"mordor\" Finished (1521 ms). Graph: nodes=5, edges=4. Map: steps=12.", EnStatusSeverity.Success), finished);
    Assert.Equal(new StatusEntry("LevelGen: seed \"007\" ValidationError (1521 ms). Graph: nodes=1, edges=0. Map: steps=3.", EnStatusSeverity.Warning), invalid);
    Assert.Equal(new StatusEntry("LevelGen: seed \"mordor\" Error (1521 ms) - boom", EnStatusSeverity.Error), error);
    Assert.Equal(new StatusEntry("LevelGen: seed \"mordor\" Cancelled (1521 ms).", EnStatusSeverity.Debug), cancelled);
    Assert.Equal(new StatusEntry("LevelGen: seed \"\" (current time) Finished (1521 ms). Graph: nodes=5, edges=4. Map: steps=12.", EnStatusSeverity.Success), timeSeeded);
    Assert.Equal(new StatusEntry("LevelGen: seed \"\" (current time) Paused (1521 ms).", EnStatusSeverity.Debug), nullSeed);
  }

  /// <summary>
  /// Verifies loop messages: seeds are quoted, run-ending messages show the generation time,
  /// failures are errors.
  /// </summary>
  [Fact]
  public void LoopMessages_ReturnExpectedTexts()
  {
    // Arrange: Generation time.
    TimeSpan elapsed = TimeSpan.FromMilliseconds(42);

    // Act: Build each loop message.
    StatusEntry start = LevelGenStatusMessages.ForLoopStart("5");
    StatusEntry iteration = LevelGenStatusMessages.ForLoopIteration("5", 5, 4, 12, elapsed);
    StatusEntry invalid = LevelGenStatusMessages.ForLoopFailure("6", EnGenResult.ValidationError, null, elapsed);
    StatusEntry error = LevelGenStatusMessages.ForLoopFailure("7", EnGenResult.Error, "boom", elapsed);
    StatusEntry startFailed = LevelGenStatusMessages.ForLoopStartFailed("8");

    // Assert: Texts and severities.
    Assert.Equal(new StatusEntry("Loop: started with seed \"5\".", EnStatusSeverity.Info), start);
    Assert.Equal(new StatusEntry("Loop: seed \"5\" Finished (42 ms). Graph: nodes=5, edges=4. Map: steps=12.", EnStatusSeverity.Success), iteration);
    Assert.Equal(new StatusEntry("Loop: seed \"6\" ended with ValidationError (42 ms). Loop stopped.", EnStatusSeverity.Error), invalid);
    Assert.Equal(new StatusEntry("Loop: seed \"7\" ended with Error (42 ms) - boom. Loop stopped.", EnStatusSeverity.Error), error);
    Assert.Equal(new StatusEntry("Loop: failed to start generation with seed \"8\". Loop stopped.", EnStatusSeverity.Error), startFailed);
    Assert.Equal(new StatusEntry("Loop: stopped by user.", EnStatusSeverity.Info), LevelGenStatusMessages.LoopStopped);
  }

  /// <summary>
  /// Verifies quick generation (F5) messages: seeds are quoted, a refused start is an error.
  /// </summary>
  [Fact]
  public void QuickGenerateMessages_ReturnExpectedTexts()
  {
    // Act: Build each quick generation message.
    StatusEntry start = LevelGenStatusMessages.ForQuickGenerateStart("42");
    StatusEntry startFailed = LevelGenStatusMessages.ForQuickGenerateStartFailed("43");

    // Assert: Texts and severities.
    Assert.Equal(new StatusEntry("F5: started generation with seed \"42\".", EnStatusSeverity.Info), start);
    Assert.Equal(new StatusEntry("F5: failed to start generation with seed \"43\".", EnStatusSeverity.Error), startFailed);
  }
}
