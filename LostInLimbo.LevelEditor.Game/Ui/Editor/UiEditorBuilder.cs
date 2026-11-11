using Stride.UI;
using Stride.UI.Panels;

namespace LostInLimbo.LevelEditor.Ui.Editor;

/// <summary>
/// Builder for the (now empty) UGS editor page.
/// </summary>
/// <remarks>
/// Gum migration (Phase 2): the toolbar, overlay switching and both overlays moved to Gum
/// (<c>GumEditorToolbar</c>, <c>OverlaySwitcher</c>, <c>GumLevelGenPanel</c>,
/// <c>GumLevelInfoPanel</c>). This builder only keeps an empty UGS panel attached until
/// Phase 3 removes the UGS page entirely.
/// </remarks>
public class UiEditorBuilder : UiBuilderBase
{
  /// <summary>Build main editor panel (empty).</summary>
  /// <returns>Panel.</returns>
  public override Panel Build()
  {
    return new Grid
    {
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Stretch,
    };
  }

  public override void Update()
  {
  }
}
