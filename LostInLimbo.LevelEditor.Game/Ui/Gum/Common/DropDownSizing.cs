namespace LostInLimbo.LevelEditor.Ui.Gum.Common;

/// <summary>
/// Places an open combo box dropdown list above its combo box when Gum could not put it below.
/// </summary>
/// <remarks>
/// Gum opens the list below the combo box. When it does not fit there, Gum only slides it up until
/// it is inside the window, so it overlaps the combo box; it never flips the list above. Engine-neutral so
/// the rule is unit-testable.
/// </remarks>
public static class DropDownSizing
{
  /// <summary>
  /// How far, in pixels, a list's top may be above the combo box's bottom edge and still count
  /// as opened below (absorbs rounding in Gum's layout).
  /// </summary>
  public const float BelowTolerance = 0.5f;

  /// <summary>
  /// Computes where the top of an open list belongs: a list Gum opened below the combo box stays;
  /// any other list (slid up over the combo box) is moved to end exactly at the combo box's top edge.
  /// </summary>
  /// <param name="listTop">Top of the list as Gum placed it, in pixels.</param>
  /// <param name="listHeight">Current height of the list, in pixels.</param>
  /// <param name="comboBoxTop">Top of the combo box, in pixels.</param>
  /// <param name="comboBoxBottom">Bottom of the combo box, in pixels.</param>
  /// <returns>New top of the list, in pixels.</returns>
  public static float ListTop(float listTop, float listHeight, float comboBoxTop, float comboBoxBottom)
  {
    bool isBelow = listTop >= comboBoxBottom - BelowTolerance;
    return isBelow ? listTop : comboBoxTop - listHeight;
  }
}
