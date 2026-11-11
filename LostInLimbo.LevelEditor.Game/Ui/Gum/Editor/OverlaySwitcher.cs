using System;
using System.Collections.Generic;
using LostInLimbo.LevelEditor.Ui.Editor;

namespace LostInLimbo.LevelEditor.Ui.Gum.Editor;

/// <summary>
/// Keeps at most one overlay visible at a time. Pure logic (no Gum or Stride), so the
/// switching rules are unit-testable.
/// </summary>
/// <remarks>
/// Rules (same as the former UGS editor toolbar): requesting the current overlay again, or
/// <see cref="EnOverlay.None"/>, closes it; requesting another overlay hides the current one
/// and shows the new one. Requesting an overlay with no registered view closes the current one.
/// </remarks>
public sealed class OverlaySwitcher
{
  /// <summary>Registered views by overlay.</summary>
  private readonly Dictionary<EnOverlay, IOverlayView> _views = new();

  /// <summary>Currently visible overlay, or <see cref="EnOverlay.None"/>.</summary>
  public EnOverlay Current { get; private set; } = EnOverlay.None;

  /// <summary>Raised after <see cref="Current"/> changes; the argument is the new value.</summary>
  public event EventHandler<EnOverlay>? CurrentChanged;

  /// <summary>
  /// Registers (or replaces) the view for an overlay. The new view is shown if its overlay is
  /// current, otherwise hidden; a replaced view that was visible is hidden.
  /// </summary>
  /// <param name="overlay">Overlay the view belongs to.</param>
  /// <param name="view">View.</param>
  /// <exception cref="ArgumentException"><paramref name="overlay"/> is <see cref="EnOverlay.None"/>.</exception>
  /// <exception cref="ArgumentNullException"><paramref name="view"/> is null.</exception>
  public void Register(EnOverlay overlay, IOverlayView view)
  {
    if (overlay == EnOverlay.None) throw new ArgumentException("Cannot register a view for EnOverlay.None.", nameof(overlay));
    ArgumentNullException.ThrowIfNull(view);

    bool isCurrent = overlay == Current;
    if (isCurrent && _views.TryGetValue(overlay, out IOverlayView? oldView))
    {
      oldView.SetVisible(false);
    }

    _views[overlay] = view;
    view.SetVisible(isCurrent);
  }

  /// <summary>
  /// Toggles an overlay: closes it if it is current, otherwise shows it instead of the current one.
  /// </summary>
  /// <param name="requested">Requested overlay.</param>
  public void Toggle(EnOverlay requested)
  {
    Show(requested == Current ? EnOverlay.None : requested);
  }

  /// <summary>
  /// Shows the given overlay (hiding the current one), or closes all for <see cref="EnOverlay.None"/>.
  /// </summary>
  /// <param name="overlay">Overlay to show.</param>
  public void Show(EnOverlay overlay)
  {
    if (overlay != EnOverlay.None && !_views.ContainsKey(overlay)) overlay = EnOverlay.None; // nothing to show
    if (overlay == Current) return;

    if (_views.TryGetValue(Current, out IOverlayView? currentView)) currentView.SetVisible(false);
    if (_views.TryGetValue(overlay, out IOverlayView? newView)) newView.SetVisible(true);

    Current = overlay;
    CurrentChanged?.Invoke(this, overlay);
  }
}
