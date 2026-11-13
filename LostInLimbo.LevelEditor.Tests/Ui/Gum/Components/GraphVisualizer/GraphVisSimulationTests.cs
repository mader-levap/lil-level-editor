using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Headless tests for <see cref="GraphVisSimulation"/> (live node physics).
/// </summary>
public sealed class GraphVisSimulationTests
{
  /// <summary>Physics constants used by the tests (the defaults).</summary>
  private static readonly GraphVisStyle Style = GraphVisStyle.Default;

  /// <summary>
  /// Tolerance for distances after settling, in world units. The simulation falls asleep once
  /// movement is imperceptible, which is slightly before a distance is reached exactly.
  /// </summary>
  private const float SettledTolerance = 1.5f;

  /// <summary>Tolerance for exact floating-point comparisons.</summary>
  private const float Tolerance = 0.001f;

  /// <summary>Step limit generous enough for every small test to settle.</summary>
  private const int ManySteps = 2000;

  /// <summary>
  /// Verifies two nodes that are too close are pushed apart until they keep the gap.
  /// </summary>
  /// <param name="startDistance">Distance between the centers at the start.</param>
  [Theory]
  [InlineData(0f)] // same spot: there is no direction to part in, a fixed one is used
  [InlineData(10f)] // overlapping
  [InlineData(50f)] // not overlapping, but closer than the gap
  public void Settle_NodesTooClose_ArePushedApartToTheGap(float startDistance)
  {
    // Arrange: Two unconnected radius-20 nodes.
    var simulation = new GraphVisSimulation([Vector2.Zero, new Vector2(startDistance, 0)], [20f, 20f], [], Style);

    // Act: Let it settle.
    bool asleep = simulation.Settle(ManySteps);

    // Assert: Asleep, and the borders are the node gap apart.
    Assert.True(asleep);
    float distance = Vector2.Distance(simulation.Positions[0], simulation.Positions[1]);
    Assert.Equal(40f + Style.NodeGap, distance, SettledTolerance);
  }

  /// <summary>
  /// Verifies connected nodes that are too far apart are pulled within the rest length.
  /// </summary>
  [Fact]
  public void Settle_ConnectedNodesTooFar_ArePulledToRestLength()
  {
    // Arrange: Two connected radius-20 nodes far apart.
    var simulation = new GraphVisSimulation([Vector2.Zero, new Vector2(800, 0)], [20f, 20f], [(0, 1)], Style);

    // Act: Let it settle.
    bool asleep = simulation.Settle(ManySteps);

    // Assert: Asleep at the rest length (both radii plus the rest gap).
    Assert.True(asleep);
    float rest = 40f + Style.NodeGap * Style.LinkRestGapFactor;
    Assert.Equal(rest, Vector2.Distance(simulation.Positions[0], simulation.Positions[1]), SettledTolerance);
  }

  /// <summary>
  /// Verifies nothing moves when no rule is broken: unconnected nodes far apart, and connected
  /// nodes between "too close" and "too far".
  /// </summary>
  /// <param name="distance">Distance between the centers.</param>
  /// <param name="connected">Whether the nodes are connected.</param>
  [Theory]
  [InlineData(800f, false)]
  [InlineData(100f, true)] // rules for radius 20: too close below 70, too far above 130
  public void Settle_NodesWithinRules_DoNotMoveAndSleepAtOnce(float distance, bool connected)
  {
    // Arrange: Two radius-20 nodes that break no rule.
    Vector2[] start = [Vector2.Zero, new Vector2(distance, 0)];
    (int, int)[] edges = connected ? [(0, 1)] : [];
    var simulation = new GraphVisSimulation(start, [20f, 20f], edges, Style);

    // Act: Run a single step.
    bool asleep = simulation.Settle(1);

    // Assert: Asleep after one step, positions untouched.
    Assert.True(asleep);
    Assert.Equal(start, simulation.Positions);
  }

  /// <summary>
  /// Verifies the mass rule: when two nodes push each other, the lighter one moves more, in
  /// inverse proportion to the masses (mass is the circle area).
  /// </summary>
  [Fact]
  public void Settle_HeavyAndLightNode_MoveInInverseProportionToMass()
  {
    // Arrange: Radius 10 (mass 100) and radius 30 (mass 900), too close.
    Vector2[] start = [Vector2.Zero, new Vector2(50, 0)];
    var simulation = new GraphVisSimulation(start, [10f, 30f], [], Style);

    // Act: Run a single step.
    simulation.Settle(1);

    // Assert: The small node moved nine times as far as the large one, in opposite directions.
    float smallMove = start[0].X - simulation.Positions[0].X;
    float largeMove = simulation.Positions[1].X - start[1].X;
    Assert.True(smallMove > 0 && largeMove > 0);
    Assert.Equal(9f, smallMove / largeMove, 0.01f);
  }

  /// <summary>
  /// Verifies a large node pulls a connected small node along, while the small node barely
  /// moves the large one.
  /// </summary>
  [Fact]
  public void Settle_SmallNodeConnectedToLargeNode_DoesMostOfTheMoving()
  {
    // Arrange: Small and large node, connected, far apart.
    Vector2[] start = [Vector2.Zero, new Vector2(1000, 0)];
    var simulation = new GraphVisSimulation(start, [10f, 30f], [(0, 1)], Style);

    // Act: Let it settle.
    simulation.Settle(ManySteps);

    // Assert: The small node travelled nine times as far as the large one.
    float smallTravel = Vector2.Distance(start[0], simulation.Positions[0]);
    float largeTravel = Vector2.Distance(start[1], simulation.Positions[1]);
    Assert.Equal(9f, smallTravel / largeTravel, 0.05f);
  }

  /// <summary>
  /// Verifies a pinned node (the one being dragged) stays put and its partner takes the whole movement.
  /// </summary>
  [Fact]
  public void Settle_PinnedNode_DoesNotMove()
  {
    // Arrange: Two overlapping nodes, the lighter one pinned.
    Vector2[] start = [Vector2.Zero, new Vector2(20, 0)];
    var simulation = new GraphVisSimulation(start, [10f, 30f], [], Style) { PinnedNode = 0 };

    // Act: Let it settle.
    simulation.Settle(ManySteps);

    // Assert: The pinned node is where it was; the heavy one made all the room.
    Assert.Equal(start[0], simulation.Positions[0]);
    Assert.Equal(40f + Style.NodeGap, simulation.Positions[1].X, SettledTolerance);
  }

  /// <summary>
  /// Verifies circles never overlap after a step, even when the soft push alone would not have
  /// separated them yet.
  /// </summary>
  [Fact]
  public void Settle_SingleStep_RemovesOverlapCompletely()
  {
    // Arrange: Two radius-20 nodes almost on top of each other.
    var simulation = new GraphVisSimulation([Vector2.Zero, new Vector2(1, 0)], [20f, 20f], [], Style);

    // Act: Run a single step.
    simulation.Settle(1);

    // Assert: The circles no longer overlap.
    Assert.True(Vector2.Distance(simulation.Positions[0], simulation.Positions[1]) >= 40f - Tolerance);
  }

  /// <summary>
  /// Verifies moving a node (dragging) wakes a sleeping simulation, and that neighbours follow.
  /// </summary>
  [Fact]
  public void MoveNode_WakesSimulationAndNeighboursFollow()
  {
    // Arrange: Two connected nodes at rest.
    var simulation = new GraphVisSimulation([Vector2.Zero, new Vector2(100, 0)], [20f, 20f], [(0, 1)], Style);
    simulation.Settle(1);
    Assert.True(simulation.IsAsleep);

    // Act: Drag node 0 far away, pinned, and let the rest settle.
    simulation.PinnedNode = 0;
    simulation.MoveNode(0, new Vector2(-600, 0));
    bool awakeAfterMove = !simulation.IsAsleep;
    simulation.Settle(ManySteps);

    // Assert: It woke up, the dragged node stayed where it was put, and the neighbour followed to the rest length.
    Assert.True(awakeAfterMove);
    Assert.Equal(new Vector2(-600, 0), simulation.Positions[0]);
    float rest = 40f + Style.NodeGap * Style.LinkRestGapFactor;
    Assert.Equal(rest, Vector2.Distance(simulation.Positions[0], simulation.Positions[1]), SettledTolerance);
  }

  /// <summary>
  /// Verifies time-based stepping: time shorter than a step accumulates instead of being lost,
  /// and nothing happens while asleep.
  /// </summary>
  [Fact]
  public void Step_AccumulatesTimeIntoFixedSteps()
  {
    // Arrange: Two overlapping nodes.
    Vector2[] start = [Vector2.Zero, new Vector2(10, 0)];
    var simulation = new GraphVisSimulation(start, [20f, 20f], [], Style);
    float halfStep = Style.SimulationStepSeconds / 2f;

    // Act + Assert: Half a step of time does nothing yet.
    simulation.Step(halfStep);
    Assert.Equal(start, simulation.Positions);

    // Act + Assert: The second half completes a step and the nodes move.
    simulation.Step(halfStep * 1.01f);
    Assert.NotEqual(start[1], simulation.Positions[1]);

    // Act + Assert: Once asleep, more time changes nothing.
    simulation.Settle(ManySteps);
    Vector2[] settled = [.. simulation.Positions];
    simulation.Step(1f);
    Assert.Equal(settled, simulation.Positions);
  }

  /// <summary>
  /// Verifies a graph of the largest expected size, started from the stress layout, comes to
  /// rest within the pre-show step budget and without overlapping nodes.
  /// </summary>
  [Fact]
  public void Settle_LargeGraphFromLayout_SleepsWithoutOverlaps()
  {
    // Arrange: 100 nodes of mixed sizes linked as a 10x10 grid (deterministic, and like a level:
    // links join nearby places, so the links can all be satisfied on a plane).
    const int side = 10;
    const int count = side * side;
    float[] radii = Enumerable.Range(0, count).Select(i => 14f + i * 7 % 27).ToArray();
    var edges = new List<(int, int)>();
    for (int row = 0; row < side; row++)
    {
      for (int column = 0; column < side; column++)
      {
        int node = row * side + column;
        if (column + 1 < side) edges.Add((node, node + 1));
        if (row + 1 < side) edges.Add((node, node + side));
      }
    }

    Vector2[] start = GraphVisLayout.Place(EnGraphLayoutBuilder.Stress, radii, edges, Style.NodeGap * Style.LinkIdealGapFactor);
    var simulation = new GraphVisSimulation(start, radii, edges, Style);

    // Act: Settle with the budget used before a graph is first shown.
    bool asleep = simulation.Settle(Style.SimulationSettleSteps);

    // Assert: At rest, and no two circles overlap.
    Assert.True(asleep);
    for (int i = 0; i < count; i++)
    {
      for (int j = i + 1; j < count; j++)
      {
        float distance = Vector2.Distance(simulation.Positions[i], simulation.Positions[j]);
        Assert.True(distance >= radii[i] + radii[j] - 0.01f, $"Nodes {i} and {j} overlap.");
      }
    }
  }

  /// <summary>
  /// Verifies invalid arguments are rejected.
  /// </summary>
  [Fact]
  public void Constructor_WithInvalidArguments_Throws()
  {
    // Act + Assert: Mismatched lengths, edge to a missing node, null style, bad node index.
    Assert.Throws<ArgumentException>(() => new GraphVisSimulation([Vector2.Zero], [20f, 20f], [], Style));
    Assert.Throws<ArgumentOutOfRangeException>(() => new GraphVisSimulation([Vector2.Zero], [20f], [(0, 1)], Style));
    Assert.Throws<ArgumentNullException>(() => new GraphVisSimulation([Vector2.Zero], [20f], [], null!));
    Assert.Throws<ArgumentOutOfRangeException>(() => new GraphVisSimulation([Vector2.Zero], [20f], [], Style).MoveNode(1, Vector2.Zero));
  }
}
