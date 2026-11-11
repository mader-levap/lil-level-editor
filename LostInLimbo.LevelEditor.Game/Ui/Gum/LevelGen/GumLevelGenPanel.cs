using System;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Ui.Gum.AppBar;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Common.Log;
using LostInLimbo.LevelEditor.Ui.Gum.Editor;
using LostInLimbo.LevelGen.Data.Prop;
using LostInLimbo.LevelGen.Data.Result;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Ui.Gum.LevelGen;

/// <summary>
/// Gum level generation overlay: transport buttons (start/step/pause/stop) on the right, the
/// generation properties form next to the toolbar, and a scrollable status log along the bottom.
/// </summary>
/// <remarks>
/// Button and form logic lives in pure helpers (<see cref="LevelGenUiState"/>,
/// <see cref="LevelGenPropsBuilder"/>, <see cref="LevelGenStatusMessages"/>,
/// <see cref="StatusLog"/>) and the reusable <see cref="GumStatusLogView"/>; this class only
/// builds visuals and wires them to the service.
/// </remarks>
public sealed class GumLevelGenPanel : IOverlayView
{
  /// <summary>Distance between panels and the window edge, in pixels.</summary>
  private const float Margin = 5;

  /// <summary>Gap between the editor toolbar and the properties form, in pixels.</summary>
  private const float ToolbarGap = 10;

  /// <summary>Padding inside panels, in pixels.</summary>
  private const float PanelPadding = 8;

  /// <summary>Spacing between transport buttons, in pixels.</summary>
  private const float TransportSpacing = 4;

  /// <summary>Spacing between form rows, in pixels.</summary>
  private const float RowSpacing = 6;

  /// <summary>Width of the form's label column, in pixels.</summary>
  private const float LabelWidth = 210;

  /// <summary>Width of the form's input column, in pixels.</summary>
  private const float InputWidth = 200;

  /// <summary>Height of the status log panel, in percent of the window height.</summary>
  private const float StatusHeightPercent = 35;

  /// <summary>Level generation service.</summary>
  private readonly ILevelGenService _levelGenService;

  /// <summary>Status log model.</summary>
  private readonly StatusLog _statusLog = new();

  /// <summary>Overlay root (full window, no events of its own).</summary>
  private ContainerRuntime? _root;

  /// <summary>Properties form panel.</summary>
  private ContainerRuntime? _propsPanel;

  /// <summary>Seed input.</summary>
  private TextBox? _seedBox;

  /// <summary>Difficulty preset choice.</summary>
  private ComboBox? _difficultyBox;

  /// <summary>Many-themes check box.</summary>
  private CheckBox? _manyThemesBox;

  /// <summary>Start/resume button.</summary>
  private Button? _btnStart;

  /// <summary>Single step button.</summary>
  private Button? _btnStep;

  /// <summary>Pause button.</summary>
  private Button? _btnPause;

  /// <summary>Stop button.</summary>
  private Button? _btnStop;

  /// <summary>View of <see cref="_statusLog"/>; null until built.</summary>
  private GumStatusLogView? _statusLogView;

  /// <summary>
  /// Is <see langword="true"/> while a request was accepted and its outcome is not reported yet;
  /// drives <see cref="Update"/> polling.
  /// </summary>
  private bool _awaitingResult;

  /// <summary>
  /// Creates the panel. Call <see cref="Build"/> to create its visuals.
  /// </summary>
  /// <param name="levelGenService">Level generation service.</param>
  /// <exception cref="ArgumentNullException"><paramref name="levelGenService"/> is null.</exception>
  public GumLevelGenPanel(ILevelGenService levelGenService)
  {
    _levelGenService = levelGenService ?? throw new ArgumentNullException(nameof(levelGenService));
  }

  /// <summary>
  /// Builds the overlay (hidden) and adds it to the Gum root.
  /// </summary>
  /// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
  /// <exception cref="InvalidOperationException">The panel was already built.</exception>
  public void Build()
  {
    if (_root != null) throw new InvalidOperationException("Level generation panel was already built.");

    // Full-window root without events, so empty areas still let the camera look around.
    var root = new ContainerRuntime
    {
      HasEvents = false,
      Visible = false,
      Width = 0,
      WidthUnits = DimensionUnitType.RelativeToParent,
      Height = 0,
      HeightUnits = DimensionUnitType.RelativeToParent,
    };

    root.AddChild(CreateTransportPanel().Visual);
    _propsPanel = CreatePropsPanel();
    root.AddChild(_propsPanel);
    root.AddChild(CreateStatusPanel());

    root.AddToRoot();
    _root = root;

    ApplyUiState(LevelGenUiState.For(_levelGenService.Status().Result));
  }

  /// <inheritdoc />
  public void SetVisible(bool visible)
  {
    if (_root == null) return;
    if (!visible)
    {
      // Release keyboard and close popups so nothing invisible keeps input.
      if (_seedBox != null) _seedBox.IsFocused = false;
      if (_difficultyBox != null) _difficultyBox.IsDropDownOpen = false;
    }
    _root.Visible = visible;
  }

  /// <summary>
  /// Polls generation status and reports outcomes. Call every frame; cheap when idle.
  /// </summary>
  public void Update()
  {
    _statusLogView?.Update();

    if (!_awaitingResult) return; // no request in flight
    GenerationStatus status = _levelGenService.Status();
    if (status.Result == EnGenResult.Running) return; // still running

    // Run reached a non-running state: hand the buttons back and report the outcome.
    _awaitingResult = false;
    ApplyUiState(LevelGenUiState.For(status.Result));

    LilWorld? world = _levelGenService.World();
    _statusLog.Add(LevelGenStatusMessages.ForOutcome(
      status.Result,
      world?.Graph.Nodes.Count ?? 0,
      world?.Graph.Edges.Count ?? 0,
      world?.Stats.Steps ?? 0,
      status.Error?.ToString()));
  }

  #region Building

  /// <summary>
  /// Creates the transport button column at the top-right.
  /// </summary>
  /// <returns>Stack panel.</returns>
  private StackPanel CreateTransportPanel()
  {
    var stack = new StackPanel
    {
      Spacing = TransportSpacing,
    };
    stack.Anchor(Anchor.TopRight);
    stack.X = -Margin;
    stack.Y = GumAppBar.Height + Margin;

    _btnStart = GumUiFactory.CreateIconButton(GumIcons.Start, "Start / resume generation");
    _btnStart.Click += (_, _) => OnStartClicked();
    stack.AddChild(_btnStart);

    _btnStep = GumUiFactory.CreateIconButton(GumIcons.Step, "One generation step");
    _btnStep.Click += (_, _) => OnStepClicked();
    stack.AddChild(_btnStep);

    _btnPause = GumUiFactory.CreateIconButton(GumIcons.Pause, "Pause generation");
    _btnPause.Click += (_, _) => OnPauseClicked();
    stack.AddChild(_btnPause);

    _btnStop = GumUiFactory.CreateIconButton(GumIcons.Stop, "Stop generation");
    _btnStop.Click += (_, _) => OnStopClicked();
    stack.AddChild(_btnStop);

    return stack;
  }

  /// <summary>
  /// Creates the properties form next to the editor toolbar.
  /// </summary>
  /// <returns>Panel container.</returns>
  private ContainerRuntime CreatePropsPanel()
  {
    ContainerRuntime panel = GumUiFactory.CreatePanel();
    panel.X = Margin + GumUiFactory.IconButtonSize + ToolbarGap;
    panel.Y = GumAppBar.Height + Margin;
    panel.Width = PanelPadding; // right padding added to the children's extent
    panel.WidthUnits = DimensionUnitType.RelativeToChildren;
    panel.Height = PanelPadding; // bottom padding added to the children's extent
    panel.HeightUnits = DimensionUnitType.RelativeToChildren;

    var rows = new StackPanel
    {
      X = PanelPadding,
      Y = PanelPadding,
      Spacing = RowSpacing,
    };

    _seedBox = new TextBox
    {
      Width = InputWidth,
      Placeholder = "empty = current time",
    };
    rows.AddChild(CreateFormRow("Seed:", _seedBox));

    _difficultyBox = GumUiFactory.CreateComboBox(InputWidth);
    var difficultyItems = _difficultyBox.Items
      ?? throw new InvalidOperationException("Gum ComboBox has no item list.");
    foreach (DifficultyPreset preset in DifficultyPresets.All) difficultyItems.Add(preset.Name);
    _difficultyBox.SelectedIndex = DifficultyPresets.DefaultIndex;
    rows.AddChild(CreateFormRow("Difficulty:", _difficultyBox));

    _manyThemesBox = new CheckBox
    {
      Text = string.Empty,
      IsChecked = PropMain.Default.ManyThemes,
    };
    rows.AddChild(CreateFormRow("Can have many themes:", _manyThemesBox));

    panel.AddChild(rows.Visual);
    return panel;
  }

  /// <summary>
  /// Creates one form row with this form's label column width.
  /// </summary>
  /// <param name="labelText">Label text.</param>
  /// <param name="input">Input control.</param>
  /// <returns>Horizontal stack panel.</returns>
  private static StackPanel CreateFormRow(string labelText, FrameworkElement input) =>
    GumUiFactory.CreateFormRow(labelText, input, LabelWidth);

  /// <summary>
  /// Creates the status log panel along the bottom of the window.
  /// </summary>
  /// <returns>Panel container.</returns>
  private ContainerRuntime CreateStatusPanel()
  {
    _statusLogView = new GumStatusLogView(_statusLog);
    ContainerRuntime panel = _statusLogView.Build();
    panel.Anchor(Anchor.Bottom);
    panel.Y = -Margin;
    panel.Width = -2 * Margin;
    panel.WidthUnits = DimensionUnitType.RelativeToParent;
    panel.Height = StatusHeightPercent;
    panel.HeightUnits = DimensionUnitType.PercentageOfParent;
    return panel;
  }

  #endregion

  #region Actions

  /// <summary>
  /// Starts generation, or resumes it when paused.
  /// </summary>
  private void OnStartClicked()
  {
    bool wasPaused = _levelGenService.Status().Result == EnGenResult.Paused;
    bool started = _levelGenService.Start(BuildProps());
    _statusLog.Add(LevelGenStatusMessages.ForStart(wasPaused, started));
    if (!started) return;

    ApplyUiState(LevelGenUiState.For(EnGenResult.Running));
    _awaitingResult = true;
  }

  /// <summary>
  /// Advances generation by one step.
  /// </summary>
  private void OnStepClicked()
  {
    bool stepped = _levelGenService.Step(BuildProps());
    _statusLog.Add(LevelGenStatusMessages.ForStep(stepped));
    if (!stepped) return;

    ApplyUiState(LevelGenUiState.For(EnGenResult.Paused));
    _awaitingResult = true;
  }

  /// <summary>
  /// Pauses generation.
  /// </summary>
  private void OnPauseClicked()
  {
    if (!_levelGenService.Pause())
    {
      _statusLog.Add(LevelGenStatusMessages.PauseFailed);
      return;
    }

    ApplyUiState(LevelGenUiState.For(EnGenResult.Paused));
    _awaitingResult = true;
  }

  /// <summary>
  /// Stops (cancels) generation.
  /// </summary>
  private void OnStopClicked()
  {
    if (!_levelGenService.Cancel())
    {
      _statusLog.Add(LevelGenStatusMessages.StopFailed);
      return;
    }

    ApplyUiState(LevelGenUiState.For(EnGenResult.Cancelled));
    _awaitingResult = true;
  }

  /// <summary>
  /// Builds generation properties from the form.
  /// </summary>
  /// <returns>Properties.</returns>
  private PropMain BuildProps()
  {
    int index = _difficultyBox?.SelectedIndex ?? DifficultyPresets.DefaultIndex;
    if (index < 0 || index >= DifficultyPresets.All.Count) index = DifficultyPresets.DefaultIndex;

    return LevelGenPropsBuilder.Build(
      _seedBox?.Text,
      DifficultyPresets.All[index].Value,
      _manyThemesBox?.IsChecked == true);
  }

  /// <summary>
  /// Applies button enable states and form visibility.
  /// </summary>
  /// <param name="state">UI state.</param>
  private void ApplyUiState(LevelGenUiState state)
  {
    if (_btnStart != null) _btnStart.IsEnabled = state.Start;
    if (_btnStep != null) _btnStep.IsEnabled = state.Step;
    if (_btnPause != null) _btnPause.IsEnabled = state.Pause;
    if (_btnStop != null) _btnStop.IsEnabled = state.Stop;

    if (_propsPanel != null)
    {
      if (!state.PropsVisible)
      {
        if (_seedBox != null) _seedBox.IsFocused = false;
        if (_difficultyBox != null) _difficultyBox.IsDropDownOpen = false;
      }
      _propsPanel.Visible = state.PropsVisible;
    }
  }

  #endregion
}
