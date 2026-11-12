using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Headless tests for <see cref="GraphVisHitTest"/> (picking nodes and edges).
/// </summary>
public sealed class GraphVisHitTestTests
{
  /// <summary>Edge tolerance used by the tests, in world units.</summary>
  private const float EdgeTolerance = 4f;

  /// <summary>Two radius-20 nodes 400 apart on the X axis.</summary>
  private static readonly Vector2[] Positions = [new Vector2(0, 0), new Vector2(400, 0)];

  /// <summary>Radii of <see cref="Positions"/>.</summary>
  private static readonly float[] Radii = [20f, 20f];

  /// <summary>
  /// Verifies a point inside a node hits it, also when an edge starts right there.
  /// </summary>
  [Fact]
  public void Find_PointInsideNode_HitsNodeBeforeEdge()
  {
    // Act: Point just inside node 0, where the edge leaves it.
    GraphVisHit hit = GraphVisHitTest.Find(new Vector2(19, 0), Positions, Radii, [new GraphVisEdgeShape(0, 1, 0f)], EdgeTolerance);

    // Assert: The node.
    Assert.Equal(GraphVisHit.ForNode(0), hit);
  }

  /// <summary>
  /// Verifies of two overlapping nodes the one drawn on top (higher index) is hit.
  /// </summary>
  [Fact]
  public void Find_OverlappingNodes_HitsTopmost()
  {
    // Act: Point inside both of two overlapping nodes.
    GraphVisHit hit = GraphVisHitTest.Find(new Vector2(10, 0), [new Vector2(0, 0), new Vector2(20, 0)], [20f, 20f], [], EdgeTolerance);

    // Assert: Node 1 (drawn later).
    Assert.Equal(GraphVisHit.ForNode(1), hit);
  }

  /// <summary>
  /// Verifies a point near a straight edge hits it within the tolerance, and nothing beyond it.
  /// </summary>
  /// <param name="offset">Distance of the point from the edge line.</param>
  /// <param name="hits">Whether the edge is hit.</param>
  [Theory]
  [InlineData(0f, true)]
  [InlineData(3.9f, true)]
  [InlineData(4.1f, false)]
  public void Find_NearStraightEdge_HitsWithinTolerance(float offset, bool hits)
  {
    // Act: Point beside the middle of the edge.
    GraphVisHit hit = GraphVisHitTest.Find(new Vector2(200, offset), Positions, Radii, [new GraphVisEdgeShape(0, 1, 0f)], EdgeTolerance);

    // Assert: Edge or nothing.
    Assert.Equal(hits ? GraphVisHit.ForEdge(0) : GraphVisHit.None, hit);
  }

  /// <summary>
  /// Verifies two parallel arcs are picked by their own curves: each arc's middle hits that arc,
  /// and the straight line between the nodes (between the arcs) hits neither.
  /// </summary>
  [Fact]
  public void Find_ParallelArcs_HitAlongTheCurves()
  {
    // Arrange: Two edges between the same nodes, bent apart like the renderer does.
    float[] bends = GraphVisGeometry.ParallelEdgeBends([(0, 1), (0, 1)], 24f);
    GraphVisEdgeShape[] edges = [new(0, 1, bends[0]), new(0, 1, bends[1])];
    Vector2 firstMiddle = GraphVisGeometry.EdgeCurve(Positions[0], 20f, Positions[1], 20f, bends[0])!.Value.Middle;
    Vector2 secondMiddle = GraphVisGeometry.EdgeCurve(Positions[0], 20f, Positions[1], 20f, bends[1])!.Value.Middle;

    // Act + Assert: Each arc at its middle, nothing on the straight line.
    Assert.Equal(GraphVisHit.ForEdge(0), GraphVisHitTest.Find(firstMiddle, Positions, Radii, edges, EdgeTolerance));
    Assert.Equal(GraphVisHit.ForEdge(1), GraphVisHitTest.Find(secondMiddle, Positions, Radii, edges, EdgeTolerance));
    Assert.Equal(GraphVisHit.None, GraphVisHitTest.Find(new Vector2(200, 0), Positions, Radii, edges, EdgeTolerance));
  }

  /// <summary>
  /// Verifies of two edges within the tolerance the closer one is hit.
  /// </summary>
  [Fact]
  public void Find_TwoEdgesInReach_HitsCloser()
  {
    // Arrange: Two straight edges 6 apart (from nodes at y = 0 and y = 6).
    Vector2[] positions = [new Vector2(0, 0), new Vector2(400, 0), new Vector2(0, 6), new Vector2(400, 6)];
    float[] radii = [5f, 5f, 5f, 5f];
    GraphVisEdgeShape[] edges = [new(0, 1, 0f), new(2, 3, 0f)];

    // Act: Point 2 from the first edge, 4 from the second.
    GraphVisHit hit = GraphVisHitTest.Find(new Vector2(200, 2), positions, radii, edges, EdgeTolerance);

    // Assert: The first edge.
    Assert.Equal(GraphVisHit.ForEdge(0), hit);
  }

  /// <summary>
  /// Verifies empty space and edges between touching nodes (not drawn) are not hit.
  /// </summary>
  [Fact]
  public void Find_EmptySpaceOrHiddenEdge_HitsNothing()
  {
    // Act + Assert: Far from everything.
    Assert.Equal(GraphVisHit.None, GraphVisHitTest.Find(new Vector2(200, 100), Positions, Radii, [new GraphVisEdgeShape(0, 1, 0f)], EdgeTolerance));

    // Act + Assert: Edge between overlapping nodes is not drawn, so the point beside both nodes misses.
    Vector2[] touching = [new Vector2(0, 0), new Vector2(30, 0)];
    Assert.Equal(GraphVisHit.None, GraphVisHitTest.Find(new Vector2(15, 21), touching, Radii, [new GraphVisEdgeShape(0, 1, 0f)], EdgeTolerance));
  }

  /// <summary>
  /// Verifies invalid arguments are rejected.
  /// </summary>
  [Fact]
  public void Find_WithInvalidArguments_Throws()
  {
    // Act + Assert: Mismatched lengths, edge to a missing node, negative tolerance, null list.
    Assert.Throws<ArgumentException>(() => GraphVisHitTest.Find(Vector2.Zero, Positions, [20f], [], EdgeTolerance));
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphVisHitTest.Find(new Vector2(200, 300), Positions, Radii, [new GraphVisEdgeShape(0, 2, 0f)], EdgeTolerance));
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphVisHitTest.Find(Vector2.Zero, Positions, Radii, [], -1f));
    Assert.Throws<ArgumentNullException>(() => GraphVisHitTest.Find(Vector2.Zero, null!, Radii, [], EdgeTolerance));
  }
}
