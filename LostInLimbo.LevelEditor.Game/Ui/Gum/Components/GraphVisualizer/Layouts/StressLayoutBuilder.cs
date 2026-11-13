using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Starting placement of graph nodes by stress layout: positions are chosen so that the distance
/// between any two nodes on screen matches their distance through the graph. Connected parts
/// come out untangled; chains look like chains and loops like loops.
/// </summary>
/// <remarks>
/// Deterministic (no randomness): the same graph always gives the same positions. The live
/// <see cref="GraphVisSimulation"/> takes over from these positions.
/// </remarks>
public sealed class StressLayoutBuilder : GraphLayoutBuilder
{
  #region Constants

  /// <summary>Most refinement rounds per connected part.</summary>
  private const int MaxIterations = 300;

  /// <summary>Refinement stops once no node moved farther than this in a round.</summary>
  private const float StopDistance = 0.05f;

  /// <summary>
  /// Radius step of the small spiral that separates nodes in the starting guess, relative to the
  /// link gap. Nodes with identical graph distances would otherwise start on the same point and
  /// never separate.
  /// </summary>
  private const float SeedSpreadFactor = 0.05f;

  /// <summary>Golden angle, in radians: successive spiral points never line up.</summary>
  private const float GoldenAngle = 2.39996323f;

  #endregion

  /// <summary>
  /// Places the nodes of a graph. <paramref name="linkGap"/> is also the space between separate
  /// parts of the graph, which are placed side by side, largest first.
  /// </summary>
  /// <param name="radii">Node radii, in node order (validated).</param>
  /// <param name="edges">Edges as pairs of node indices (validated).</param>
  /// <param name="linkGap">Free space aimed for between connected nodes (validated).</param>
  /// <param name="roots">Ignored by this layout.</param>
  /// <returns>Node centers, in node order.</returns>
  protected override Vector2[] PlaceCore(
    IReadOnlyList<float> radii, IReadOnlyList<(int From, int To)> edges, float linkGap, IReadOnlyList<int> roots)
  {
    int count = radii.Count;
    var neighbours = new List<int>[count];
    for (int i = 0; i < count; i++) neighbours[i] = [];
    foreach ((int from, int to) in edges)
    {
      if (from == to) continue; // a self-loop does not affect placement
      neighbours[from].Add(to);
      neighbours[to].Add(from);
    }

    var positions = new Vector2[count];
    float cursorX = 0;
    foreach (int[] part in FindParts(neighbours))
    {
      Vector2[] local = PlacePart(part, radii, neighbours, linkGap);

      // Pack parts left to right, each centered on the horizontal axis.
      GraphVisBounds bounds = GraphVisBounds.AroundCircles(local.Select((position, index) => (position, radii[part[index]])))
        ?? new GraphVisBounds(Vector2.Zero, Vector2.Zero);
      var shift = new Vector2(cursorX - bounds.Min.X, -bounds.Center.Y);
      for (int i = 0; i < part.Length; i++) positions[part[i]] = local[i] + shift;
      cursorX += bounds.Size.X + linkGap;
    }

    return positions;
  }

  #region Connected parts

  /// <summary>
  /// Splits the graph into connected parts.
  /// </summary>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <returns>Parts as node indices in breadth-first order; largest part first, ties by lowest node index.</returns>
  private static List<int[]> FindParts(List<int>[] neighbours)
  {
    var parts = new List<int[]>();
    var visited = new bool[neighbours.Length];
    for (int start = 0; start < neighbours.Length; start++)
    {
      if (visited[start]) continue;

      var part = new List<int> { start };
      visited[start] = true;
      for (int head = 0; head < part.Count; head++)
      {
        foreach (int next in neighbours[part[head]])
        {
          if (visited[next]) continue;
          visited[next] = true;
          part.Add(next);
        }
      }

      parts.Add([.. part]);
    }

    // Parts were found in order of their lowest node index; a stable sort keeps that for ties.
    return [.. parts.OrderByDescending(part => part.Length)];
  }

  #endregion

  #region One part

  /// <summary>
  /// Places one connected part around its own origin.
  /// </summary>
  /// <param name="part">Node indices of the part.</param>
  /// <param name="radii">Radii of all nodes.</param>
  /// <param name="neighbours">Neighbours of all nodes.</param>
  /// <param name="linkGap">Free space aimed for between connected nodes.</param>
  /// <returns>Positions, in the order of <paramref name="part"/>.</returns>
  private static Vector2[] PlacePart(int[] part, IReadOnlyList<float> radii, List<int>[] neighbours, float linkGap)
  {
    int count = part.Length;
    if (count == 1) return [Vector2.Zero];

    float[,] ideal = IdealDistances(part, radii, neighbours, linkGap);
    Vector2[] positions = Seed(ideal, count, linkGap);
    Refine(positions, ideal, count);
    MakeLandscape(positions);
    return positions;
  }

  /// <summary>
  /// Computes the distance every pair of nodes should have: the shortest way through the graph,
  /// where a link between two nodes is as long as both radii plus the link gap. Larger nodes
  /// therefore get more room.
  /// </summary>
  /// <param name="part">Node indices of the part.</param>
  /// <param name="radii">Radii of all nodes.</param>
  /// <param name="neighbours">Neighbours of all nodes.</param>
  /// <param name="linkGap">Free space aimed for between connected nodes.</param>
  /// <returns>Ideal distance for every pair, indexed by position in <paramref name="part"/>.</returns>
  private static float[,] IdealDistances(int[] part, IReadOnlyList<float> radii, List<int>[] neighbours, float linkGap)
  {
    int count = part.Length;
    var localIndex = new Dictionary<int, int>(count);
    for (int i = 0; i < count; i++) localIndex[part[i]] = i;

    var distance = new float[count, count];
    for (int i = 0; i < count; i++)
    {
      for (int j = 0; j < count; j++) distance[i, j] = i == j ? 0 : float.PositiveInfinity;
    }

    for (int i = 0; i < count; i++)
    {
      foreach (int neighbour in neighbours[part[i]])
      {
        distance[i, localIndex[neighbour]] = radii[part[i]] + radii[neighbour] + linkGap;
      }
    }

    // Floyd-Warshall: fine for the few hundred nodes this visualizer is meant for.
    for (int via = 0; via < count; via++)
    {
      for (int i = 0; i < count; i++)
      {
        float toVia = distance[i, via];
        if (float.IsPositiveInfinity(toVia)) continue;
        for (int j = 0; j < count; j++)
        {
          float through = toVia + distance[via, j];
          if (through < distance[i, j]) distance[i, j] = through;
        }
      }
    }

    return distance;
  }

  /// <summary>
  /// Builds the starting guess: each node's coordinates are its graph distances to two nodes at
  /// opposite ends of the part. This already unfolds the graph, so refinement does not have to
  /// untie knots.
  /// </summary>
  /// <param name="ideal">Ideal distances.</param>
  /// <param name="count">Number of nodes.</param>
  /// <param name="linkGap">Free space aimed for between connected nodes.</param>
  /// <returns>Starting positions.</returns>
  private static Vector2[] Seed(float[,] ideal, int count, float linkGap)
  {
    // Two far-apart reference nodes: the node farthest from the first one, and the node farthest from that.
    int first = Farthest(ideal, count, 0);
    int second = Farthest(ideal, count, first);

    var positions = new Vector2[count];
    for (int i = 0; i < count; i++)
    {
      // Small spiral offset: separates nodes that have the same distances to both references.
      float angle = i * GoldenAngle;
      float spread = linkGap * SeedSpreadFactor * MathF.Sqrt(i + 1);
      positions[i] = new Vector2(ideal[first, i], ideal[second, i]) + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * spread;
    }

    return positions;
  }

  /// <summary>
  /// Finds the node farthest from another through the graph.
  /// </summary>
  /// <param name="ideal">Ideal distances.</param>
  /// <param name="count">Number of nodes.</param>
  /// <param name="from">Node to measure from.</param>
  /// <returns>Farthest node; the lowest index among equally far ones.</returns>
  private static int Farthest(float[,] ideal, int count, int from)
  {
    int farthest = from;
    for (int i = 0; i < count; i++)
    {
      if (ideal[from, i] > ideal[from, farthest]) farthest = i;
    }

    return farthest;
  }

  /// <summary>
  /// Refines positions by stress majorization: each node in turn moves to the spot where, on
  /// average, every other node sees it at the ideal distance. Near pairs count more than far
  /// pairs (weight 1 / distance squared).
  /// </summary>
  /// <param name="positions">Positions, refined in place.</param>
  /// <param name="ideal">Ideal distances.</param>
  /// <param name="count">Number of nodes.</param>
  private static void Refine(Vector2[] positions, float[,] ideal, int count)
  {
    for (int iteration = 0; iteration < MaxIterations; iteration++)
    {
      float largestMove = 0;
      for (int i = 0; i < count; i++)
      {
        Vector2 weightedSum = Vector2.Zero;
        float weightSum = 0;
        for (int j = 0; j < count; j++)
        {
          if (j == i) continue;

          float wanted = ideal[i, j];
          float weight = 1f / (wanted * wanted);
          Vector2 offset = positions[i] - positions[j];
          float length = offset.Length();

          // Where node j would like node i to be: at the ideal distance, in the current direction.
          Vector2 target = length > 0 ? positions[j] + offset * (wanted / length) : positions[j];
          weightedSum += target * weight;
          weightSum += weight;
        }

        Vector2 moved = weightedSum / weightSum;
        largestMove = Math.Max(largestMove, Vector2.Distance(moved, positions[i]));
        positions[i] = moved;
      }

      if (largestMove < StopDistance) break;
    }
  }

  /// <summary>
  /// Rotates a part so its longest extent runs horizontally (the drawing area is wider than
  /// tall) and centers it on the origin.
  /// </summary>
  /// <param name="positions">Positions, rotated in place.</param>
  private static void MakeLandscape(Vector2[] positions)
  {
    Vector2 center = Vector2.Zero;
    foreach (Vector2 position in positions) center += position;
    center /= positions.Length;

    // Covariance of the point cloud; its main axis is the direction of largest spread.
    float xx = 0, xy = 0, yy = 0;
    foreach (Vector2 position in positions)
    {
      Vector2 offset = position - center;
      xx += offset.X * offset.X;
      xy += offset.X * offset.Y;
      yy += offset.Y * offset.Y;
    }

    float mainAxisAngle = 0.5f * MathF.Atan2(2 * xy, xx - yy);
    float cos = MathF.Cos(-mainAxisAngle);
    float sin = MathF.Sin(-mainAxisAngle);
    for (int i = 0; i < positions.Length; i++)
    {
      Vector2 offset = positions[i] - center;
      positions[i] = new Vector2(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos);
    }
  }

  #endregion
}
