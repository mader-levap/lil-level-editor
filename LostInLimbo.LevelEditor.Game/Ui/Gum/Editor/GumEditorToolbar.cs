using System;
using Gum.Forms.Controls;
using LostInLimbo.LevelEditor.Ui.Editor;
using LostInLimbo.LevelEditor.Ui.Gum.AppBar;
using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Ui.Gum.Editor;

/// <summary>
/// Gum editor toolbar: a vertical column of toggle buttons at the top-left, under the app bar,
/// that open and close overlays through an <see cref="OverlaySwitcher"/>.
/// </summary>
public sealed class GumEditorToolbar
{
  /// <summary>Distance of the toolbar from the window edge and app bar, in pixels.</summary>
  private const float Margin = 5;

  /// <summary>Vertical spacing between toolbar buttons, in pixels.</summary>
  private const float ButtonSpacing = 4;

  /// <summary>Switcher driven by the buttons.</summary>
  private readonly OverlaySwitcher _switcher;

  /// <summary>Toggles the level generation overlay; null until built.</summary>
  private ToggleButton? _btnLevelGen;

  /// <summary>Toggles the level information overlay; null until built.</summary>
  private ToggleButton? _btnLevelInfo;

  /// <summary>
  /// Creates the toolbar. Call <see cref="Build"/> to create its visuals.
  /// </summary>
  /// <param name="switcher">Switcher driven by the buttons.</param>
  /// <exception cref="ArgumentNullException"><paramref name="switcher"/> is null.</exception>
  public GumEditorToolbar(OverlaySwitcher switcher)
  {
    _switcher = switcher ?? throw new ArgumentNullException(nameof(switcher));
  }

  /// <summary>Root panel of the toolbar; null until <see cref="Build"/> is called.</summary>
  public StackPanel? Root { get; private set; }

  /// <summary>
  /// Builds the toolbar and adds it to the Gum root.
  /// </summary>
  /// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
  /// <exception cref="InvalidOperationException">The toolbar was already built.</exception>
  public void Build()
  {
    if (Root != null) throw new InvalidOperationException("Toolbar was already built.");

    var stack = new StackPanel
    {
      X = Margin,
      Y = GumAppBar.Height + Margin,
      Spacing = ButtonSpacing,
    };

    _btnLevelGen = GumUiFactory.CreateIconToggleButton(GumIcons.Gears, "Level generation");
    _btnLevelGen.Click += (_, _) => OnToggleClicked(EnOverlay.LevelGen);
    stack.AddChild(_btnLevelGen);

    _btnLevelInfo = GumUiFactory.CreateIconToggleButton(GumIcons.Info, "Level information");
    _btnLevelInfo.Click += (_, _) => OnToggleClicked(EnOverlay.LevelInfo);
    stack.AddChild(_btnLevelInfo);

    _switcher.CurrentChanged += (_, _) => SyncChecked();
    SyncChecked();

    stack.AddToRoot();
    Root = stack;
  }

  /// <summary>
  /// Handles a toolbar button click: toggles the overlay, then re-syncs checked states in case
  /// the switcher did not change (Gum already flipped the clicked button's state).
  /// </summary>
  /// <param name="overlay">Overlay of the clicked button.</param>
  private void OnToggleClicked(EnOverlay overlay)
  {
    _switcher.Toggle(overlay);
    SyncChecked();
  }

  /// <summary>
  /// Makes the checked state of every button match the current overlay. Called after each
  /// switch; also undoes Gum's own toggle-on-click when the switcher refuses a change.
  /// </summary>
  private void SyncChecked()
  {
    if (_btnLevelGen != null) _btnLevelGen.IsChecked = _switcher.Current == EnOverlay.LevelGen;
    if (_btnLevelInfo != null) _btnLevelInfo.IsChecked = _switcher.Current == EnOverlay.LevelInfo;
  }
}
