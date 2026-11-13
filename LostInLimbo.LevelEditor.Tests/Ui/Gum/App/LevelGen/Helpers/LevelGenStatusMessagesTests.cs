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
  /// Verifies outcome messages: quoted seed (time-based when empty) and counts for finished and
  /// validation-error runs.
  /// </summary>
  [Fact]
  public void ForOutcome_ReturnsExpectedMessages()
  {
    // Act: Build messages for each kind of outcome.
    StatusEntry finished = LevelGenStatusMessages.ForOutcome(EnGenResult.Finished, "mordor", 5, 4, 12, null);
    StatusEntry invalid = LevelGenStatusMessages.ForOutcome(EnGenResult.ValidationError, "007", 1, 0, 3, null);
    StatusEntry error = LevelGenStatusMessages.ForOutcome(EnGenResult.Error, "mordor", 0, 0, 0, "boom");
    StatusEntry cancelled = LevelGenStatusMessages.ForOutcome(EnGenResult.Cancelled, "mordor", 0, 0, 0, null);
    StatusEntry timeSeeded = LevelGenStatusMessages.ForOutcome(EnGenResult.Finished, "", 5, 4, 12, null);
    StatusEntry nullSeed = LevelGenStatusMessages.ForOutcome(EnGenResult.Paused, null, 0, 0, 0, null);

    // Assert: Texts (with quoted seed) and severities.
    Assert.Equal(new StatusEntry("LevelGen: seed \"mordor\" Finished. Graph: nodes=5, edges=4. Map: steps=12.", EnStatusSeverity.Success), finished);
    Assert.Equal(new StatusEntry("LevelGen: seed \"007\" ValidationError. Graph: nodes=1, edges=0. Map: steps=3.", EnStatusSeverity.Warning), invalid);
    Assert.Equal(new StatusEntry("LevelGen: seed \"mordor\" Error - boom", EnStatusSeverity.Error), error);
    Assert.Equal(new StatusEntry("LevelGen: seed \"mordor\" Cancelled.", EnStatusSeverity.Debug), cancelled);
    Assert.Equal(new StatusEntry("LevelGen: seed \"\" (current time) Finished. Graph: nodes=5, edges=4. Map: steps=12.", EnStatusSeverity.Success), timeSeeded);
    Assert.Equal(new StatusEntry("LevelGen: seed \"\" (current time) Paused.", EnStatusSeverity.Debug), nullSeed);
  }

  /// <summary>
  /// Verifies loop messages: seeds are quoted, failures are errors.
  /// </summary>
  [Fact]
  public void LoopMessages_ReturnExpectedTexts()
  {
    // Act: Build each loop message.
    StatusEntry start = LevelGenStatusMessages.ForLoopStart("5");
    StatusEntry iteration = LevelGenStatusMessages.ForLoopIteration("5", 5, 4, 12);
    StatusEntry invalid = LevelGenStatusMessages.ForLoopFailure("6", EnGenResult.ValidationError, null);
    StatusEntry error = LevelGenStatusMessages.ForLoopFailure("7", EnGenResult.Error, "boom");
    StatusEntry startFailed = LevelGenStatusMessages.ForLoopStartFailed("8");

    // Assert: Texts and severities.
    Assert.Equal(new StatusEntry("Loop: started with seed \"5\".", EnStatusSeverity.Info), start);
    Assert.Equal(new StatusEntry("Loop: seed \"5\" Finished. Graph: nodes=5, edges=4. Map: steps=12.", EnStatusSeverity.Success), iteration);
    Assert.Equal(new StatusEntry("Loop: seed \"6\" ended with ValidationError. Loop stopped.", EnStatusSeverity.Error), invalid);
    Assert.Equal(new StatusEntry("Loop: seed \"7\" ended with Error - boom. Loop stopped.", EnStatusSeverity.Error), error);
    Assert.Equal(new StatusEntry("Loop: failed to start generation with seed \"8\". Loop stopped.", EnStatusSeverity.Error), startFailed);
    Assert.Equal(new StatusEntry("Loop: stopped by user.", EnStatusSeverity.Info), LevelGenStatusMessages.LoopStopped);
  }
}
