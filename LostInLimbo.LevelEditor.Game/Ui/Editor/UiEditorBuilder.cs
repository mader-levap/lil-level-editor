using Stride.UI;
using Stride.UI.Events;
using Stride.UI.Controls;
using Stride.Graphics;
using Stride.UI.Panels;
using Stride.Rendering.Sprites;
using LostInLimbo.LevelEditor.Ui.LevelGen;
using LostInLimbo.LevelEditor.Ui.Main;
using LostInLimbo.LevelEditor.Ui.Info;

namespace LostInLimbo.LevelEditor.Ui.Editor;

/// <summary>
/// Builder for editor UI overlay.
/// </summary>
/// <remarks>
/// This UI is main overlay on screen and contains only some basic data (like app name or version)
/// and buttons that show/hide other UI overlays.
/// </remarks>
public class UiEditorBuilder : UiBuilderBase
{
  private EnOverlay _currOverlay = EnOverlay.None;

  /// <summary>Builder for level generation overlay.</summary>
  private readonly UiLevelGenBuilder _levelGenBuilder = new();
  /// <summary>Level generation overlay.</summary>
  private Panel _levelGenPanel = default!;

  /// <summary>Builder for level information overlay.</summary>
  private readonly UiLevelInfoBuilder _levelInfoBuilder = new();
  /// <summary>Level information overlay.</summary>
  private Panel _levelInfoPanel = default!;

  /// <summary>Shows/hides level generation overlay.</summary>
  private Button _btnLevelGen = default!;
  /// <summary>Shows/hides level info overlay.</summary>
  private Button _btnLevelInfo = default!;
  /// <summary>Panel for overlay buttons.</summary>
  private StackPanel _buttonsPanel = default!;

  /// <summary>Base panel that contains app bar and buttons.</summary>
  private Grid _basePanel = default!;

  /// <summary>Main panel. Contains base panel and all overlays (by default hidden).</summary>
  private Grid _mainPanel = default!;

  /// <summary>Build main editor overlay.</summary>
  /// <returns>Panel.</returns>
  public override Panel Build()
  {
    BuildComponents();
    return _mainPanel;
  }

  public override void Update()
  {
    _levelGenBuilder.Update();
    _levelInfoBuilder.Update();
  }

  /// <summary>
  /// Build all components.
  /// </summary>
  private void BuildComponents()
  {
    _levelGenPanel = _levelGenBuilder.Build();
    _levelInfoPanel = _levelInfoBuilder.Build();
    BuildButtonsPanel();
    _basePanel = CreateBasePanel();
    _mainPanel = CreateMainPanel();
  }

  #region Buttons panel

  /// <summary>
  /// Build buttons panel.
  /// </summary>
  private void BuildButtonsPanel()
  {
    _btnLevelGen = CreateImgButton("BtnLevelGen", "Graphics/Icons/IconGears");
    _btnLevelInfo = CreateImgButton("BtnLevelInfo", "Graphics/Icons/IconInfo");
    _buttonsPanel = CreateButtonsPanel();

    _btnLevelGen.Click += BtnLevelGenClick;
    _btnLevelInfo.Click += BtnLevelInfoClick;
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
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        Children =
        {
          _btnLevelGen, _btnLevelInfo
        }
      };
  }

  #endregion

  #region Base panel

  /// <summary>
  /// Create base panel.
  /// </summary>
  /// <returns>Base panel.</returns>
  private Grid CreateBasePanel()
  {
    var panel = new Grid
    {
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Stretch,
      RowDefinitions =
      {
        new StripDefinition(StripType.Auto), // buttons
        new StripDefinition(StripType.Star), // spacer
      },
      Children =
      {
        _buttonsPanel
      }
    };

    _buttonsPanel.SetGridRow(0);
    return panel;
  }

  #endregion

  #region Main panel

  /// <summary>
  /// Create main panel.
  /// </summary>
  /// <returns>Main panel.</returns>
  private Grid CreateMainPanel()
  {
    // Panels will fully overlap if put in same cell.
    var panel = new Grid
    {
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Stretch,
      Children =
      {
        _basePanel, _levelGenPanel, _levelInfoPanel
      }
    };
    return panel;
  }

  #endregion

  #region Button code

  /// <summary>
  /// Shows or hides UI overlay for level generation.
  /// </summary>
  /// <param name="sender">Sender.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void BtnLevelGenClick(object? sender, RoutedEventArgs eventArgs)
  {
    SwitchOverlay(EnOverlay.LevelGen);
  }

  /// <summary>
  /// Shows or hides UI overlay for level information.
  /// </summary>
  /// <param name="sender">Sender.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void BtnLevelInfoClick(object? sender, RoutedEventArgs eventArgs)
  {
    SwitchOverlay(EnOverlay.LevelInfo);
  }

  /// <summary>
  /// Switch to new overlay. If overlay is already selected, deselect it.
  /// </summary>
  /// <param name="newOverlay">New overlay.</param>
  private void SwitchOverlay(EnOverlay newOverlay)
  {
    DeselectCurrButton();
    HideCurrPanel();

    if (_currOverlay == newOverlay || newOverlay == EnOverlay.None)
    {
      _currOverlay = EnOverlay.None;
      return;
    }

    _currOverlay = newOverlay;
    Button? button = FindButton(_currOverlay);
    Panel? panel = FindPanel(_currOverlay);
    if (button == null || panel == null) return;
    SelButton(button, true);
    panel.Visibility = Visibility.Visible;
  }

  /// <summary>
  /// Finds selected button and changes its state to unselected.
  /// </summary>
  private void DeselectCurrButton()
  {
    Button? currButton = FindButton(_currOverlay);
    if (currButton == null) return; // No button selected.
    SelButton(currButton, false);
  }

  /// <summary>
  /// Find button for given overlay.
  /// </summary>
  /// <returns>Button for given overlat or null if no button is assigned to overlay.</returns>
  private Button? FindButton(EnOverlay overlay) =>
    overlay switch
    {
      EnOverlay.LevelGen => _btnLevelGen,
      EnOverlay.LevelInfo => _btnLevelInfo,
      _ => null
    };

  /// <summary>
  /// Set selection state of button.
  /// </summary>
  /// <param name="button">Button to change.</param>
  private static void SelButton(Button button, bool sel)
  {
    SpriteSheet newSheet = sel ? UiRes.SpriteBtnSelected : UiRes.SpriteBtnUnselected;
    button.PressedImage = new SpriteFromSheet
      {
        Sheet = newSheet,
        CurrentFrame = 2,
      };
    button.NotPressedImage = new SpriteFromSheet
      {
        Sheet = newSheet,
        CurrentFrame = 1,
      };
    button.MouseOverImage = new SpriteFromSheet
      {
        Sheet = newSheet,
        CurrentFrame = 0,
      };
  }

  /// <summary>Hide currently selected panel.</summary>
  private void HideCurrPanel()
  {
    Panel? panel = FindPanel(_currOverlay);
    if (panel == null) return; // No panel selected.
    panel.Visibility = Visibility.Hidden;
  }

  /// <summary>
  /// Find panel for given overlay.
  /// </summary>
  /// <returns>Panel for given overlat or null if no panel is assigned to overlay.</returns>
  private Panel? FindPanel(EnOverlay overlay) =>
    overlay switch
    {
      EnOverlay.LevelGen => _levelGenPanel,
      EnOverlay.LevelInfo => _levelInfoPanel,
      _ => null
    };

  #endregion
}
