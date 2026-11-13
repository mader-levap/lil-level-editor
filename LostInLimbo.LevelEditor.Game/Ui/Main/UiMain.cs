using System;
using Gum;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Services.Settings;
using LostInLimbo.LevelEditor.Services.SystemInfo;
using LostInLimbo.LevelEditor.Ui.Gum.App.AppBar;
using LostInLimbo.LevelEditor.Ui.Gum.App.Editor;
using LostInLimbo.LevelEditor.Ui.Gum.App.Info;
using LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen;
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
  /// <summary>
  /// Whether the Gum UI was built. The app bar and toolbar are not kept in fields: the Gum root
  /// holds their visuals, and their button handlers keep them alive.
  /// </summary>
  private bool _uiBuilt;

  /// <summary>Keeps at most one overlay visible; driven by the Gum toolbar.</summary>
  private readonly OverlaySwitcher _overlaySwitcher = new();

  /// <summary>Gum level generation overlay; null until the Gum UI is built.</summary>
  private GumLevelGenPanel? _gumLevelGenPanel;

  /// <summary>Gum level information overlay; null until the Gum UI is built.</summary>
  private GumLevelInfoPanel? _gumLevelInfoPanel;

  public override void Start()
  {
    EnsureGumUiBuilt();
  }

  public override void Cancel()
  {
    // Shutdown: keep the level generation forms for the next start.
    _gumLevelGenPanel?.SaveSettings();

    // Panels holding unmanaged drawing resources release them with the script.
    _gumLevelInfoPanel?.Dispose();
    _gumLevelInfoPanel = null;
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
    if (_uiBuilt) return; // already built
    if (!GumService.Default.IsInitialized) return; // Gum not ready yet; retry next frame

    ISystemInfoService systemInfo = Game.Services.GetService<ISystemInfoService>()
      ?? throw new InvalidOperationException("ISystemInfoService was not registered.");
    ILevelGenService levelGenService = Game.Services.GetService<ILevelGenService>()
      ?? throw new InvalidOperationException("ILevelGenService was not registered.");
    ISettingsService settingsService = Game.Services.GetService<ISettingsService>()
      ?? throw new InvalidOperationException("ISettingsService was not registered.");

    // Set before building: a build that fails part-way must not run again and add duplicates.
    _uiBuilt = true;

    new GumAppBar(systemInfo).Build();

    var levelGenPanel = new GumLevelGenPanel(levelGenService, settingsService);
    levelGenPanel.Build();
    _gumLevelGenPanel = levelGenPanel;
    _overlaySwitcher.Register(EnOverlay.LevelGen, levelGenPanel);

    // Raw keys: the graph pans with WASD only while the pointer is over it, and claims the
    // keyboard from gameplay meanwhile (see IClaimsKeyboardWhileHovered).
    var levelInfoPanel = new GumLevelInfoPanel(levelGenService, key => Input.IsKeyDown(key));
    levelInfoPanel.Build();
    _gumLevelInfoPanel = levelInfoPanel;
    _overlaySwitcher.Register(EnOverlay.LevelInfo, levelInfoPanel);

    new GumEditorToolbar(_overlaySwitcher).Build();
  }
}
