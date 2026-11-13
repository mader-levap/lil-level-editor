using System;
using System.Collections.Generic;
using System.Linq;
using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Render;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;
using RenderingLibrary.Graphics;
using SkiaSharp;
using Stride.Core.Mathematics;
using Stride.Input;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Drawing area of the graph visualizer: a canvas showing the graph with scroll bars, or a text
/// when there is no graph to show.
/// </summary>
/// <remarks>
/// <para>Part of <see cref="GumGraphVisualizer"/>. The graph is redrawn every frame by Gum through
/// the canvas element; this class keeps what to draw (graph, node positions) and feeds input to
/// <see cref="GraphVisInteraction"/>, which owns the camera.</para>
/// <para>Selection: a click selects a node or edge (empty space clears it); dragging a node moves
/// it while its neighbours make room.</para>
/// <para>Navigation: left-drag on empty space or an edge pans, the wheel zooms about the pointer, WASD pans while the
/// pointer is over the canvas (the canvas claims the keyboard meanwhile, so the 3D camera stays
/// still; see <see cref="IClaimsKeyboardWhileHovered"/>). Scroll bars lie over the canvas edges
/// and appear only on axes where the graph is larger than the viewport.</para>
/// </remarks>
public sealed class GumGraphViewport : IDisposable
{
  /// <summary>Text shown when there is no graph.</summary>
  public const string NoGraphText = "No graph data available";

  /// <summary>Scroll bar thickness, in pixels.</summary>
  private const float ScrollBarThickness = GumScrollBarSizing.DefaultThickness;

  /// <summary>Smallest scroll bar change that is written back to a bar, in pixels (avoids needless Gum layout passes).</summary>
  private const float ScrollBarEpsilon = 0.01f;

  /// <summary>Look of the graph.</summary>
  private readonly GraphVisStyle _style;

  /// <summary>Raw key state reader (UI code reads the keyboard directly, see <c>GameInput</c>).</summary>
  private readonly Func<Keys, bool> _isKeyDown;

  /// <summary>Navigation state and camera.</summary>
  private readonly GraphVisInteraction _interaction;

  /// <summary>Eased brightening of hovered and selected elements.</summary>
  private readonly GraphVisHighlightFade _highlightFade;

  /// <summary>Renderer; null until built.</summary>
  private GraphVisRenderer? _renderer;

  /// <summary>Canvas element; null until built.</summary>
  private GraphVisCanvasElement? _canvasElement;

  /// <summary>Label shown when there is no graph; null until built.</summary>
  private Label? _noGraphLabel;

  /// <summary>Horizontal scroll bar; null until built.</summary>
  private ScrollBar? _horizontalScrollBar;

  /// <summary>Vertical scroll bar; null until built.</summary>
  private ScrollBar? _verticalScrollBar;

  /// <summary>Filler covering the corner where both scroll bars meet; null until built.</summary>
  private ContainerRuntime? _scrollBarCorner;

  /// <summary>Indices of the nodes marked <see cref="GraphVisNode.IsLayoutRoot"/> in the current graph.</summary>
  private int[] _layoutRoots = [];

  /// <summary>Physics moving the nodes of the current graph; null when there is no graph.</summary>
  private GraphVisSimulation? _simulation;

  /// <summary>Viewport size the camera was last adapted to; null forces a fit on the next update.</summary>
  private Vector2? _fittedSize;

  /// <summary>Whether <see cref="Dispose"/> was called.</summary>
  private bool _disposed;

  /// <summary>Graph extent at the last update; null when there is no graph.</summary>
  private GraphVisBounds? _bounds;

  /// <summary>Whether nodes moved since <see cref="_bounds"/> was computed.</summary>
  private bool _boundsStale;

  /// <summary>Viewport size at the last update, in pixels.</summary>
  private Vector2 _viewportSize;

  /// <summary>Whether <see cref="SyncScrollBars"/> is writing to the scroll bars (their change events are then ignored).</summary>
  private bool _isWritingScrollBars;

  /// <summary>
  /// Creates the viewport. Call <see cref="Build"/> to create its visuals.
  /// </summary>
  /// <param name="style">Look of the graph.</param>
  /// <param name="isKeyDown">Reports the raw down-state of a key.</param>
  /// <exception cref="ArgumentNullException">An argument is null.</exception>
  public GumGraphViewport(GraphVisStyle style, Func<Keys, bool> isKeyDown)
  {
    _style = style ?? throw new ArgumentNullException(nameof(style));
    _isKeyDown = isKeyDown ?? throw new ArgumentNullException(nameof(isKeyDown));
    _interaction = new GraphVisInteraction(style);
    _highlightFade = new GraphVisHighlightFade(style.HoverFadeSeconds);
  }

  /// <summary>Root container; null until <see cref="Build"/> is called.</summary>
  public ContainerRuntime? Root { get; private set; }

  #region Building

  /// <summary>
  /// Builds the visuals, showing the "no graph" text.
  /// </summary>
  /// <returns><see cref="Root"/>, to be added to a parent and sized by the caller.</returns>
  /// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
  /// <exception cref="InvalidOperationException">The viewport was already built.</exception>
  /// <exception cref="ObjectDisposedException">The viewport was disposed.</exception>
  public ContainerRuntime Build()
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    if (Root != null) throw new InvalidOperationException("Graph viewport was already built.");

    var root = new ContainerRuntime();

    _renderer = new GraphVisRenderer(_style);
    var canvasElement = new GraphVisCanvasElement(new GraphVisCanvasRenderable(Render))
    {
      HasEvents = true,
      Visible = false,
    };
    canvasElement.Dock(Dock.Fill);
    root.AddChild(canvasElement);
    _canvasElement = canvasElement;

    _horizontalScrollBar = CreateScrollBar(EnGraphVisAxis.Horizontal);
    root.AddChild(_horizontalScrollBar);
    _verticalScrollBar = CreateScrollBar(EnGraphVisAxis.Vertical);
    root.AddChild(_verticalScrollBar);
    _scrollBarCorner = CreateScrollBarCorner();
    root.AddChild(_scrollBarCorner);

    Label noGraphLabel = GumUiFactory.CreateLabel(NoGraphText, EditorTheme.Palette.TextMuted);
    noGraphLabel.Anchor(Anchor.Center);
    root.AddChild(noGraphLabel);
    _noGraphLabel = noGraphLabel;

    Root = root;
    return root;
  }

  /// <summary>
  /// Creates a hidden scroll bar along the bottom (horizontal) or right (vertical) edge, leaving
  /// the corner free so the two bars do not overlap.
  /// </summary>
  /// <param name="axis">Axis the bar scrolls.</param>
  /// <returns>Scroll bar.</returns>
  private ScrollBar CreateScrollBar(EnGraphVisAxis axis)
  {
    bool horizontal = axis == EnGraphVisAxis.Horizontal;
    var scrollBar = new ScrollBar
    {
      Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical,
      Minimum = 0,
      SmallChange = _style.ScrollSmallChange,
      IsVisible = false,
    };

    scrollBar.Anchor(horizontal ? Anchor.BottomLeft : Anchor.TopRight);
    scrollBar.Width = horizontal ? -ScrollBarThickness : ScrollBarThickness;
    scrollBar.WidthUnits = horizontal ? DimensionUnitType.RelativeToParent : DimensionUnitType.Absolute;
    scrollBar.Height = horizontal ? ScrollBarThickness : -ScrollBarThickness;
    scrollBar.HeightUnits = horizontal ? DimensionUnitType.Absolute : DimensionUnitType.RelativeToParent;

    GumScrollBarSizing.ApplyThickness(scrollBar, ScrollBarThickness);

    // ValueChanged, not ValueChangedByUi: the latter is raised for thumb drags only, while arrow
    // and track clicks raise just ValueChanged. Values written by SyncScrollBars are ignored there.
    scrollBar.ValueChanged += (_, _) => OnScrollBarMoved(axis, scrollBar);
    return scrollBar;
  }

  /// <summary>
  /// Creates the hidden filler for the bottom-right corner, shown while both scroll bars are.
  /// </summary>
  /// <returns>Corner filler.</returns>
  /// <remarks>
  /// Has events so the pointer over the corner is not over the canvas: a press or the wheel
  /// there must not pan or zoom the graph underneath.
  /// </remarks>
  private static ContainerRuntime CreateScrollBarCorner()
  {
    var corner = new ContainerRuntime
    {
      HasEvents = true,
      Visible = false,
      Width = ScrollBarThickness,
      WidthUnits = DimensionUnitType.Absolute,
      Height = ScrollBarThickness,
      HeightUnits = DimensionUnitType.Absolute,
    };
    corner.Anchor(Anchor.BottomRight);

    var fill = new RectangleRuntime();
    fill.Dock(Dock.Fill);
    GumUiFactory.ApplyColor(fill, EditorTheme.Palette.SurfaceVariant); // same as the scroll bar track
    corner.AddChild(fill);
    return corner;
  }

  #endregion

  #region Graph and frame update

  /// <summary>
  /// Shows a graph, or the "no graph" text.
  /// </summary>
  /// <param name="data">Graph to show; null or an empty graph shows the text.</param>
  public void SetGraph(GraphVisData? data)
  {
    if (_renderer == null || _canvasElement == null || _noGraphLabel == null) return; // not built yet, or disposed

    GraphVisData? graph = data is { IsEmpty: false } ? data : null;
    _canvasElement.Visible = graph != null;
    _noGraphLabel.IsVisible = graph == null;
    _bounds = null;
    HideScrollBars();

    if (graph == null)
    {
      _simulation = null;
      _layoutRoots = [];
      _interaction.SetGraph(null, [], []);
      return;
    }

    _renderer.SetGraph(graph);
    _layoutRoots = Enumerable.Range(0, graph.Nodes.Count).Where(index => graph.Nodes[index].IsLayoutRoot).ToArray();
    RebuildLayout(_renderer);
  }

  /// <summary>Layout algorithm placing the nodes of each new graph.</summary>
  public EnGraphLayoutBuilder LayoutBuilder { get; private set; }

  /// <summary>
  /// Physics in use: the <see cref="LayoutBuilder"/>'s default, with any overrides applied (see
  /// <see cref="SetEdgePullStrength"/>).
  /// </summary>
  public GraphVisPhysics Physics { get; private set; } = GraphLayoutBuilderPicker.For(default).DefaultPhysics;

  /// <summary>
  /// Changes the layout algorithm and resets <see cref="Physics"/> to its default. A shown graph
  /// is laid out again, which clears the selection and fits the view anew.
  /// </summary>
  /// <param name="builder">Layout algorithm.</param>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="builder"/> is not a known layout.</exception>
  public void SetLayoutBuilder(EnGraphLayoutBuilder builder)
  {
    if (builder == LayoutBuilder) return;
    Physics = GraphLayoutBuilderPicker.For(builder).DefaultPhysics;
    LayoutBuilder = builder;
    if (_simulation != null && _renderer != null) RebuildLayout(_renderer);
  }

  /// <summary>
  /// Starts the current layout afresh, as if another layout had been picked and then this one
  /// again: <see cref="Physics"/> goes back to the layout's default and a shown graph is laid out
  /// again, which clears the selection and fits the view anew.
  /// </summary>
  public void ResetLayout()
  {
    Physics = GraphLayoutBuilderPicker.For(LayoutBuilder).DefaultPhysics;
    if (_simulation != null && _renderer != null) RebuildLayout(_renderer);
  }

  /// <summary>
  /// Overrides the edge pull strength (<see cref="GraphVisPhysics.AttractStrength"/>) of
  /// <see cref="Physics"/>. Applies live: the nodes move at once, the layout is not rebuilt and the
  /// view is kept. Choosing a layout resets the override.
  /// </summary>
  /// <param name="strength">Share of a "too far" distance corrected per step (0..1; 0 turns edge pull off).</param>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="strength"/> is outside 0..1.</exception>
  public void SetEdgePullStrength(float strength)
  {
    GraphVisPhysics physics = Physics with { AttractStrength = strength };
    physics.Validate();
    Physics = physics;
    if (_simulation != null) _simulation.Physics = physics;
  }

  /// <summary>
  /// Lays out the renderer's current graph from scratch: starting positions by
  /// <see cref="LayoutBuilder"/>, a fresh simulation with <see cref="Physics"/>, no selection,
  /// view fitted on the next update.
  /// </summary>
  /// <param name="renderer">Renderer holding the graph.</param>
  private void RebuildLayout(GraphVisRenderer renderer)
  {
    HideScrollBars();
    _bounds = null;

    // Edges as node index pairs, which is what layout and physics work with.
    (int From, int To)[] edges = renderer.EdgeShapes.Select(shape => (shape.From, shape.To)).ToArray();

    // Starting layout, then let the physics settle before the graph is first shown, so the
    // view is fitted to the final arrangement and nothing jumps on screen.
    Vector2[] positions = GraphVisLayout.Place(LayoutBuilder, renderer.Radii, edges, Physics.LinkIdealGap, _layoutRoots);
    _simulation = new GraphVisSimulation(positions, renderer.Radii, edges, Physics);
    _simulation.Settle(Physics.SettleSteps);
    _interaction.SetGraph(_simulation, renderer.Radii, renderer.EdgeShapes);
    _highlightFade.Reset(renderer.Radii.Count, renderer.EdgeShapes.Count);
    _fittedSize = null; // new layout: fit it on the next update
  }

  /// <summary>Selected node or edge (indices into the graph last passed to <see cref="SetGraph"/>), or <see cref="GraphVisHit.None"/>.</summary>
  public GraphVisHit Selection => _interaction.Selection;

  /// <summary>
  /// Advances the physics and applies navigation input. Call every frame while the visualizer is
  /// visible; cheap once the nodes have come to rest and nothing is pressed.
  /// </summary>
  /// <param name="deltaSeconds">Elapsed time since the last frame, in seconds.</param>
  public void Update(float deltaSeconds)
  {
    if (_simulation == null || _renderer == null || _canvasElement == null) return;

    // Nodes move only while the simulation is awake (dragging wakes it), so the bounds go stale
    // only then; once asleep they are kept, which keeps a still graph allocation-free.
    if (!_simulation.IsAsleep) _boundsStale = true;
    _simulation.Step(deltaSeconds);

    var viewportSize = new Vector2(_canvasElement.AbsoluteWidth, _canvasElement.AbsoluteHeight);
    if (viewportSize.X <= 0 || viewportSize.Y <= 0) return; // not laid out yet
    _viewportSize = viewportSize;

    if (_boundsStale || _bounds == null)
    {
      _bounds = GraphVisBounds.AroundCircles(_simulation.Positions, _renderer.Radii);
      _boundsStale = false;
    }
    if (_bounds is not { } bounds) return;

    // A new graph is fitted; on a window resize a fitted view is fitted again, a navigated one
    // keeps its zoom and center.
    if (_fittedSize is not { } previousSize) _interaction.FitView(bounds, viewportSize);
    else if (previousSize != viewportSize) _interaction.ResizeView(bounds, previousSize, viewportSize);
    _fittedSize = viewportSize;

    _interaction.Update(ReadInput(_canvasElement, deltaSeconds), bounds, viewportSize);
    _highlightFade.Step(deltaSeconds, _interaction.Hover, _interaction.Selection);
    SyncScrollBars(bounds, viewportSize);
  }

  /// <summary>
  /// Collects this frame's input from the Gum cursor and the keyboard.
  /// </summary>
  /// <param name="canvasElement">Canvas element (gives the viewport position and hover state).</param>
  /// <param name="deltaSeconds">Elapsed time since the last frame, in seconds.</param>
  /// <returns>Input frame in viewport coordinates.</returns>
  private GraphVisInputFrame ReadInput(GraphVisCanvasElement canvasElement, float deltaSeconds)
  {
    ICursor cursor = GumService.Default.Cursor;
    var pointer = new Vector2(
      cursor.XRespectingGumZoomAndBounds() - canvasElement.AbsoluteLeft,
      cursor.YRespectingGumZoomAndBounds() - canvasElement.AbsoluteTop);
    bool isPointerOver = cursor.VisualOver == canvasElement;

    // Keys belong to a focused text box or open list first.
    Vector2 panAxis = Vector2.Zero;
    if (isPointerOver && !GumFocus.IsKeyboardFocused())
    {
      if (_isKeyDown(Keys.A)) panAxis.X -= 1;
      if (_isKeyDown(Keys.D)) panAxis.X += 1;
      if (_isKeyDown(Keys.W)) panAxis.Y -= 1;
      if (_isKeyDown(Keys.S)) panAxis.Y += 1;
    }

    return new GraphVisInputFrame(pointer, isPointerOver, cursor.PrimaryDown, cursor.ScrollWheelChange, panAxis, deltaSeconds);
  }

  #endregion

  #region Scroll bars

  /// <summary>
  /// Shows the scroll bars the graph needs and moves their thumbs to the camera position.
  /// </summary>
  /// <param name="bounds">World rectangle of the graph.</param>
  /// <param name="viewportSize">Viewport size, in pixels.</param>
  private void SyncScrollBars(GraphVisBounds bounds, Vector2 viewportSize)
  {
    if (_horizontalScrollBar == null || _verticalScrollBar == null) return;

    GraphVisCamera camera = _interaction.Camera;
    _isWritingScrollBars = true;
    try
    {
      SyncScrollBar(_horizontalScrollBar, camera.ScrollRange(EnGraphVisAxis.Horizontal, bounds, viewportSize, _style.ViewPadding));
      SyncScrollBar(_verticalScrollBar, camera.ScrollRange(EnGraphVisAxis.Vertical, bounds, viewportSize, _style.ViewPadding));
      SyncScrollBarCorner(_horizontalScrollBar, _verticalScrollBar);
    }
    finally
    {
      _isWritingScrollBars = false;
    }
  }

  /// <summary>
  /// Lets each scroll bar span its whole edge while the other is hidden, and leaves the corner
  /// to the filler while both are shown.
  /// </summary>
  /// <param name="horizontal">Horizontal scroll bar.</param>
  /// <param name="vertical">Vertical scroll bar.</param>
  private void SyncScrollBarCorner(ScrollBar horizontal, ScrollBar vertical)
  {
    float horizontalLength = vertical.IsVisible ? -ScrollBarThickness : 0f;
    if (horizontal.Width != horizontalLength) horizontal.Width = horizontalLength;

    float verticalLength = horizontal.IsVisible ? -ScrollBarThickness : 0f;
    if (vertical.Height != verticalLength) vertical.Height = verticalLength;

    bool showCorner = horizontal.IsVisible && vertical.IsVisible;
    if (_scrollBarCorner != null && _scrollBarCorner.Visible != showCorner) _scrollBarCorner.Visible = showCorner;
  }

  /// <summary>
  /// Updates one scroll bar, writing only values that changed (each Gum property change costs a
  /// layout pass).
  /// </summary>
  /// <param name="scrollBar">Scroll bar.</param>
  /// <param name="range">Values to show.</param>
  private static void SyncScrollBar(ScrollBar scrollBar, GraphVisScrollRange range)
  {
    if (scrollBar.IsVisible != range.IsNeeded) scrollBar.IsVisible = range.IsNeeded;
    if (!range.IsNeeded) return;

    // Range first, so the value is not clamped to an outdated maximum.
    if (Differs(scrollBar.Maximum, range.Maximum)) scrollBar.Maximum = range.Maximum;
    if (Differs(scrollBar.ViewportSize, range.ViewportSize))
    {
      scrollBar.ViewportSize = range.ViewportSize;
      scrollBar.LargeChange = range.ViewportSize; // a click on the track moves one viewport
    }
    if (Differs(scrollBar.Value, range.Value)) scrollBar.Value = range.Value;
  }

  /// <summary>
  /// Tells whether a scroll bar value differs from a wanted value by more than <see cref="ScrollBarEpsilon"/>.
  /// </summary>
  /// <param name="current">Value the scroll bar has.</param>
  /// <param name="wanted">Value it should have.</param>
  /// <returns>True if it should be written.</returns>
  private static bool Differs(double current, float wanted) => Math.Abs(current - wanted) > ScrollBarEpsilon;

  /// <summary>
  /// Moves the camera after the user moved a scroll bar.
  /// </summary>
  /// <param name="axis">Axis of the scroll bar.</param>
  /// <param name="scrollBar">Scroll bar that moved.</param>
  private void OnScrollBarMoved(EnGraphVisAxis axis, ScrollBar scrollBar)
  {
    if (_isWritingScrollBars) return; // our own sync, not the user
    if (_bounds is not { } bounds) return;
    _interaction.ScrollTo(axis, (float)scrollBar.Value, bounds, _viewportSize);
  }

  /// <summary>
  /// Hides both scroll bars (no graph, or a new graph not fitted yet).
  /// </summary>
  private void HideScrollBars()
  {
    if (_horizontalScrollBar != null) _horizontalScrollBar.IsVisible = false;
    if (_verticalScrollBar != null) _verticalScrollBar.IsVisible = false;
    if (_scrollBarCorner != null) _scrollBarCorner.Visible = false;
  }

  #endregion

  /// <summary>
  /// Releases the renderer's Skia resources and hides the canvas, which then draws nothing. The
  /// visuals stay in the Gum tree; remove <see cref="Root"/> separately. Safe to call twice.
  /// </summary>
  public void Dispose()
  {
    if (_disposed) return;
    _disposed = true;

    if (_canvasElement != null) _canvasElement.Visible = false;
    _simulation = null;
    _interaction.SetGraph(null, [], []);
    _renderer?.Dispose();
    _renderer = null; // Render and Update check for null
  }

  /// <summary>
  /// Draws the graph. Called by Gum every frame while the canvas is visible.
  /// </summary>
  /// <param name="rect">Canvas element rectangle, in window pixels.</param>
  /// <param name="canvas">Canvas (window space).</param>
  private void Render(SKRect rect, SKCanvas canvas)
  {
    if (_renderer == null || _simulation == null || _fittedSize == null) return; // nothing fitted to draw yet
    _renderer.Draw(canvas, rect, _interaction.Camera, _simulation.Positions, _interaction.Selection, _highlightFade);
  }
}

/// <summary>
/// Canvas element of the graph viewport. Uses the keyboard (WASD panning) while the pointer is
/// over it, so gameplay ignores the keyboard meanwhile.
/// </summary>
/// <param name="renderable">Renderable drawing the graph.</param>
internal sealed class GraphVisCanvasElement(IRenderable renderable) : InteractiveGue(renderable), IClaimsKeyboardWhileHovered;
