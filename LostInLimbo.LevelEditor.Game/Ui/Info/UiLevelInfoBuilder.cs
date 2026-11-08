using LostInLimbo.LevelEditor.Ui.Main;
using Stride.Core.Mathematics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace LostInLimbo.LevelEditor.Ui.Info;

/// <summary>
/// Builder for general information overlay.
/// </summary>
public class UiLevelInfoBuilder : UiBuilderBase
{
  /// <summary>Test text.</summary>
  private TextBlock _testText = default!;
  /// <summary>Main panel.</summary>
  private Grid _infoPanel = default!;

  /// <summary>Build level information overlay.</summary>
  /// <returns>Panel.</returns>
  public override Panel Build()
  {
    BuildComponents();
    return _infoPanel;
  }

  public override void Update()
  {

  }

  /// <summary>
  /// Build all components.
  /// </summary>
  private void BuildComponents()
  {
    // TODO: call subpanels like BuildOverlayPanel();
    _infoPanel = CreateLevelGenPanel();
  }

  #region Level information panel

  /// <summary>
  /// Create level information panel.
  /// </summary>
  /// <returns>Level information panel.</returns>
  private Grid CreateLevelGenPanel()
  {
    _testText = new TextBlock
    {
      Text = "TODO: HERE WILL BE LEVEL INFORMATION OVERLAY",
      TextColor = Color.White,
      OutlineThickness = 1,
      Font = UiRes.Font,
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

    panel.Visibility = Visibility.Hidden; // hidden by default
    return panel;
  }

  #endregion
}
