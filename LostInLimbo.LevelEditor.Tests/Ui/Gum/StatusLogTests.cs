using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.LevelGen;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

/// <summary>
/// Headless tests for <see cref="StatusLog"/> and <see cref="LevelGenStatusMessages"/>.
/// </summary>
public sealed class StatusLogTests
{
  /// <summary>
  /// Verifies that entries are kept in order and reported through <see cref="StatusLog.Added"/>.
  /// </summary>
  [Fact]
  public void Add_UnderCap_KeepsEntriesInOrder()
  {
    // Arrange: Log with room for three; record added entries.
    var log = new StatusLog(3);
    var added = new List<StatusEntry>();
    log.Added += (_, entry) => added.Add(entry);
    var first = new StatusEntry("a", EnStatusSeverity.Info);
    var second = new StatusEntry("b", EnStatusSeverity.Error);

    // Act: Add two entries.
    log.Add(first);
    log.Add(second);

    // Assert: Both kept in order and reported.
    Assert.Equal([first, second], log.Entries);
    Assert.Equal([first, second], added);
  }

  /// <summary>
  /// Verifies that the oldest entries are dropped past the cap and the drop is reported.
  /// </summary>
  [Fact]
  public void Add_OverCap_DropsOldest()
  {
    // Arrange: Log with room for two; record trims.
    var log = new StatusLog(2);
    int trimmed = 0;
    log.Trimmed += (_, count) => trimmed += count;

    // Act: Add three entries.
    log.Add(new StatusEntry("1", EnStatusSeverity.Info));
    log.Add(new StatusEntry("2", EnStatusSeverity.Info));
    log.Add(new StatusEntry("3", EnStatusSeverity.Info));

    // Assert: Only the newest two remain; one entry was trimmed.
    Assert.Equal(["2", "3"], log.Entries.Select(entry => entry.Text));
    Assert.Equal(1, trimmed);
  }

  /// <summary>
  /// Verifies that clearing empties the log and raises <see cref="StatusLog.Cleared"/>.
  /// </summary>
  [Fact]
  public void Clear_RemovesAllEntries()
  {
    // Arrange: Log with one entry.
    var log = new StatusLog();
    log.Add(new StatusEntry("x", EnStatusSeverity.Info));
    bool cleared = false;
    log.Cleared += (_, _) => cleared = true;

    // Act: Clear.
    log.Clear();

    // Assert: Empty and reported.
    Assert.Empty(log.Entries);
    Assert.True(cleared);
  }

  /// <summary>
  /// Verifies that a cap below one is rejected.
  /// </summary>
  [Fact]
  public void Constructor_CapBelowOne_Throws()
  {
    // Act + Assert: Zero capacity is invalid.
    Assert.Throws<ArgumentOutOfRangeException>(() => new StatusLog(0));
  }

  /// <summary>
  /// Verifies severity colors (same as the former UGS log).
  /// </summary>
  [Fact]
  public void ColorFor_EachSeverity_ReturnsExpectedColor()
  {
    // Act + Assert: White, light green, orange, red.
    Assert.Equal(UiRgba.White, StatusLog.ColorFor(EnStatusSeverity.Info));
    Assert.Equal(new UiRgba(144, 238, 144), StatusLog.ColorFor(EnStatusSeverity.Success));
    Assert.Equal(new UiRgba(255, 165, 0), StatusLog.ColorFor(EnStatusSeverity.Warning));
    Assert.Equal(new UiRgba(255, 0, 0), StatusLog.ColorFor(EnStatusSeverity.Error));
  }

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
    Assert.Equal(new StatusEntry("LevelGen: Cancelled.", EnStatusSeverity.Info), cancelled);
  }
}
