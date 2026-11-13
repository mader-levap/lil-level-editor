using System;
using System.Collections.Generic;
using System.Linq;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Render;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Starting placement of graph nodes in rows (a layered, Sugiyama-style layout): the root sits
/// in the top row, and every other node in the row matching its number of steps from the root,
/// so the graph reads top to bottom as distance from the start. Inside each row the order is
/// chosen to reduce edge crossings, and nodes are moved towards their neighbours.
/// </summary>
/// <remarks>
/// <para>Method, per connected part:</para>
/// <list type="number">
/// <item>Root: the lowest marked root of the part; without one, a far end of the part (the node
/// farthest from its lowest node), which gives the deepest layering. Other marked nodes of the
/// part are placed by their distance from the root, like any other node.</item>
/// <item>Layers: breadth-first distance from the root. Every edge therefore joins adjacent
/// rows or stays inside one row; no edge skips a row.</item>
/// <item>Side rooms: a node with a single neighbour (other than the root) leaves its row and goes
/// to a side row halfway between its neighbour's row and the next one, so it does not sit on the
/// edges that run along rows. It keeps clear of the edges crossing that gap and of other side
/// rooms, as near below its neighbour as that allows.</item>
/// <item>Order inside rows (side rooms aside): barycenter sweeps (each node goes to the average position of its
/// neighbours in the row before or after), keeping the order with the fewest crossings.</item>
/// <item>Coordinates: rows as far apart as their largest nodes need, nodes lined up in order
/// and pulled sideways towards their neighbours without ever coming closer than the link gap.</item>
/// </list>
/// <para>Direction (<see cref="Direction"/>): with <see cref="EnLayeredDirection.Auto"/> each part
/// is laid out in rows, and a part whose rows would be taller than wide (a deep, chain-like part)
/// is turned into columns, root on the left, so it suits a wide view either way.</para>
/// <para>Parts are placed left to right, largest first, with their roots on one horizontal line.</para>
/// <para>Limits: edges inside one row are drawn straight and may pass over other nodes of
/// that row; edge direction is ignored.</para>
/// <para>Deterministic: no randomness; ties are broken by node index or current order.</para>
/// </remarks>
public sealed class LayeredLayoutBuilder : GraphLayoutBuilder
{
  #region Constants

  /// <summary>Most rounds of crossing reduction (one round is a down sweep and an up sweep).</summary>
  private const int MaxSweepRounds = 24;

  /// <summary>Rounds of moving nodes towards their neighbours (one round is a down pass and an up pass).</summary>
  private const int AlignmentRounds = 4;

  /// <summary>Free space kept between a side room and an edge crossing its side row, as a share of the link gap.</summary>
  private const float EdgeClearanceShare = 0.5f;

  /// <summary>Tolerance when telling whether a place lies inside a forbidden stretch, in pixels.</summary>
  private const float PlaceTolerance = 0.001f;

  /// <summary>
  /// Default physics without edge pull: rows would otherwise bend and long edges inside a
  /// row would pull nodes out of it. Nodes still push each other apart and never overlap.
  /// </summary>
  private static readonly GraphVisPhysics LayeredPhysics = GraphVisPhysics.Default with { AttractStrength = 0f };

  #endregion

  /// <summary>
  /// Creates the builder.
  /// </summary>
  /// <param name="direction">Direction of the layers; see <see cref="Direction"/>.</param>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="direction"/> is not a known value.</exception>
  public LayeredLayoutBuilder(EnLayeredDirection direction = EnLayeredDirection.Auto)
  {
    if (!Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown layer direction.");
    Direction = direction;
  }

  /// <summary>Direction of the layers: chosen per part (default), or always rows or always columns.</summary>
  public EnLayeredDirection Direction { get; }

  /// <summary>Physics without edge pull, so the rows stay straight (see <see cref="LayeredPhysics"/>).</summary>
  public override GraphVisPhysics DefaultPhysics => LayeredPhysics;

  /// <summary>
  /// Places the nodes in rows.
  /// </summary>
  /// <param name="radii">Node radii, in node order (validated).</param>
  /// <param name="edges">Edges (validated); direction is ignored.</param>
  /// <param name="linkGap">Free space kept between nodes, and between rows and parts (validated).</param>
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

    float cursorX = 0;
    foreach (int[] part in FindParts(neighbours))
    {
      int root = ChooseRoot(part, isRoot, neighbours);
      List<List<int>> layers = BuildLayers(root, neighbours, count);
      List<List<int>> sideRows = SplitOffSideRooms(layers, neighbours, root);
      ReduceCrossings(layers, neighbours, count);
      Dictionary<int, Vector2> local = AssignCoordinates(layers, sideRows, neighbours, radii, linkGap);

      // Root at the origin (its row is at Y = 0; alignment may have moved it sideways), so roots
      // line up whether the part is turned or not.
      float rootX = local[root].X;
      foreach (int node in part) local[node] -= new Vector2(rootX, 0);
      GraphVisBounds bounds = BoundsOf(local, radii);

      // Turn rows into columns (swap X and Y) when asked, or when the rows are taller than wide.
      if (Direction == EnLayeredDirection.LeftToRight || (Direction == EnLayeredDirection.Auto && bounds.Size.Y > bounds.Size.X))
      {
        foreach (int node in part) local[node] = new Vector2(local[node].Y, local[node].X);
        bounds = BoundsOf(local, radii);
      }

      // Place parts left to right; Y is kept, so the roots (at Y = 0 either way) line up.
      var shift = new Vector2(cursorX - bounds.Min.X, 0);
      foreach ((int node, Vector2 position) in local) positions[node] = position + shift;
      cursorX += bounds.Size.X + linkGap;
    }

    return positions;
  }

  /// <summary>
  /// Computes the bounds around the circles of a part.
  /// </summary>
  /// <param name="local">Centers of the part's nodes.</param>
  /// <param name="radii">Node radii.</param>
  /// <returns>Bounds; empty at the origin for an empty part.</returns>
  private static GraphVisBounds BoundsOf(Dictionary<int, Vector2> local, IReadOnlyList<float> radii)
  {
    return GraphVisBounds.AroundCircles(local.Select(pair => (pair.Value, radii[pair.Key])))
      ?? new GraphVisBounds(Vector2.Zero, Vector2.Zero);
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
  /// Puts the nodes of a part into rows by breadth-first distance from the root.
  /// </summary>
  /// <param name="root">Root node.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="count">Number of nodes in the whole graph.</param>
  /// <returns>Rows, root row first; nodes in the order the walk reached them.</returns>
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

  /// <summary>
  /// Takes the side rooms (nodes with a single neighbour, other than the root) out of the rows.
  /// Rows left empty at the end (rows of side rooms only) are dropped.
  /// </summary>
  /// <param name="layers">Rows; side rooms are removed in place.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="root">Root node (stays in the first row whatever its neighbour count).</param>
  /// <returns>Side rooms per row, in walk order: entry k holds those whose neighbour is in row k; as many entries as rows remain.</returns>
  private static List<List<int>> SplitOffSideRooms(List<List<int>> layers, int[][] neighbours, int root)
  {
    var sideRows = new List<List<int>>();
    foreach (List<int> _ in layers) sideRows.Add([]);

    // A side room's only neighbour is the node it was reached from, one row before.
    for (int k = 1; k < layers.Count; k++)
    {
      foreach (int node in layers[k])
      {
        if (node != root && neighbours[node].Length == 1) sideRows[k - 1].Add(node);
      }

      layers[k].RemoveAll(node => node != root && neighbours[node].Length == 1);
    }

    // A row of side rooms only has no successors (they would need a non-side-room before them).
    while (layers.Count > 1 && layers[^1].Count == 0)
    {
      layers.RemoveAt(layers.Count - 1);
      sideRows.RemoveAt(sideRows.Count - 1);
    }

    return sideRows;
  }

  #endregion

  #region Crossing reduction

  /// <summary>
  /// Reorders the rows to reduce crossings between neighbouring rows, by alternating down
  /// and up barycenter sweeps; keeps the best order found.
  /// </summary>
  /// <param name="layers">Rows, reordered in place.</param>
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
  /// <param name="layers">Current rows.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="position">Position of each node inside its row (current).</param>
  private static void Keep(ref int best, ref List<List<int>> bestOrder, List<List<int>> layers, int[][] neighbours, int[] position)
  {
    int crossings = CountCrossings(layers, neighbours, position);
    if (crossings >= best) return;
    best = crossings;
    bestOrder = Copy(layers);
  }

  /// <summary>
  /// Sorts one row by the average position of each node's neighbours in a reference row.
  /// Nodes without neighbours there keep their own position as key; the sort is stable.
  /// </summary>
  /// <param name="layer">Row to sort, in place.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="position">Position of each node inside its row; updated for the sorted row.</param>
  /// <param name="layers">All rows.</param>
  /// <param name="referenceLayer">Index of the reference row.</param>
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
  /// Counts edge crossings between every pair of neighbouring rows.
  /// </summary>
  /// <param name="layers">Rows.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="position">Position of each node inside its row.</param>
  /// <returns>Number of crossing edge pairs.</returns>
  private static int CountCrossings(List<List<int>> layers, int[][] neighbours, int[] position)
  {
    int crossings = 0;
    for (int k = 0; k + 1 < layers.Count; k++)
    {
      // Edges between row k and k+1 as (position in k, position in k+1).
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
  /// Records each node's position inside its row.
  /// </summary>
  /// <param name="layers">Rows.</param>
  /// <param name="position">Positions, filled.</param>
  private static void UpdatePositions(List<List<int>> layers, int[] position)
  {
    foreach (List<int> layer in layers)
    {
      for (int i = 0; i < layer.Count; i++) position[layer[i]] = i;
    }
  }

  /// <summary>
  /// Copies the rows.
  /// </summary>
  /// <param name="layers">Rows.</param>
  /// <returns>Independent copy.</returns>
  private static List<List<int>> Copy(List<List<int>> layers) => [.. layers.Select(layer => new List<int>(layer))];

  #endregion

  #region Coordinates

  /// <summary>
  /// Computes node centers for ordered rows: rows top to bottom, nodes lined up left to right in
  /// order and pulled sideways towards their neighbours; side rooms in side rows between them.
  /// </summary>
  /// <param name="layers">Ordered rows, root row first.</param>
  /// <param name="sideRows">Side rooms per row (see <see cref="SplitOffSideRooms"/>).</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="radii">Node radii.</param>
  /// <param name="linkGap">Free space kept between nodes and between rows.</param>
  /// <returns>Center of each node of the part.</returns>
  private static Dictionary<int, Vector2> AssignCoordinates(
    List<List<int>> layers, List<List<int>> sideRows, int[][] neighbours, IReadOnlyList<float> radii, float linkGap)
  {
    // Rows: far enough apart that the largest nodes of two neighbouring rows keep the gap; a side
    // row in between adds its largest node and another gap.
    var rowY = new float[layers.Count];
    var sideY = new float[layers.Count];
    for (int k = 0; k < layers.Count; k++)
    {
      float bottom = rowY[k] + layers[k].Max(node => radii[node]);
      if (sideRows[k].Count > 0)
      {
        float sideRadius = sideRows[k].Max(node => radii[node]);
        sideY[k] = bottom + linkGap + sideRadius;
        bottom = sideY[k] + sideRadius;
      }

      if (k + 1 < layers.Count) rowY[k + 1] = bottom + linkGap + layers[k + 1].Max(node => radii[node]);
    }

    // Places along the rows: packed in order and centered, then moved towards the neighbours.
    var x = new Dictionary<int, float>();
    foreach (List<int> layer in layers)
    {
      float cursor = 0;
      for (int i = 0; i < layer.Count; i++)
      {
        if (i > 0) cursor += Separation(layer[i - 1], layer[i], radii, linkGap);
        x[layer[i]] = cursor;
      }

      float middle = cursor / 2;
      foreach (int node in layer) x[node] -= middle;
    }

    for (int round = 0; round < AlignmentRounds; round++)
    {
      for (int k = 1; k < layers.Count; k++) Align(layers[k], layers[k - 1], neighbours, radii, linkGap, x);
      for (int k = layers.Count - 2; k >= 0; k--) Align(layers[k], layers[k + 1], neighbours, radii, linkGap, x);
    }

    // A last down pass, so every row follows the one before it from the root outwards.
    for (int k = 1; k < layers.Count; k++) Align(layers[k], layers[k - 1], neighbours, radii, linkGap, x);

    var centers = new Dictionary<int, Vector2>();
    for (int k = 0; k < layers.Count; k++)
    {
      foreach (int node in layers[k]) centers[node] = new Vector2(x[node], rowY[k]);
      if (sideRows[k].Count == 0) continue;

      List<int> next = k + 1 < layers.Count ? layers[k + 1] : [];
      float share = next.Count == 0 ? 0 : (sideY[k] - rowY[k]) / (rowY[k + 1] - rowY[k]);
      foreach ((int node, float place) in PlaceSideRow(sideRows[k], layers[k], next, share, neighbours, radii, linkGap, x))
      {
        centers[node] = new Vector2(place, sideY[k]);
      }
    }

    return centers;
  }

  /// <summary>
  /// Places the side rooms of one side row: each as near below its neighbour as possible, without
  /// touching the edges that cross the side row or the side rooms placed before it.
  /// </summary>
  /// <param name="sideRooms">Side rooms whose neighbour is in <paramref name="row"/>.</param>
  /// <param name="row">Row above the side row (placed).</param>
  /// <param name="next">Row below the side row (placed); empty when there is none.</param>
  /// <param name="share">Where the side row lies between the two rows: 0 at the upper row, 1 at the lower.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="radii">Node radii.</param>
  /// <param name="linkGap">Free space kept between nodes.</param>
  /// <param name="x">Places of the rows' nodes along their rows.</param>
  /// <returns>Place of each side room along the side row.</returns>
  private static Dictionary<int, float> PlaceSideRow(
    List<int> sideRooms, List<int> row, List<int> next, float share, int[][] neighbours, IReadOnlyList<float> radii,
    float linkGap, Dictionary<int, float> x)
  {
    // Where the edges between the two rows cross the side row.
    var inNext = new HashSet<int>(next);
    var crossings = new List<float>();
    foreach (int upper in row)
    {
      foreach (int lower in neighbours[upper].Where(inNext.Contains)) crossings.Add(x[upper] + (x[lower] - x[upper]) * share);
    }

    // Side rooms in the order of their neighbours' places, ties by node index; each takes the free
    // place nearest below its neighbour, preferring the right on a tie.
    float edgeClearance = linkGap * EdgeClearanceShare;
    var placed = new Dictionary<int, float>();
    foreach (int room in sideRooms.OrderBy(room => x[neighbours[room][0]]).ThenBy(room => room))
    {
      float radius = radii[room];
      var forbidden = new List<(float Low, float High)>();
      foreach (float crossing in crossings) forbidden.Add((crossing - radius - edgeClearance, crossing + radius + edgeClearance));
      foreach ((int other, float place) in placed)
      {
        float separation = Separation(other, room, radii, linkGap);
        forbidden.Add((place - separation, place + separation));
      }

      placed[room] = NearestFreePlace(x[neighbours[room][0]], forbidden);
    }

    return placed;
  }

  /// <summary>
  /// Finds the place nearest to a wanted one that lies inside none of the forbidden stretches
  /// (their ends are allowed).
  /// </summary>
  /// <param name="wanted">Wanted place.</param>
  /// <param name="forbidden">Forbidden stretches.</param>
  /// <returns>Nearest free place; on a tie the larger one.</returns>
  private static float NearestFreePlace(float wanted, List<(float Low, float High)> forbidden)
  {
    bool IsFree(float place) => forbidden.All(stretch => place <= stretch.Low + PlaceTolerance || place >= stretch.High - PlaceTolerance);

    if (IsFree(wanted)) return wanted;

    // The nearest free place is an end of some stretch.
    float best = float.NaN;
    foreach (float candidate in forbidden.SelectMany(stretch => new[] { stretch.Low, stretch.High }))
    {
      if (!IsFree(candidate)) continue;
      float distance = MathF.Abs(candidate - wanted);
      float bestDistance = float.IsNaN(best) ? float.PositiveInfinity : MathF.Abs(best - wanted);
      if (distance < bestDistance - PlaceTolerance || (MathF.Abs(distance - bestDistance) <= PlaceTolerance && candidate > best)) best = candidate;
    }

    return best; // the highest end of all stretches is always free, so a place is found
  }

  /// <summary>
  /// Moves the nodes of one row sideways towards the average place of their neighbours in a
  /// reference row, keeping their order and the required separation (priority method).
  /// </summary>
  /// <remarks>
  /// <para>Better connected nodes (more neighbours) are placed first and keep their wanted place;
  /// less connected ones make way around them. A main path therefore stays straight while its side
  /// rooms step aside, instead of both splitting the difference and drifting row by row.</para>
  /// <para>Spacing is handled by subtracting each node's packed offset: in those shifted places the
  /// separation rule becomes "never decreasing", so nodes of equal priority are fitted by isotonic
  /// regression (pool adjacent violators), which centers nodes that want the same place, and then
  /// clamped between the already placed nodes.</para>
  /// </remarks>
  /// <param name="layer">Row to move (ordered).</param>
  /// <param name="reference">Reference row.</param>
  /// <param name="neighbours">Neighbours of each node.</param>
  /// <param name="radii">Node radii.</param>
  /// <param name="linkGap">Free space kept between nodes.</param>
  /// <param name="x">Places of all nodes along their rows; updated for <paramref name="layer"/>.</param>
  private static void Align(
    List<int> layer, List<int> reference, int[][] neighbours, IReadOnlyList<float> radii, float linkGap, Dictionary<int, float> x)
  {
    int n = layer.Count;

    // Packed offset of each node from the first one; shifted place = place - offset.
    var offset = new float[n];
    for (int i = 1; i < n; i++) offset[i] = offset[i - 1] + Separation(layer[i - 1], layer[i], radii, linkGap);

    var inReference = new HashSet<int>(reference);
    var wanted = new float[n];
    for (int i = 0; i < n; i++)
    {
      int[] linked = neighbours[layer[i]].Where(inReference.Contains).ToArray();
      float place = linked.Length == 0 ? x[layer[i]] : linked.Average(other => x[other]);
      wanted[i] = place - offset[i];
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

    for (int i = 0; i < n; i++) x[layer[i]] = shifted[i]!.Value + offset[i]; // every node belongs to one priority group
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
  /// Computes the center distance two neighbouring nodes of a row need.
  /// </summary>
  /// <param name="first">Node on the left.</param>
  /// <param name="second">Node on the right.</param>
  /// <param name="radii">Node radii.</param>
  /// <param name="linkGap">Free space kept between them.</param>
  /// <returns>Both radii plus the gap.</returns>
  private static float Separation(int first, int second, IReadOnlyList<float> radii, float linkGap) => radii[first] + radii[second] + linkGap;

  #endregion
}
