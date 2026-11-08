using LostInLimbo.LevelEditor.Ui.Editor;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Panels;

namespace LostInLimbo.LevelEditor.Ui.Main;

/// <summary>
/// Script for main UI.
/// </summary>
public class UiMain : SyncScript
{
  /// <summary>Builder for editor panel.</summary>
  private readonly UiEditorBuilder _editorBuilder = new();
  /// <summary>Underlying editor panel. Always present.</summary>
  private Panel _editorPanel = default!;

  /// <summary>Main panel.</summary>
  private Grid _mainPanel = default!;

  public override void Start()
  {
    UiRes.InitResources(this);
    BuildComponents();
    AttachUi();
  }

  public override void Update()
  {
    _editorBuilder.Update();
  }

  /// <summary>
  /// Build all components.
  /// </summary>
  private void BuildComponents()
  {
    _editorPanel = _editorBuilder.Build();
    _mainPanel = CreateMainPanel();
  }

  /// <summary>
  /// Attach UI to entity.
  /// </summary>
  private void AttachUi()
  {
    var ui = Entity.GetOrCreate<UIComponent>();
    ui.RenderGroup = Stride.Rendering.RenderGroup.Group31; // UI render group.
    ui.Page = new UIPage
    {
      RootElement = _mainPanel
    };
  }

  #region Main panel

  /// <summary>
  /// Create main panel.
  /// </summary>
  /// <returns>Main panel.</returns>
  private Grid CreateMainPanel()
  {
    var panel = new Grid
    {
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Stretch,
      Children =
      {
        _editorPanel
      }
    };
    return panel;
  }

  #endregion
}
