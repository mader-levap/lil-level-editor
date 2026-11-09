using LostInLimbo.LevelEditor.Services.SystemInfo.Data;
using LostInLimbo.LevelEditor.Ui.Editor;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
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
    UiRes.InitResources(this);
    BuildComponents();
    AttachUi();
  }

  public override void Update()
  {
    _editorBuilder.Update();

    // Blur the focused text field when the user clicks anywhere outside of it.
    UiFocus.ReleaseTextInputFocusOnOutsidePress(Game.Services);
  }

  /// <summary>
  /// Build all components.
  /// </summary>
  private void BuildComponents()
  {
    BuildAppBarPanel();
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
      RowDefinitions =
      {
        new StripDefinition(StripType.Auto), // app bar
        new StripDefinition(StripType.Star), // rest
      },
      Children =
      {
        _appBarPanel, _editorPanel
      }
    };

    _appBarPanel.SetGridRow(0);
    _editorPanel.SetGridRow(1);

    return panel;
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
      BackgroundColor = new Color(0, 0, 0, 64), // somewhat translucent
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

  #region Utils

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
