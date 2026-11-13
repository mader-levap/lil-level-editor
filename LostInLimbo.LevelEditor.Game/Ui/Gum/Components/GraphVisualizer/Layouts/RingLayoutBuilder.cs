using System;
using System.Collections.Generic;
using System.Linq;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Starting placement of graph nodes on a ring, starting at the top and going clockwise. Nodes
/// follow a depth-first walk of the graph, so connected nodes sit next to each other and few
/// edges cross the ring. Each node gets a share of the ring matching its size, and the ring is
/// large enough that no two nodes come closer than the link gap.
/// </summary>
/// <remarks>
/// <para>Ring order: the walk starts at the lowest node index and walks dead ends and short
/// side branches before the main path goes on (neighbours by fewest links first, then by
/// index), so a branch sits right beside the node it hangs off. Each separate part of the graph
/// takes one unbroken stretch of the ring, in order of its lowest node index. Without edges the
/// ring order is the node order.</para>
/// <para>Deterministic: the same input always gives the same positions.</para>
/// </remarks>
public sealed class RingLayoutBuilder : GraphLayoutBuilder
{
  /// <summary>How much the ring grows per attempt while neighbouring nodes still overlap.</summary>
  private const float RingGrowthFactor = 1.1f;

  /// <summary>Upper bound of ring growth attempts (1.1^64 is far beyond any real need).</summary>
  private const int MaxRingGrowthSteps = 64;

  /// <summary>
  /// Default physics without edge pull: edges crossing the ring are long and would pull it into a
  /// clump. Nodes still push each other apart and never overlap.
  /// </summary>
  private static readonly GraphVisPhysics RingPhysics = GraphVisPhysics.Default with { AttractStrength = 0f };

  /// <summary>Physics without edge pull, so the ring keeps its shape (see <see cref="RingPhysics"/>).</summary>
  public override GraphVisPhysics DefaultPhysics => RingPhysics;

  /// <summary>
  /// Places the nodes on the ring.
  /// </summary>
  /// <param name="radii">Node radii, in node order (validated).</param>
  /// <param name="edges">Edges (validated); they decide the order of the nodes around the ring.</param>
  /// <param name="linkGap">Free space kept between neighbouring nodes (validated).</param>
  /// <param name="roots">Ignored by this layout.</param>
  /// <returns>Node centers, in node order; the ring is centered on the origin.</returns>
  protected override Vector2[] PlaceCore(
    IReadOnlyList<float> radii, IReadOnlyList<(int From, int To)> edges, float linkGap, IReadOnlyList<int> roots)
  {
    int count = radii.Count;
    var positions = new Vector2[count];
    if (count <= 1) return positions; // nothing, or a single node at the origin

    int[] order = RingOrder(count, edges);

    // Each node takes an arc as long as its diameter plus the gap.
    float circumference = 0;
    for (int i = 0; i < count; i++) circumference += 2 * radii[i] + linkGap;

    var angles = new float[count];
    float travelled = 0;
    foreach (int node in order)
    {
      float arc = 2 * radii[node] + linkGap;
      // Middle of the node's arc; -90 degrees puts the first node at the top (screen Y grows down).
      angles[node] = (travelled + arc / 2) / circumference * MathUtil.TwoPi - MathUtil.PiOverTwo;
      travelled += arc;
    }

    // A chord is shorter than its arc, so with few nodes the arc-based ring is too tight; grow it
    // until every pair of ring neighbours has its gap.
    float ringRadius = circumference / MathUtil.TwoPi;
    for (int step = 0; step < MaxRingGrowthSteps; step++)
    {
      WriteRingPositions(positions, angles, ringRadius);
      if (RingNeighboursHaveGap(positions, order, radii, linkGap)) break;
      ringRadius *= RingGrowthFactor;
    }

    return positions;
  }

  #region Ring order

  /// <summary>
  /// Orders the nodes around the ring by a depth-first walk (see the class remarks).
  /// </summary>
  /// <param name="count">Number of nodes.</param>
  /// <param name="edges">Edges as pairs of existing node indices.</param>
  /// <returns>Every node index exactly once, in ring order.</returns>
  private static int[] RingOrder(int count, IReadOnlyList<(int From, int To)> edges)
  {
    int[][] neighbours = SortedNeighbours(count, edges);

    var order = new List<int>(count);
    var visited = new bool[count];

    // Stack of (node, position of the next neighbour to try): an iterative walk, so long chains
    // cannot overflow the call stack.
    var stack = new Stack<(int Node, int Next)>();
    for (int start = 0; start < count; start++)
    {
      if (visited[start]) continue; // already walked as part of an earlier part

      visited[start] = true;
      order.Add(start);
      stack.Push((start, 0));
      while (stack.Count > 0)
      {
        (int node, int next) = stack.Pop();
        if (next >= neighbours[node].Length) continue; // all neighbours done: back up

        stack.Push((node, next + 1)); // come back for the remaining neighbours later
        int neighbour = neighbours[node][next];
        if (visited[neighbour]) continue;

        visited[neighbour] = true;
        order.Add(neighbour);
        stack.Push((neighbour, 0));
      }
    }

    return [.. order];
  }

  /// <summary>
  /// Builds each node's neighbours in walk priority: fewest links first (dead ends and short
  /// branches are finished before the main path), ties by lowest index.
  /// </summary>
  /// <param name="count">Number of nodes.</param>
  /// <param name="edges">Edges as pairs of existing node indices.</param>
  /// <returns>Neighbours of each node, without duplicates or self-loops, in walk priority.</returns>
  private static int[][] SortedNeighbours(int count, IReadOnlyList<(int From, int To)> edges)
  {
    var sets = new SortedSet<int>[count];
    for (int i = 0; i < count; i++) sets[i] = [];
    foreach ((int from, int to) in edges)
    {
      if (from == to) continue; // a self-loop does not affect the order
      sets[from].Add(to);
      sets[to].Add(from);
    }

    // SortedSet enumerates by index, and OrderBy is stable, so ties keep the lowest index first.
    return sets.Select(set => set.OrderBy(neighbour => sets[neighbour].Count).ToArray()).ToArray();
  }

  #endregion

  #region Geometry

  /// <summary>
  /// Writes ring positions for the given angles.
  /// </summary>
  /// <param name="positions">Positions to fill.</param>
  /// <param name="angles">Angle of each node, in radians.</param>
  /// <param name="ringRadius">Ring radius.</param>
  private static void WriteRingPositions(Vector2[] positions, float[] angles, float ringRadius)
  {
    for (int i = 0; i < positions.Length; i++)
    {
      positions[i] = new Vector2(MathF.Cos(angles[i]), MathF.Sin(angles[i])) * ringRadius;
    }
  }

  /// <summary>
  /// Checks that every pair of neighbours on the ring is far enough apart.
  /// </summary>
  /// <param name="positions">Node centers, in node order.</param>
  /// <param name="order">Node indices in ring order.</param>
  /// <param name="radii">Node radii, in node order.</param>
  /// <param name="gap">Required free space between neighbours.</param>
  /// <returns>True if all ring neighbours keep the gap.</returns>
  private static bool RingNeighboursHaveGap(Vector2[] positions, int[] order, IReadOnlyList<float> radii, float gap)
  {
    for (int k = 0; k < order.Length; k++)
    {
      int node = order[k];
      int next = order[(k + 1) % order.Length];
      if (Vector2.Distance(positions[node], positions[next]) < radii[node] + radii[next] + gap) return false;
    }

    return true;
  }

  #endregion
}
