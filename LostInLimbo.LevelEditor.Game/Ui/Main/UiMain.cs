using System;
using Gum;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Services.SystemInfo;
using LostInLimbo.LevelEditor.Ui.Editor;
using LostInLimbo.LevelEditor.Ui.Gum.AppBar;
using LostInLimbo.LevelEditor.Ui.Gum.Editor;
using LostInLimbo.LevelEditor.Ui.Gum.Info;
using LostInLimbo.LevelEditor.Ui.Gum.LevelGen;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Panels;

namespace LostInLimbo.LevelEditor.Ui.Main;

/// <summary>
/// Script for main UI.
/// </summary>
/// <remarks>
/// During the UGS → Gum migration (see <c>GUM-MIGRATION.md</c>) this script hosts both UI
/// stacks: it owns the remaining UGS tree and builds the Gum UI lazily, as soon as Gum
/// reports it is initialized. Gum panels receive their services through constructors.
/// </remarks>
public class UiMain : SyncScript
{
  /// <summary>Builder for editor panel.</summary>
  private readonly UiEditorBuilder _editorBuilder = new();
  /// <summary>Underlying editor panel. Always present.</summary>
  private Panel _editorPanel = default!;

  /// <summary>Main panel.</summary>
  private Grid _mainPanel = default!;

  /// <summary>Gum app bar; null until the Gum UI is built.</summary>
  private GumAppBar? _gumAppBar;

  /// <summary>Keeps at most one overlay visible; driven by the Gum toolbar.</summary>
  private readonly OverlaySwitcher _overlaySwitcher = new();

  /// <summary>Gum editor toolbar; null until the Gum UI is built.</summary>
  private GumEditorToolbar? _gumToolbar;

  /// <summary>Gum level generation overlay; null until the Gum UI is built.</summary>
  private GumLevelGenPanel? _gumLevelGenPanel;

  /// <summary>Gum level information overlay; null until the Gum UI is built.</summary>
  private GumLevelInfoPanel? _gumLevelInfoPanel;

  public override void Start()
  {
    UiRes.InitResources(this);
    BuildComponents();
    AttachUi();
    EnsureGumUiBuilt();
  }

  public override void Update()
  {
    EnsureGumUiBuilt();

    _editorBuilder.Update();
    _gumLevelGenPanel?.Update();
    _gumLevelInfoPanel?.Update();

    // Blur the focused text field when the user clicks anywhere outside of it.
    UiFocus.ReleaseTextInputFocusOnOutsidePress(Game.Services);
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

  #region Gum

  /// <summary>
  /// Builds the Gum UI once, as soon as Gum is initialized (by <c>LostInLimboEditor</c>).
  /// </summary>
  /// <exception cref="InvalidOperationException">A required service is not registered.</exception>
  private void EnsureGumUiBuilt()
  {
    if (_gumAppBar != null) return; // already built
    if (!GumService.Default.IsInitialized) return; // Gum not ready yet; retry next frame

    ISystemInfoService systemInfo = Game.Services.GetService<ISystemInfoService>()
      ?? throw new InvalidOperationException("ISystemInfoService was not registered.");
    ILevelGenService levelGenService = Game.Services.GetService<ILevelGenService>()
      ?? throw new InvalidOperationException("ILevelGenService was not registered.");

    var appBar = new GumAppBar(systemInfo);
    appBar.Build();
    _gumAppBar = appBar;

    var levelGenPanel = new GumLevelGenPanel(levelGenService);
    levelGenPanel.Build();
    _gumLevelGenPanel = levelGenPanel;
    _overlaySwitcher.Register(EnOverlay.LevelGen, levelGenPanel);

    var levelInfoPanel = new GumLevelInfoPanel(levelGenService);
    levelInfoPanel.Build();
    _gumLevelInfoPanel = levelInfoPanel;
    _overlaySwitcher.Register(EnOverlay.LevelInfo, levelInfoPanel);

    var toolbar = new GumEditorToolbar(_overlaySwitcher);
    toolbar.Build();
    _gumToolbar = toolbar;
  }

  #endregion

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
      RowDefinitions =
      {
        // Spacer under the Gum app bar, so the remaining UGS panels keep their layout.
        new StripDefinition(StripType.Fixed, GumAppBar.Height),
        new StripDefinition(StripType.Star), // rest
      },
      Children =
      {
        _editorPanel
      }
    };

    _editorPanel.SetGridRow(1);

    return panel;
  }

  #endregion
}
