using LostInLimbo.LevelEditor.Ui.Gum.Common.Log;
using LostInLimbo.LevelEditor.Ui.Gum.LevelGen;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

/// <summary>
/// Headless tests for <see cref="LevelGenStatusMessages"/> (level generation log texts).
/// </summary>
public sealed class LevelGenStatusMessagesTests
{
  /// <summary>
  /// Verifies start/resume messages for success and failure.
  /// </summary>
  [Theory]
  [InlineData(false, true, "Started generation.", EnStatusSeverity.Info)]
  [InlineData(true, true, "Resumed generation.", EnStatusSeverity.Info)]
  [InlineData(false, false, "Failed to start generation.", EnStatusSeverity.Error)]
  [InlineData(true, false, "Failed to resume generation.", EnStatusSeverity.Error)]
  public void ForStart_ReturnsExpectedMessage(bool wasPaused, bool succeeded, string text, EnStatusSeverity severity)
  {
    // Act: Build the message.
    StatusEntry entry = LevelGenStatusMessages.ForStart(wasPaused, succeeded);

    // Assert: Text and severity.
    Assert.Equal(new StatusEntry(text, severity), entry);
  }

  /// <summary>
  /// Verifies outcome messages, including counts for finished and validation-error runs.
  /// </summary>
  [Fact]
  public void ForOutcome_ReturnsExpectedMessages()
  {
    // Act: Build messages for each kind of outcome.
    StatusEntry finished = LevelGenStatusMessages.ForOutcome(EnGenResult.Finished, 5, 4, 12, null);
    StatusEntry invalid = LevelGenStatusMessages.ForOutcome(EnGenResult.ValidationError, 1, 0, 3, null);
    StatusEntry error = LevelGenStatusMessages.ForOutcome(EnGenResult.Error, 0, 0, 0, "boom");
    StatusEntry cancelled = LevelGenStatusMessages.ForOutcome(EnGenResult.Cancelled, 0, 0, 0, null);

    // Assert: Texts and severities.
    Assert.Equal(new StatusEntry("LevelGen: Finished. Graph: nodes=5, edges=4. Map: steps=12.", EnStatusSeverity.Success), finished);
    Assert.Equal(new StatusEntry("LevelGen: ValidationError. Graph: nodes=1, edges=0. Map: steps=3.", EnStatusSeverity.Warning), invalid);
    Assert.Equal(new StatusEntry("LevelGen: Error - boom", EnStatusSeverity.Error), error);
    Assert.Equal(new StatusEntry("LevelGen: Cancelled.", EnStatusSeverity.Debug), cancelled);
  }
}
