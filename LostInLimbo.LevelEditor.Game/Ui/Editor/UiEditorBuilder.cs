using Stride.UI;
using Stride.UI.Panels;
using LostInLimbo.LevelEditor.Ui.LevelGen;
using LostInLimbo.LevelEditor.Ui.Info;

namespace LostInLimbo.LevelEditor.Ui.Editor;

/// <summary>
/// Builder for the remaining UGS editor overlays.
/// </summary>
/// <remarks>
/// Gum migration (Phase 2): the toolbar buttons and overlay switching moved to Gum
/// (<c>GumEditorToolbar</c>, <c>OverlaySwitcher</c>). This builder only hosts the UGS overlay
/// panels that are not ported yet; the Gum toolbar shows and hides them through
/// <c>UgsOverlayView</c> adapters.
/// </remarks>
public class UiEditorBuilder : UiBuilderBase
{
  /// <summary>Builder for level generation overlay.</summary>
  private readonly UiLevelGenBuilder _levelGenBuilder = new();

  /// <summary>Builder for level information overlay.</summary>
  private readonly UiLevelInfoBuilder _levelInfoBuilder = new();

  /// <summary>Main panel. Contains all overlays (by default hidden).</summary>
  private Grid _mainPanel = default!;

  /// <summary>Level generation overlay (UGS). Valid after <see cref="Build"/>.</summary>
  public Panel LevelGenPanel { get; private set; } = default!;

  /// <summary>Level information overlay (UGS). Valid after <see cref="Build"/>.</summary>
  public Panel LevelInfoPanel { get; private set; } = default!;

  /// <summary>Build main editor overlay.</summary>
  /// <returns>Panel.</returns>
  public override Panel Build()
  {
    LevelGenPanel = _levelGenBuilder.Build();
    LevelInfoPanel = _levelInfoBuilder.Build();
    _mainPanel = CreateMainPanel();
    return _mainPanel;
  }

  public override void Update()
  {
    _levelGenBuilder.Update();
    _levelInfoBuilder.Update();
  }

  /// <summary>
  /// Create main panel.
  /// </summary>
  /// <returns>Main panel.</returns>
  private Grid CreateMainPanel()
  {
    // Panels will fully overlap if put in same cell.
    return new Grid
    {
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Stretch,
      Children =
      {
        LevelGenPanel, LevelInfoPanel
      }
    };
  }
}
