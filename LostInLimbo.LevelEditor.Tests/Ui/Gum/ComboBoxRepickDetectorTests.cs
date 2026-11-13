using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

/// <summary>
/// Headless tests for <see cref="ComboBoxRepickDetector"/> (picking the already selected item).
/// </summary>
public sealed class ComboBoxRepickDetectorTests
{
  /// <summary>
  /// Verifies picking the already selected item counts as a re-pick when the item event comes
  /// before the close.
  /// </summary>
  [Fact]
  public void Closed_AfterItemOnSameSelection_IsRepick()
  {
    // Arrange: Open with item 1 selected; the item event arrives first.
    var detector = new ComboBoxRepickDetector();
    detector.Opened(1);
    bool atItem = detector.ItemActivated();

    // Act: Close with the selection unchanged.
    bool atClose = detector.Closed(1);

    // Assert: Reported once, at the close.
    Assert.False(atItem);
    Assert.True(atClose);
  }

  /// <summary>
  /// Verifies picking the already selected item counts as a re-pick when Gum closes the
  /// dropdown before the item event reaches us.
  /// </summary>
  [Fact]
  public void ItemActivated_AfterCloseOnSameSelection_IsRepick()
  {
    // Arrange: Open with item 1 selected; the close arrives first.
    var detector = new ComboBoxRepickDetector();
    detector.Opened(1);
    bool atClose = detector.Closed(1);

    // Act: The item event follows.
    bool atItem = detector.ItemActivated();

    // Assert: Reported once, at the item event.
    Assert.False(atClose);
    Assert.True(atItem);
  }

  /// <summary>
  /// Verifies a re-pick is reported only once per opening, even with both a push and a click.
  /// </summary>
  [Fact]
  public void Repick_WithPushAndClick_IsReportedOnce()
  {
    // Arrange: Open with item 1 selected.
    var detector = new ComboBoxRepickDetector();
    detector.Opened(1);

    // Act: Push, close unchanged, then the click.
    bool atPush = detector.ItemActivated();
    bool atClose = detector.Closed(1);
    bool atClick = detector.ItemActivated();

    // Assert: Exactly one report.
    Assert.Equal(1, new[] { atPush, atClose, atClick }.Count(reported => reported));
  }

  /// <summary>
  /// Verifies picking a different item is not a re-pick, in either event order (the selection
  /// change handles it).
  /// </summary>
  [Fact]
  public void OtherItem_IsNotRepick()
  {
    // Arrange + Act: Item event before close, then close before item event.
    var detector = new ComboBoxRepickDetector();
    detector.Opened(1);
    bool first = detector.ItemActivated() | detector.Closed(0);
    detector.Opened(1);
    bool second = detector.Closed(0) | detector.ItemActivated();

    // Assert: Neither is a re-pick.
    Assert.False(first);
    Assert.False(second);
  }

  /// <summary>
  /// Verifies closing without an item event (Escape, clicking away) is not a re-pick.
  /// </summary>
  [Fact]
  public void Closed_WithoutItem_IsNotRepick()
  {
    // Arrange: Open with item 1 selected.
    var detector = new ComboBoxRepickDetector();
    detector.Opened(1);

    // Act: Close unchanged.
    bool repicked = detector.Closed(1);

    // Assert: Not a re-pick.
    Assert.False(repicked);
  }

  /// <summary>
  /// Verifies an item event from an earlier opening does not carry over to the next one.
  /// </summary>
  [Fact]
  public void ItemFromEarlierOpening_DoesNotCarryOver()
  {
    // Arrange: One opening with an item event that changed the selection, then a new opening.
    var detector = new ComboBoxRepickDetector();
    detector.Opened(1);
    detector.ItemActivated();
    detector.Closed(0);
    detector.Opened(0);

    // Act: Close the new opening unchanged without an item event.
    bool repicked = detector.Closed(0);

    // Assert: Not a re-pick.
    Assert.False(repicked);
  }

  /// <summary>
  /// Verifies events outside an opening, or with nothing selected, are not re-picks.
  /// </summary>
  [Fact]
  public void WithoutOpeningOrSelection_IsNotRepick()
  {
    // Arrange + Act: Item and close without opening; then an opening with nothing selected.
    var detector = new ComboBoxRepickDetector();
    bool strayItem = detector.ItemActivated();
    bool strayClose = detector.Closed(1);
    detector.Opened(-1);
    bool nothingSelected = detector.ItemActivated() | detector.Closed(-1);

    // Assert: None is a re-pick.
    Assert.False(strayItem);
    Assert.False(strayClose);
    Assert.False(nothingSelected);
  }
}
