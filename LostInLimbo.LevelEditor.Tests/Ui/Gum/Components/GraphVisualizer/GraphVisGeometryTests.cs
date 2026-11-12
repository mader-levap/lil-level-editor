using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Headless tests for <see cref="GraphVisGeometry"/> (node radii, edge lines, arrowheads, markers).
/// </summary>
public sealed class GraphVisGeometryTests
{
  /// <summary>Tolerance for floating-point comparisons.</summary>
  private const float Tolerance = 0.001f;

  /// <summary>Style with round numbers, so expected values are easy to read.</summary>
  private static readonly GraphVisStyle Style = new() { NodeBaseRadius = 20f, NodeMinRadius = 10f, NodeMaxRadius = 50f };

  /// <summary>
  /// Asserts two points are equal within <see cref="Tolerance"/>.
  /// </summary>
  /// <param name="expected">Expected point.</param>
  /// <param name="actual">Actual point.</param>
  private static void AssertClose(Vector2 expected, Vector2 actual)
  {
    Assert.Equal(expected.X, actual.X, Tolerance);
    Assert.Equal(expected.Y, actual.Y, Tolerance);
  }

  /// <summary>
  /// Creates markers named after their identifiers.
  /// </summary>
  /// <param name="ids">Marker identifiers.</param>
  /// <returns>Markers.</returns>
  private static GraphVisMarker[] Markers(params string[] ids) => ids.Select(id => new GraphVisMarker(id, $"label-{id}")).ToArray();

  #region Node radius

  /// <summary>
  /// Verifies the radius grows with the square root of the size (area proportional to size) and
  /// is limited at both ends.
  /// </summary>
  /// <param name="relativeSize">Relative size.</param>
  /// <param name="expectedRadius">Expected radius.</param>
  [Theory]
  [InlineData(1f, 20f)]
  [InlineData(4f, 40f)]
  [InlineData(0.01f, 10f)] // 2 before clamping: raised to the minimum
  [InlineData(100f, 50f)] // 200 before clamping: lowered to the maximum
  public void NodeRadius_ScalesWithSquareRootAndClamps(float relativeSize, float expectedRadius)
  {
    // Act + Assert: Radius for the size.
    Assert.Equal(expectedRadius, GraphVisGeometry.NodeRadius(relativeSize, Style), Tolerance);
  }

  /// <summary>
  /// Verifies unusable sizes are rejected.
  /// </summary>
  /// <param name="relativeSize">Invalid relative size.</param>
  [Theory]
  [InlineData(0f)]
  [InlineData(-2f)]
  [InlineData(float.NaN)]
  public void NodeRadius_WithInvalidSize_Throws(float relativeSize)
  {
    // Act + Assert: Non-positive or non-finite size is rejected.
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphVisGeometry.NodeRadius(relativeSize, Style));
  }

  #endregion

  #region Edges

  /// <summary>
  /// Verifies an edge runs between the two circle borders, not the centers.
  /// </summary>
  [Fact]
  public void EdgeSegment_RunsFromBorderToBorder()
  {
    // Act: Edge between a radius-10 node at the origin and a radius-20 node 100 to the right.
    GraphVisSegment? segment = GraphVisGeometry.EdgeSegment(Vector2.Zero, 10f, new Vector2(100, 0), 20f);

    // Assert: The line covers 10..80 and its middle is at 45.
    Assert.NotNull(segment);
    AssertClose(new Vector2(10, 0), segment.Value.Start);
    AssertClose(new Vector2(80, 0), segment.Value.End);
    AssertClose(new Vector2(45, 0), segment.Value.Middle);
    Assert.Equal(70f, segment.Value.Length, Tolerance);
  }

  /// <summary>
  /// Verifies touching, overlapping and coincident nodes have no visible edge.
  /// </summary>
  /// <param name="distance">Distance between the node centers.</param>
  [Theory]
  [InlineData(30f)] // touching
  [InlineData(15f)] // overlapping
  [InlineData(0f)] // same position
  public void EdgeSegment_WhenCirclesTouchOrOverlap_ReturnsNull(float distance)
  {
    // Act + Assert: Nothing to draw between radius-10 and radius-20 nodes this close.
    Assert.Null(GraphVisGeometry.EdgeSegment(Vector2.Zero, 10f, new Vector2(distance, 0), 20f));
  }

  /// <summary>
  /// Verifies the line is trimmed only at ends that carry an arrowhead.
  /// </summary>
  /// <param name="arrowAtStart">Arrowhead at the start.</param>
  /// <param name="arrowAtEnd">Arrowhead at the end.</param>
  /// <param name="expectedStartX">Expected line start.</param>
  /// <param name="expectedEndX">Expected line end.</param>
  [Theory]
  [InlineData(false, false, 0f, 100f)]
  [InlineData(true, false, 12f, 100f)]
  [InlineData(false, true, 0f, 88f)]
  [InlineData(true, true, 12f, 88f)]
  public void LineBetweenArrows_TrimsArrowEnds(bool arrowAtStart, bool arrowAtEnd, float expectedStartX, float expectedEndX)
  {
    // Arrange: 100-long horizontal segment, 12-long arrowheads.
    var segment = new GraphVisSegment(Vector2.Zero, new Vector2(100, 0));

    // Act: Trim.
    GraphVisSegment? line = GraphVisGeometry.LineBetweenArrows(segment, arrowAtStart, arrowAtEnd, 12f);

    // Assert: Ends moved inward by the arrow length where an arrow sits.
    Assert.NotNull(line);
    AssertClose(new Vector2(expectedStartX, 0), line.Value.Start);
    AssertClose(new Vector2(expectedEndX, 0), line.Value.End);
  }

  /// <summary>
  /// Verifies no line is drawn when the arrowheads use up the whole segment.
  /// </summary>
  [Fact]
  public void LineBetweenArrows_WhenArrowsFillSegment_ReturnsNull()
  {
    // Arrange: 20-long segment, two 12-long arrowheads.
    var segment = new GraphVisSegment(Vector2.Zero, new Vector2(20, 0));

    // Act + Assert: No room for a line.
    Assert.Null(GraphVisGeometry.LineBetweenArrows(segment, true, true, 12f));
  }

  /// <summary>
  /// Verifies the arrowhead points along the edge with a base perpendicular to it.
  /// </summary>
  [Fact]
  public void ArrowHead_PointsAlongDirection()
  {
    // Act: Arrow at (100,0) coming from the origin, 12 long, base half-width 6.
    GraphVisArrowHead? arrow = GraphVisGeometry.ArrowHead(new Vector2(100, 0), Vector2.Zero, 12f, 6f);

    // Assert: Tip at the end; base corners 12 behind it, 6 to each side.
    Assert.NotNull(arrow);
    AssertClose(new Vector2(100, 0), arrow.Value.Tip);
    AssertClose(new Vector2(88, 6), arrow.Value.BaseLeft);
    AssertClose(new Vector2(88, -6), arrow.Value.BaseRight);
  }

  /// <summary>
  /// Verifies the arrowhead follows a diagonal direction (base stays perpendicular and symmetric).
  /// </summary>
  [Fact]
  public void ArrowHead_OnDiagonal_IsSymmetricAroundTheLine()
  {
    // Act: Arrow pointing down-right.
    GraphVisArrowHead? arrow = GraphVisGeometry.ArrowHead(new Vector2(30, 40), Vector2.Zero, 10f, 5f);

    // Assert: Base midpoint lies on the line, 10 before the tip; both corners are 5 from it.
    Assert.NotNull(arrow);
    Vector2 baseMiddle = (arrow.Value.BaseLeft + arrow.Value.BaseRight) / 2f;
    AssertClose(new Vector2(24, 32), baseMiddle);
    Assert.Equal(5f, Vector2.Distance(arrow.Value.BaseLeft, baseMiddle), Tolerance);
    Assert.Equal(5f, Vector2.Distance(arrow.Value.BaseRight, baseMiddle), Tolerance);
  }

  /// <summary>
  /// Verifies there is no arrowhead without a direction.
  /// </summary>
  [Fact]
  public void ArrowHead_WithoutDirection_ReturnsNull()
  {
    // Act + Assert: Tip and origin coincide.
    Assert.Null(GraphVisGeometry.ArrowHead(Vector2.One, Vector2.One, 10f, 5f));
  }

  #endregion

  #region Markers

  /// <summary>
  /// Verifies a node without markers shows no dots.
  /// </summary>
  [Fact]
  public void MarkerDots_WithoutMarkers_IsEmpty()
  {
    // Act + Assert: Nothing to show.
    Assert.Empty(GraphVisGeometry.MarkerDots([], 3));
  }

  /// <summary>
  /// Verifies up to the limit every marker gets its own labelled dot.
  /// </summary>
  [Fact]
  public void MarkerDots_UpToLimit_ShowsEachMarker()
  {
    // Act: Three markers with a limit of three.
    IReadOnlyList<GraphVisMarkerDot> dots = GraphVisGeometry.MarkerDots(Markers("a", "b", "c"), 3);

    // Assert: One dot per marker, in order, carrying label and identifier.
    Assert.Equal(3, dots.Count);
    Assert.Equal(["label-a", "label-b", "label-c"], dots.Select(dot => dot.Label));
    Assert.All(dots, dot => Assert.False(dot.IsCollapsed));
    Assert.Equal(["b"], dots[1].MarkerIds);
  }

  /// <summary>
  /// Verifies more markers than the limit collapse into one dot labelled with the count, which
  /// still knows every marker it stands for (needed to highlight it).
  /// </summary>
  [Fact]
  public void MarkerDots_AboveLimit_CollapsesIntoCountDot()
  {
    // Act: Four markers with a limit of three.
    IReadOnlyList<GraphVisMarkerDot> dots = GraphVisGeometry.MarkerDots(Markers("a", "b", "c", "d"), 3);

    // Assert: A single collapsed dot labelled "4" holding all identifiers.
    GraphVisMarkerDot dot = Assert.Single(dots);
    Assert.True(dot.IsCollapsed);
    Assert.Equal("4", dot.Label);
    Assert.Equal(["a", "b", "c", "d"], dot.MarkerIds);
  }

  /// <summary>
  /// Verifies a limit below one is rejected.
  /// </summary>
  [Fact]
  public void MarkerDots_WithLimitBelowOne_Throws()
  {
    // Act + Assert: Zero is not a usable limit.
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphVisGeometry.MarkerDots(Markers("a"), 0));
  }

  /// <summary>
  /// Verifies a single dot sits on the node center.
  /// </summary>
  [Fact]
  public void MarkerOffsets_SingleDot_IsCentered()
  {
    // Act + Assert: One dot, no offset.
    Assert.Equal([0f], GraphVisGeometry.MarkerOffsets([40f], 12f, 4f));
  }

  /// <summary>
  /// Verifies dots with short labels keep the minimum spacing and the row is centered.
  /// </summary>
  [Fact]
  public void MarkerOffsets_ShortLabels_UseMinimumSpacing()
  {
    // Act: Three dots with 2-wide labels, minimum spacing 12.
    IReadOnlyList<float> offsets = GraphVisGeometry.MarkerOffsets([2f, 2f, 2f], 12f, 4f);

    // Assert: 12 apart, symmetric around zero.
    Assert.Equal([-12f, 0f, 12f], offsets);
  }

  /// <summary>
  /// Verifies wide labels push dots apart so the labels do not overlap (the spike showed
  /// "Start" and "Red Key" running together with fixed spacing).
  /// </summary>
  [Fact]
  public void MarkerOffsets_WideLabels_DoNotOverlap()
  {
    // Arrange: Label widths and the gap between labels.
    float[] widths = [30f, 50f, 20f];
    const float gap = 4f;

    // Act: Place the dots.
    IReadOnlyList<float> offsets = GraphVisGeometry.MarkerOffsets(widths, 12f, gap);

    // Assert: Each label's right edge is at least the gap left of the next label's left edge.
    for (int i = 1; i < offsets.Count; i++)
    {
      float previousRight = offsets[i - 1] + widths[i - 1] / 2f;
      float currentLeft = offsets[i] - widths[i] / 2f;
      Assert.True(currentLeft - previousRight >= gap - Tolerance);
    }

    // Assert: The row is centered on the node.
    Assert.Equal(0f, offsets[0] + offsets[^1], Tolerance);
  }

  /// <summary>
  /// Verifies no labels give no offsets.
  /// </summary>
  [Fact]
  public void MarkerOffsets_WithoutDots_IsEmpty()
  {
    // Act + Assert: Nothing to place.
    Assert.Empty(GraphVisGeometry.MarkerOffsets([], 12f, 4f));
  }

  #endregion

  #region Parallel edges

  /// <summary>Spacing between parallel edges used by the tests.</summary>
  private const float Spacing = 24f;

  /// <summary>
  /// Verifies a lone edge stays straight, and edges of other pairs do not influence it.
  /// </summary>
  [Fact]
  public void ParallelEdgeBends_LoneEdges_AreStraight()
  {
    // Act: Three edges, each between a different pair.
    float[] bends = GraphVisGeometry.ParallelEdgeBends([(0, 1), (1, 2), (2, 0)], Spacing);

    // Assert: No bends.
    Assert.Equal([0f, 0f, 0f], bends);
  }

  /// <summary>
  /// Verifies parallel edges of one pair spread symmetrically around the straight line, in edge
  /// order, while an edge of another pair stays straight.
  /// </summary>
  [Fact]
  public void ParallelEdgeBends_SameDirection_SpreadSymmetrically()
  {
    // Act: Two edges 0→1, three edges 2→3, one edge 1→2.
    float[] bends = GraphVisGeometry.ParallelEdgeBends([(0, 1), (2, 3), (0, 1), (2, 3), (1, 2), (2, 3)], Spacing);

    // Assert: Pair 0-1 at ±spacing/2, pair 2-3 at -spacing, 0, +spacing, the lone edge straight.
    Assert.Equal([-12f, -24f, 12f, 0f, 0f, 24f], bends);
  }

  /// <summary>
  /// Verifies two edges in opposite directions end up on different sides: their bends are equal
  /// in their own directions, and the left side of 1→0 is the right side of 0→1.
  /// </summary>
  [Fact]
  public void ParallelEdgeBends_OppositeDirections_LandOnDifferentSides()
  {
    // Act: Edge 0→1 and edge 1→0.
    float[] bends = GraphVisGeometry.ParallelEdgeBends([(0, 1), (1, 0)], Spacing);
    GraphVisCurve forward = GraphVisGeometry.EdgeCurve(new Vector2(0, 0), 10f, new Vector2(200, 0), 10f, bends[0])!.Value;
    GraphVisCurve backward = GraphVisGeometry.EdgeCurve(new Vector2(200, 0), 10f, new Vector2(0, 0), 10f, bends[1])!.Value;

    // Assert: Same stored bend; the arcs bulge to opposite sides of the line, at least spacing apart.
    Assert.Equal(-12f, bends[0]);
    Assert.Equal(-12f, bends[1]);
    Assert.True(forward.Middle.Y * backward.Middle.Y < 0);
    Assert.True(MathF.Abs(forward.Middle.Y - backward.Middle.Y) >= Spacing);
  }

  /// <summary>
  /// Verifies invalid arguments are rejected.
  /// </summary>
  [Fact]
  public void ParallelEdgeBends_WithInvalidArguments_Throws()
  {
    // Act + Assert: Null edges, negative or infinite spacing.
    Assert.Throws<ArgumentNullException>(() => GraphVisGeometry.ParallelEdgeBends(null!, Spacing));
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphVisGeometry.ParallelEdgeBends([(0, 1)], -1f));
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphVisGeometry.ParallelEdgeBends([(0, 1)], float.PositiveInfinity));
  }

  /// <summary>
  /// Verifies a curve without bend is the straight border-to-border line.
  /// </summary>
  [Fact]
  public void EdgeCurve_WithoutBend_MatchesEdgeSegment()
  {
    // Arrange: Two circles on a diagonal.
    var from = new Vector2(10, 20);
    var to = new Vector2(130, 180);

    // Act: Straight curve and straight segment.
    GraphVisCurve curve = GraphVisGeometry.EdgeCurve(from, 15f, to, 25f, 0f)!.Value;
    GraphVisSegment segment = GraphVisGeometry.EdgeSegment(from, 15f, to, 25f)!.Value;

    // Assert: Same ends, control point in the middle.
    AssertClose(segment.Start, curve.Start);
    AssertClose(segment.End, curve.End);
    AssertClose(segment.Middle, curve.Middle);
  }

  /// <summary>
  /// Verifies a bent curve starts and ends on the circle borders and its middle lies at least the
  /// bend away from the line between the centers, on the left of the edge direction.
  /// </summary>
  [Fact]
  public void EdgeCurve_WithBend_EndsOnBordersAndBulgesLeft()
  {
    // Arrange: Edge along +X; with screen Y pointing down, "left" of +X is +Y.
    var from = new Vector2(0, 0);
    var to = new Vector2(200, 0);
    const float bend = 20f;

    // Act: Bent curve.
    GraphVisCurve curve = GraphVisGeometry.EdgeCurve(from, 10f, to, 30f, bend)!.Value;

    // Assert: Ends on the borders, middle at least the bend away on the +Y side.
    Assert.Equal(10f, Vector2.Distance(from, curve.Start), Tolerance);
    Assert.Equal(30f, Vector2.Distance(to, curve.End), Tolerance);
    Assert.True(curve.Middle.Y >= bend);
  }

  /// <summary>
  /// Verifies the middle of a curve between small nodes lies (almost exactly) the bend away from
  /// the line, on the right for a negative bend.
  /// </summary>
  [Fact]
  public void EdgeCurve_SmallNodes_MiddleIsTheBendAway()
  {
    // Act: Tiny circles far apart, bend to the right (-Y for an edge along +X).
    GraphVisCurve curve = GraphVisGeometry.EdgeCurve(new Vector2(0, 0), 1f, new Vector2(400, 0), 1f, -15f)!.Value;

    // Assert: Middle 15 below the line, at the middle of the edge.
    Assert.Equal(-15f, curve.Middle.Y, 0.1f);
    Assert.Equal(200f, curve.Middle.X, 0.1f);
  }

  /// <summary>
  /// Verifies there is no curve when the circles touch or overlap.
  /// </summary>
  [Fact]
  public void EdgeCurve_OverlappingCircles_ReturnsNull()
  {
    // Act + Assert: Centers 30 apart, radii 20 + 20.
    Assert.Null(GraphVisGeometry.EdgeCurve(new Vector2(0, 0), 20f, new Vector2(30, 0), 20f, 12f));
  }

  /// <summary>
  /// Verifies arrowheads shorten a curve along its tangents and keep the control point, and that
  /// a curve too short for its arrowheads is not drawn.
  /// </summary>
  [Fact]
  public void CurveBetweenArrows_MovesEndsAlongTangents()
  {
    // Arrange: A bent curve.
    GraphVisCurve curve = GraphVisGeometry.EdgeCurve(new Vector2(0, 0), 10f, new Vector2(200, 0), 10f, 20f)!.Value;

    // Act: Arrow at the end only, then arrows too long for the curve.
    GraphVisCurve? shortened = GraphVisGeometry.CurveBetweenArrows(curve, false, true, 12f);
    GraphVisCurve? tooShort = GraphVisGeometry.CurveBetweenArrows(curve, true, true, 100f);

    // Assert: Start kept, end moved back 12 along the end tangent, control unchanged; no room → null.
    Assert.NotNull(shortened);
    AssertClose(curve.Start, shortened.Value.Start);
    AssertClose(curve.End - curve.EndTangent * 12f, shortened.Value.End);
    AssertClose(curve.Control, shortened.Value.Control);
    Assert.Null(tooShort);
  }

  /// <summary>
  /// Verifies the tangents of a curve point from the ends towards the control point.
  /// </summary>
  [Fact]
  public void Curve_Tangents_PointAlongTheCurve()
  {
    // Arrange: Start, control and end of a simple arch.
    var curve = new GraphVisCurve(new Vector2(0, 0), new Vector2(50, 50), new Vector2(100, 0));

    // Assert: Leaves towards the control point, arrives from it; both unit length.
    float diagonal = MathF.Sqrt(0.5f);
    AssertClose(new Vector2(diagonal, diagonal), curve.StartTangent);
    AssertClose(new Vector2(diagonal, -diagonal), curve.EndTangent);
  }

  #endregion
}
