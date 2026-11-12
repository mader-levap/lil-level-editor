using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Straight line between two points.
/// </summary>
/// <param name="Start">Start point.</param>
/// <param name="End">End point.</param>
public readonly record struct GraphVisSegment(Vector2 Start, Vector2 End)
{
  /// <summary>Point halfway between start and end.</summary>
  public Vector2 Middle => (Start + End) / 2f;

  /// <summary>Length of the segment.</summary>
  public float Length => Vector2.Distance(Start, End);
}

/// <summary>
/// Triangular arrowhead.
/// </summary>
/// <param name="Tip">Point of the arrow.</param>
/// <param name="BaseLeft">One corner of the base.</param>
/// <param name="BaseRight">The other corner of the base.</param>
public readonly record struct GraphVisArrowHead(Vector2 Tip, Vector2 BaseLeft, Vector2 BaseRight);

/// <summary>
/// One dot drawn inside a node: either a single marker or several collapsed into one.
/// </summary>
/// <param name="Label">Text above the dot: the marker's label, or the marker count when collapsed.</param>
/// <param name="MarkerIds">Identifiers of the markers this dot stands for.</param>
/// <param name="IsCollapsed">Whether the dot stands for several markers.</param>
public sealed record GraphVisMarkerDot(string Label, IReadOnlyList<string> MarkerIds, bool IsCollapsed);

/// <summary>
/// Pure geometry of the graph visualizer: node radii, edge lines between node borders,
/// arrowheads and marker placement. Works in any consistent unit (world units or pixels).
/// </summary>
public static class GraphVisGeometry
{
  /// <summary>
  /// Computes a node's radius. The circle area (not the radius) grows with the relative size, so
  /// a node four times as big looks four times as big.
  /// </summary>
  /// <param name="relativeSize">Node size relative to other nodes (positive).</param>
  /// <param name="style">Style holding the base, minimum and maximum radius.</param>
  /// <returns>Radius between the style's minimum and maximum.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="style"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="relativeSize"/> is not positive and finite.</exception>
  public static float NodeRadius(float relativeSize, GraphVisStyle style)
  {
    ArgumentNullException.ThrowIfNull(style);
    if (!float.IsFinite(relativeSize) || relativeSize <= 0)
      throw new ArgumentOutOfRangeException(nameof(relativeSize), relativeSize, "Relative size must be positive and finite.");

    return Math.Clamp(style.NodeBaseRadius * MathF.Sqrt(relativeSize), style.NodeMinRadius, style.NodeMaxRadius);
  }

  /// <summary>
  /// Computes the visible part of an edge: the line between the borders of two node circles.
  /// </summary>
  /// <param name="fromCenter">Center of the node the edge starts at.</param>
  /// <param name="fromRadius">Radius of that node.</param>
  /// <param name="toCenter">Center of the node the edge ends at.</param>
  /// <param name="toRadius">Radius of that node.</param>
  /// <returns>Border-to-border segment, or null when the circles touch or overlap (nothing to draw).</returns>
  public static GraphVisSegment? EdgeSegment(Vector2 fromCenter, float fromRadius, Vector2 toCenter, float toRadius)
  {
    float distance = Vector2.Distance(fromCenter, toCenter);
    if (distance <= fromRadius + toRadius) return null;

    Vector2 direction = (toCenter - fromCenter) / distance;
    return new GraphVisSegment(fromCenter + direction * fromRadius, toCenter - direction * toRadius);
  }

  /// <summary>
  /// Shortens an edge line so it ends at the bases of its arrowheads instead of poking through
  /// their tips.
  /// </summary>
  /// <param name="segment">Border-to-border segment.</param>
  /// <param name="arrowAtStart">Whether an arrowhead sits at the start.</param>
  /// <param name="arrowAtEnd">Whether an arrowhead sits at the end.</param>
  /// <param name="arrowLength">Arrowhead length.</param>
  /// <returns>Line to draw, or null when the arrowheads leave no room for a line.</returns>
  public static GraphVisSegment? LineBetweenArrows(GraphVisSegment segment, bool arrowAtStart, bool arrowAtEnd, float arrowLength)
  {
    float length = segment.Length;
    float trimmed = (arrowAtStart ? arrowLength : 0) + (arrowAtEnd ? arrowLength : 0);
    if (length <= trimmed) return null;

    Vector2 direction = (segment.End - segment.Start) / length;
    return new GraphVisSegment(
      arrowAtStart ? segment.Start + direction * arrowLength : segment.Start,
      arrowAtEnd ? segment.End - direction * arrowLength : segment.End);
  }

  /// <summary>
  /// Computes an arrowhead.
  /// </summary>
  /// <param name="tip">Point of the arrow.</param>
  /// <param name="from">Any point the arrow comes from (gives the direction); must differ from <paramref name="tip"/>.</param>
  /// <param name="length">Arrowhead length, from tip to base.</param>
  /// <param name="halfWidth">Half of the base width.</param>
  /// <returns>Arrowhead triangle, or null when <paramref name="from"/> equals <paramref name="tip"/>.</returns>
  public static GraphVisArrowHead? ArrowHead(Vector2 tip, Vector2 from, float length, float halfWidth)
  {
    float distance = Vector2.Distance(from, tip);
    if (distance <= 0) return null;

    Vector2 direction = (tip - from) / distance;
    var normal = new Vector2(-direction.Y, direction.X);
    Vector2 baseCenter = tip - direction * length;
    return new GraphVisArrowHead(tip, baseCenter + normal * halfWidth, baseCenter - normal * halfWidth);
  }

  /// <summary>
  /// Decides which dots a node shows for its markers: one per marker, or a single dot labelled
  /// with the count when there are more than <paramref name="maxShown"/>.
  /// </summary>
  /// <param name="markers">Markers of the node.</param>
  /// <param name="maxShown">Most markers shown individually (at least 1).</param>
  /// <returns>Dots in display order; empty when the node has no markers.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="markers"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxShown"/> is below 1.</exception>
  public static IReadOnlyList<GraphVisMarkerDot> MarkerDots(IReadOnlyList<GraphVisMarker> markers, int maxShown)
  {
    ArgumentNullException.ThrowIfNull(markers);
    ArgumentOutOfRangeException.ThrowIfLessThan(maxShown, 1);

    if (markers.Count == 0) return [];
    if (markers.Count > maxShown)
    {
      string count = markers.Count.ToString(CultureInfo.InvariantCulture);
      return [new GraphVisMarkerDot(count, markers.Select(marker => marker.Id).ToArray(), IsCollapsed: true)];
    }

    return markers.Select(marker => new GraphVisMarkerDot(marker.Label, [marker.Id], IsCollapsed: false)).ToArray();
  }

  /// <summary>
  /// Places marker dots in a horizontal row centered on a node, far enough apart that their
  /// labels (centered above each dot) do not run into each other.
  /// </summary>
  /// <param name="labelWidths">Measured width of each dot's label, in display order.</param>
  /// <param name="minSpacing">Smallest distance between two neighbouring dot centers.</param>
  /// <param name="labelGap">Free space kept between two neighbouring labels.</param>
  /// <returns>Horizontal offset of each dot from the node center; the row is centered on zero.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="labelWidths"/> is null.</exception>
  public static IReadOnlyList<float> MarkerOffsets(IReadOnlyList<float> labelWidths, float minSpacing, float labelGap)
  {
    ArgumentNullException.ThrowIfNull(labelWidths);
    if (labelWidths.Count == 0) return [];

    // Lay the dots out left to right from zero, then shift so the row is centered.
    var offsets = new float[labelWidths.Count];
    for (int i = 1; i < offsets.Length; i++)
    {
      float labelsApart = (labelWidths[i - 1] + labelWidths[i]) / 2f + labelGap;
      offsets[i] = offsets[i - 1] + Math.Max(minSpacing, labelsApart);
    }

    float shift = offsets[^1] / 2f;
    for (int i = 0; i < offsets.Length; i++) offsets[i] -= shift;
    return offsets;
  }
}
