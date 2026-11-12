using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Headless tests for <see cref="GraphVisCamera"/> and <see cref="GraphVisBounds"/> (pan, zoom,
/// fit and clamp math).
/// </summary>
public sealed class GraphVisCameraTests
{
  /// <summary>Tolerance for floating-point comparisons.</summary>
  private const float Tolerance = 0.001f;

  /// <summary>Smallest zoom used by the tests.</summary>
  private const float MinZoom = 0.2f;

  /// <summary>Largest zoom used by the tests.</summary>
  private const float MaxZoom = 4f;

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
  /// Verifies world and screen conversions are inverse to each other.
  /// </summary>
  [Fact]
  public void WorldToScreen_AndBack_RoundTrips()
  {
    // Arrange: Zoomed and moved camera.
    var camera = new GraphVisCamera(2f, new Vector2(30, -10));
    var world = new Vector2(5, 7);

    // Act: Convert there and back.
    Vector2 screen = camera.WorldToScreen(world);

    // Assert: screen = world * zoom + translation, and the inverse returns the original.
    AssertClose(new Vector2(40, 4), screen);
    AssertClose(world, camera.ScreenToWorld(screen));
  }

  /// <summary>
  /// Verifies panning moves the graph on screen by exactly the given pixels at any zoom.
  /// </summary>
  [Fact]
  public void PanBy_MovesScreenPositionByDelta()
  {
    // Arrange: Zoomed camera.
    var camera = new GraphVisCamera(3f, Vector2.Zero);
    var world = new Vector2(1, 1);

    // Act: Pan.
    GraphVisCamera panned = camera.PanBy(new Vector2(12, -4));

    // Assert: The world point moved by the pan delta on screen.
    AssertClose(camera.WorldToScreen(world) + new Vector2(12, -4), panned.WorldToScreen(world));
  }

  /// <summary>
  /// Verifies zooming keeps the world point under the cursor in place.
  /// </summary>
  [Fact]
  public void ZoomAbout_KeepsPointUnderCursorFixed()
  {
    // Arrange: Camera and a cursor position.
    var camera = new GraphVisCamera(1f, new Vector2(15, 25));
    var cursor = new Vector2(200, 120);
    Vector2 worldUnderCursor = camera.ScreenToWorld(cursor);

    // Act: Zoom in.
    GraphVisCamera zoomed = camera.ZoomAbout(cursor, 1.5f, MinZoom, MaxZoom);

    // Assert: Zoom changed, the same world point is still under the cursor.
    Assert.Equal(1.5f, zoomed.Zoom, Tolerance);
    AssertClose(cursor, zoomed.WorldToScreen(worldUnderCursor));
  }

  /// <summary>
  /// Verifies zoom stops at its limits and the view does not drift once there.
  /// </summary>
  /// <param name="startZoom">Zoom before.</param>
  /// <param name="factor">Zoom multiplier.</param>
  /// <param name="expectedZoom">Zoom after clamping.</param>
  [Theory]
  [InlineData(3f, 2f, MaxZoom)]
  [InlineData(0.3f, 0.5f, MinZoom)]
  [InlineData(MaxZoom, 2f, MaxZoom)]
  public void ZoomAbout_ClampsToLimits(float startZoom, float factor, float expectedZoom)
  {
    // Arrange: Camera near a zoom limit.
    var camera = new GraphVisCamera(startZoom, new Vector2(5, 5));
    var cursor = new Vector2(50, 60);
    Vector2 worldUnderCursor = camera.ScreenToWorld(cursor);

    // Act: Zoom past the limit.
    GraphVisCamera zoomed = camera.ZoomAbout(cursor, factor, MinZoom, MaxZoom);

    // Assert: Zoom is clamped and the cursor anchor still holds.
    Assert.Equal(expectedZoom, zoomed.Zoom, Tolerance);
    AssertClose(cursor, zoomed.WorldToScreen(worldUnderCursor));
  }

  /// <summary>
  /// Verifies invalid zoom arguments are rejected.
  /// </summary>
  /// <param name="factor">Zoom multiplier.</param>
  /// <param name="minZoom">Minimum zoom.</param>
  /// <param name="maxZoom">Maximum zoom.</param>
  [Theory]
  [InlineData(0f, MinZoom, MaxZoom)]
  [InlineData(-1f, MinZoom, MaxZoom)]
  [InlineData(1f, 0f, MaxZoom)]
  [InlineData(1f, 2f, 1f)]
  public void ZoomAbout_WithInvalidArguments_Throws(float factor, float minZoom, float maxZoom)
  {
    // Act + Assert: Non-positive factor or broken limits are rejected.
    Assert.Throws<ArgumentOutOfRangeException>(
      () => GraphVisCamera.Identity.ZoomAbout(Vector2.Zero, factor, minZoom, maxZoom));
  }

  /// <summary>
  /// Verifies fitting a large graph shrinks it into the padded viewport, centered.
  /// </summary>
  [Fact]
  public void Fit_LargeGraph_ShrinksAndCenters()
  {
    // Arrange: 1000x200 graph, 520x220 viewport with 10 px padding (500x200 usable).
    var bounds = new GraphVisBounds(new Vector2(-500, 0), new Vector2(500, 200));
    var viewport = new Vector2(520, 220);

    // Act: Fit.
    GraphVisCamera camera = GraphVisCamera.Fit(bounds, viewport, 10f, MinZoom, MaxZoom);

    // Assert: Width is the limiting axis (zoom 0.5); the graph center sits at the viewport center.
    Assert.Equal(0.5f, camera.Zoom, Tolerance);
    AssertClose(viewport / 2f, camera.WorldToScreen(bounds.Center));
    AssertClose(new Vector2(10, 60), camera.WorldToScreen(bounds.Min));
  }

  /// <summary>
  /// Verifies fitting a small graph does not magnify it beyond the given maximum.
  /// </summary>
  [Fact]
  public void Fit_SmallGraph_IsNotMagnifiedBeyondMaximum()
  {
    // Arrange: Tiny graph in a big viewport, maximum fit zoom 1.
    var bounds = new GraphVisBounds(new Vector2(0, 0), new Vector2(10, 10));
    var viewport = new Vector2(800, 600);

    // Act: Fit.
    GraphVisCamera camera = GraphVisCamera.Fit(bounds, viewport, 10f, MinZoom, 1f);

    // Assert: Zoom stays 1 and the graph is centered.
    Assert.Equal(1f, camera.Zoom, Tolerance);
    AssertClose(viewport / 2f, camera.WorldToScreen(bounds.Center));
  }

  /// <summary>
  /// Verifies fitting a single point (zero-size bounds) centers it without dividing by zero.
  /// </summary>
  [Fact]
  public void Fit_SinglePoint_CentersIt()
  {
    // Arrange: Zero-size bounds.
    var point = new Vector2(40, 40);
    var viewport = new Vector2(300, 200);

    // Act: Fit.
    GraphVisCamera camera = GraphVisCamera.Fit(new GraphVisBounds(point, point), viewport, 10f, MinZoom, 1f);

    // Assert: Finite zoom, point in the middle.
    Assert.Equal(1f, camera.Zoom, Tolerance);
    AssertClose(viewport / 2f, camera.WorldToScreen(point));
  }

  /// <summary>
  /// Verifies a graph too large for the minimum zoom is still centered.
  /// </summary>
  [Fact]
  public void Fit_HugeGraph_UsesMinimumZoomAndCenters()
  {
    // Arrange: Graph far larger than the viewport allows at the minimum zoom.
    var bounds = new GraphVisBounds(Vector2.Zero, new Vector2(100000, 100000));
    var viewport = new Vector2(400, 300);

    // Act: Fit.
    GraphVisCamera camera = GraphVisCamera.Fit(bounds, viewport, 10f, MinZoom, MaxZoom);

    // Assert: Zoom is the minimum; the center is shown.
    Assert.Equal(MinZoom, camera.Zoom, Tolerance);
    AssertClose(viewport / 2f, camera.WorldToScreen(bounds.Center));
  }

  /// <summary>
  /// Verifies a graph smaller than the viewport cannot be panned out of it.
  /// </summary>
  /// <param name="translationX">Translation before clamping.</param>
  /// <param name="expectedX">Translation after clamping.</param>
  [Theory]
  [InlineData(-500f, 10f)] // pushed far left: stops with the graph start at the left padding
  [InlineData(900f, 290f)] // pushed far right: stops with the graph end at the right padding
  [InlineData(100f, 100f)] // inside the range: untouched
  public void ClampTo_SmallerGraph_StaysInsideViewport(float translationX, float expectedX)
  {
    // Arrange: 100-wide graph (0..100) in a 400-wide viewport, 10 px padding.
    var bounds = new GraphVisBounds(Vector2.Zero, new Vector2(100, 100));
    var camera = new GraphVisCamera(1f, new Vector2(translationX, 50));

    // Act: Clamp.
    GraphVisCamera clamped = camera.ClampTo(bounds, new Vector2(400, 400), 10f);

    // Assert: X is clamped to 10..290; the in-range Y is untouched.
    Assert.Equal(expectedX, clamped.Translation.X, Tolerance);
    Assert.Equal(50f, clamped.Translation.Y, Tolerance);
  }

  /// <summary>
  /// Verifies a graph larger than the viewport always covers it (no empty band can be scrolled in).
  /// </summary>
  /// <param name="translationX">Translation before clamping.</param>
  /// <param name="expectedX">Translation after clamping.</param>
  [Theory]
  [InlineData(500f, 10f)] // start cannot move right of the left padding
  [InlineData(-5000f, -1610f)] // end cannot move left of the right padding: 400 - 10 - 1000 * 2
  [InlineData(-800f, -800f)] // inside the range: untouched
  public void ClampTo_LargerGraph_AlwaysCoversViewport(float translationX, float expectedX)
  {
    // Arrange: 1000-wide graph at zoom 2 (2000 px) in a 400-wide viewport, 10 px padding.
    var bounds = new GraphVisBounds(Vector2.Zero, new Vector2(1000, 1000));
    var camera = new GraphVisCamera(2f, new Vector2(translationX, -800));

    // Act: Clamp.
    GraphVisCamera clamped = camera.ClampTo(bounds, new Vector2(400, 400), 10f);

    // Assert: X is clamped to -1610..10.
    Assert.Equal(expectedX, clamped.Translation.X, Tolerance);
  }

  /// <summary>
  /// Verifies the bounds around circles include their radii.
  /// </summary>
  [Fact]
  public void AroundCircles_IncludesRadii()
  {
    // Act: Bounds of two circles.
    GraphVisBounds? bounds = GraphVisBounds.AroundCircles([(new Vector2(0, 0), 10f), (new Vector2(100, 50), 20f)]);

    // Assert: Extent reaches each circle's border.
    Assert.NotNull(bounds);
    AssertClose(new Vector2(-10, -10), bounds.Value.Min);
    AssertClose(new Vector2(120, 70), bounds.Value.Max);
    AssertClose(new Vector2(130, 80), bounds.Value.Size);
    AssertClose(new Vector2(55, 30), bounds.Value.Center);
  }

  /// <summary>
  /// Verifies there are no bounds without circles.
  /// </summary>
  [Fact]
  public void AroundCircles_WithoutCircles_ReturnsNull()
  {
    // Assert: Nothing to enclose.
    Assert.Null(GraphVisBounds.AroundCircles([]));
  }

  /// <summary>
  /// Verifies the scroll range of a graph wider than the viewport: the bar spans the graph plus
  /// padding, and its value is 0 at the graph's start and the maximum at its end.
  /// </summary>
  [Fact]
  public void ScrollRange_GraphLargerThanViewport_SpansGraphAndPadding()
  {
    // Arrange: Graph 0..1000 wide at zoom 1, viewport 400 wide, padding 20 (content 1040 px).
    var bounds = new GraphVisBounds(new Vector2(0, 0), new Vector2(1000, 100));
    var viewport = new Vector2(400, 300);
    const float padding = 20f;
    var atStart = new GraphVisCamera(1f, new Vector2(padding, 0));
    var atEnd = new GraphVisCamera(1f, new Vector2(400 - padding - 1000, 0));

    // Act: Horizontal ranges at both ends.
    GraphVisScrollRange startRange = atStart.ScrollRange(EnGraphVisAxis.Horizontal, bounds, viewport, padding);
    GraphVisScrollRange endRange = atEnd.ScrollRange(EnGraphVisAxis.Horizontal, bounds, viewport, padding);

    // Assert: Needed; maximum = content minus viewport; value follows the position.
    Assert.True(startRange.IsNeeded);
    Assert.Equal(640f, startRange.Maximum, Tolerance);
    Assert.Equal(400f, startRange.ViewportSize, Tolerance);
    Assert.Equal(0f, startRange.Value, Tolerance);
    Assert.Equal(640f, endRange.Value, Tolerance);
  }

  /// <summary>
  /// Verifies the scroll range follows zoom and works on the vertical axis.
  /// </summary>
  [Fact]
  public void ScrollRange_Vertical_ScalesWithZoom()
  {
    // Arrange: Graph -50..150 tall, zoom 2 (400 px), viewport 300 tall, no padding; graph start 30 px above the top.
    var bounds = new GraphVisBounds(new Vector2(0, -50), new Vector2(10, 150));
    var camera = new GraphVisCamera(2f, new Vector2(0, 70)); // -50 * 2 + 70 = -30

    // Act: Vertical range.
    GraphVisScrollRange range = camera.ScrollRange(EnGraphVisAxis.Vertical, bounds, new Vector2(300, 300), 0f);

    // Assert: Content 400 in a 300 viewport, scrolled 30 down.
    Assert.True(range.IsNeeded);
    Assert.Equal(100f, range.Maximum, Tolerance);
    Assert.Equal(30f, range.Value, Tolerance);
  }

  /// <summary>
  /// Verifies a graph that fits the viewport needs no scroll bar.
  /// </summary>
  [Fact]
  public void ScrollRange_GraphSmallerThanViewport_IsNotNeeded()
  {
    // Arrange: Graph 100 wide in a 400 wide viewport.
    var bounds = new GraphVisBounds(new Vector2(0, 0), new Vector2(100, 100));

    // Act: Horizontal range.
    GraphVisScrollRange range = new GraphVisCamera(1f, new Vector2(50, 50))
      .ScrollRange(EnGraphVisAxis.Horizontal, bounds, new Vector2(400, 300), 20f);

    // Assert: Not needed, nothing to scroll.
    Assert.False(range.IsNeeded);
    Assert.Equal(0f, range.Maximum, Tolerance);
  }

  /// <summary>
  /// Verifies scrolling to a value and reading the value back agree, and that values outside the
  /// range are clamped; the other axis is untouched.
  /// </summary>
  /// <param name="value">Value to scroll to.</param>
  /// <param name="expected">Value read back.</param>
  [Theory]
  [InlineData(0f, 0f)]
  [InlineData(250f, 250f)]
  [InlineData(640f, 640f)]
  [InlineData(-30f, 0f)]
  [InlineData(900f, 640f)]
  public void ScrolledTo_RoundTripsAndClamps(float value, float expected)
  {
    // Arrange: Same setup as the horizontal range test (maximum 640).
    var bounds = new GraphVisBounds(new Vector2(0, 0), new Vector2(1000, 100));
    var viewport = new Vector2(400, 300);
    const float padding = 20f;
    var camera = new GraphVisCamera(1f, new Vector2(padding, 77));

    // Act: Scroll and read back.
    GraphVisCamera scrolled = camera.ScrolledTo(EnGraphVisAxis.Horizontal, value, bounds, viewport, padding);

    // Assert: Value as expected, zoom and vertical translation unchanged.
    Assert.Equal(expected, scrolled.ScrollRange(EnGraphVisAxis.Horizontal, bounds, viewport, padding).Value, Tolerance);
    Assert.Equal(1f, scrolled.Zoom, Tolerance);
    Assert.Equal(77f, scrolled.Translation.Y, Tolerance);
  }
}
