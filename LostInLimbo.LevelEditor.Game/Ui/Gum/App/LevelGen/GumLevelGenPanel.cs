using System;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Ui.Gum.App.AppBar;
using LostInLimbo.LevelEditor.Ui.Gum.App.Editor;
using LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Data;
using LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Helpers;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.Log;
using LostInLimbo.LevelEditor.Ui.Gum.Components.ProgressBar;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;
using LostInLimbo.LevelGen.Data.Result;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen;

/// <summary>
/// Gum level generation overlay: transport buttons (start/step/pause/stop) on the right, the
/// forms panel next to the toolbar (generation forms, replaced by a progress bar while a run is
/// in progress), and a scrollable status log along the bottom.
/// </summary>
/// <remarks>
/// Button and form logic lives in pure helpers (<see cref="LevelGenUiState"/>,
/// <see cref="LevelGenPropsBuilder"/>, <see cref="LevelGenStatusMessages"/>,
/// <see cref="StatusLog"/>), the forms in <see cref="GumPropertiesPanel"/> and
/// <see cref="GumConfigPanel"/>, and the reusable <see cref="GumStatusLogView"/>; this class only
/// builds visuals and wires them to the service.
/// </remarks>
public sealed class GumLevelGenPanel : IOverlayView
{
  /// <summary>Distance between panels and the window edge, in pixels.</summary>
  private const float Margin = 5;

  /// <summary>Gap between the editor toolbar and the forms panel, in pixels.</summary>
  private const float ToolbarGap = 10;

  /// <summary>Padding inside panels, in pixels.</summary>
  private const float PanelPadding = 8;

  /// <summary>Spacing between transport buttons, in pixels.</summary>
  private const float TransportSpacing = 4;

  /// <summary>Height of the status log panel, in percent of the window height.</summary>
  private const float StatusHeightPercent = 35;

  /// <summary>Height of the progress bar, in pixels.</summary>
  private const float ProgressBarHeight = 22;

  /// <summary>Spacing between the progress caption and the bar, in pixels.</summary>
  private const float ProgressSpacing = 6;

  /// <summary>Horizontal gap between forms in the forms panel, in pixels.</summary>
  private const float FormSpacing = 24;

  /// <summary>Progress caption while generation runs.</summary>
  private const string CaptionRunning = "Generating...";

  /// <summary>Progress caption while generation is paused.</summary>
  private const string CaptionPaused = "Paused";

  /// <summary>Loop button tooltip while no loop runs.</summary>
  private const string ToolTipLoopStart = "Start loop";

  /// <summary>Loop button tooltip while a loop runs.</summary>
  private const string ToolTipLoopStop = "Stop loop";

  /// <summary>Level generation service.</summary>
  private readonly ILevelGenService _levelGenService;

  /// <summary>Status log model.</summary>
  private readonly StatusLog _statusLog = new();

  /// <summary>Loop generation state.</summary>
  private readonly LevelGenLoop _loop = new();

  /// <summary>Overlay root (full window, no events of its own).</summary>
  private ContainerRuntime? _root;

  /// <summary>
  /// Forms panel: holds the generation forms, or the progress view while a run is in progress.
  /// </summary>
  private ContainerRuntime? _formsPanel;

  /// <summary>Generation properties form inside <see cref="_formsPanel"/>.</summary>
  private readonly GumPropertiesPanel _propertiesPanel = new();

  /// <summary>Configuration overrides form, right of the properties form.</summary>
  private readonly GumConfigPanel _configPanel = new();

  /// <summary>Progress view (caption + bar), centered in <see cref="_formsPanel"/>.</summary>
  private StackPanel? _progressView;

  /// <summary>Progress caption (running/paused).</summary>
  private Label? _progressCaption;

  /// <summary>Progress bar.</summary>
  private readonly GumProgressBar _progressBar = new();

  /// <summary>
  /// Whether <see cref="_formsPanel"/> was switched to a fixed size (measured with the forms
  /// shown), so it keeps its size when the forms are swapped for the progress view.
  /// </summary>
  private bool _formsPanelSizeFrozen;

  /// <summary>Start/resume button.</summary>
  private Button? _btnStart;
  /// <summary>Single step button.</summary>
  private Button? _btnStep;
  /// <summary>Pause button.</summary>
  private Button? _btnPause;
  /// <summary>Stop button.</summary>
  private Button? _btnStop;
  /// <summary>Loop button. Starts or stops loop.</summary>
  private Button? _btnLoop;

  /// <summary>View of <see cref="_statusLog"/>; null until built.</summary>
  private GumStatusLogView? _statusLogView;

  /// <summary>
  /// Is <see langword="true"/> while a request was accepted and its outcome is not reported yet;
  /// drives <see cref="Update"/> polling. After Step/Pause/Stop the buttons stay disabled until
  /// polling sees a non-running state; then the actual state is applied.
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
    _formsPanel = CreateFormsPanel();
    root.AddChild(_formsPanel);
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
      _propertiesPanel.ReleaseInput();
      _configPanel.ReleaseInput();
    }
    _root.Visible = visible;
  }

  /// <summary>
  /// Polls generation status, animates progress and reports outcomes. Call every frame; cheap when idle.
  /// </summary>
  /// <param name="deltaSeconds">Elapsed time since the last frame, in seconds.</param>
  public void Update(float deltaSeconds)
  {
    _statusLogView?.Update();
    if (_progressView?.IsVisible == true) _progressBar.Update(deltaSeconds);

    if (!_awaitingResult) return; // no request in flight
    GenerationStatus status = _levelGenService.Status();
    _progressBar.SetTarget(status.Completion);
    if (status.Result == EnGenResult.Running) return; // still running

    _awaitingResult = false;
    if (_loop.IsActive)
    {
      OnLoopRunEnded(status);
      return;
    }

    // Run reached a non-running state: hand the buttons back and report the outcome.
    ApplyUiState(LevelGenUiState.For(status.Result));

    LilWorld? world = _levelGenService.World();
    _statusLog.Add(LevelGenStatusMessages.ForOutcome(
      status.Result,
      _levelGenService.RunInputs()?.Props.TextSeed,
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

    _btnLoop = GumUiFactory.CreateIconButton(GumIcons.Loop, ToolTipLoopStart);
    _btnLoop.Click += (_, _) => OnLoopClicked();
    stack.AddChild(_btnLoop);

    return stack;
  }

  /// <summary>
  /// Creates the forms panel next to the editor toolbar: the generation forms and the (hidden)
  /// progress view in the same panel.
  /// </summary>
  /// <returns>Panel container.</returns>
  private ContainerRuntime CreateFormsPanel()
  {
    ContainerRuntime panel = GumUiFactory.CreatePanel();
    panel.X = Margin + GumUiFactory.IconButtonSize + ToolbarGap;
    panel.Y = GumAppBar.Height + Margin;
    panel.Width = PanelPadding; // right padding added to the children's extent
    panel.WidthUnits = DimensionUnitType.RelativeToChildren;
    panel.Height = PanelPadding; // bottom padding added to the children's extent
    panel.HeightUnits = DimensionUnitType.RelativeToChildren;

    // Forms side by side, top-aligned, inside the panel's padding.
    var forms = new StackPanel
    {
      Orientation = Orientation.Horizontal,
      Spacing = FormSpacing,
      X = PanelPadding,
      Y = PanelPadding,
    };
    forms.AddChild(_propertiesPanel.Build());
    forms.AddChild(_configPanel.Build());
    panel.AddChild(forms.Visual);

    panel.AddChild(CreateProgressView().Visual);
    return panel;
  }

  /// <summary>
  /// Creates the progress view (caption above the bar), centered in the forms panel and
  /// hidden until a run is in progress.
  /// </summary>
  /// <returns>Stack panel.</returns>
  private StackPanel CreateProgressView()
  {
    var view = new StackPanel
    {
      Spacing = ProgressSpacing,
      IsVisible = false,
    };
    view.Anchor(Anchor.Center);
    // Does not size the panel: the panel is sized by the forms (see FreezeFormsPanelSize).
    view.Visual.IgnoredByParentSize = true;

    _progressCaption = GumUiFactory.CreateLabel(CaptionRunning, EditorTheme.Palette.TextPrimary);
    _progressCaption.Anchor(Anchor.Top); // centers horizontally inside the stack
    view.AddChild(_progressCaption);

    // As wide as the forms row, less the padding on each side (the panel adds its own padding).
    float formsWidth = GumPropertiesPanel.ContentWidth + FormSpacing + GumConfigPanel.ContentWidth;
    ContainerRuntime bar = _progressBar.Build(formsWidth - 2 * PanelPadding, ProgressBarHeight);
    view.AddChild(bar);

    _progressView = view;
    return view;
  }

  /// <summary>
  /// Fixes the forms panel at its current size (measured while the forms are shown), so it does
  /// not shrink when the forms are hidden. Does nothing until the panel has been laid out.
  /// </summary>
  private void FreezeFormsPanelSize()
  {
    if (_formsPanelSizeFrozen || _formsPanel == null) return;

    float width = _formsPanel.AbsoluteWidth;
    float height = _formsPanel.AbsoluteHeight;
    if (width <= 2 * PanelPadding || height <= 2 * PanelPadding) return; // not laid out yet

    _formsPanel.WidthUnits = DimensionUnitType.Absolute;
    _formsPanel.Width = width;
    _formsPanel.HeightUnits = DimensionUnitType.Absolute;
    _formsPanel.Height = height;
    _formsPanelSizeFrozen = true;
  }

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
    bool started = _levelGenService.Start(_propertiesPanel.BuildProps(), _configPanel.BuildOverrides());
    _statusLog.Add(LevelGenStatusMessages.ForStart(wasPaused, started, _propertiesPanel.Seed));
    if (!started) return;

    ApplyUiState(LevelGenUiState.For(EnGenResult.Running));
    _awaitingResult = true;
  }

  /// <summary>
  /// Advances generation by one step.
  /// </summary>
  private void OnStepClicked()
  {
    bool stepped = _levelGenService.Step(_propertiesPanel.BuildProps(), _configPanel.BuildOverrides());
    _statusLog.Add(LevelGenStatusMessages.ForStep(stepped));
    if (!stepped) return;

    // The step runs on the worker; keep everything disabled until Update sees it finish.
    ApplyUiState(LevelGenUiState.AllDisabled);
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

    // Pause is only requested; the worker finishes its current step first. Keep everything
    // disabled until Update sees a non-running state and applies the real one.
    ApplyUiState(LevelGenUiState.AllDisabled);
    _awaitingResult = true;
  }

  /// <summary>
  /// Stops generation: cancels a running run, or clears a paused one (paused runs can't be
  /// cancelled).
  /// </summary>
  private void OnStopClicked()
  {
    if (_levelGenService.Status().Result == EnGenResult.Paused)
    {
      ClearPausedRun();
      return;
    }

    if (!_levelGenService.Cancel())
    {
      _statusLog.Add(LevelGenStatusMessages.StopFailed);
      return;
    }

    // Cancel is only requested; the worker finishes its current step first. Keep everything
    // disabled until Update sees a non-running state and applies the real one.
    ApplyUiState(LevelGenUiState.AllDisabled);
    _awaitingResult = true;
  }

  /// <summary>
  /// Discards a paused run. Clearing is synchronous, so the new state is applied right away
  /// (no polling).
  /// </summary>
  private void ClearPausedRun()
  {
    if (!_levelGenService.Clear())
    {
      _statusLog.Add(LevelGenStatusMessages.StopFailed);
      return;
    }

    _statusLog.Add(LevelGenStatusMessages.Cleared);
    ApplyUiState(LevelGenUiState.For(_levelGenService.Status().Result));
  }

  #endregion

  #region Loop

  /// <summary>
  /// Starts or stops generation in loop.
  /// </summary>
  private void OnLoopClicked()
  {
    if (_loop.IsActive) StopLoop();
    else StartLoop();
  }

  /// <summary>
  /// Starts a generation loop from the form seed (see <see cref="LevelGenLoop"/>).
  /// </summary>
  private void StartLoop()
  {
    _loop.Begin(_propertiesPanel.Seed);
    SetLoopButtonIcon(looping: true);
    _statusLog.Add(LevelGenStatusMessages.ForLoopStart(_loop.CurrentSeed));
    StartLoopIteration();
  }

  /// <summary>
  /// Starts generation of the current loop iteration. Its seed is written to the seed box first,
  /// so the form always holds the seed of the latest run (a failure is reproducible with Start).
  /// </summary>
  private void StartLoopIteration()
  {
    _propertiesPanel.Seed = _loop.CurrentSeed;
    if (!_levelGenService.Start(_propertiesPanel.BuildProps(), _configPanel.BuildOverrides()))
    {
      _statusLog.Add(LevelGenStatusMessages.ForLoopStartFailed(_loop.CurrentSeed));
      EndLoop();
      return;
    }

    ApplyUiState(LevelGenUiState.Looping);
    _progressBar.Reset(_levelGenService.Status().Completion); // new run: no easing from the previous one
    _awaitingResult = true;
  }

  /// <summary>
  /// Handles the end of a loop iteration's run: starts the next iteration, or ends the loop on
  /// failure.
  /// </summary>
  /// <param name="status">Non-running status the run reached.</param>
  private void OnLoopRunEnded(GenerationStatus status)
  {
    string seed = _loop.CurrentSeed; // seed of the run that just ended (advanced on success)
    LilWorld? world = _levelGenService.World();

    switch (_loop.OnRunEnded(status.Result))
    {
      case EnLoopDecision.Continue:
        _statusLog.Add(LevelGenStatusMessages.ForLoopIteration(
          seed,
          world?.Graph.Nodes.Count ?? 0,
          world?.Graph.Edges.Count ?? 0,
          world?.Stats.Steps ?? 0));
        StartLoopIteration();
        break;
      case EnLoopDecision.Failed:
        _statusLog.Add(LevelGenStatusMessages.ForLoopFailure(seed, status.Result, status.Error?.ToString()));
        EndLoop();
        break;
    }
  }

  /// <summary>
  /// Stops the loop on user request: cancels the current run. The run's outcome is reported by
  /// <see cref="Update"/> as for a normal run.
  /// </summary>
  private void StopLoop()
  {
    _loop.End();
    SetLoopButtonIcon(looping: false);
    _statusLog.Add(LevelGenStatusMessages.LoopStopped);

    if (_levelGenService.Cancel())
    {
      // Cancel is only requested; keep everything disabled until Update sees it end.
      ApplyUiState(LevelGenUiState.AllDisabled);
      _awaitingResult = true;
      return;
    }

    // The run is not running anymore (it ended this frame); its result is polled as usual.
    if (!_awaitingResult) ApplyUiState(LevelGenUiState.For(_levelGenService.Status().Result));
  }

  /// <summary>
  /// Ends the loop (failure) and restores the loop button and the state of the other buttons.
  /// </summary>
  private void EndLoop()
  {
    _loop.End();
    SetLoopButtonIcon(looping: false);
    ApplyUiState(LevelGenUiState.For(_levelGenService.Status().Result));
  }

  /// <summary>
  /// Shows the loop button's start or stop icon.
  /// </summary>
  /// <param name="looping">Whether a loop runs (button stops it).</param>
  private void SetLoopButtonIcon(bool looping)
  {
    if (_btnLoop == null) return;
    if (looping) GumUiFactory.SetIconButtonIcon(_btnLoop, GumIcons.LoopStop, ToolTipLoopStop);
    else GumUiFactory.SetIconButtonIcon(_btnLoop, GumIcons.Loop, ToolTipLoopStart);
  }

  #endregion

  #region Form and state

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
    if (_btnLoop != null) _btnLoop.IsEnabled = state.Loop;

    if (!state.PropsVisible)
    {
      _propertiesPanel.ReleaseInput();
      _configPanel.ReleaseInput();
      FreezeFormsPanelSize(); // forms are still shown, so this measures their size
    }
    _propertiesPanel.IsVisible = state.PropsVisible;
    _configPanel.IsVisible = state.PropsVisible;
    ApplyProgressState(state);
  }

  /// <summary>
  /// Shows the progress view while a run is in progress (form hidden) and sets its caption,
  /// value and animation from the current status.
  /// </summary>
  /// <param name="state">UI state.</param>
  private void ApplyProgressState(LevelGenUiState state)
  {
    if (_progressView == null) return;

    bool show = !state.PropsVisible;
    bool wasShown = _progressView.IsVisible;
    _progressView.IsVisible = show;
    if (!show) return;

    GenerationStatus status = _levelGenService.Status();
    bool paused = status.Result == EnGenResult.Paused;
    if (_progressCaption != null) _progressCaption.Text = paused ? CaptionPaused : CaptionRunning;
    _progressBar.SetAnimating(!paused); // animate only if not paused
    if (wasShown) _progressBar.SetTarget(status.Completion);
    else _progressBar.Reset(status.Completion); // fresh view: no easing from a stale value
  }

  #endregion
}
