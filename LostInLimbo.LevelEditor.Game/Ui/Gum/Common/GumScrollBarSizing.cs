using System;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;

namespace LostInLimbo.LevelEditor.Ui.Gum.Common;

/// <summary>
/// Gives Gum scroll bars a custom thickness with square arrow buttons.
/// </summary>
/// <remarks>
/// Gum's default scroll bar visual makes each arrow button a fixed 24 px long along the bar and
/// the track container (<c>ThumbContainer</c>) 48 px shorter than the bar. Its horizontal layout
/// also keeps the buttons 24 px tall, so they stuck out of a thinner bar.
/// </remarks>
public static class GumScrollBarSizing
{
  /// <summary>Thickness of the editor's scroll bars, in pixels.</summary>
  public const float DefaultThickness = 15f;

  /// <summary>Name of the element holding track and thumb in Gum's default scroll bar visual.</summary>
  private const string TrackContainerName = "ThumbContainer";

  /// <summary>
  /// Sets a scroll bar's thickness (across the bar) and sizes its arrow buttons as squares with
  /// that side, the track between them filling the rest. The bar's length is left alone.
  /// </summary>
  /// <param name="scrollBar">Scroll bar (orientation already set).</param>
  /// <param name="thickness">Thickness, in pixels.</param>
  /// <exception cref="ArgumentNullException"><paramref name="scrollBar"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="thickness"/> is not positive.</exception>
  public static void ApplyThickness(ScrollBar scrollBar, float thickness = DefaultThickness)
  {
    ArgumentNullException.ThrowIfNull(scrollBar);
    if (!(thickness > 0)) throw new ArgumentOutOfRangeException(nameof(thickness), thickness, "Thickness must be positive.");

    bool horizontal = scrollBar.Orientation == Orientation.Horizontal;
    GraphicalUiElement bar = scrollBar.Visual;
    if (horizontal)
    {
      bar.Height = thickness;
      bar.HeightUnits = DimensionUnitType.Absolute;
    }
    else
    {
      bar.Width = thickness;
      bar.WidthUnits = DimensionUnitType.Absolute;
    }

    foreach (Button? button in new[] { scrollBar.UpButton, scrollBar.DownButton })
    {
      if (button == null) continue;
      GraphicalUiElement visual = button.Visual;
      visual.Width = horizontal ? thickness : 0;
      visual.WidthUnits = horizontal ? DimensionUnitType.Absolute : DimensionUnitType.RelativeToParent;
      visual.Height = horizontal ? 0 : thickness;
      visual.HeightUnits = horizontal ? DimensionUnitType.RelativeToParent : DimensionUnitType.Absolute;
    }

    if (bar.GetGraphicalUiElementByName(TrackContainerName) is { } trackContainer)
    {
      if (horizontal) trackContainer.Width = -2 * thickness;
      else trackContainer.Height = -2 * thickness;
    }
  }
}
