using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace LostInLimbo.LevelEditor.Ui.LevelGen;

/// <summary>
/// Script for level generation overlay.
/// </summary>
public class UiLevelGen : StartupScript
{
  /// <summary>Font used by text.</summary>
  private SpriteFont _font = default!;

  /// <summary>Test text.</summary>
  private TextBlock _testText = default!;
  /// <summary>Main panel.</summary>
  private Grid _mainPanel = default!;

  public override void Start()
  {
    BuildComponents();
    BuildLayout();
  }

  /// <summary>
  /// Build all components.
  /// </summary>
  private void BuildComponents()
  {
    _font = Content.Load<SpriteFont>("UI/Fonts/BasicFont");

    // TODO: call subpanels like BuildOverlayPanel();
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

  #region Main panel

  /// <summary>
  /// Create main panel.
  /// </summary>
  /// <returns>Main panel.</returns>
  private Grid CreateMainPanel()
  {
    _testText = new TextBlock
    {
      Text = "TEST TEXT",
      TextColor = Color.White,
      OutlineThickness = 1,
      Font = _font,
      Margin = new Thickness(5, 0, 5, 0),
      HorizontalAlignment = HorizontalAlignment.Center,
      VerticalAlignment = VerticalAlignment.Center,
    };

    var panel = new Grid()
    {
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Stretch,
      RowDefinitions =
      {
        new StripDefinition(StripType.Star), // spacer
        new StripDefinition(StripType.Auto), // entry
        new StripDefinition(StripType.Star), // spacer
      },
      ColumnDefinitions =
      {
        new StripDefinition(StripType.Star), // spacer
        new StripDefinition(StripType.Auto), // entry
        new StripDefinition(StripType.Star), // spacer
      },
      Children =
      {
        _testText
      }
    };

    _testText.SetGridColumn(1);
    _testText.SetGridRow(1);
    return panel;
  }

  #endregion
}
