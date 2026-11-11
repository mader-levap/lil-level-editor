using System;
using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

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
}
