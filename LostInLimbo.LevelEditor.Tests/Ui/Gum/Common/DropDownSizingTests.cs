using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Common;

/// <summary>
/// Headless tests for <see cref="DropDownSizing"/> (placement of an open dropdown list).
/// </summary>
public sealed class DropDownSizingTests
{
  /// <summary>Top of the combo box used by the tests, in pixels.</summary>
  private const float ComboBoxTop = 500;

  /// <summary>Bottom of the combo box used by the tests, in pixels.</summary>
  private const float ComboBoxBottom = 530;

  /// <summary>
  /// Verifies that a list Gum opened below the combo box keeps its top edge, also with a tiny
  /// rounding difference from the combo box's bottom edge.
  /// </summary>
  /// <param name="listTop">Top Gum placed the list at.</param>
  [Theory]
  [InlineData(530f)]   // exactly at the combo box's bottom edge
  [InlineData(529.8f)] // layout rounding
  public void ListTop_ListBelowComboBox_DoesNotMove(float listTop)
  {
    // Act: A 100 px list placed under the combo box.
    float top = DropDownSizing.ListTop(listTop, listHeight: 100, ComboBoxTop, ComboBoxBottom);

    // Assert: Unchanged.
    Assert.Equal(listTop, top);
  }

  /// <summary>
  /// Verifies that a list Gum slid up over the combo box (to stay inside the window) is moved to
  /// end exactly at the combo box's top edge: no gap, no overlap.
  /// </summary>
  /// <param name="listTop">Top Gum placed the list at.</param>
  [Theory]
  [InlineData(520f)] // slid up a little: top inside the combo box
  [InlineData(450f)] // slid up a lot: covers the whole combo box
  [InlineData(380f)] // already above, but with a gap
  public void ListTop_ListOverComboBox_EndsAtComboBoxTop(float listTop)
  {
    // Act: A 100 px list.
    float top = DropDownSizing.ListTop(listTop, listHeight: 100, ComboBoxTop, ComboBoxBottom);

    // Assert: Bottom edge (top + height) touches the combo box's top.
    Assert.Equal(ComboBoxTop - 100, top);
  }
}
