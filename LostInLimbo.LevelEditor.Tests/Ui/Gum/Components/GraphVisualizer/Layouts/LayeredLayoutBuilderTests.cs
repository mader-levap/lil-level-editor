using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Headless tests for <see cref="LayeredLayoutBuilder"/> (rows by distance from the root, top to bottom).
/// </summary>
public sealed class LayeredLayoutBuilderTests
{
  /// <summary>Tolerance for floating-point comparisons.</summary>
  private const float Tolerance = 0.01f;

  /// <summary>Gap between nodes used by the tests.</summary>
  private const float Gap = 60f;

  /// <summary>Node radius used by most tests.</summary>
  private const float Radius = 20f;

  /// <summary>Builder under test, always in rows, so the structure tests do not depend on the part's shape.</summary>
  private static readonly LayeredLayoutBuilder Builder = new(EnLayeredDirection.TopToBottom);

  /// <summary>Builder choosing the direction per part, as the layout picker uses it.</summary>
  private static readonly LayeredLayoutBuilder AutoBuilder = new();

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
  /// Computes the distance from a point to a line segment.
  /// </summary>
  /// <param name="point">Point.</param>
  /// <param name="a">Start of the segment.</param>
  /// <param name="b">End of the segment.</param>
  /// <returns>Shortest distance.</returns>
  private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
  {
    Vector2 ab = b - a;
    float t = Math.Clamp(Vector2.Dot(point - a, ab) / ab.LengthSquared(), 0f, 1f);
    return Vector2.Distance(point, a + ab * t);
  }

  /// <summary>
  /// Groups nodes into rows by their Y coordinate.
  /// </summary>
  /// <param name="positions">Node centers.</param>
  /// <returns>Row index of each node (0 = top).</returns>
  private static int[] Rows(Vector2[] positions)
  {
    float[] ys = positions.Select(position => MathF.Round(position.Y, 1)).Distinct().Order().ToArray();
    return positions.Select(position => Array.IndexOf(ys, MathF.Round(position.Y, 1))).ToArray();
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
  /// Verifies a chain from its first node runs top to bottom on one vertical line.
  /// </summary>
  [Fact]
  public void Place_ChainFromRoot_IsStraightTopToBottom()
  {
    // Arrange: Chain 0-1-2-3-4, root 0.
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (3, 4)];

    // Act: Place it.
    Vector2[] positions = Builder.Place(Radii(5), edges, Gap, [0]);

    // Assert: Y grows along the chain; all on one line.
    for (int i = 1; i < positions.Length; i++)
    {
      Assert.True(positions[i].Y > positions[i - 1].Y, $"Node {i} is not below node {i - 1}.");
      Assert.Equal(positions[0].X, positions[i].X, Tolerance);
    }
  }

  /// <summary>
  /// Verifies a marked root in the middle of a chain gets the top row and the rest follow by
  /// distance from it.
  /// </summary>
  [Fact]
  public void Place_MarkedRootInMiddle_RowsFollowDistance()
  {
    // Arrange: Chain 0-1-2-3-4, root 2.
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (3, 4)];

    // Act: Place it.
    int[] rows = Rows(Builder.Place(Radii(5), edges, Gap, [2]));

    // Assert: Root first, then 1 and 3, then 0 and 4.
    Assert.Equal([2, 1, 0, 1, 2], rows);
  }

  /// <summary>
  /// Verifies the marked root is used even when it is not the lowest node index.
  /// </summary>
  [Fact]
  public void Place_MarkedRoot_IsOnTop()
  {
    // Arrange: A star around node 0 with node 3 marked as root.
    (int From, int To)[] edges = [(0, 1), (0, 2), (0, 3)];

    // Act: Place it.
    int[] rows = Rows(Builder.Place(Radii(4), edges, Gap, [3]));

    // Assert: 3 alone in the top row, the hub next, the other leaves last.
    Assert.Equal([1, 2, 2, 0], rows);
  }

  /// <summary>
  /// Verifies that with several marked roots in one part the lowest index is used and the others
  /// are placed by their distance from it.
  /// </summary>
  [Fact]
  public void Place_SeveralRootsInOnePart_LowestIsUsed()
  {
    // Arrange: Loop 0-1-2-3-4-0 (no side rooms) with roots 3 and 1.
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 0)];

    // Act: Place it.
    int[] rows = Rows(Builder.Place(Radii(5), edges, Gap, [3, 1]));

    // Assert: 1 on top; 3 is two steps away like any other node.
    Assert.Equal([1, 0, 1, 2, 2], rows);
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

    // Assert: Same result; node 3 on top, then 1, 0, 2.
    Assert.Equal(first, second);
    Assert.Equal([2, 1, 3, 0], Rows(first));
  }

  /// <summary>
  /// Verifies the order inside rows removes a crossing the breadth-first order would have.
  /// </summary>
  [Fact]
  public void Place_CrossingInBreadthFirstOrder_IsRemoved()
  {
    // Arrange: Root 0 joins 1 and 2; 3 joins 1 and 2; 4 hangs off 1, 5 off 2. Breadth-first order
    // puts 3 left of 4 in the last row, which makes 2-3 cross 1-4.
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
    // Arrange: Mixed radii; a loop with branches and an edge inside a row (1-2).
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
  /// Verifies every edge joins nodes in the same or neighbouring rows.
  /// </summary>
  [Fact]
  public void Place_EdgesSkipNoRow()
  {
    // Arrange: A loop with a shortcut and a tail.
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 0), (1, 3), (4, 5), (5, 6)];

    // Act: Place it.
    int[] rows = Rows(Builder.Place(Radii(7), edges, Gap, [0]));

    // Assert: Row difference of every edge is at most one.
    Assert.All(edges, edge => Assert.True(Math.Abs(rows[edge.From] - rows[edge.To]) <= 1, $"Edge {edge} skips a row."));
  }

  /// <summary>
  /// Verifies separate parts are placed left to right, larger first, without overlapping, and
  /// their root rows line up.
  /// </summary>
  [Fact]
  public void Place_SeparateParts_ArePlacedLargestFirst()
  {
    // Arrange: A pair 0-1 and a chain 2-3-4, roots 0 and 2.
    (int From, int To)[] edges = [(0, 1), (2, 3), (3, 4)];

    // Act: Place it.
    Vector2[] positions = Builder.Place(Radii(5), edges, Gap, [0, 2]);

    // Assert: The chain (larger) is left of the pair, with the gap between; roots share Y.
    float chainRight = new[] { 2, 3, 4 }.Max(i => positions[i].X) + Radius;
    float pairLeft = new[] { 0, 1 }.Min(i => positions[i].X) - Radius;
    Assert.True(pairLeft - chainRight >= Gap - Tolerance);
    Assert.Equal(positions[2].Y, positions[0].Y, Tolerance);
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
  /// Verifies a main path with a side room at every step stays on one vertical line: the side
  /// rooms make way for the path instead of pushing it sideways row by row.
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

    // Assert: All spine nodes share the root's X.
    for (int i = 1; i < spineLength; i++)
    {
      Assert.Equal(positions[0].X, positions[i].X, Tolerance);
    }
  }

  /// <summary>
  /// Verifies that in automatic direction a deep part (a chain) is turned into columns: root on
  /// the left, on one horizontal line.
  /// </summary>
  [Fact]
  public void Place_AutoDeepChain_RunsLeftToRight()
  {
    // Arrange: Chain 0-1-2-3-4, root 0.
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (3, 4)];

    // Act: Place it with automatic direction.
    Vector2[] positions = AutoBuilder.Place(Radii(5), edges, Gap, [0]);

    // Assert: X grows along the chain; all on the root's line.
    for (int i = 1; i < positions.Length; i++)
    {
      Assert.True(positions[i].X > positions[i - 1].X, $"Node {i} is not right of node {i - 1}.");
      Assert.Equal(positions[0].Y, positions[i].Y, Tolerance);
    }
  }

  /// <summary>
  /// Verifies that in automatic direction a wide part (a star) stays in rows: root on top.
  /// </summary>
  [Fact]
  public void Place_AutoWideStar_RunsTopToBottom()
  {
    // Arrange: Root 0 with five leaves.
    (int From, int To)[] edges = [(0, 1), (0, 2), (0, 3), (0, 4), (0, 5)];

    // Act: Place it with automatic direction.
    Vector2[] positions = AutoBuilder.Place(Radii(6), edges, Gap, [0]);

    // Assert: Root in the top row, leaves together in the row below.
    Assert.Equal([0, 1, 1, 1, 1, 1], Rows(positions));
  }

  /// <summary>
  /// Verifies the forced left-to-right direction puts the root in the left column even for a
  /// wide part.
  /// </summary>
  [Fact]
  public void Place_LeftToRight_RootIsLeftmost()
  {
    // Arrange: Root 0 with five leaves; the forced direction.
    (int From, int To)[] edges = [(0, 1), (0, 2), (0, 3), (0, 4), (0, 5)];
    var builder = new LayeredLayoutBuilder(EnLayeredDirection.LeftToRight);

    // Act: Place it.
    Vector2[] positions = builder.Place(Radii(6), edges, Gap, [0]);

    // Assert: Every leaf is right of the root.
    for (int i = 1; i < positions.Length; i++) Assert.True(positions[i].X > positions[0].X, $"Leaf {i} is not right of the root.");
  }

  /// <summary>
  /// Verifies that in automatic direction a turned part and a part in rows keep their roots on
  /// one horizontal line.
  /// </summary>
  [Fact]
  public void Place_AutoMixedParts_RootsLineUp()
  {
    // Arrange: A chain 0-1-2-3-4-5-6 (turned) and a star 7 with leaves 8..12 (rows), roots 0 and 7.
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (7, 8), (7, 9), (7, 10), (7, 11), (7, 12)];

    // Act: Place it with automatic direction.
    Vector2[] positions = AutoBuilder.Place(Radii(13), edges, Gap, [0, 7]);

    // Assert: Both roots at the same height.
    Assert.Equal(positions[0].Y, positions[7].Y, Tolerance);
  }

  /// <summary>
  /// Verifies an unknown direction is rejected.
  /// </summary>
  [Fact]
  public void Constructor_UnknownDirection_Throws()
  {
    // Act + Assert: A value outside the enum.
    Assert.Throws<ArgumentOutOfRangeException>(() => new LayeredLayoutBuilder((EnLayeredDirection)99));
  }

  /// <summary>
  /// Verifies a node with a single neighbour (a side room) is placed between its neighbour's row
  /// and the next row, not on the next row where an edge runs along the row.
  /// </summary>
  [Fact]
  public void Place_SideRoom_IsBetweenRows()
  {
    // Arrange: Root 0 joins 1 and 2, which are joined to each other (an edge along row 1); 4 joins
    // 1 and 2 (row 2); 3 hangs off 1 only.
    (int From, int To)[] edges = [(0, 1), (0, 2), (1, 2), (1, 3), (1, 4), (2, 4)];

    // Act: Place it in rows.
    Vector2[] positions = Builder.Place(Radii(5), edges, Gap, [0]);

    // Assert: The side room is below its neighbour's row and above the next row.
    Assert.True(positions[3].Y > positions[1].Y + Radius, "Side room is not below row 1.");
    Assert.True(positions[3].Y < positions[4].Y - Radius, "Side room is not above row 2.");
  }

  /// <summary>
  /// Verifies side rooms between rows keep clear of the edges that cross between those rows, also
  /// along a straight main path.
  /// </summary>
  [Fact]
  public void Place_SideRooms_KeepClearOfEdges()
  {
    // Arrange: Spine 0-1-2-3-4-5; every spine node after the root has one side room (6..10).
    const int spineLength = 6;
    var edges = new List<(int From, int To)>();
    for (int i = 1; i < spineLength; i++)
    {
      edges.Add((i - 1, i));
      edges.Add((i, spineLength + i - 1));
    }

    // Act: Place it in rows.
    Vector2[] positions = Builder.Place(Radii(spineLength * 2 - 1), edges, Gap, [0]);

    // Assert: No side room touches a spine edge.
    for (int leaf = spineLength; leaf < positions.Length; leaf++)
    {
      for (int i = 1; i < spineLength; i++)
      {
        float distance = DistanceToSegment(positions[leaf], positions[i - 1], positions[i]);
        Assert.True(distance > Radius, $"Side room {leaf} lies on spine edge {i - 1}-{i} ({distance} from it).");
      }
    }
  }
}
