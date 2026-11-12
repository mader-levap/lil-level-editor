using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Look and limits of the graph visualizer. Lengths are in world units, which equal screen
/// pixels at zoom 1.
/// </summary>
public sealed record GraphVisStyle
{
  /// <summary>Style used unless a caller supplies another.</summary>
  public static GraphVisStyle Default { get; } = new();

  #region Nodes

  /// <summary>Radius of a node with relative size 1.</summary>
  public float NodeBaseRadius { get; init; } = 22f;

  /// <summary>Smallest node radius, so tiny nodes stay clickable and can hold a marker.</summary>
  public float NodeMinRadius { get; init; } = 14f;

  /// <summary>Largest node radius, so one huge node cannot fill the view.</summary>
  public float NodeMaxRadius { get; init; } = 70f;

  /// <summary>Node outline width.</summary>
  public float NodeOutlineWidth { get; init; } = 4f;

  /// <summary>Brightness of the outline relative to the fill (0 = black, 1 = same as fill).</summary>
  public float NodeOutlineBrightness { get; init; } = 0.6f;

  /// <summary>Fill color of nodes without a color of their own (light gray).</summary>
  public UiRgba DefaultNodeColor { get; init; } = new(211, 211, 211);

  #endregion

  #region Edges

  /// <summary>Edge line width.</summary>
  public float EdgeWidth { get; init; } = 3f;

  /// <summary>Line color of edges without a color of their own.</summary>
  public UiRgba DefaultEdgeColor { get; init; } = UiRgba.Black;

  /// <summary>Arrowhead length, from tip to base.</summary>
  public float ArrowLength { get; init; } = 12f;

  /// <summary>Half of the arrowhead base width.</summary>
  public float ArrowHalfWidth { get; init; } = 6f;

  /// <summary>Font size of the text over an edge.</summary>
  public float EdgeLabelFontSize { get; init; } = 13f;

  #endregion

  #region Dashes

  /// <summary>Length of one dash of dashed outlines and lines.</summary>
  public float DashLength { get; init; } = 8f;

  /// <summary>Gap between two dashes.</summary>
  public float DashGap { get; init; } = 6f;

  #endregion

  #region Markers

  /// <summary>Marker dot radius.</summary>
  public float MarkerRadius { get; init; } = 3.5f;

  /// <summary>Most markers shown individually; more collapse into one dot labelled with the count.</summary>
  public int MaxMarkersShown { get; init; } = 3;

  /// <summary>Font size of marker labels.</summary>
  public float MarkerFontSize { get; init; } = 10f;

  /// <summary>Smallest distance between the centers of two neighbouring marker dots.</summary>
  public float MarkerMinSpacing { get; init; } = 12f;

  /// <summary>Free space kept between two neighbouring marker labels.</summary>
  public float MarkerLabelGap { get; init; } = 4f;

  /// <summary>Distance from a marker dot's center up to the baseline of its label.</summary>
  public float MarkerLabelRise { get; init; } = 7f;

  /// <summary>Color of marker dots and their labels.</summary>
  public UiRgba MarkerColor { get; init; } = UiRgba.Black;

  #endregion

  #region Highlight

  /// <summary>Color of the selection highlight and of highlighted markers.</summary>
  public UiRgba HighlightColor { get; init; } = new(255, 200, 0);

  #endregion

  #region View

  /// <summary>Smallest zoom factor.</summary>
  public float MinZoom { get; init; } = 0.2f;

  /// <summary>Largest zoom factor.</summary>
  public float MaxZoom { get; init; } = 4f;

  /// <summary>Largest zoom factor chosen by fit-to-view, so small graphs are not blown up.</summary>
  public float MaxFitZoom { get; init; } = 1f;

  /// <summary>Zoom change per mouse wheel notch.</summary>
  public float ZoomStep { get; init; } = 1.15f;

  /// <summary>Free space kept between the graph and the viewport edges, in screen pixels.</summary>
  public float ViewPadding { get; init; } = 24f;

  #endregion
}
