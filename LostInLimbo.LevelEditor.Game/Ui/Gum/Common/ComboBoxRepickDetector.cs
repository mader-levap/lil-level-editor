namespace LostInLimbo.LevelEditor.Ui.Gum.Common;

/// <summary>
/// Detects that the user picked the item a combo box already had selected. Gum raises no
/// selection change then, so callers that want "pick again" to mean something (for example a
/// reset) feed this the dropdown's open, item and close events.
/// </summary>
/// <remarks>
/// <para>A re-pick is an opening in which an item was pushed or clicked and that closed with the
/// same item selected as when it opened. Closing by Escape or by clicking away is not a re-pick.
/// Only mouse picks count: Gum raises item pushes and clicks for the cursor only.</para>
/// <para>The item event may arrive before or after the close (Gum closes the dropdown from its own
/// handler of the same input, which can run first), so both orders are recognized. Each opening
/// reports at most one re-pick. Engine-neutral so the rule is unit-testable.</para>
/// </remarks>
public sealed class ComboBoxRepickDetector
{
  /// <summary>Whether an opening is being tracked (from open until a re-pick is reported or the next open).</summary>
  private bool _isTracking;

  /// <summary>Whether the dropdown is open.</summary>
  private bool _isOpen;

  /// <summary>Selected index when the dropdown opened.</summary>
  private int _indexAtOpen = -1;

  /// <summary>Whether an item was pushed or clicked during the tracked opening.</summary>
  private bool _wasItemActivated;

  /// <summary>Whether the tracked opening closed with the selection it opened with.</summary>
  private bool _closedUnchanged;

  /// <summary>
  /// Call when the dropdown opened.
  /// </summary>
  /// <param name="selectedIndex">Selected index at that moment (-1 for none).</param>
  public void Opened(int selectedIndex)
  {
    _isTracking = true;
    _isOpen = true;
    _indexAtOpen = selectedIndex;
    _wasItemActivated = false;
    _closedUnchanged = false;
  }

  /// <summary>
  /// Call when an item of the dropdown was pushed or clicked.
  /// </summary>
  /// <returns>True if this completes a re-pick (the dropdown already closed unchanged).</returns>
  public bool ItemActivated()
  {
    if (!_isTracking) return false;
    _wasItemActivated = true;
    return TryReport();
  }

  /// <summary>
  /// Call when the dropdown closed, by any route.
  /// </summary>
  /// <param name="selectedIndex">Selected index after closing (-1 for none).</param>
  /// <returns>True if this completes a re-pick (an item was already pushed or clicked).</returns>
  public bool Closed(int selectedIndex)
  {
    if (!_isTracking || !_isOpen) return false;
    _isOpen = false;
    _closedUnchanged = selectedIndex >= 0 && selectedIndex == _indexAtOpen;
    if (!_closedUnchanged)
    {
      _isTracking = false; // changed or cleared: the selection change handles it
      return false;
    }

    return TryReport();
  }

  /// <summary>
  /// Reports a re-pick once both halves (item activated, closed unchanged) have happened, and
  /// stops tracking so the opening is reported only once.
  /// </summary>
  /// <returns>True if a re-pick is complete.</returns>
  private bool TryReport()
  {
    if (!_wasItemActivated || !_closedUnchanged) return false;
    _isTracking = false;
    return true;
  }
}
