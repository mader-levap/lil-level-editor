using System;
using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Common;

/// <summary>
/// Headless tests for <see cref="DropDownSizing"/> (open dropdown height).
/// </summary>
public sealed class DropDownSizingTests
{
  /// <summary>
  /// Verifies that a short list gets exactly its content height plus chrome.
  /// </summary>
  [Fact]
  public void Compute_FewRows_FitsContent()
  {
    // Act: Three 22 px rows, 4 px chrome, cap of 8 rows.
    float height = DropDownSizing.Compute(contentHeight: 66, chromeHeight: 4, rowHeight: 22, maxVisibleRows: 8);

    // Assert: No empty space below the rows.
    Assert.Equal(70, height);
  }

  /// <summary>
  /// Verifies that a long list is capped at the maximum number of rows (it scrolls).
  /// </summary>
  [Fact]
  public void Compute_ManyRows_IsCapped()
  {
    // Act: Twenty 22 px rows, cap of 8 rows.
    float height = DropDownSizing.Compute(contentHeight: 440, chromeHeight: 4, rowHeight: 22, maxVisibleRows: 8);

    // Assert: 8 rows plus chrome.
    Assert.Equal(8 * 22 + 4, height);
  }

  /// <summary>
  /// Verifies that invalid inputs are rejected.
  /// </summary>
  [Theory]
  [InlineData(-1, 0, 22, 8)]
  [InlineData(0, -1, 22, 8)]
  [InlineData(0, 0, -1, 8)]
  [InlineData(0, 0, 22, 0)]
  public void Compute_InvalidInput_Throws(float content, float chrome, float row, int maxRows)
  {
    // Act + Assert: Invalid sizes throw.
    Assert.Throws<ArgumentOutOfRangeException>(() => DropDownSizing.Compute(content, chrome, row, maxRows));
  }

  /// <summary>
  /// Verifies that a list opened above the combo box ends exactly at the combo box's top edge
  /// after shrinking, whatever height Gum placed it with (no gap, no overlap).
  /// </summary>
  /// <param name="listTop">Top Gum placed the list at.</param>
  [Theory]
  [InlineData(372f)] // placed with 128 px
  [InlineData(420f)] // placed with a smaller measured height
  public void ListTopAfterResize_ListAboveComboBox_EndsAtComboBoxTop(float listTop)
  {
    // Act: Shrink the list to 50 px; the combo box top is at 500.
    float top = DropDownSizing.ListTopAfterResize(listTop, newHeight: 50, comboBoxTop: 500);

    // Assert: Bottom edge (top + height) touches the combo box.
    Assert.Equal(450, top);
  }

  /// <summary>
  /// Verifies that a list opened below the combo box keeps its top edge.
  /// </summary>
  [Fact]
  public void ListTopAfterResize_ListBelowComboBox_DoesNotMove()
  {
    // Act: List starting below a combo box whose top is at 500.
    float top = DropDownSizing.ListTopAfterResize(listTop: 530, newHeight: 50, comboBoxTop: 500);

    // Assert: Unchanged.
    Assert.Equal(530, top);
  }
}
