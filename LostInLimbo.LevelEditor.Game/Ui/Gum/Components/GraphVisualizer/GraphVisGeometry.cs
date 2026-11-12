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
/// Curved line between two points: a quadratic Bézier curve pulled towards a control point.
/// </summary>
/// <param name="Start">Start point.</param>
/// <param name="Control">Control point; the curve leaves the start towards it and arrives at the end from it.</param>
/// <param name="End">End point.</param>
public readonly record struct GraphVisCurve(Vector2 Start, Vector2 Control, Vector2 End)
{
  /// <summary>Point halfway along the curve (parameter 0.5).</summary>
  public Vector2 Middle => PointAt(0.5f);

  /// <summary>
  /// Computes a point on the curve.
  /// </summary>
  /// <param name="t">Curve parameter: 0 at the start, 1 at the end.</param>
  /// <returns>Point on the curve.</returns>
  public Vector2 PointAt(float t)
  {
    float rest = 1f - t;
    return Start * (rest * rest) + Control * (2f * rest * t) + End * (t * t);
  }

  /// <summary>Unit direction in which the curve leaves the start.</summary>
  public Vector2 StartTangent => Direction(Control - Start);

  /// <summary>Unit direction in which the curve arrives at the end.</summary>
  public Vector2 EndTangent => Direction(End - Control);

  /// <summary>
  /// Normalizes a tangent; when the control point sits on an end (zero tangent) the curve is a
  /// straight line there, so the chord direction is used, and the X axis if the curve is a point.
  /// </summary>
  /// <param name="tangent">Unnormalized tangent.</param>
  /// <returns>Unit vector.</returns>
  private Vector2 Direction(Vector2 tangent)
  {
    if (tangent.LengthSquared() == 0) tangent = End - Start;
    if (tangent.LengthSquared() == 0) return Vector2.UnitX;
    tangent.Normalize();
    return tangent;
  }
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
/// Sizes that decide how marker dots and their labels are arranged inside a node.
/// </summary>
/// <param name="DotRadius">Radius of a marker dot.</param>
/// <param name="LabelRise">Distance from a dot's center up to the baseline of its label.</param>
/// <param name="LabelHeight">Height of a label above its baseline (cap height of the font).</param>
/// <param name="RowGap">Free space between two rows of markers.</param>
/// <param name="MinSpacing">Smallest distance between the centers of two neighbouring dots in a row.</param>
/// <param name="LabelGap">Free space kept between two neighbouring labels in a row.</param>
public readonly record struct GraphVisMarkerMetrics(
  float DotRadius, float LabelRise, float LabelHeight, float RowGap, float MinSpacing, float LabelGap);

/// <summary>
/// Pure geometry of the graph visualizer: node radii, edge lines between node borders,
/// arrowheads and marker placement. Works in any consistent unit (world units or pixels).
/// </summary>
public static class GraphVisGeometry
{
  /// <summary>
  /// Most markers for which every split into rows is tried; more are laid out in one row. The
  /// number of splits doubles with every marker, and nodes show only a few markers anyway.
  /// </summary>
  private const int MaxMarkersForRowSearch = 8;

  /// <summary>Scores closer than this count as equal, so the layout with fewer rows wins.</summary>
  private const float MarkerScoreEpsilon = 0.01f;

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
  /// Decides how far each edge bends away from the straight line, so several edges between the
  /// same two nodes do not lie on top of each other. A lone edge stays straight.
  /// </summary>
  /// <param name="edges">Edges as node index pairs.</param>
  /// <param name="spacing">Distance between the peaks of two neighbouring edges of one pair.</param>
  /// <returns>
  /// Bend of each edge, in edge order, for <see cref="EdgeCurve"/>: positive bends to the left of
  /// the edge's own direction. A pair with n edges gets bends of (k − (n − 1) / 2) × spacing,
  /// k = 0..n−1 in edge order, all measured on the same side of the pair; for
  /// an edge stored the other way round the sign is flipped, so it still lands on its own side.
  /// </returns>
  /// <exception cref="ArgumentNullException"><paramref name="edges"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="spacing"/> is negative or not finite.</exception>
  public static float[] ParallelEdgeBends(IReadOnlyList<(int From, int To)> edges, float spacing)
  {
    ArgumentNullException.ThrowIfNull(edges);
    if (!float.IsFinite(spacing) || spacing < 0)
      throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "Spacing must be finite and not negative.");

    // Edges of each unordered pair, in edge order.
    var edgesByPair = new Dictionary<(int Low, int High), List<int>>();
    for (int i = 0; i < edges.Count; i++)
    {
      (int from, int to) = edges[i];
      var pair = (Math.Min(from, to), Math.Max(from, to));
      if (!edgesByPair.TryGetValue(pair, out List<int>? group))
      {
        group = [];
        edgesByPair[pair] = group;
      }
      group.Add(i);
    }

    var bends = new float[edges.Count];
    foreach (List<int> group in edgesByPair.Values)
    {
      if (group.Count < 2) continue; // lone edge: straight
      for (int k = 0; k < group.Count; k++)
      {
        int edge = group[k];
        float bend = (k - (group.Count - 1) / 2f) * spacing; // relative to the low-to-high direction
        bends[edge] = edges[edge].From <= edges[edge].To ? bend : -bend;
      }
    }

    return bends;
  }

  /// <summary>
  /// Computes the visible part of an edge drawn as a curve between the borders of two node circles.
  /// </summary>
  /// <param name="fromCenter">Center of the node the edge starts at.</param>
  /// <param name="fromRadius">Radius of that node.</param>
  /// <param name="toCenter">Center of the node the edge ends at.</param>
  /// <param name="toRadius">Radius of that node.</param>
  /// <param name="bend">
  /// How far the curve bulges from the straight line between the centers, to the left of the
  /// edge's direction (negative: to the right); 0 gives the same line as <see cref="EdgeSegment"/>.
  /// The middle of the curve lies at least this far from the line: exactly for small nodes, a
  /// little further for nodes large compared with their distance, because the curve's ends move
  /// sideways along the circle borders towards the bulge.
  /// </param>
  /// <returns>Curve from border to border, or null when the circles touch or overlap (nothing to draw).</returns>
  public static GraphVisCurve? EdgeCurve(Vector2 fromCenter, float fromRadius, Vector2 toCenter, float toRadius, float bend)
  {
    if (EdgeSegment(fromCenter, fromRadius, toCenter, toRadius) is not { } straight) return null;

    // The curve's middle lies halfway between the straight line's middle and the control point,
    // so the control point sits twice the bend away for the middle to be "bend" away. Using the
    // border-to-border middle keeps an unbent curve identical to the straight edge.
    Vector2 direction = Vector2.Normalize(toCenter - fromCenter);
    var left = new Vector2(-direction.Y, direction.X);
    Vector2 control = straight.Middle + left * (2f * bend);

    // Leave and enter each circle where the curve crosses its border: towards the control point.
    Vector2 start = fromCenter + Vector2.Normalize(control - fromCenter) * fromRadius;
    Vector2 end = toCenter + Vector2.Normalize(control - toCenter) * toRadius;
    return new GraphVisCurve(start, control, end);
  }

  /// <summary>
  /// Shortens a curved edge so it ends at the bases of its arrowheads instead of poking through
  /// their tips. The ends move back along their tangents; the control point stays.
  /// </summary>
  /// <param name="curve">Border-to-border curve.</param>
  /// <param name="arrowAtStart">Whether an arrowhead sits at the start.</param>
  /// <param name="arrowAtEnd">Whether an arrowhead sits at the end.</param>
  /// <param name="arrowLength">Arrowhead length.</param>
  /// <returns>Curve to draw, or null when the arrowheads leave no room for a line.</returns>
  public static GraphVisCurve? CurveBetweenArrows(GraphVisCurve curve, bool arrowAtStart, bool arrowAtEnd, float arrowLength)
  {
    float trimmed = (arrowAtStart ? arrowLength : 0) + (arrowAtEnd ? arrowLength : 0);
    if (Vector2.Distance(curve.Start, curve.End) <= trimmed) return null;

    return curve with
    {
      Start = arrowAtStart ? curve.Start + curve.StartTangent * arrowLength : curve.Start,
      End = arrowAtEnd ? curve.End - curve.EndTangent * arrowLength : curve.End,
    };
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

  /// <summary>
  /// Arranges the marker dots of a node so that dots and labels stay inside the node as far as
  /// possible, without overlapping each other.
  /// </summary>
  /// <param name="labelWidths">Measured width of each dot's label, in display order.</param>
  /// <param name="availableRadius">Radius of the area markers should stay inside (node radius minus its outline).</param>
  /// <param name="metrics">Sizes of dots, labels and gaps.</param>
  /// <returns>Center of each dot relative to the node center, in display order; empty without markers.</returns>
  /// <remarks>
  /// <para>Every split of the markers, in their order, into consecutive rows is tried: one row,
  /// two rows (2 + 1 or 1 + 2 for three markers), one marker per row, and so on. Within a row the
  /// dots are spaced by <see cref="MarkerOffsets"/> and centered; rows are stacked so a label never
  /// reaches the dots of the row above, and the whole block is centered vertically.</para>
  /// <para>Each layout is scored by how far its label rectangles and dots stick out past
  /// <paramref name="availableRadius"/>; the lowest score wins, and among equal scores the layout
  /// with fewer rows. A node whose markers fit in one row therefore keeps that row.</para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"><paramref name="labelWidths"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="availableRadius"/> is not positive and finite.</exception>
  public static IReadOnlyList<Vector2> MarkerPositions(
    IReadOnlyList<float> labelWidths, float availableRadius, GraphVisMarkerMetrics metrics)
  {
    ArgumentNullException.ThrowIfNull(labelWidths);
    if (!float.IsFinite(availableRadius) || availableRadius <= 0)
      throw new ArgumentOutOfRangeException(nameof(availableRadius), availableRadius, "Available radius must be positive and finite.");
    if (labelWidths.Count == 0) return [];

    Vector2[] best = []; // replaced by the first split, whose score is always finite
    float bestScore = float.PositiveInfinity;
    foreach (int[] rowSizes in RowSplits(labelWidths.Count))
    {
      Vector2[] positions = LayOutRows(labelWidths, rowSizes, metrics);
      float score = MarkerOverflow(labelWidths, positions, availableRadius, metrics);

      // Splits come with fewer rows first, so only a clearly better score replaces the best.
      if (score < bestScore - MarkerScoreEpsilon)
      {
        best = positions;
        bestScore = score;
      }
    }

    return best;
  }

  /// <summary>
  /// Lists the ways to split markers, in their order, into consecutive rows.
  /// </summary>
  /// <param name="count">Number of markers (at least 1).</param>
  /// <returns>Row sizes of each split, ordered by number of rows (one row first).</returns>
  private static IEnumerable<int[]> RowSplits(int count)
  {
    if (count > MaxMarkersForRowSearch) return [[count]];

    // Each of the count - 1 gaps between neighbouring markers either starts a new row or not.
    var splits = new List<int[]>();
    int gaps = count - 1;
    for (int mask = 0; mask < 1 << gaps; mask++)
    {
      var sizes = new List<int>();
      int size = 1;
      for (int gap = 0; gap < gaps; gap++)
      {
        if ((mask & (1 << gap)) != 0)
        {
          sizes.Add(size);
          size = 1;
        }
        else
        {
          size++;
        }
      }
      sizes.Add(size);
      splits.Add([.. sizes]);
    }

    // Stable sort keeps the order within the same row count deterministic.
    return splits.OrderBy(split => split.Length);
  }

  /// <summary>
  /// Places markers in rows of the given sizes: rows centered horizontally, block centered vertically.
  /// </summary>
  /// <param name="labelWidths">Label widths, in display order.</param>
  /// <param name="rowSizes">Number of markers in each row, top to bottom.</param>
  /// <param name="metrics">Sizes of dots, labels and gaps.</param>
  /// <returns>Dot centers relative to the node center.</returns>
  private static Vector2[] LayOutRows(IReadOnlyList<float> labelWidths, int[] rowSizes, GraphVisMarkerMetrics metrics)
  {
    // One row spans from the top of its labels to the bottom of its dots.
    float rowHeight = metrics.LabelHeight + metrics.LabelRise + metrics.DotRadius;
    float blockHeight = rowSizes.Length * rowHeight + (rowSizes.Length - 1) * metrics.RowGap;
    float rowTop = -blockHeight / 2f;

    var positions = new Vector2[labelWidths.Count];
    int first = 0;
    foreach (int rowSize in rowSizes)
    {
      float dotY = rowTop + metrics.LabelHeight + metrics.LabelRise;
      float[] rowWidths = Enumerable.Range(first, rowSize).Select(i => labelWidths[i]).ToArray();
      IReadOnlyList<float> offsets = MarkerOffsets(rowWidths, metrics.MinSpacing, metrics.LabelGap);
      for (int i = 0; i < rowSize; i++) positions[first + i] = new Vector2(offsets[i], dotY);

      first += rowSize;
      rowTop += rowHeight + metrics.RowGap;
    }

    return positions;
  }

  /// <summary>
  /// Measures how far markers stick out of a circle: for every marker, how far each corner of its
  /// label rectangle and the outer edge of its dot lie beyond the radius, summed up. A rectangle
  /// whose corners are all inside a circle is entirely inside, so 0 means everything fits.
  /// </summary>
  /// <param name="labelWidths">Label widths, in display order.</param>
  /// <param name="positions">Dot centers relative to the node center.</param>
  /// <param name="radius">Radius markers should stay inside.</param>
  /// <param name="metrics">Sizes of dots and labels.</param>
  /// <returns>Total overflow (0 when everything is inside).</returns>
  private static float MarkerOverflow(
    IReadOnlyList<float> labelWidths, IReadOnlyList<Vector2> positions, float radius, GraphVisMarkerMetrics metrics)
  {
    float overflow = 0;
    for (int i = 0; i < positions.Count; i++)
    {
      Vector2 dot = positions[i];
      float halfWidth = labelWidths[i] / 2f;
      float baseline = dot.Y - metrics.LabelRise;
      float top = baseline - metrics.LabelHeight;

      overflow += Beyond(new Vector2(dot.X - halfWidth, top), radius);
      overflow += Beyond(new Vector2(dot.X + halfWidth, top), radius);
      overflow += Beyond(new Vector2(dot.X - halfWidth, baseline), radius);
      overflow += Beyond(new Vector2(dot.X + halfWidth, baseline), radius);
      overflow += Math.Max(0f, dot.Length() + metrics.DotRadius - radius);
    }

    return overflow;

    static float Beyond(Vector2 point, float limit) => Math.Max(0f, point.Length() - limit);
  }
}
