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
}
