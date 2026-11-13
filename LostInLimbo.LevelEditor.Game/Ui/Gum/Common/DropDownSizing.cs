using System;

namespace LostInLimbo.LevelEditor.Ui.Gum.Common;

/// <summary>
/// Computes the height of an open dropdown list so it fits its items, up to a maximum number of
/// rows (beyond that the list scrolls).
/// </summary>
/// <remarks>
/// Gum's default ComboBox dropdown has a fixed height (128 px), which leaves empty space below
/// short lists. Engine-neutral so the rule is unit-testable.
/// </remarks>
public static class DropDownSizing
{
  /// <summary>Default maximum number of rows shown before the list scrolls.</summary>
  public const int DefaultMaxVisibleRows = 8;

  /// <summary>
  /// Computes the list height.
  /// </summary>
  /// <param name="contentHeight">Height of all rows together (the list's inner panel), in pixels.</param>
  /// <param name="chromeHeight">List height not used by rows (borders, padding), in pixels.</param>
  /// <param name="rowHeight">Height of one row, in pixels; used for the cap.</param>
  /// <param name="maxVisibleRows">Rows shown at most before scrolling.</param>
  /// <returns>List height in pixels: content plus chrome, capped at <paramref name="maxVisibleRows"/> rows plus chrome.</returns>
  /// <exception cref="ArgumentOutOfRangeException">A size is negative, or <paramref name="maxVisibleRows"/> is less than one.</exception>
  public static float Compute(float contentHeight, float chromeHeight, float rowHeight, int maxVisibleRows = DefaultMaxVisibleRows)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(contentHeight);
    ArgumentOutOfRangeException.ThrowIfNegative(chromeHeight);
    ArgumentOutOfRangeException.ThrowIfNegative(rowHeight);
    ArgumentOutOfRangeException.ThrowIfLessThan(maxVisibleRows, 1);

    float maxContentHeight = rowHeight * maxVisibleRows;
    return Math.Min(contentHeight, maxContentHeight) + chromeHeight;
  }

  /// <summary>
  /// Computes where the top of an open list belongs after its height changed: a list opened
  /// above its combo box must end exactly at the combo box's top edge, a list opened below stays.
  /// Gum places a list that does not fit below the combo box above it, using the height the list
  /// had then; resizing it afterwards would otherwise leave a gap or an overlap.
  /// </summary>
  /// <param name="listTop">Top of the list before the resize, in pixels.</param>
  /// <param name="newHeight">List height after the resize, in pixels.</param>
  /// <param name="comboBoxTop">Top of the combo box, in pixels.</param>
  /// <returns>New top of the list, in pixels.</returns>
  public static float ListTopAfterResize(float listTop, float newHeight, float comboBoxTop)
  {
    // A list below the combo box starts at or under its top; one above starts over it.
    bool isAbove = listTop < comboBoxTop;
    return isAbove ? comboBoxTop - newHeight : listTop;
  }
}
