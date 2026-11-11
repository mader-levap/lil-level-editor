using LostInLimbo.LevelEditor.Ui.Gum.Common.Log;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

/// <summary>
/// Headless tests for the reusable <see cref="StatusLog"/> model.
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
  /// Verifies that the text/severity overload adds an equivalent entry (Info by default).
  /// </summary>
  [Fact]
  public void Add_TextOverload_AddsEntry()
  {
    // Arrange: Empty log.
    var log = new StatusLog();

    // Act: Add with and without explicit severity.
    log.Add("plain");
    log.Add("bad", EnStatusSeverity.Error);

    // Assert: Entries match.
    Assert.Equal([new StatusEntry("plain", EnStatusSeverity.Info), new StatusEntry("bad", EnStatusSeverity.Error)], log.Entries);
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
}
