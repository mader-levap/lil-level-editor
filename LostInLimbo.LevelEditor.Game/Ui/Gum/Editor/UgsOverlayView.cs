using System;
using Stride.Core;
using Stride.UI;
using Stride.UI.Panels;

namespace LostInLimbo.LevelEditor.Ui.Gum.Editor;

/// <summary>
/// Temporary adapter that lets the Gum toolbar show and hide an overlay that is still built
/// with UGS UI. Remove once every overlay is ported to Gum (Phase 2, step 4).
/// </summary>
public sealed class UgsOverlayView : IOverlayView
{
  /// <summary>UGS panel of the overlay.</summary>
  private readonly Panel _panel;

  /// <summary>Services used to reach the UGS focus state.</summary>
  private readonly IServiceRegistry _services;

  /// <summary>
  /// Creates the adapter.
  /// </summary>
  /// <param name="panel">UGS panel of the overlay.</param>
  /// <param name="services">Game services (for UGS focus queries).</param>
  /// <exception cref="ArgumentNullException">An argument is null.</exception>
  public UgsOverlayView(Panel panel, IServiceRegistry services)
  {
    _panel = panel ?? throw new ArgumentNullException(nameof(panel));
    _services = services ?? throw new ArgumentNullException(nameof(services));
  }

  /// <inheritdoc />
  public void SetVisible(bool visible)
  {
    if (!visible)
    {
      // The engine keeps keyboard focus (and routes typed keys to) an EditText even after its
      // panel is hidden; release it so camera input is not blocked by an invisible text field.
      if (UiFocus.IsInside(UiFocus.GetFocusedElement(_services), _panel))
      {
        UiFocus.ReleaseTextInputFocus(_services);
      }
    }

    _panel.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
  }
}
