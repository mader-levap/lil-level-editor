using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Headless tests for <see cref="RingLayoutBuilder"/> (nodes on a ring).
/// </summary>
public sealed class RingLayoutBuilderTests
{
  /// <summary>Tolerance for floating-point comparisons.</summary>
  private const float Tolerance = 0.01f;

  /// <summary>Gap between nodes used by the tests.</summary>
  private const float Gap = 30f;

  /// <summary>Builder under test.</summary>
  private static readonly RingLayoutBuilder Builder = new();

  /// <summary>
  /// Lists the nodes in the order they sit on the ring: clockwise on screen (Y down), starting
  /// at the top.
  /// </summary>
  /// <param name="positions">Node centers around the origin.</param>
  /// <returns>Node indices in ring order.</returns>
  private static int[] RingSequence(Vector2[] positions)
  {
    // Angle measured clockwise from straight up; screen Y grows downwards.
    static float ClockwiseFromTop(Vector2 position)
    {
      float angle = MathF.Atan2(position.X, -position.Y);
      return angle < 0 ? angle + MathF.Tau : angle;
    }

    return Enumerable.Range(0, positions.Length).OrderBy(i => ClockwiseFromTop(positions[i])).ToArray();
  }

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
  /// Verifies no nodes give no positions.
  /// </summary>
  [Fact]
  public void Place_WithoutNodes_IsEmpty()
  {
    // Act + Assert: Nothing to place.
    Assert.Empty(Builder.Place([], [], Gap));
  }

  /// <summary>
  /// Verifies a single node sits at the origin.
  /// </summary>
  [Fact]
  public void Place_SingleNode_IsAtOrigin()
  {
    // Act: Place one node.
    Vector2[] positions = Builder.Place([20f], [], Gap);

    // Assert: It is at the origin.
    Assert.Equal(Vector2.Zero, Assert.Single(positions));
  }

  /// <summary>
  /// Verifies no two nodes come closer than the gap, for few and many nodes and for very
  /// different sizes (two nodes are the tightest case: the ring must grow beyond its arc length).
  /// </summary>
  /// <param name="radii">Node radii.</param>
  [Theory]
  [InlineData(new[] { 20f, 20f })]
  [InlineData(new[] { 14f, 70f })]
  [InlineData(new[] { 20f, 20f, 20f })]
  [InlineData(new[] { 70f, 14f, 14f, 14f, 70f, 14f })]
  [InlineData(new[] { 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f })]
  public void Place_KeepsGapBetweenAllNodes(float[] radii)
  {
    // Act: Place the nodes.
    Vector2[] positions = Builder.Place(radii, [], Gap);

    // Assert: Every pair of circles is at least the gap apart.
    Assert.Equal(radii.Length, positions.Length);
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
  /// Verifies the first node is at the top of the ring and the ring is centered on the origin.
  /// </summary>
  [Fact]
  public void Place_EqualNodes_StartsAtTopAndIsCentered()
  {
    // Act: Place four equal nodes.
    Vector2[] positions = Builder.Place([20f, 20f, 20f, 20f], [], Gap);

    // Assert: All on one ring around the origin; their centers average to the origin.
    float ringRadius = positions[0].Length();
    Assert.All(positions, position => Assert.Equal(ringRadius, position.Length(), Tolerance));
    Vector2 sum = positions.Aggregate(Vector2.Zero, (total, position) => total + position);
    Assert.Equal(0f, sum.Length(), Tolerance);

    // Assert: Clockwise on screen (Y down) starting in the top-right quadrant: the first node's arc starts at the top.
    Assert.True(positions[0].Y < 0 && positions[0].X > 0);
    Assert.True(positions[1].Y > 0 && positions[1].X > 0);
  }

  /// <summary>
  /// Verifies placement is deterministic.
  /// </summary>
  [Fact]
  public void Place_SameInput_GivesSamePositions()
  {
    // Arrange: Mixed radii.
    float[] radii = [14f, 30f, 70f, 22f, 22f];

    // Act + Assert: Two runs agree exactly.
    Assert.Equal(Builder.Place(radii, [], Gap), Builder.Place(radii, [], Gap));
  }

  /// <summary>
  /// Verifies a chain with scrambled node indices is laid along the ring in chain order, so
  /// every edge joins ring neighbours.
  /// </summary>
  [Fact]
  public void Place_ScrambledChain_IsAdjacentOnRing()
  {
    // Arrange: Chain 0-3-1-4-2.
    (int From, int To)[] edges = [(0, 3), (3, 1), (1, 4), (4, 2)];

    // Act: Place five equal nodes.
    int[] sequence = RingSequence(Builder.Place([20f, 20f, 20f, 20f, 20f], edges, Gap));

    // Assert: The ring follows the chain.
    Assert.Equal([0, 3, 1, 4, 2], sequence);
  }

  /// <summary>
  /// Verifies a loop with scrambled node indices comes out as a polygon without crossing chords.
  /// </summary>
  [Fact]
  public void Place_ScrambledLoop_HasNoCrossingEdges()
  {
    // Arrange: Loop 0-5-2-7-1-6-3-4-0.
    int[] loop = [0, 5, 2, 7, 1, 6, 3, 4];
    (int From, int To)[] edges = loop.Select((node, i) => (node, loop[(i + 1) % loop.Length])).ToArray();

    // Act: Place eight equal nodes.
    Vector2[] positions = Builder.Place(Enumerable.Repeat(20f, loop.Length).ToArray(), edges, Gap);

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
  /// Verifies a dead end is walked before the main path goes on, so it sits right next to the
  /// node it hangs off.
  /// </summary>
  [Fact]
  public void Place_Branch_SitsBesideItsParent()
  {
    // Arrange: Main path 0-1-2-3 and leaf 4 hanging off node 1.
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (1, 4)];

    // Act: Place five equal nodes.
    int[] sequence = RingSequence(Builder.Place([20f, 20f, 20f, 20f, 20f], edges, Gap));

    // Assert: Path start, then the leaf beside its parent, then the rest of the path.
    Assert.Equal([0, 1, 4, 2, 3], sequence);
  }

  /// <summary>
  /// Verifies separate parts of the graph each take one unbroken stretch of the ring.
  /// </summary>
  [Fact]
  public void Place_SeparateParts_AreContiguous()
  {
    // Arrange: Pairs 0-2 and 1-3.
    (int From, int To)[] edges = [(0, 2), (1, 3)];

    // Act: Place four equal nodes.
    int[] sequence = RingSequence(Builder.Place([20f, 20f, 20f, 20f], edges, Gap));

    // Assert: Each pair together, the part with the lowest node first.
    Assert.Equal([0, 2, 1, 3], sequence);
  }

  /// <summary>
  /// Verifies the lowest node index still starts the ring at the top when edges reorder the rest.
  /// </summary>
  [Fact]
  public void Place_WithEdges_LowestNodeStartsAtTop()
  {
    // Act: Place a scrambled chain 0-3-1-4-2.
    Vector2[] positions = Builder.Place([20f, 20f, 20f, 20f, 20f], [(0, 3), (3, 1), (1, 4), (4, 2)], Gap);

    // Assert: Node 0 is first clockwise from the top (top-right quadrant).
    Assert.True(positions[0].Y < 0 && positions[0].X > 0);
  }

  /// <summary>
  /// Verifies the gap between all nodes holds when edges reorder the ring (the ring must grow
  /// for nodes that are ring neighbours, not index neighbours).
  /// </summary>
  /// <param name="radii">Node radii.</param>
  [Theory]
  [InlineData(new[] { 20f, 20f })]
  [InlineData(new[] { 14f, 70f, 14f })]
  [InlineData(new[] { 70f, 14f, 14f, 14f, 70f, 14f })]
  [InlineData(new[] { 14f, 70f, 14f, 70f, 14f, 70f, 14f, 70f })]
  public void Place_WithEdges_KeepsGapBetweenAllNodes(float[] radii)
  {
    // Arrange: Chain 0, n-1, n-2, ..., 1: the ring order differs from the index order.
    int last = radii.Length - 1;
    (int From, int To)[] edges = [(0, last), .. Enumerable.Range(2, last - 1).Reverse().Select(i => (i, i - 1))];

    // Act: Place the nodes.
    Vector2[] positions = Builder.Place(radii, edges, Gap);

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
  /// Verifies placement with edges is deterministic.
  /// </summary>
  [Fact]
  public void Place_WithEdges_SameInput_GivesSamePositions()
  {
    // Arrange: Mixed radii and a loop with a tail.
    float[] radii = [14f, 30f, 70f, 22f, 22f];
    (int From, int To)[] edges = [(0, 2), (2, 4), (4, 0), (4, 1), (1, 3)];

    // Act + Assert: Two runs agree exactly.
    Assert.Equal(Builder.Place(radii, edges, Gap), Builder.Place(radii, edges, Gap));
  }

  /// <summary>
  /// Verifies the ring keeps its shape under its own default physics, even when many edges
  /// cross it (with standard edge pull such a graph clumps).
  /// </summary>
  [Fact]
  public void Place_WithDefaultPhysics_RingKeepsItsShape()
  {
    // Arrange: Ten equal nodes; a loop around the ring plus chords across it.
    const int count = 10;
    float[] radii = Enumerable.Repeat(22f, count).ToArray();
    (int From, int To)[] edges =
    [
      .. Enumerable.Range(0, count).Select(i => (i, (i + 1) % count)),
      (0, 5), (1, 6), (2, 7), (3, 8), (4, 9),
    ];
    GraphVisPhysics physics = Builder.DefaultPhysics;
    Vector2[] start = Builder.Place(radii, edges, physics.LinkIdealGap);

    // Act: Simulate until settled.
    var simulation = new GraphVisSimulation(start, radii, edges, physics);
    simulation.Settle(physics.SettleSteps);

    // Assert: Every node is still where the ring put it.
    for (int i = 0; i < count; i++)
    {
      Assert.True(Vector2.Distance(start[i], simulation.Positions[i]) < 1f, $"Node {i} moved away from the ring.");
    }
  }
}
