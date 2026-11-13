using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Headless tests for <see cref="GraphVisInteraction"/> (pan, zoom and scroll navigation).
/// </summary>
public sealed class GraphVisInteractionTests
{
  /// <summary>Tolerance for floating-point comparisons.</summary>
  private const float Tolerance = 0.001f;

  /// <summary>Style used by the tests: the defaults.</summary>
  private static readonly GraphVisStyle Style = GraphVisStyle.Default;

  /// <summary>Viewport size used by the tests.</summary>
  private static readonly Vector2 Viewport = new(800, 400);

  /// <summary>A graph much larger than the viewport at zoom 1, so panning has room on both axes.</summary>
  private static readonly GraphVisBounds LargeGraph = new(new Vector2(-2000, -2000), new Vector2(2000, 2000));

  /// <summary>
  /// Creates an interaction whose camera is at zoom 1, world origin at the viewport center.
  /// </summary>
  /// <returns>Interaction.</returns>
  private static GraphVisInteraction CreateCentered()
  {
    var interaction = new GraphVisInteraction(Style);
    interaction.Reset(new GraphVisCamera(1f, Viewport / 2f));
    return interaction;
  }

  /// <summary>
  /// Creates an input frame with the pointer over the viewport and nothing pressed.
  /// </summary>
  /// <param name="pointer">Pointer position.</param>
  /// <returns>Input frame.</returns>
  private static GraphVisInputFrame Idle(Vector2 pointer) => new(pointer, true, false, 0f, Vector2.Zero, 0.016f);

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
  /// Verifies dragging moves the graph with the pointer, and releasing ends the drag.
  /// </summary>
  [Fact]
  public void Update_DragOverViewport_PansByPointerMovement()
  {
    // Arrange: Centered camera.
    GraphVisInteraction interaction = CreateCentered();
    Vector2 start = interaction.Camera.Translation;

    // Act: Press at (100,100), move to (130,90), then release and move on.
    interaction.Update(Idle(new Vector2(100, 100)) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    interaction.Update(Idle(new Vector2(130, 90)) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    bool panningWhileHeld = interaction.IsPanning;
    interaction.Update(Idle(new Vector2(130, 90)), LargeGraph, Viewport);
    interaction.Update(Idle(new Vector2(300, 300)), LargeGraph, Viewport);

    // Assert: Moved by the drag only; the drag ended with the release.
    Assert.True(panningWhileHeld);
    Assert.False(interaction.IsPanning);
    AssertClose(start + new Vector2(30, -10), interaction.Camera.Translation);
  }

  /// <summary>
  /// Verifies a drag continues when the pointer leaves the viewport while the button is held.
  /// </summary>
  [Fact]
  public void Update_DragLeavingViewport_KeepsPanning()
  {
    // Arrange: Drag started inside.
    GraphVisInteraction interaction = CreateCentered();
    Vector2 start = interaction.Camera.Translation;
    interaction.Update(Idle(new Vector2(100, 100)) with { IsPrimaryDown = true }, LargeGraph, Viewport);

    // Act: Move outside the viewport with the button held.
    interaction.Update(
      Idle(new Vector2(-50, 100)) with { IsPrimaryDown = true, IsPointerOver = false }, LargeGraph, Viewport);

    // Assert: Still panning, by the whole movement.
    Assert.True(interaction.IsPanning);
    AssertClose(start + new Vector2(-150, 0), interaction.Camera.Translation);
  }

  /// <summary>
  /// Verifies a press outside the viewport (for example on a scroll bar) that moves into it does
  /// not pan.
  /// </summary>
  [Fact]
  public void Update_PressOutsideViewport_DoesNotPan()
  {
    // Arrange: Centered camera.
    GraphVisInteraction interaction = CreateCentered();
    GraphVisCamera before = interaction.Camera;

    // Act: Press outside, then move over the viewport with the button still held.
    interaction.Update(
      Idle(new Vector2(100, 100)) with { IsPrimaryDown = true, IsPointerOver = false }, LargeGraph, Viewport);
    bool changed = interaction.Update(Idle(new Vector2(200, 150)) with { IsPrimaryDown = true }, LargeGraph, Viewport);

    // Assert: Nothing happened.
    Assert.False(changed);
    Assert.False(interaction.IsPanning);
    Assert.Equal(before, interaction.Camera);
  }

  /// <summary>
  /// Verifies the wheel zooms in by the zoom step per notch, keeping the world point under the
  /// pointer in place.
  /// </summary>
  [Fact]
  public void Update_WheelOverViewport_ZoomsAboutPointer()
  {
    // Arrange: Centered camera; remember the world point under the pointer.
    GraphVisInteraction interaction = CreateCentered();
    var pointer = new Vector2(600, 100);
    Vector2 anchor = interaction.Camera.ScreenToWorld(pointer);

    // Act: Two notches in.
    bool changed = interaction.Update(Idle(pointer) with { WheelNotches = 2 }, LargeGraph, Viewport);

    // Assert: Zoomed by step², anchor still under the pointer.
    Assert.True(changed);
    Assert.Equal(Style.ZoomStep * Style.ZoomStep, interaction.Camera.Zoom, Tolerance);
    AssertClose(pointer, interaction.Camera.WorldToScreen(anchor));
  }

  /// <summary>
  /// Verifies zooming out stops at the zoom that shows the whole graph, and zooming in at the maximum.
  /// </summary>
  [Fact]
  public void Update_WheelBeyondLimits_StopsAtFitAndMaximumZoom()
  {
    // Arrange: Centered camera and the zoom fit-to-view would choose for the large graph.
    GraphVisInteraction interaction = CreateCentered();
    float fitZoom = GraphVisCamera.Fit(LargeGraph, Viewport, Style.ViewPadding, Style.MinZoom, Style.MaxFitZoom).Zoom;

    // Act + Assert: Far out stops at the fit zoom.
    interaction.Update(Idle(Viewport / 2f) with { WheelNotches = -100 }, LargeGraph, Viewport);
    Assert.Equal(fitZoom, interaction.Camera.Zoom, Tolerance);

    // Act + Assert: Far in stops at the maximum.
    interaction.Update(Idle(Viewport / 2f) with { WheelNotches = 100 }, LargeGraph, Viewport);
    Assert.Equal(Style.MaxZoom, interaction.Camera.Zoom, Tolerance);
  }

  /// <summary>
  /// Verifies keys move the view along the axis at the keyboard speed (the graph moves the other
  /// way), with diagonals no faster than straight moves.
  /// </summary>
  /// <param name="axisX">Pan axis X.</param>
  /// <param name="axisY">Pan axis Y.</param>
  [Theory]
  [InlineData(1f, 0f)]
  [InlineData(0f, -1f)]
  [InlineData(1f, 1f)]
  public void Update_PanKeys_MoveViewAtKeyboardSpeed(float axisX, float axisY)
  {
    // Arrange: Centered camera, half a second of key press.
    GraphVisInteraction interaction = CreateCentered();
    Vector2 start = interaction.Camera.Translation;
    var axis = new Vector2(axisX, axisY);
    const float seconds = 0.5f;

    // Act: One frame with the keys held.
    interaction.Update(Idle(Viewport / 2f) with { PanAxis = axis, DeltaSeconds = seconds }, LargeGraph, Viewport);

    // Assert: Graph moved against the axis by speed × time, whatever the direction.
    Vector2 moved = interaction.Camera.Translation - start;
    Assert.Equal(Style.KeyboardPanSpeed * seconds, moved.Length(), Tolerance);
    Assert.True(Vector2.Dot(moved, axis) < 0);
  }

  /// <summary>
  /// Verifies wheel and keys do nothing while the pointer is not over the viewport.
  /// </summary>
  [Fact]
  public void Update_WheelAndKeysWithPointerOutside_AreIgnored()
  {
    // Arrange: Centered camera.
    GraphVisInteraction interaction = CreateCentered();
    GraphVisCamera before = interaction.Camera;

    // Act: Wheel and keys with the pointer elsewhere.
    bool changed = interaction.Update(
      Idle(Viewport / 2f) with { IsPointerOver = false, WheelNotches = 3, PanAxis = new Vector2(1, 0) },
      LargeGraph, Viewport);

    // Assert: Nothing happened.
    Assert.False(changed);
    Assert.Equal(before, interaction.Camera);
  }

  /// <summary>
  /// Verifies panning cannot push a small graph out of the viewport.
  /// </summary>
  [Fact]
  public void Update_PanSmallGraphAway_StaysInsideViewport()
  {
    // Arrange: Graph 100×100 around the origin, shown in the viewport center.
    var smallGraph = new GraphVisBounds(new Vector2(-50, -50), new Vector2(50, 50));
    GraphVisInteraction interaction = CreateCentered();

    // Act: Drag far to the right.
    interaction.Update(Idle(new Vector2(100, 100)) with { IsPrimaryDown = true }, smallGraph, Viewport);
    interaction.Update(Idle(new Vector2(5000, 100)) with { IsPrimaryDown = true }, smallGraph, Viewport);

    // Assert: The graph's right side stops at the viewport edge minus padding.
    float right = interaction.Camera.WorldToScreen(smallGraph.Max).X;
    Assert.Equal(Viewport.X - Style.ViewPadding, right, Tolerance);
  }

  /// <summary>
  /// Verifies scrolling moves only the given axis to the given scroll value.
  /// </summary>
  [Fact]
  public void ScrollTo_MovesViewToScrollValue()
  {
    // Arrange: Centered camera.
    GraphVisInteraction interaction = CreateCentered();
    float startY = interaction.Camera.Translation.Y;

    // Act: Scroll horizontally to 500.
    interaction.ScrollTo(EnGraphVisAxis.Horizontal, 500f, LargeGraph, Viewport);

    // Assert: The horizontal scroll value is 500; vertical position unchanged.
    GraphVisScrollRange range = interaction.Camera.ScrollRange(EnGraphVisAxis.Horizontal, LargeGraph, Viewport, Style.ViewPadding);
    Assert.Equal(500f, range.Value, Tolerance);
    Assert.Equal(startY, interaction.Camera.Translation.Y, Tolerance);
  }

  /// <summary>
  /// Verifies resetting replaces the camera and ends a drag.
  /// </summary>
  [Fact]
  public void Reset_EndsDragAndReplacesCamera()
  {
    // Arrange: A drag in progress.
    GraphVisInteraction interaction = CreateCentered();
    interaction.Update(Idle(new Vector2(100, 100)) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    var camera = new GraphVisCamera(0.5f, new Vector2(10, 20));

    // Act: Reset.
    interaction.Reset(camera);

    // Assert: New camera, no drag; further movement with the button held does not pan.
    Assert.False(interaction.IsPanning);
    interaction.Update(Idle(new Vector2(300, 100)) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    Assert.Equal(camera, interaction.Camera);
  }

  #region Window resize

  /// <summary>
  /// Verifies a fitted view is fitted again on resize.
  /// </summary>
  [Fact]
  public void ResizeView_WhileFitted_FitsAgain()
  {
    // Arrange: Fitted to the large graph.
    var interaction = new GraphVisInteraction(Style);
    interaction.FitView(LargeGraph, Viewport);
    var larger = new Vector2(1200, 900);

    // Act: Viewport grows.
    interaction.ResizeView(LargeGraph, Viewport, larger);

    // Assert: Same as fitting the new size directly; still fitted.
    GraphVisCamera expected = GraphVisCamera.Fit(LargeGraph, larger, Style.ViewPadding, Style.MinZoom, Style.MaxFitZoom);
    Assert.Equal(expected, interaction.Camera);
    Assert.True(interaction.IsFitted);
  }

  /// <summary>
  /// Verifies that after the user navigated, a resize keeps the zoom and the world point at the
  /// viewport center.
  /// </summary>
  [Fact]
  public void ResizeView_AfterNavigation_KeepsZoomAndCenter()
  {
    // Arrange: Fitted, then zoomed in by the wheel.
    var interaction = new GraphVisInteraction(Style);
    interaction.FitView(LargeGraph, Viewport);
    interaction.Update(Idle(new Vector2(300, 150)) with { WheelNotches = 5 }, LargeGraph, Viewport);
    float zoom = interaction.Camera.Zoom;
    Vector2 center = interaction.Camera.ScreenToWorld(Viewport / 2f);
    var larger = new Vector2(1000, 600);

    // Act: Viewport grows.
    interaction.ResizeView(LargeGraph, Viewport, larger);

    // Assert: Not fitted; same zoom; the same world point sits at the new center.
    Assert.False(interaction.IsFitted);
    Assert.Equal(zoom, interaction.Camera.Zoom, Tolerance);
    AssertClose(center, interaction.Camera.ScreenToWorld(larger / 2f));
  }

  /// <summary>
  /// Verifies panning, zooming and scrolling all end the fitted state; a drag-free click does not.
  /// </summary>
  [Fact]
  public void Navigation_EndsFittedState()
  {
    // Arrange: Three fitted interactions.
    GraphVisInteraction Fitted()
    {
      var interaction = new GraphVisInteraction(Style);
      interaction.FitView(LargeGraph, Viewport);
      return interaction;
    }

    GraphVisInteraction panned = Fitted();
    GraphVisInteraction scrolled = Fitted();
    GraphVisInteraction clicked = Fitted();

    // Act: Key pan, scroll bar, click.
    panned.Update(Idle(Viewport / 2f) with { PanAxis = new Vector2(1, 0) }, LargeGraph, Viewport);
    scrolled.ScrollTo(EnGraphVisAxis.Vertical, 10f, LargeGraph, Viewport);
    clicked.Update(Idle(Viewport / 2f) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    clicked.Update(Idle(Viewport / 2f), LargeGraph, Viewport);

    // Assert: Only the click keeps the view fitted.
    Assert.False(panned.IsFitted);
    Assert.False(scrolled.IsFitted);
    Assert.True(clicked.IsFitted);
  }

  #endregion

  #region Selection and dragging

  /// <summary>Node radius of <see cref="CreateWithGraph"/>.</summary>
  private const float NodeRadius = 20f;

  /// <summary>
  /// Creates an interaction over two radius-20 nodes at world (0,0) and (400,0) joined by a
  /// straight edge, with the camera at zoom 1 and world origin at viewport (100,100). So node 0 is
  /// at screen (100,100), node 1 at (500,100) and the edge's middle at (300,100).
  /// </summary>
  /// <param name="simulation">The simulation holding the node positions.</param>
  /// <returns>Interaction.</returns>
  private static GraphVisInteraction CreateWithGraph(out GraphVisSimulation simulation)
  {
    float[] radii = [NodeRadius, NodeRadius];
    simulation = new GraphVisSimulation([new Vector2(0, 0), new Vector2(400, 0)], radii, [(0, 1)], GraphVisPhysics.Default);
    var interaction = new GraphVisInteraction(Style);
    interaction.SetGraph(simulation, radii, [new GraphVisEdgeShape(0, 1, 0f)]);
    interaction.Reset(new GraphVisCamera(1f, new Vector2(100, 100)));
    return interaction;
  }

  /// <summary>
  /// Clicks (press and release without moving) at a point.
  /// </summary>
  /// <param name="interaction">Interaction.</param>
  /// <param name="pointer">Click position.</param>
  private static void Click(GraphVisInteraction interaction, Vector2 pointer)
  {
    interaction.Update(Idle(pointer) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    interaction.Update(Idle(pointer), LargeGraph, Viewport);
  }

  /// <summary>
  /// Verifies a click selects a node or an edge, and a click on empty space clears the selection.
  /// </summary>
  [Fact]
  public void Update_Click_SelectsNodeEdgeOrNothing()
  {
    // Arrange: Two nodes and an edge.
    GraphVisInteraction interaction = CreateWithGraph(out _);

    // Act + Assert: Node 1, then the edge, then empty space.
    Click(interaction, new Vector2(505, 95));
    Assert.Equal(GraphVisHit.ForNode(1), interaction.Selection);
    Click(interaction, new Vector2(300, 103));
    Assert.Equal(GraphVisHit.ForEdge(0), interaction.Selection);
    Click(interaction, new Vector2(300, 300));
    Assert.Equal(GraphVisHit.None, interaction.Selection);
  }

  /// <summary>
  /// Verifies the edge tolerance is in screen pixels: a point a few pixels beside the line hits
  /// at zoom 1 and also when zoomed out, though that is far more world units away.
  /// </summary>
  [Fact]
  public void HitAt_EdgeTolerance_ScalesWithZoom()
  {
    // Arrange: Zoom 1; the screen point a pixel less than the tolerance below the edge's middle.
    GraphVisInteraction interaction = CreateWithGraph(out _);
    float offset = Style.EdgeHitTolerance - 1f;
    GraphVisHit atZoomOne = interaction.HitAt(new Vector2(300, 100 + offset));

    // Act: Zoom out to 0.25 and look the same few pixels below the edge's (moved) middle.
    interaction.Reset(new GraphVisCamera(0.25f, new Vector2(100, 100)));
    GraphVisHit zoomedOut = interaction.HitAt(new Vector2(150, 100 + offset));

    // Assert: Both hit the edge, and something twice as far away does not.
    Assert.Equal(GraphVisHit.ForEdge(0), atZoomOne);
    Assert.Equal(GraphVisHit.ForEdge(0), zoomedOut);
    Assert.Equal(GraphVisHit.None, interaction.HitAt(new Vector2(150, 100 + 2 * offset)));
  }

  /// <summary>
  /// Verifies a press that moves less than the drag threshold is still a click: no pan, selection made.
  /// </summary>
  [Fact]
  public void Update_SmallMoveBelowThreshold_IsClick()
  {
    // Arrange: Two nodes.
    GraphVisInteraction interaction = CreateWithGraph(out _);
    GraphVisCamera before = interaction.Camera;
    var press = new Vector2(500, 100);

    // Act: Press on node 1, wiggle by less than the threshold, release.
    interaction.Update(Idle(press) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    interaction.Update(Idle(press + new Vector2(Style.DragThreshold / 2f, 0)) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    interaction.Update(Idle(press), LargeGraph, Viewport);

    // Assert: Node selected, view unchanged.
    Assert.Equal(GraphVisHit.ForNode(1), interaction.Selection);
    Assert.Equal(before, interaction.Camera);
  }

  /// <summary>
  /// Verifies dragging a node selects and pins it, moves it with the pointer (keeping the grab
  /// point), leaves the view alone, and releases the pin on release.
  /// </summary>
  [Fact]
  public void Update_DragNode_MovesPinnedNodeWithPointer()
  {
    // Arrange: Two nodes; grab node 1 off-center.
    GraphVisInteraction interaction = CreateWithGraph(out GraphVisSimulation simulation);
    GraphVisCamera before = interaction.Camera;
    var press = new Vector2(505, 100);

    // Act: Press on node 1 and move 50 down.
    interaction.Update(Idle(press) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    interaction.Update(Idle(press + new Vector2(0, 50)) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    int? dragged = interaction.DraggedNode;
    int? pinned = simulation.PinnedNode;
    Vector2 moved = simulation.Positions[1];
    interaction.Update(Idle(press + new Vector2(0, 50)), LargeGraph, Viewport);

    // Assert: Node 1 dragged, pinned and selected; it moved by the pointer movement; view still.
    Assert.Equal(1, dragged);
    Assert.Equal(1, pinned);
    AssertClose(new Vector2(400, 50), moved);
    Assert.Equal(GraphVisHit.ForNode(1), interaction.Selection);
    Assert.Equal(before, interaction.Camera);

    // Assert: Release ends the drag and frees the node.
    Assert.Null(interaction.DraggedNode);
    Assert.Null(simulation.PinnedNode);
  }

  /// <summary>
  /// Verifies dragging from an edge pans the view and keeps the selection.
  /// </summary>
  [Fact]
  public void Update_DragFromEdge_PansAndKeepsSelection()
  {
    // Arrange: Node 0 selected.
    GraphVisInteraction interaction = CreateWithGraph(out _);
    Click(interaction, new Vector2(100, 100));
    Vector2 start = interaction.Camera.Translation;

    // Act: Drag from the edge's middle by (40, 0), release.
    interaction.Update(Idle(new Vector2(300, 100)) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    interaction.Update(Idle(new Vector2(340, 100)) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    interaction.Update(Idle(new Vector2(340, 100)), LargeGraph, Viewport);

    // Assert: Panned by the drag; node 0 still selected.
    AssertClose(start + new Vector2(40, 0), interaction.Camera.Translation);
    Assert.Equal(GraphVisHit.ForNode(0), interaction.Selection);
  }

  /// <summary>
  /// Verifies hover follows the pointer over nodes and edges, and is empty with the pointer outside.
  /// </summary>
  [Fact]
  public void Update_PointerMoves_HoverFollows()
  {
    // Arrange: Two nodes and an edge.
    GraphVisInteraction interaction = CreateWithGraph(out _);

    // Act + Assert: Over node 0, over the edge, over empty space, outside the viewport.
    interaction.Update(Idle(new Vector2(100, 100)), LargeGraph, Viewport);
    Assert.Equal(GraphVisHit.ForNode(0), interaction.Hover);
    interaction.Update(Idle(new Vector2(300, 101)), LargeGraph, Viewport);
    Assert.Equal(GraphVisHit.ForEdge(0), interaction.Hover);
    interaction.Update(Idle(new Vector2(300, 300)), LargeGraph, Viewport);
    Assert.Equal(GraphVisHit.None, interaction.Hover);
    interaction.Update(Idle(new Vector2(100, 100)) with { IsPointerOver = false }, LargeGraph, Viewport);
    Assert.Equal(GraphVisHit.None, interaction.Hover);
  }

  /// <summary>
  /// Verifies the dragged node stays hovered during a drag, and nothing is hovered while panning.
  /// </summary>
  [Fact]
  public void Update_DragAndPan_HoverIsDraggedNodeOrNothing()
  {
    // Arrange: Two nodes.
    GraphVisInteraction interaction = CreateWithGraph(out _);

    // Act + Assert: Drag node 1 far away (the pointer ends over empty space): still hovered.
    interaction.Update(Idle(new Vector2(500, 100)) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    interaction.Update(Idle(new Vector2(500, 300)) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    Assert.Equal(GraphVisHit.ForNode(1), interaction.Hover);
    interaction.Update(Idle(new Vector2(500, 300)), LargeGraph, Viewport);

    // Act + Assert: Pan from empty space across node 0: nothing hovered during the pan.
    interaction.Update(Idle(new Vector2(100, 300)) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    interaction.Update(Idle(new Vector2(100, 310)) with { IsPrimaryDown = true }, LargeGraph, Viewport);
    Assert.True(interaction.IsPanning);
    Assert.Equal(GraphVisHit.None, interaction.Hover);
  }

  /// <summary>
  /// Verifies a new graph clears the selection, while a view reset (resize) keeps it.
  /// </summary>
  [Fact]
  public void SetGraph_ClearsSelection_ResetKeepsIt()
  {
    // Arrange: Node 0 selected.
    GraphVisInteraction interaction = CreateWithGraph(out GraphVisSimulation simulation);
    Click(interaction, new Vector2(100, 100));

    // Act + Assert: Reset keeps it.
    interaction.Reset(new GraphVisCamera(0.5f, Vector2.Zero));
    Assert.Equal(GraphVisHit.ForNode(0), interaction.Selection);

    // Act + Assert: A new graph clears it.
    interaction.SetGraph(simulation, [NodeRadius, NodeRadius], []);
    Assert.Equal(GraphVisHit.None, interaction.Selection);
  }

  #endregion
}
