namespace LostInLimbo.LevelEditor.Ui.Gum.Editor;

/// <summary>
/// An overlay that <see cref="OverlaySwitcher"/> can show and hide. Implemented by the Gum
/// overlay panels.
/// </summary>
public interface IOverlayView
{
  /// <summary>
  /// Shows or hides the overlay. Hiding must also release any keyboard focus held inside it.
  /// </summary>
  /// <param name="visible"><see langword="true"/> to show; <see langword="false"/> to hide.</param>
  void SetVisible(bool visible);
}
