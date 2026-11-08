using Stride.UI;
using Stride.UI.Events;
using Stride.UI.Controls;
using Stride.Graphics;
using Stride.Core.Mathematics;
using LostInLimbo.LevelEditor.Services.SystemInfo.Data;
using Stride.UI.Panels;
using Stride.Rendering.Sprites;
using LostInLimbo.LevelEditor.Ui.LevelGen;
using LostInLimbo.LevelEditor.Ui.Main;
using LostInLimbo.LevelEditor.Ui.Info;
using Stride.Shaders.Parsing.SDSL.AST;
using System.Net;

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

  /// <summary>Author and name of app.</summary>
  private TextBlock _appText = default!;
  /// <summary>Version text.</summary>
  private TextBlock _versionText = default!;
  /// <summary>Build text.</summary>
  private TextBlock _buildText = default!;
  /// <summary>Stack panel for app bar.</summary>
  private Grid _appBarPanel = default!;

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
    BuildAppBarPanel();
    _basePanel = CreateBasePanel();
    _mainPanel = CreateMainPanel();
  }

  #region Buttons panel

  /// <summary>
  /// Build buttons panel.
  /// </summary>
  private void BuildButtonsPanel()
  {
    _btnLevelGen = CreateButton("BtnLevelGen", "Graphics/Icons/IconGears");
    _btnLevelInfo = CreateButton("BtnLevelInfo", "Graphics/Icons/IconInfo");
    _buttonsPanel = CreateOverlayPanel();

    _btnLevelGen.Click += BtnLevelGenClick;
    _btnLevelInfo.Click += BtnLevelInfoClick;
  }

  /// <summary>
  /// Create new button.
  /// </summary>
  /// <returns>Button.</returns>
  private Button CreateButton(string name, string imageUrl)
  {
    var button = new Button()
    {
      Name = name,
      Width = 48,
      Height = 48,
      Margin = new Thickness(0, 2, 0, 2),
      HorizontalAlignment = HorizontalAlignment.Center,
      VerticalAlignment = VerticalAlignment.Center,
      PressedImage = new SpriteFromSheet
      {
        Sheet = UiRes.SpriteBtnUnselected,
        CurrentFrame = 2,
      },
      NotPressedImage = new SpriteFromSheet
      {
        Sheet = UiRes.SpriteBtnUnselected,
        CurrentFrame = 1,
      },
      MouseOverImage = new SpriteFromSheet
      {
        Sheet = UiRes.SpriteBtnUnselected,
        CurrentFrame = 0,
      },
      Content = CreateImage(imageUrl)
    };
    return button;
  }

  /// <summary>
  /// Create app bar panel.
  /// </summary>
  /// <returns>App bar panel.</returns>
  private StackPanel CreateOverlayPanel()
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

  #region App bar panel

  /// <summary>
  /// Build app bar panel.
  /// </summary>
  private void BuildAppBarPanel()
  {
    _appText = CreateAppText();
    _versionText = CreateVersionText();
    _buildText = CreateBuildText();
    _appBarPanel = CreateAppBarPanel();
  }

  /// <summary>
  /// Create app text.
  /// </summary>
  /// <returns>Text block.</returns>
  private static TextBlock CreateAppText()
  {
    return new TextBlock
    {
      Text = $"{UiRes.SystemInfoService.GetName()} © by {UiRes.SystemInfoService.GetAuthor()}",
      TextColor = Color.White,
      OutlineThickness = 1,
      Font = UiRes.Font,
      Margin = new Thickness(5, 0, 5, 0),
      HorizontalAlignment = HorizontalAlignment.Left,
      VerticalAlignment = VerticalAlignment.Center,
    };
  }

  /// <summary>
  /// Create version text.
  /// </summary>
  /// <returns>Text block.</returns>
  private static TextBlock CreateVersionText()
  {
    return new TextBlock
    {
      Text = $"v. {UiRes.SystemInfoService.GetVersion()}",
      TextColor = Color.White,
      OutlineThickness = 1,
      Font = UiRes.Font,
      Margin = new Thickness(5, 0, 5, 0),
      HorizontalAlignment = HorizontalAlignment.Right,
      VerticalAlignment = VerticalAlignment.Center,
    };
  }

  /// <summary>
  /// Create build text.
  /// </summary>
  /// <returns>Text block.</returns>
  private static TextBlock CreateBuildText()
  {
    Color color = GetBuildColor(UiRes.SystemInfoService.GetBuild());
    return new TextBlock
    {
      Text = $"{UiRes.SystemInfoService.GetBuild()}",
      TextColor = color,
      OutlineThickness = 1,
      Font = UiRes.Font,
      Margin = new Thickness(5, 0, 5, 0),
      HorizontalAlignment = HorizontalAlignment.Right,
      VerticalAlignment = VerticalAlignment.Center,
    };
  }

  /// <summary>
  /// Create app bar panel.
  /// </summary>
  /// <returns>App bar panel.</returns>
  private Grid CreateAppBarPanel()
  {
    var panel = new Grid
    {
      BackgroundColor = new Color(0, 0, 0, 64), // very translucent
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Bottom,
      ColumnDefinitions =
      {
        new StripDefinition(StripType.Auto), // app name
        new StripDefinition(StripType.Star), // spacer
        new StripDefinition(StripType.Auto), // version
        new StripDefinition(StripType.Auto), // build
      },
      Children =
      {
        _appText, _versionText, _buildText
      }
    };

    _appText.SetGridColumn(0);
    _versionText.SetGridColumn(2);
    _buildText.SetGridColumn(3);

    return panel;
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
        new StripDefinition(StripType.Auto), // app bar
        new StripDefinition(StripType.Auto), // buttons
        new StripDefinition(StripType.Star), // spacer
      },
      Children =
      {
        _buttonsPanel, _appBarPanel
      }
    };

     _appBarPanel.SetGridRow(0);
     _buttonsPanel.SetGridRow(1);
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

  #region Utils

  /// <summary>
  /// Create image for given URL.
  /// </summary>
  /// <param name="imageUrl">URL of image.</param>
  /// <returns></returns>
  private ImageElement CreateImage(string imageUrl)
  {
    return new ImageElement()
    {
      Source = new SpriteFromTexture
      {
        Texture = UiRes.Script.Content.Load<Texture>(imageUrl)
      }
    };
  }

  /// <summary>
  /// Get text color for given build type.
  /// </summary>
  /// <param name="enBuildType">Build type.</param>
  /// <returns>Text color for given build type.</returns>
  private static Color GetBuildColor(EnBuildType enBuildType) => enBuildType switch
  {
    EnBuildType.Development => Color.Orange,
    EnBuildType.Debug => Color.Yellow,
    EnBuildType.Release => Color.Green,
    _ => Color.Red
  };

  #endregion
}
