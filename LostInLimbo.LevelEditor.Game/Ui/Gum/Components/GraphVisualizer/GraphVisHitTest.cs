using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Kind of graph element under a point.
/// </summary>
public enum EnGraphVisHitKind
{
  /// <summary>Nothing (empty space).</summary>
  None,

  /// <summary>A node.</summary>
  Node,

  /// <summary>An edge.</summary>
  Edge,
}

/// <summary>
/// Graph element under a point, or the current selection.
/// </summary>
/// <param name="Kind">Kind of element.</param>
/// <param name="Index">Index of the node or edge in the graph; -1 for <see cref="EnGraphVisHitKind.None"/>.</param>
public readonly record struct GraphVisHit(EnGraphVisHitKind Kind, int Index)
{
  /// <summary>Nothing.</summary>
  public static GraphVisHit None { get; } = new(EnGraphVisHitKind.None, -1);

  /// <summary>
  /// Creates a hit on a node.
  /// </summary>
  /// <param name="index">Node index.</param>
  /// <returns>Hit.</returns>
  public static GraphVisHit ForNode(int index) => new(EnGraphVisHitKind.Node, index);

  /// <summary>
  /// Creates a hit on an edge.
  /// </summary>
  /// <param name="index">Edge index.</param>
  /// <returns>Hit.</returns>
  public static GraphVisHit ForEdge(int index) => new(EnGraphVisHitKind.Edge, index);
}

/// <summary>
/// Shape of an edge as drawn: the nodes it joins and how far it bends.
/// </summary>
/// <param name="From">Index of the start node.</param>
/// <param name="To">Index of the end node.</param>
/// <param name="Bend">Bend for <see cref="GraphVisGeometry.EdgeCurve"/> (0: straight).</param>
public readonly record struct GraphVisEdgeShape(int From, int To, float Bend);

/// <summary>
/// Finds the node or edge under a point, using the same shapes as the renderer.
/// </summary>
public static class GraphVisHitTest
{
  /// <summary>Number of straight pieces a curve is split into to measure the distance to it.</summary>
  private const int CurveSamples = 16;

  /// <summary>
  /// Finds the element under a point. Nodes win over edges, because they are drawn on top; of
  /// overlapping nodes the one drawn last (highest index) wins; of several edges the closest.
  /// </summary>
  /// <param name="point">Point, in world units.</param>
  /// <param name="positions">Node centers, in world units.</param>
  /// <param name="radii">Node radii, in node order.</param>
  /// <param name="edges">Edge shapes, in edge order.</param>
  /// <param name="edgeTolerance">Largest distance from an edge's line that still hits it, in world units.</param>
  /// <returns>Element under the point, or <see cref="GraphVisHit.None"/>.</returns>
  /// <exception cref="ArgumentNullException">A list is null.</exception>
  /// <exception cref="ArgumentException"><paramref name="positions"/> and <paramref name="radii"/> differ in length.</exception>
  /// <exception cref="ArgumentOutOfRangeException">An edge refers to a missing node, or the tolerance is negative.</exception>
  public static GraphVisHit Find(
    Vector2 point,
    IReadOnlyList<Vector2> positions,
    IReadOnlyList<float> radii,
    IReadOnlyList<GraphVisEdgeShape> edges,
    float edgeTolerance)
  {
    ArgumentNullException.ThrowIfNull(positions);
    ArgumentNullException.ThrowIfNull(radii);
    ArgumentNullException.ThrowIfNull(edges);
    if (positions.Count != radii.Count) throw new ArgumentException("Every node needs a position and a radius.", nameof(radii));
    if (!(edgeTolerance >= 0)) throw new ArgumentOutOfRangeException(nameof(edgeTolerance), edgeTolerance, "Tolerance must not be negative.");

    for (int i = positions.Count - 1; i >= 0; i--)
    {
      if (Vector2.Distance(point, positions[i]) <= radii[i]) return GraphVisHit.ForNode(i);
    }

    GraphVisHit best = GraphVisHit.None;
    float bestDistance = edgeTolerance;
    for (int i = 0; i < edges.Count; i++)
    {
      GraphVisEdgeShape edge = edges[i];
      if ((uint)edge.From >= (uint)positions.Count || (uint)edge.To >= (uint)positions.Count)
        throw new ArgumentOutOfRangeException(nameof(edges), $"Edge {i} refers to a missing node.");

      GraphVisCurve? curve = GraphVisGeometry.EdgeCurve(
        positions[edge.From], radii[edge.From], positions[edge.To], radii[edge.To], edge.Bend);
      if (curve is not { } shape) continue; // touching nodes: the edge is not drawn

      float distance = DistanceToCurve(point, shape);
      if (distance <= bestDistance)
      {
        best = GraphVisHit.ForEdge(i);
        bestDistance = distance;
      }
    }

    return best;
  }

  /// <summary>
  /// Measures the distance from a point to a curve (closely approximated by straight pieces).
  /// </summary>
  /// <param name="point">Point.</param>
  /// <param name="curve">Curve.</param>
  /// <returns>Shortest distance.</returns>
  public static float DistanceToCurve(Vector2 point, GraphVisCurve curve)
  {
    float shortest = float.PositiveInfinity;
    Vector2 previous = curve.Start;
    for (int i = 1; i <= CurveSamples; i++)
    {
      Vector2 next = curve.PointAt(i / (float)CurveSamples);
      shortest = Math.Min(shortest, DistanceToSegment(point, previous, next));
      previous = next;
    }

    return shortest;
  }

  /// <summary>
  /// Measures the distance from a point to a line segment.
  /// </summary>
  /// <param name="point">Point.</param>
  /// <param name="start">Segment start.</param>
  /// <param name="end">Segment end.</param>
  /// <returns>Shortest distance.</returns>
  private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
  {
    Vector2 along = end - start;
    float lengthSquared = along.LengthSquared();
    if (lengthSquared == 0) return Vector2.Distance(point, start);

    float t = Math.Clamp(Vector2.Dot(point - start, along) / lengthSquared, 0f, 1f);
    return Vector2.Distance(point, start + along * t);
  }
}
