using System;
using LostInLimbo.LevelEditor.Ui.Main;
using LostInLimbo.LevelGen.Data.Prop;
using LostInLimbo.LevelGen.Data.Result;
using LostInLimbo.LevelGen.Data.Status;
using Stride.Core.Mathematics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using Stride.UI.Panels;

namespace LostInLimbo.LevelEditor.Ui.LevelGen;

/// <summary>
/// Builder for level generation overlay.
/// </summary>
public class UiLevelGenBuilder : UiBuilderBase
{
  /// <summary>Main panel.</summary>
  private Grid _levelGenPanel = default!;

  /// <summary>Starts generation.</summary>
  private Button _btnStart = default!;
  /// <summary>Starts generation.</summary>
  private Button _btnStep = default!;
  /// <summary>Pauses generation.</summary>
  private Button _btnPause = default!;
  /// <summary>Starts generation.</summary>
  private Button _btnStop = default!;
  /// <summary>Panel for level gen buttons.</summary>
  private StackPanel _buttonsPanel = default!;

  /// <summary>Text: Text seed.</summary>
  private TextBlock _textSeed = default!;
  /// <summary>Input for Text seed.</summary>
  private EditText _inpTextSeed = default!;
  /// <summary>Text: Difficulty.</summary>
  private TextBlock _difficulty = default!;
  /// <summary>Input for Difficulty.</summary>
  private EditText _inpDifficulty = default!;
  /// <summary>Text: Many themes.</summary>
  private TextBlock _manyThemes = default!;
  /// <summary>Checkbox for Many themes.</summary>
  private ToggleButton _inpManyThemes = default!;
  /// <summary>Panel for properties used in config generation.</summary>
  private Grid _propsPanel = default!;

  /// <summary>Panel for status texts.</summary>
  private StackPanel _statusTextPanel = default!;
  /// <summary>View scroller for status text.</summary>
  private ScrollViewer _scrollStatusText = default!;
  /// <summary>Panel for level generation status.</summary>
  private StackPanel _statusPanel = default!;

  /// <summary>Properties used for configuration generation.</summary>
  private PropMain _propMain = PropMain.Default;

  /// <summary>Build level generation overlay.</summary>
  /// <returns>Panel.</returns>
  public override Panel Build()
  {
    BuildComponents();
    return _levelGenPanel;
  }

  public override void Update()
  {
    PollRun();
  }

  /// <summary>
  /// Build all components.
  /// </summary>
  private void BuildComponents()
  {
    BuildButtonsPanel();
    BuildPropsPanel();
    BuildStatusPanel();
    _levelGenPanel = CreateLevelGenPanel();
  }

  #region Level generation panel

  /// <summary>
  /// Create level generation panel.
  /// </summary>
  /// <returns>Level generation panel.</returns>
  private Grid CreateLevelGenPanel()
  {
    var panel = new Grid()
    {
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Stretch,
      RowDefinitions =
      {
        new StripDefinition(StripType.Auto), // buttons
        new StripDefinition(StripType.Star),
        new StripDefinition(StripType.Star), // status (must be bounded so ScrollViewer can scroll)
      },
      ColumnDefinitions =
      {
        new StripDefinition(StripType.Star, 2), // spacer
        new StripDefinition(StripType.Star, 10), // panel space
        new StripDefinition(StripType.Star, 1), // spacer
        new StripDefinition(StripType.Auto), // entry
      },
      Children =
      {
        _buttonsPanel, _propsPanel, _statusPanel
      }
    };

    _buttonsPanel.SetGridRow(0);
    _buttonsPanel.SetGridColumn(4);
    _propsPanel.SetGridRow(1);
    _propsPanel.SetGridColumn(1);
    _statusPanel.SetGridRow(2);
    _statusPanel.SetGridColumn(0);
    _statusPanel.SetGridColumnSpan(4);

    panel.Visibility = Visibility.Hidden; // hidden by default
    return panel;
  }

  #endregion

  #region Buttons panel

  /// <summary>
  /// Build buttons panel.
  /// </summary>
  private void BuildButtonsPanel()
  {
    _btnStart = CreateImgButton("BtnStart", "Graphics/Icons/IconStart");
    _btnStep = CreateImgButton("BtnStep", "Graphics/Icons/IconStep");
    _btnPause = CreateImgButton("BtnPause", "Graphics/Icons/IconPause");
    _btnStop = CreateImgButton("BtnStop", "Graphics/Icons/IconStop");
    ReenableButtons(EnGenResult.None);
    _buttonsPanel = CreateButtonsPanel();

    _btnStart.Click += BtnStartClick;
    _btnStep.Click += BtnStepClick;
    _btnPause.Click += BtnPauseClick;
    _btnStop.Click += BtnStopClick;
  }

  /// <summary>
  /// Create buttons panel.
  /// </summary>
  /// <returns>Buttons panel.</returns>
  private StackPanel CreateButtonsPanel()
  {
    return new StackPanel
      {
        Orientation = Orientation.Vertical,
        Margin = new Thickness(5, 5, 5, 5),
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Top,
        Children =
        {
          _btnStart, _btnStep, _btnPause, _btnStop
        }
      };
  }

  #endregion

  #region Props panel

  /// <summary>
  /// Build status panel.
  /// </summary>
  private void BuildPropsPanel()
  {
    _textSeed = CreateTextBlock("Seed:");
    _inpTextSeed = CreateEditText("");
    _difficulty = CreateTextBlock("Difficulty:");
    _inpDifficulty = CreateEditText("");
    _manyThemes = CreateTextBlock("Can have many themes:");
    _inpManyThemes = CreateToggleButton();

    _propsPanel = CreatePropsPanel();
  }

  /// <summary>
  /// Create props panel.
  /// </summary>
  /// <returns>Props panel.</returns>
  private Grid CreatePropsPanel()
  {
    var panel = new Grid()
    {
      BackgroundColor = new Color(0, 0, 0, 64), // somewhat translucent
      Margin = new Thickness(5, 5, 5, 5),
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Stretch,
      ColumnGap = 15,
      RowDefinitions =
      {
        new StripDefinition(StripType.Auto),
        new StripDefinition(StripType.Auto),
        new StripDefinition(StripType.Auto),
      },
      ColumnDefinitions =
      {
        new StripDefinition(StripType.Auto), // text
        new StripDefinition(StripType.Star), // input
      },
      Children =
      {
        _textSeed, _inpTextSeed, _difficulty, _inpDifficulty, _manyThemes, _inpManyThemes,
      }
    };

    _textSeed.SetGridRow(0);
    _textSeed.SetGridColumn(0);
    _inpTextSeed.SetGridRow(0);
    _inpTextSeed.SetGridColumn(1);
    _difficulty.SetGridRow(1);
    _difficulty.SetGridColumn(0);
    _inpDifficulty.SetGridRow(1);
    _inpDifficulty.SetGridColumn(1);
    _manyThemes.SetGridRow(2);
    _manyThemes.SetGridColumn(0);
    _inpManyThemes.SetGridRow(2);
    _inpManyThemes.SetGridColumn(1);

    return panel;
  }

  #endregion

  #region Status panel

  /// <summary>
  /// Build status panel.
  /// </summary>
  private void BuildStatusPanel()
  {
    _statusTextPanel = CreateStatusTextPanel();
    _scrollStatusText = CreateScrollStatusText();
    _statusPanel = CreateStatusPanel();
  }

  /// <summary>
  /// Create status text.
  /// </summary>
  /// <returns>Text block.</returns>
  private static StackPanel CreateStatusTextPanel()
  {
    return new StackPanel
      {
        Orientation = Orientation.Vertical,
        Margin = new Thickness(5, 5, 5, 5),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
        Children =
        {
          // Will be filled with texts.
        }
      };
  }

  /// <summary>
  /// Create scroller for status text.
  /// </summary>
  /// <returns>Scroll viewer.</returns>
  private ScrollViewer CreateScrollStatusText()
  {
    return new ScrollViewer
    {
      Content = _statusTextPanel,
      ScrollMode = ScrollingMode.Vertical,
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Stretch,
    };
  }

  /// <summary>
  /// Create status panel.
  /// </summary>
  /// <returns>Status panel.</returns>
  private StackPanel CreateStatusPanel()
  {
    return new StackPanel
      {
        BackgroundColor = new Color(0, 0, 0, 64), // somewhat translucent
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(5, 5, 5, 5),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
        Children =
        {
          _scrollStatusText
        }
      };
  }

  #endregion

  #region Button code

  /// <summary>
  /// Is <see langword="true"/> when a level generation run started and has not yet reached a terminal state;
  /// gates re-entry and drives the poll.
  /// </summary>
  private bool _awaitingResult = false;

  /// <summary>
  /// Start level generation.
  /// </summary>
  /// <param name="sender">Sender.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void BtnStartClick(object? sender, RoutedEventArgs eventArgs)
  {
    RebuildProps();
    GenerationStatus status = UiRes.LevelGenService.Status();
    bool started = UiRes.LevelGenService.Start(_propMain);
    if (!started) {
      if (status.Result == EnGenResult.Paused) AddStatusLine("Failed to resume generation.", Color.Red);
      else AddStatusLine("Failed to start generation.", Color.Red);
      return; // failed to start/resume generation
    }

    if (status.Result == EnGenResult.Paused) AddStatusLine("Resumed generation.", Color.White);
    else AddStatusLine("Started generation.", Color.White);

    ReenableButtons(EnGenResult.Running);
    _awaitingResult = true;
  }

  /// <summary>
  /// Advance one step in level generation.
  /// </summary>
  /// <param name="sender">Sender.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void BtnStepClick(object? sender, RoutedEventArgs eventArgs)
  {
    RebuildProps();
    bool step = UiRes.LevelGenService.Step(_propMain);
    if (!step) {
      AddStatusLine("Failed to make one generation step.", Color.Red);
      return; // failed to make step
    }

    AddStatusLine("Made one generation step.", Color.White);
    ReenableButtons(EnGenResult.Paused);
    _awaitingResult = true;
  }

  /// <summary>
  /// Pause level generation.
  /// </summary>
  /// <param name="sender">Sender.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void BtnPauseClick(object? sender, RoutedEventArgs eventArgs)
  {
    bool step = UiRes.LevelGenService.Pause();
    if (!step) {
      AddStatusLine("Failed to pause generation.", Color.Red);
      return; // failed to pause
    }

    ReenableButtons(EnGenResult.Paused);
    _awaitingResult = true;
  }

  /// <summary>
  /// Stop level generation.
  /// </summary>
  /// <param name="sender">Sender.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void BtnStopClick(object? sender, RoutedEventArgs eventArgs)
  {
    bool step = UiRes.LevelGenService.Cancel();
    if (!step) {
      AddStatusLine("Failed to stop generation.", Color.Red);
      return; // failed to cancel
    }

    ReenableButtons(EnGenResult.Cancelled);
    _awaitingResult = true;
  }

  /// <summary>
  /// Add status line.
  /// </summary>
  /// <param name="statusLine">Status line.</param>
  private void AddStatusLine(string statusLine, Color color)
  {
    TextBlock statusTextLine = new()
    {
      Text = statusLine,
      TextColor = color,
      TextSize = 14,
      OutlineThickness = 1,
      Font = UiRes.Font,
      Margin = new Thickness(5, 0, 5, 0),
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Center,
    };
    _statusTextPanel.Children.Add(statusTextLine);
    _scrollStatusText.ScrollToEnd(Orientation.Vertical);
  }

  #endregion

  #region Props

  /// <summary>
  /// Rebuilds props based on current state of props form.
  /// </summary>
  private void RebuildProps()
  {
    _propMain = PropMain.Default;
  }

  #endregion

  #region Polling

  /// <summary>Check state of run and update stuff that needs to be updated.</summary>
  private void PollRun()
  {
    if (!_awaitingResult) return; // no run ongoing
    var status = UiRes.LevelGenService.Status();
    if (status.Result == EnGenResult.Running) return; // still running

    // Terminal state reached: hand the button back to the user and report the outcome.
    _awaitingResult = false;
    ReenableButtons(status.Result);

    LilWorld? world = UiRes.LevelGenService.World();
    switch (status.Result)
    {
      case EnGenResult.Finished:
        AddStatusLine($"LevelGen: Finished. Graph: nodes={world?.Graph.Nodes.Count ?? 0}, edges={world?.Graph.Edges.Count ?? 0}. Map: steps={world?.Stats.Steps ?? 0}).", Color.LightGreen);
        break;
      case EnGenResult.ValidationError:
        AddStatusLine($"LevelGen: ValidationError. Graph: nodes={world?.Graph.Nodes.Count ?? 0}, edges={world?.Graph.Edges.Count ?? 0}. Map: steps={world?.Stats.Steps ?? 0}).", Color.Orange);
        break;
      case EnGenResult.Error:
        AddStatusLine($"LevelGen: Error - {status.Error}", Color.Red);
        break;
      default:
        AddStatusLine($"LevelGen: {status.Result}.", Color.White);
        break;
    }
  }

  /// <summary>
  /// Set up enable status of all buttons.
  /// </summary>
  /// <param name="genResult">Generation result. Note null is allowed (disables all buttons).</param>
  private void ReenableButtons(EnGenResult? genResult)
  {
    switch (genResult)
    {
      case null:
        EnableButton(_btnStart, false);
        EnableButton(_btnStep, false);
        EnableButton(_btnPause, false);
        EnableButton(_btnStop, false);
        return;
      case EnGenResult.None:
        EnableButton(_btnStart, true);
        EnableButton(_btnStep, true);
        EnableButton(_btnPause, false);
        EnableButton(_btnStop, false);
        return;
      case EnGenResult.Running:
        EnableButton(_btnStart, false);
        EnableButton(_btnStep, false);
        EnableButton(_btnPause, true);
        EnableButton(_btnStop, true);
        return;
      case EnGenResult.Finished:
      case EnGenResult.Cancelled:
      case EnGenResult.Error:
      case EnGenResult.ValidationError:
        EnableButton(_btnStart, true);
        EnableButton(_btnStep, true);
        EnableButton(_btnPause, false);
        EnableButton(_btnStop, false);
        return;
      case EnGenResult.Paused:
        EnableButton(_btnStart, true);
        EnableButton(_btnStep, true);
        EnableButton(_btnPause, false);
        EnableButton(_btnStop, false);
        return;
      default: return;
    }
  }

  #endregion
}
