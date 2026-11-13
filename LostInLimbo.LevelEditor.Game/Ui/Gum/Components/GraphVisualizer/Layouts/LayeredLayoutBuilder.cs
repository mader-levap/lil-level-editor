using System;
using System.Collections.Generic;
using System.Linq;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Starting placement of graph nodes in columns (a layered, Sugiyama-style layout): the root sits
/// in the leftmost column, and every other node in the column matching its number of steps from
/// the root, so the graph reads left to right as distance from the start. Inside each column the
/// order is chosen to reduce edge crossings, and nodes are moved towards their neighbours.
/// </summary>
/// <remarks>
/// <para>Method, per connected part:</para>
/// <list type="number">
/// <item>Root: the lowest marked root of the part; without one, a far end of the part (the node
/// farthest from its lowest node), which gives the deepest layering. Other marked nodes of the
/// part are placed by their distance from the root, like any other node.</item>
/// <item>Layers: breadth-first distance from the root. Every edge therefore joins adjacent
/// columns or stays inside one column; no edge skips a column.</item>
/// <item>Order inside columns: barycenter sweeps (each node goes to the average position of its
/// neighbours in the column before or after), keeping the order with the fewest crossings.</item>
/// <item>Coordinates: columns as far apart as their largest nodes need, nodes stacked in order
/// and pulled towards their neighbours' height without ever coming closer than the link gap.</item>
/// </list>
/// <para>Parts are stacked top to bottom, largest first, with their root columns aligned.</para>
/// <para>Limits: edges inside one column are drawn straight and may pass over other nodes of
/// that column; edge direction is ignored.</para>
/// <para>Deterministic: no randomness; ties are broken by node index or current order.</para>
/// </remarks>
public sealed class LayeredLayoutBuilder : GraphLayoutBuilder
{
  #region Constants

  /// <summary>Most rounds of crossing reduction (one round is a down sweep and an up sweep).</summary>
  private const int MaxSweepRounds = 24;

  /// <summary>Rounds of moving nodes towards their neighbours (one round is a down pass and an up pass).</summary>
  private const int AlignmentRounds = 4;

  /// <summary>
  /// Default physics without edge pull: columns would otherwise bend and long edges inside a
  /// column would pull nodes out of it. Nodes still push each other apart and never overlap.
  /// </summary>
  private static readonly GraphVisPhysics LayeredPhysics = GraphVisPhysics.Default with { AttractStrength = 0f };

  #endregion

  /// <summary>Physics without edge pull, so the columns stay straight (see <see cref="LayeredPhysics"/>).</summary>
  public override GraphVisPhysics DefaultPhysics => LayeredPhysics;

  /// <summary>
  /// Places the nodes in columns.
  /// </summary>
  /// <param name="radii">Node radii, in node order (validated).</param>
  /// <param name="edges">Edges (validated); direction is ignored.</param>
  /// <param name="linkGap">Free space kept between nodes, and between columns and parts (validated).</param>
  /// <param name="roots">Nodes to start from (validated); see the class remarks.</param>
  /// <returns>Node centers, in node order.</returns>
  protected override Vector2[] PlaceCore(
    IReadOnlyList<float> radii, IReadOnlyList<(int From, int To)> edges, float linkGap, IReadOnlyList<int> roots)
  {
    int count = radii.Count;
    var positions = new Vector2[count];
    if (count == 0) return positions;

    int[][] neighbours = Neighbours(count, edges);
    var isRoot = new bool[count];
    foreach (int root in roots) isRoot[root] = true;

    float cursorY = 0;
    foreach (int[] part in FindParts(neighbours))
    {
      int root = ChooseRoot(part, isRoot, neighbours);
      List<List<int>> layers = BuildLayers(root, neighbours, count);
      ReduceCrossings(layers, neighbours, count);
      Dictionary<int, Vector2> local = AssignCoordinates(layers, neighbours, radii, linkGap);

      // Stack parts top to bottom; columns keep their X so the root columns line up.
      GraphVisBounds bounds = GraphVisBounds.AroundCircles(local.Select(pair => (pair.Value, radii[pair.Key])))
        ?? new GraphVisBounds(Vector2.Zero, Vector2.Zero);
      var shift = new Vector2(0, cursorY - bounds.Min.Y);
      foreach ((int node, Vector2 position) in local) positions[node] = position + shift;
      cursorY += bounds.Size.Y + linkGap;
    }

    return positions;
  }

  #region Graph structure

  /// <summary>
  /// Builds undirected neighbour lists.
  /// </summary>
  /// <param name="count">Number of nodes.</param>
  /// <param name="edges">Edges as pairs of existing node indices.</param>
  /// <returns>Neighbours of each node, without duplicates or self-loops, by ascending index.</returns>
  private static int[][] Neighbours(int count, IReadOnlyList<(int From, int To)> edges)
  {
    var sets = new SortedSet<int>[count];
    for (int i = 0; i < count; i++) sets[i] = [];
    foreach ((int from, int to) in edges)
    {
      if (from == to) continue; // a self-loop does not affect placement
      sets[from].Add(to);
      sets[to].Add(from);
    }

    return sets.Select(set => set.ToArray()).ToArray();
  }

  /// <summary>
  /// Splits the graph into connected parts.
  /// </summary>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <returns>Parts as node indices; largest part first, ties by lowest node index.</returns>
  private static List<int[]> FindParts(int[][] neighbours)
  {
    var parts = new List<int[]>();
    var visited = new bool[neighbours.Length];
    for (int start = 0; start < neighbours.Length; start++)
    {
      if (visited[start]) continue;
      parts.Add([.. BreadthFirst(start, neighbours, visited).Select(step => step.Node)]);
    }

    // Parts were found in order of their lowest node index; a stable sort keeps that for ties.
    return [.. parts.OrderByDescending(part => part.Length)];
  }

  /// <summary>
  /// Walks breadth-first from a node, marking nodes visited.
  /// </summary>
  /// <param name="start">Start node.</param>
  /// <param name="neighbours">Neighbours of each node (ascending index, so the walk is deterministic).</param>
  /// <param name="visited">Visited flags, updated; nodes already visited are not entered.</param>
  /// <returns>Nodes in the order reached, with their distance from <paramref name="start"/>.</returns>
  private static List<(int Node, int Distance)> BreadthFirst(int start, int[][] neighbours, bool[] visited)
  {
    var reached = new List<(int Node, int Distance)> { (start, 0) };
    visited[start] = true;
    for (int head = 0; head < reached.Count; head++)
    {
      (int node, int distance) = reached[head];
      foreach (int next in neighbours[node])
      {
        if (visited[next]) continue;
        visited[next] = true;
        reached.Add((next, distance + 1));
      }
    }

    return reached;
  }

  /// <summary>
  /// Chooses the root of a part: its lowest marked root, otherwise a far end.
  /// </summary>
  /// <param name="part">Node indices of the part, lowest first.</param>
  /// <param name="isRoot">Marked roots.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <returns>Root node.</returns>
  private static int ChooseRoot(int[] part, bool[] isRoot, int[][] neighbours)
  {
    int marked = part.Where(node => isRoot[node]).DefaultIfEmpty(-1).Min();
    if (marked >= 0) return marked;

    // Far end: the node farthest from the part's lowest node; ties go to the lowest index.
    int lowest = part.Min();
    List<(int Node, int Distance)> reached = BreadthFirst(lowest, neighbours, new bool[neighbours.Length]);
    int farthest = reached.Max(step => step.Distance);
    return reached.Where(step => step.Distance == farthest).Min(step => step.Node);
  }

  /// <summary>
  /// Puts the nodes of a part into columns by breadth-first distance from the root.
  /// </summary>
  /// <param name="root">Root node.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="count">Number of nodes in the whole graph.</param>
  /// <returns>Columns, root column first; nodes in the order the walk reached them.</returns>
  private static List<List<int>> BuildLayers(int root, int[][] neighbours, int count)
  {
    var layers = new List<List<int>>();
    foreach ((int node, int distance) in BreadthFirst(root, neighbours, new bool[count]))
    {
      if (distance == layers.Count) layers.Add([]);
      layers[distance].Add(node);
    }

    return layers;
  }

  #endregion

  #region Crossing reduction

  /// <summary>
  /// Reorders the columns to reduce crossings between neighbouring columns, by alternating down
  /// and up barycenter sweeps; keeps the best order found.
  /// </summary>
  /// <param name="layers">Columns, reordered in place.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="count">Number of nodes in the whole graph.</param>
  private static void ReduceCrossings(List<List<int>> layers, int[][] neighbours, int count)
  {
    if (layers.Count < 2) return;

    var position = new int[count];
    UpdatePositions(layers, position);

    int best = CountCrossings(layers, neighbours, position);
    List<List<int>> bestOrder = Copy(layers);
    for (int round = 0; round < MaxSweepRounds && best > 0; round++)
    {
      int bestBeforeRound = best;

      for (int k = 1; k < layers.Count; k++) SortByBarycenter(layers[k], neighbours, position, layers, k - 1);
      Keep(ref best, ref bestOrder, layers, neighbours, position);

      for (int k = layers.Count - 2; k >= 0; k--) SortByBarycenter(layers[k], neighbours, position, layers, k + 1);
      Keep(ref best, ref bestOrder, layers, neighbours, position);

      if (best >= bestBeforeRound) break; // a whole round brought nothing
    }

    for (int k = 0; k < layers.Count; k++)
    {
      layers[k].Clear();
      layers[k].AddRange(bestOrder[k]);
    }
  }

  /// <summary>
  /// Remembers the current order if it has fewer crossings than the best so far.
  /// </summary>
  /// <param name="best">Fewest crossings so far, updated.</param>
  /// <param name="bestOrder">Order with the fewest crossings, updated.</param>
  /// <param name="layers">Current columns.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="position">Position of each node inside its column (current).</param>
  private static void Keep(ref int best, ref List<List<int>> bestOrder, List<List<int>> layers, int[][] neighbours, int[] position)
  {
    int crossings = CountCrossings(layers, neighbours, position);
    if (crossings >= best) return;
    best = crossings;
    bestOrder = Copy(layers);
  }

  /// <summary>
  /// Sorts one column by the average position of each node's neighbours in a reference column.
  /// Nodes without neighbours there keep their own position as key; the sort is stable.
  /// </summary>
  /// <param name="layer">Column to sort, in place.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="position">Position of each node inside its column; updated for the sorted column.</param>
  /// <param name="layers">All columns.</param>
  /// <param name="referenceLayer">Index of the reference column.</param>
  private static void SortByBarycenter(List<int> layer, int[][] neighbours, int[] position, List<List<int>> layers, int referenceLayer)
  {
    var inReference = new HashSet<int>(layers[referenceLayer]);
    List<int> sorted =
    [
      .. layer.OrderBy(node =>
      {
        int[] linked = neighbours[node].Where(inReference.Contains).ToArray();
        return linked.Length == 0 ? position[node] : linked.Average(other => (double)position[other]);
      }),
    ];

    layer.Clear();
    layer.AddRange(sorted);
    for (int i = 0; i < layer.Count; i++) position[layer[i]] = i;
  }

  /// <summary>
  /// Counts edge crossings between every pair of neighbouring columns.
  /// </summary>
  /// <param name="layers">Columns.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="position">Position of each node inside its column.</param>
  /// <returns>Number of crossing edge pairs.</returns>
  private static int CountCrossings(List<List<int>> layers, int[][] neighbours, int[] position)
  {
    int crossings = 0;
    for (int k = 0; k + 1 < layers.Count; k++)
    {
      // Edges between column k and k+1 as (position in k, position in k+1).
      var next = new HashSet<int>(layers[k + 1]);
      var between = new List<(int Left, int Right)>();
      foreach (int node in layers[k])
      {
        foreach (int other in neighbours[node])
        {
          if (next.Contains(other)) between.Add((position[node], position[other]));
        }
      }

      for (int i = 0; i < between.Count; i++)
      {
        for (int j = i + 1; j < between.Count; j++)
        {
          int left = between[i].Left - between[j].Left;
          int right = between[i].Right - between[j].Right;
          if ((long)left * right < 0) crossings++; // opposite order on the two sides
        }
      }
    }

    return crossings;
  }

  /// <summary>
  /// Records each node's position inside its column.
  /// </summary>
  /// <param name="layers">Columns.</param>
  /// <param name="position">Positions, filled.</param>
  private static void UpdatePositions(List<List<int>> layers, int[] position)
  {
    foreach (List<int> layer in layers)
    {
      for (int i = 0; i < layer.Count; i++) position[layer[i]] = i;
    }
  }

  /// <summary>
  /// Copies the columns.
  /// </summary>
  /// <param name="layers">Columns.</param>
  /// <returns>Independent copy.</returns>
  private static List<List<int>> Copy(List<List<int>> layers) => [.. layers.Select(layer => new List<int>(layer))];

  #endregion

  #region Coordinates

  /// <summary>
  /// Computes node centers for ordered columns: columns left to right, nodes stacked in order and
  /// pulled towards their neighbours' height.
  /// </summary>
  /// <param name="layers">Ordered columns, root column first.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="radii">Node radii.</param>
  /// <param name="linkGap">Free space kept between nodes and between columns.</param>
  /// <returns>Center of each node of the part.</returns>
  private static Dictionary<int, Vector2> AssignCoordinates(
    List<List<int>> layers, int[][] neighbours, IReadOnlyList<float> radii, float linkGap)
  {
    // Columns: far enough apart that the largest nodes of two neighbouring columns keep the gap.
    var columnX = new float[layers.Count];
    for (int k = 1; k < layers.Count; k++)
    {
      columnX[k] = columnX[k - 1] + layers[k - 1].Max(node => radii[node]) + layers[k].Max(node => radii[node]) + linkGap;
    }

    // Heights: packed in order and centered, then moved towards the neighbours.
    var y = new Dictionary<int, float>();
    foreach (List<int> layer in layers)
    {
      float cursor = 0;
      for (int i = 0; i < layer.Count; i++)
      {
        if (i > 0) cursor += Separation(layer[i - 1], layer[i], radii, linkGap);
        y[layer[i]] = cursor;
      }

      float middle = cursor / 2;
      foreach (int node in layer) y[node] -= middle;
    }

    for (int round = 0; round < AlignmentRounds; round++)
    {
      for (int k = 1; k < layers.Count; k++) Align(layers[k], layers[k - 1], neighbours, radii, linkGap, y);
      for (int k = layers.Count - 2; k >= 0; k--) Align(layers[k], layers[k + 1], neighbours, radii, linkGap, y);
    }

    // A last down pass, so every column follows the one before it from the root outwards.
    for (int k = 1; k < layers.Count; k++) Align(layers[k], layers[k - 1], neighbours, radii, linkGap, y);

    var centers = new Dictionary<int, Vector2>();
    for (int k = 0; k < layers.Count; k++)
    {
      foreach (int node in layers[k]) centers[node] = new Vector2(columnX[k], y[node]);
    }

    return centers;
  }

  /// <summary>
  /// Moves the nodes of one column towards the average height of their neighbours in a reference
  /// column, keeping their order and the required separation (priority method).
  /// </summary>
  /// <remarks>
  /// <para>Better connected nodes (more neighbours) are placed first and keep their wanted height;
  /// less connected ones make way around them. A main path therefore stays straight while its side
  /// rooms step aside, instead of both splitting the difference and drifting column by column.</para>
  /// <para>Spacing is handled by subtracting each node's packed offset: in those shifted heights the
  /// separation rule becomes "never decreasing", so nodes of equal priority are fitted by isotonic
  /// regression (pool adjacent violators), which centers nodes that want the same height, and then
  /// clamped between the already placed nodes.</para>
  /// </remarks>
  /// <param name="layer">Column to move (ordered).</param>
  /// <param name="reference">Reference column.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="radii">Node radii.</param>
  /// <param name="linkGap">Free space kept between nodes.</param>
  /// <param name="y">Heights of all nodes; updated for <paramref name="layer"/>.</param>
  private static void Align(
    List<int> layer, List<int> reference, int[][] neighbours, IReadOnlyList<float> radii, float linkGap, Dictionary<int, float> y)
  {
    int n = layer.Count;

    // Packed offset of each node from the first one; shifted height = height - offset.
    var offset = new float[n];
    for (int i = 1; i < n; i++) offset[i] = offset[i - 1] + Separation(layer[i - 1], layer[i], radii, linkGap);

    var inReference = new HashSet<int>(reference);
    var wanted = new float[n];
    for (int i = 0; i < n; i++)
    {
      int[] linked = neighbours[layer[i]].Where(inReference.Contains).ToArray();
      float height = linked.Length == 0 ? y[layer[i]] : linked.Average(other => y[other]);
      wanted[i] = height - offset[i];
    }

    var shifted = new float?[n];
    foreach (int priority in layer.Select(node => neighbours[node].Length).Distinct().OrderDescending())
    {
      int[] group = [.. Enumerable.Range(0, n).Where(i => neighbours[layer[i]].Length == priority)];
      float[] fitted = FitNonDecreasing([.. group.Select(i => wanted[i])]);
      for (int g = 0; g < group.Length; g++)
      {
        int i = group[g];
        float low = float.NegativeInfinity;
        float high = float.PositiveInfinity;
        for (int j = 0; j < i; j++) if (shifted[j] is float above) low = Math.Max(low, above);
        for (int j = i + 1; j < n; j++) if (shifted[j] is float below) high = Math.Min(high, below);
        shifted[i] = Math.Clamp(fitted[g], low, high);
      }
    }

    for (int i = 0; i < n; i++) y[layer[i]] = shifted[i]!.Value + offset[i]; // every node belongs to one priority group
  }

  /// <summary>
  /// Fits a never-decreasing sequence as close as possible (least squares) to the given values,
  /// by pooling adjacent values that are out of order into their average.
  /// </summary>
  /// <param name="values">Values in order.</param>
  /// <returns>Never-decreasing fit, same length.</returns>
  private static float[] FitNonDecreasing(float[] values)
  {
    // Blocks of pooled values as (average, size).
    var blocks = new List<(float Average, int Size)>();
    foreach (float value in values)
    {
      (float average, int size) = (value, 1);
      while (blocks.Count > 0 && blocks[^1].Average > average)
      {
        (float previousAverage, int previousSize) = blocks[^1];
        blocks.RemoveAt(blocks.Count - 1);
        average = (previousAverage * previousSize + average * size) / (previousSize + size);
        size += previousSize;
      }

      blocks.Add((average, size));
    }

    return [.. blocks.SelectMany(block => Enumerable.Repeat(block.Average, block.Size))];
  }

  /// <summary>
  /// Computes the center distance two stacked nodes need.
  /// </summary>
  /// <param name="upper">Upper node.</param>
  /// <param name="lower">Lower node.</param>
  /// <param name="radii">Node radii.</param>
  /// <param name="linkGap">Free space kept between them.</param>
  /// <returns>Both radii plus the gap.</returns>
  private static float Separation(int upper, int lower, IReadOnlyList<float> radii, float linkGap) => radii[upper] + radii[lower] + linkGap;

  #endregion
}
