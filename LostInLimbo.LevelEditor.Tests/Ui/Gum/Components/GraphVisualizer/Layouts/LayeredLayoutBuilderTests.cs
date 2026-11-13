using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Headless tests for <see cref="LayeredLayoutBuilder"/> (columns by distance from the root).
/// </summary>
public sealed class LayeredLayoutBuilderTests
{
  /// <summary>Tolerance for floating-point comparisons.</summary>
  private const float Tolerance = 0.01f;

  /// <summary>Gap between nodes used by the tests.</summary>
  private const float Gap = 60f;

  /// <summary>Node radius used by most tests.</summary>
  private const float Radius = 20f;

  /// <summary>Builder under test.</summary>
  private static readonly LayeredLayoutBuilder Builder = new();

  /// <summary>
  /// Creates equal radii.
  /// </summary>
  /// <param name="count">Number of nodes.</param>
  /// <returns>Radii.</returns>
  private static float[] Radii(int count) => Enumerable.Repeat(Radius, count).ToArray();

  /// <summary>
  /// Tells whether two line segments cross each other (proper crossing, shared ends do not count).
  /// </summary>
  /// <param name="a">Start of the first segment.</param>
  /// <param name="b">End of the first segment.</param>
  /// <param name="c">Start of the second segment.</param>
  /// <param name="d">End of the second segment.</param>
  /// <returns>True if the segments cross.</returns>
  private static bool Cross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
  {
    static float Side(Vector2 p, Vector2 q, Vector2 r) => (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
    return Side(a, b, c) * Side(a, b, d) < 0 && Side(c, d, a) * Side(c, d, b) < 0;
  }

  /// <summary>
  /// Groups nodes into columns by their X coordinate.
  /// </summary>
  /// <param name="positions">Node centers.</param>
  /// <returns>Column index of each node (0 = leftmost).</returns>
  private static int[] Columns(Vector2[] positions)
  {
    float[] xs = positions.Select(position => MathF.Round(position.X, 1)).Distinct().Order().ToArray();
    return positions.Select(position => Array.IndexOf(xs, MathF.Round(position.X, 1))).ToArray();
  }

  /// <summary>
  /// Verifies no nodes give no positions and one node gets a finite one.
  /// </summary>
  [Fact]
  public void Place_EmptyAndSingle_AreHandled()
  {
    // Act + Assert: Nothing to place; one node is placed somewhere finite.
    Assert.Empty(Builder.Place([], [], Gap));
    Vector2 single = Assert.Single(Builder.Place([Radius], [], Gap));
    Assert.True(float.IsFinite(single.X) && float.IsFinite(single.Y));
  }

  /// <summary>
  /// Verifies a chain from its first node runs left to right on one horizontal line.
  /// </summary>
  [Fact]
  public void Place_ChainFromRoot_IsStraightLeftToRight()
  {
    // Arrange: Chain 0-1-2-3-4, root 0.
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (3, 4)];

    // Act: Place it.
    Vector2[] positions = Builder.Place(Radii(5), edges, Gap, [0]);

    // Assert: X grows along the chain; all on one line.
    for (int i = 1; i < positions.Length; i++)
    {
      Assert.True(positions[i].X > positions[i - 1].X, $"Node {i} is not right of node {i - 1}.");
      Assert.Equal(positions[0].Y, positions[i].Y, Tolerance);
    }
  }

  /// <summary>
  /// Verifies a marked root in the middle of a chain gets the leftmost column and the rest
  /// follow by distance from it.
  /// </summary>
  [Fact]
  public void Place_MarkedRootInMiddle_ColumnsFollowDistance()
  {
    // Arrange: Chain 0-1-2-3-4, root 2.
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (3, 4)];

    // Act: Place it.
    int[] columns = Columns(Builder.Place(Radii(5), edges, Gap, [2]));

    // Assert: Root first, then 1 and 3, then 0 and 4.
    Assert.Equal([2, 1, 0, 1, 2], columns);
  }

  /// <summary>
  /// Verifies the marked root is used even when it is not the lowest node index.
  /// </summary>
  [Fact]
  public void Place_MarkedRoot_IsLeftmost()
  {
    // Arrange: A star around node 0 with node 3 marked as root.
    (int From, int To)[] edges = [(0, 1), (0, 2), (0, 3)];

    // Act: Place it.
    int[] columns = Columns(Builder.Place(Radii(4), edges, Gap, [3]));

    // Assert: 3 alone in the first column, the hub next, the other leaves last.
    Assert.Equal([1, 2, 2, 0], columns);
  }

  /// <summary>
  /// Verifies that with several marked roots in one part the lowest index is used and the others
  /// are placed by their distance from it.
  /// </summary>
  [Fact]
  public void Place_SeveralRootsInOnePart_LowestIsUsed()
  {
    // Arrange: Chain 0-1-2-3-4 with roots 3 and 1.
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (3, 4)];

    // Act: Place it.
    int[] columns = Columns(Builder.Place(Radii(5), edges, Gap, [3, 1]));

    // Assert: 1 leftmost; 3 is two steps away like any other node.
    Assert.Equal([1, 0, 1, 2, 3], columns);
  }

  /// <summary>
  /// Verifies that without a marked root a far end of the graph starts the layering, and the
  /// result is deterministic.
  /// </summary>
  [Fact]
  public void Place_WithoutRoot_StartsAtFarEndDeterministically()
  {
    // Arrange: Chain 2-0-1-3. From the lowest node (0) the farthest node is 3 (two steps), so 3 is the root.
    (int From, int To)[] edges = [(2, 0), (0, 1), (1, 3)];

    // Act: Place it twice.
    Vector2[] first = Builder.Place(Radii(4), edges, Gap);
    Vector2[] second = Builder.Place(Radii(4), edges, Gap);

    // Assert: Same result; node 3 leftmost, then 1, 0, 2.
    Assert.Equal(first, second);
    Assert.Equal([2, 1, 3, 0], Columns(first));
  }

  /// <summary>
  /// Verifies the order inside columns removes a crossing the breadth-first order would have.
  /// </summary>
  [Fact]
  public void Place_CrossingInBreadthFirstOrder_IsRemoved()
  {
    // Arrange: Root 0 joins 1 and 2; 3 joins 1 and 2; 4 hangs off 1, 5 off 2. Breadth-first order
    // puts 3 above 4 in the last column, which makes 2-3 cross 1-4.
    (int From, int To)[] edges = [(0, 1), (0, 2), (1, 3), (1, 4), (2, 3), (2, 5)];

    // Act: Place it.
    Vector2[] positions = Builder.Place(Radii(6), edges, Gap, [0]);

    // Assert: No pair of edges crosses.
    for (int i = 0; i < edges.Length; i++)
    {
      for (int j = i + 1; j < edges.Length; j++)
      {
        Assert.False(
          Cross(positions[edges[i].From], positions[edges[i].To], positions[edges[j].From], positions[edges[j].To]),
          $"Edges {i} and {j} cross.");
      }
    }
  }

  /// <summary>
  /// Verifies no two nodes come closer than the gap, also with mixed sizes and a loop.
  /// </summary>
  [Fact]
  public void Place_KeepsGapBetweenAllNodes()
  {
    // Arrange: Mixed radii; a loop with branches and an edge inside a column (1-2).
    float[] radii = [30f, 14f, 70f, 22f, 14f, 50f, 22f, 14f];
    (int From, int To)[] edges = [(0, 1), (0, 2), (1, 2), (1, 3), (2, 4), (3, 5), (4, 5), (5, 6), (5, 7)];

    // Act: Place it.
    Vector2[] positions = Builder.Place(radii, edges, Gap, [0]);

    // Assert: Every pair of circles is at least the gap apart.
    for (int i = 0; i < positions.Length; i++)
    {
      for (int j = i + 1; j < positions.Length; j++)
      {
        float free = Vector2.Distance(positions[i], positions[j]) - radii[i] - radii[j];
        Assert.True(free >= Gap - Tolerance, $"Nodes {i} and {j} are only {free} apart.");
      }
    }
  }

  /// <summary>
  /// Verifies every edge joins nodes in the same or neighbouring columns.
  /// </summary>
  [Fact]
  public void Place_EdgesSkipNoColumn()
  {
    // Arrange: A loop with a shortcut and a tail.
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 0), (1, 3), (4, 5), (5, 6)];

    // Act: Place it.
    int[] columns = Columns(Builder.Place(Radii(7), edges, Gap, [0]));

    // Assert: Column difference of every edge is at most one.
    Assert.All(edges, edge => Assert.True(Math.Abs(columns[edge.From] - columns[edge.To]) <= 1, $"Edge {edge} skips a column."));
  }

  /// <summary>
  /// Verifies separate parts are stacked top to bottom, larger first, without overlapping, and
  /// their root columns line up.
  /// </summary>
  [Fact]
  public void Place_SeparateParts_AreStackedLargestFirst()
  {
    // Arrange: A pair 0-1 and a chain 2-3-4, roots 0 and 2.
    (int From, int To)[] edges = [(0, 1), (2, 3), (3, 4)];

    // Act: Place it.
    Vector2[] positions = Builder.Place(Radii(5), edges, Gap, [0, 2]);

    // Assert: The chain (larger) is above the pair, with the gap between; roots share X.
    float chainBottom = new[] { 2, 3, 4 }.Max(i => positions[i].Y) + Radius;
    float pairTop = new[] { 0, 1 }.Min(i => positions[i].Y) - Radius;
    Assert.True(pairTop - chainBottom >= Gap - Tolerance);
    Assert.Equal(positions[2].X, positions[0].X, Tolerance);
  }

  /// <summary>
  /// Verifies the layout keeps its shape under its own default physics.
  /// </summary>
  [Fact]
  public void Place_WithDefaultPhysics_KeepsItsShape()
  {
    // Arrange: A loop with a shortcut and a tail, placed with the default physics' gap.
    float[] radii = Radii(7);
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 0), (1, 3), (4, 5), (5, 6)];
    GraphVisPhysics physics = Builder.DefaultPhysics;
    Vector2[] start = Builder.Place(radii, edges, physics.LinkIdealGap, [0]);

    // Act: Simulate until settled.
    var simulation = new GraphVisSimulation(start, radii, edges, physics);
    simulation.Settle(physics.SettleSteps);

    // Assert: Every node is still where the layout put it.
    for (int i = 0; i < start.Length; i++)
    {
      Assert.True(Vector2.Distance(start[i], simulation.Positions[i]) < 1f, $"Node {i} moved.");
    }
  }

  /// <summary>
  /// Verifies a main path with a side room at every step stays on one horizontal line: the
  /// side rooms make way for the path instead of pushing it up or down column by column.
  /// </summary>
  [Fact]
  public void Place_SpineWithLeaves_SpineStaysStraight()
  {
    // Arrange: Spine 0-1-2-3-4-5; every spine node after the root has one leaf (6..10).
    const int spineLength = 6;
    var edges = new List<(int From, int To)>();
    for (int i = 1; i < spineLength; i++)
    {
      edges.Add((i - 1, i));
      edges.Add((i, spineLength + i - 1));
    }

    // Act: Place it from the root.
    Vector2[] positions = Builder.Place(Radii(spineLength * 2 - 1), edges, Gap, [0]);

    // Assert: All spine nodes share the root's height.
    for (int i = 1; i < spineLength; i++)
    {
      Assert.Equal(positions[0].Y, positions[i].Y, Tolerance);
    }
  }
}
