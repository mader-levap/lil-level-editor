using Stride.Engine;
using Stride.UI;
using Stride.UI.Events;
using Stride.UI.Controls;
using Stride.Graphics;
using Stride.Core.Mathematics;
using System;
using LostInLimbo.LevelEditor.Services.SystemInfo;
using LostInLimbo.LevelEditor.Services.SystemInfo.Data;
using Stride.UI.Panels;
using Stride.Rendering.Sprites;

namespace LostInLimbo.LevelEditor.Ui.Editor;

/// <summary>
/// Script for main editor UI overlay. This UI is main overlay and contains only some basic data (like app name or version)
/// and icons that show/hide other UI overlays.
/// </summary>
public class UiMain : StartupScript
{
  /// <summary>System info service.</summary>
  private ISystemInfoService _systemInfoService = default!;
  /// <summary>Font used by text.</summary>
  private SpriteFont _font = default!;
  /// <summary>Button for unselected overlay.</summary>
  private SpriteSheet _spriteBtnUnselected = default!;
  /// <summary>Button for selected overlay.</summary>
  private SpriteSheet _spriteBtnSelected = default!;

  /// <summary>Shows/hides level generation overlay.</summary>
  private Button _btnLevelGen = default!;
  /// <summary>Some other button.</summary>
  private Button _btnOther = default!;
  /// <summary>Panel for overlay buttons.</summary>
  private StackPanel _overlayPanel = default!;

  /// <summary>Author and name of app.</summary>
  private TextBlock _appText = default!;
  /// <summary>Version text.</summary>
  private TextBlock _versionText = default!;
  /// <summary>Build text.</summary>
  private TextBlock _buildText = default!;
  /// <summary>Stack panel for app bar.</summary>
  private Grid _appBarPanel = default!;

  /// <summary>Main panel.</summary>
  private Grid _mainPanel = default!;

  public override void Start()
  {
    InitResources();
    BuildComponents();
    BuildLayout();
  }

  /// <summary>
  /// Initialize resources.
  /// </summary>
  private void InitResources()
  {
    _systemInfoService = Services.GetService<ISystemInfoService>()
      ?? throw new InvalidOperationException("ISystemInfoService was not registered.");
      
    _font = Content.Load<SpriteFont>("UI/Fonts/BasicFont");
    _spriteBtnUnselected = Content.Load<SpriteSheet>("/LostInLimbo.LevelEditor/Graphics/Ui/BtnUnselected");
    _spriteBtnSelected = Content.Load<SpriteSheet>("/LostInLimbo.LevelEditor/Graphics/Ui/BtnSelected");
  }

  /// <summary>
  /// Build all components.
  /// </summary>
  private void BuildComponents()
  {
    BuildOverlayPanel();
    BuildAppBarPanel();
    _mainPanel = CreateMainPanel();
  }

  /// <summary>
  /// Build layout.
  /// </summary>
  private void BuildLayout()
  {
    var ui = Entity.GetOrCreate<UIComponent>();
    ui.RenderGroup = Stride.Rendering.RenderGroup.Group31; // UI render group.
    ui.Page = new UIPage
    {
      RootElement = _mainPanel
    };
  }

  #region Overlay panel

  /// <summary>
  /// Build overlay panel.
  /// </summary>
  private void BuildOverlayPanel()
  {
    _btnLevelGen = CreateButton("BtnLevelGen", "Graphics/Icons/IconGears");
    _btnOther = CreateButton("BtnOther", "Graphics/Icons/IconQuestion");
    _overlayPanel = CreateOverlayPanel();

    _btnLevelGen.Click += BtnLevelGenClick;
    _btnOther.Click += BtnOtherClick;
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
        Sheet = _spriteBtnUnselected,
        CurrentFrame = 2,
      },
      NotPressedImage = new SpriteFromSheet
      {
        Sheet = _spriteBtnUnselected,
        CurrentFrame = 1,
      },
      MouseOverImage = new SpriteFromSheet
      {
        Sheet = _spriteBtnUnselected,
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
          _btnLevelGen, _btnOther
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
  private TextBlock CreateAppText()
  {
    return new TextBlock
    {
      Text = $"{_systemInfoService.GetName()} © by {_systemInfoService.GetAuthor()}",
      TextColor = Color.White,
      OutlineThickness = 1,
      Font = _font,
      Margin = new Thickness(5, 0, 5, 0),
      HorizontalAlignment = HorizontalAlignment.Left,
      VerticalAlignment = VerticalAlignment.Center,
    };
  }

  /// <summary>
  /// Create version text.
  /// </summary>
  /// <returns>Text block.</returns>
  private TextBlock CreateVersionText()
  {
    return new TextBlock
    {
      Text = $"v. {_systemInfoService.GetVersion()}",
      TextColor = Color.White,
      OutlineThickness = 1,
      Font = _font,
      Margin = new Thickness(5, 0, 5, 0),
      HorizontalAlignment = HorizontalAlignment.Right,
      VerticalAlignment = VerticalAlignment.Center,
    };
  }

  /// <summary>
  /// Create build text.
  /// </summary>
  /// <returns>Text block.</returns>
  private TextBlock CreateBuildText()
  {
    Color color = GetBuildColor(_systemInfoService.GetBuild());
    return new TextBlock
    {
      Text = $"{_systemInfoService.GetBuild()}",
      TextColor = color,
      OutlineThickness = 1,
      Font = _font,
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
        new StripDefinition(StripType.Auto), // buttons
        new StripDefinition(StripType.Star), // spacer
        new StripDefinition(StripType.Auto), // app bar
      },
      Children =
      {
        _overlayPanel, _appBarPanel
      }
    };

     _overlayPanel.SetGridRow(0);
     _appBarPanel.SetGridRow(2);
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
    // TODO: check status of UI overlay for level generation and show/hide it
    Log.Warning("BtnLevelGenClick() called.");
  }

  /// <summary>
  /// Does nothing.
  /// </summary>
  /// <param name="sender">Sender.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void BtnOtherClick(object? sender, RoutedEventArgs eventArgs)
  {
    Log.Warning("BtnOtherClick() called.");
  }

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
        Texture = Content.Load<Texture>(imageUrl)
      }
    };
  }

  /// <summary>
  /// Get text color for given build type.
  /// </summary>
  /// <param name="enBuildType">Build type.</param>
  /// <returns>Text color for given build type.</returns>
  private Color GetBuildColor(EnBuildType enBuildType) => enBuildType switch
  {
    EnBuildType.Development => Color.Red,
    EnBuildType.Debug => Color.Yellow,
    EnBuildType.Release => Color.Green,
    _ => Color.Orange
  };

  #endregion
}
