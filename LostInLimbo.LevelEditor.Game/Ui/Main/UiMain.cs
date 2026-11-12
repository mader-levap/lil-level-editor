using System;
using Gum;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Services.SystemInfo;
using LostInLimbo.LevelEditor.Ui.Gum.AppBar;
using LostInLimbo.LevelEditor.Ui.Gum.Editor;
using LostInLimbo.LevelEditor.Ui.Gum.Info;
using LostInLimbo.LevelEditor.Ui.Gum.LevelGen;
using Stride.Engine;

namespace LostInLimbo.LevelEditor.Ui.Main;

/// <summary>
/// Script hosting the editor's Gum UI: app bar, toolbar and overlays.
/// </summary>
/// <remarks>
/// Builds the Gum UI lazily, as soon as Gum reports it is initialized (initialization itself
/// happens in <c>LostInLimboEditor</c>), and ticks panels that poll generation state. Gum
/// panels receive their services through constructors. Gum handles outside-click blur of
/// text boxes itself (<c>LosesFocusWhenClickedOff</c>); overlays release focus when hidden.
/// </remarks>
public sealed class UiMain : SyncScript
{
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
    EnsureGumUiBuilt();
  }

  public override void Update()
  {
    EnsureGumUiBuilt();

    float deltaSeconds = (float)Game.UpdateTime.Elapsed.TotalSeconds;
    _gumLevelGenPanel?.Update(deltaSeconds);
    _gumLevelInfoPanel?.Update(deltaSeconds);
  }

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
}
