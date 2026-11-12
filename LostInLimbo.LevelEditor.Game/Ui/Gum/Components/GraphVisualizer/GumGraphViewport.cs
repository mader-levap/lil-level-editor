using System;
using System.Collections.Generic;
using System.Linq;
using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
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
/// <para>Navigation: left-drag pans, the wheel zooms about the pointer, WASD pans while the
/// pointer is over the canvas (the canvas claims the keyboard meanwhile, so the 3D camera stays
/// still; see <see cref="IClaimsKeyboardWhileHovered"/>). Scroll bars lie over the canvas edges
/// and appear only on axes where the graph is larger than the viewport.</para>
/// </remarks>
public sealed class GumGraphViewport
{
  /// <summary>Text shown when there is no graph.</summary>
  public const string NoGraphText = "No graph data available";

  /// <summary>Scroll bar thickness, in pixels.</summary>
  private const float ScrollBarThickness = 15f;

  /// <summary>Name of the element holding track and thumb in Gum's default scroll bar visual.</summary>
  private const string ScrollBarTrackContainerName = "ThumbContainer";

  /// <summary>Smallest scroll bar change that is written back to a bar, in pixels (avoids needless Gum layout passes).</summary>
  private const float ScrollBarEpsilon = 0.01f;

  /// <summary>Look of the graph.</summary>
  private readonly GraphVisStyle _style;

  /// <summary>Raw key state reader (UI code reads the keyboard directly, see <c>GameInput</c>).</summary>
  private readonly Func<Keys, bool> _isKeyDown;

  /// <summary>Navigation state and camera.</summary>
  private readonly GraphVisInteraction _interaction;

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

  /// <summary>Physics moving the nodes of the current graph; null when there is no graph.</summary>
  private GraphVisSimulation? _simulation;

  /// <summary>Viewport size the camera was last fitted to; null forces a fit on the next update.</summary>
  private Vector2? _fittedSize;

  /// <summary>Graph extent at the last update; null when there is no graph.</summary>
  private GraphVisBounds? _bounds;

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
  public ContainerRuntime Build()
  {
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

    MakeArrowButtonsSquare(scrollBar, horizontal);

    // ValueChanged, not ValueChangedByUi: the latter is raised for thumb drags only, while arrow
    // and track clicks raise just ValueChanged. Values written by SyncScrollBars are ignored there.
    scrollBar.ValueChanged += (_, _) => OnScrollBarMoved(axis, scrollBar);
    return scrollBar;
  }

  /// <summary>
  /// Sizes the arrow buttons as squares with the bar's thickness as their side, and the track
  /// between them to fill the rest.
  /// </summary>
  /// <param name="scrollBar">Scroll bar (orientation already set).</param>
  /// <param name="horizontal">Whether the bar is horizontal.</param>
  /// <remarks>
  /// Gum's default scroll bar visual makes each button a fixed 24 px long along the bar and the
  /// track container (<c>ThumbContainer</c>) 48 px shorter than the bar. Across the bar, its
  /// horizontal layout also keeps the buttons 24 px tall, so they stuck out of a thinner bar.
  /// </remarks>
  private static void MakeArrowButtonsSquare(ScrollBar scrollBar, bool horizontal)
  {
    foreach (Button? button in new[] { scrollBar.UpButton, scrollBar.DownButton })
    {
      if (button == null) continue;
      GraphicalUiElement visual = button.Visual;
      visual.Width = horizontal ? ScrollBarThickness : 0;
      visual.WidthUnits = horizontal ? DimensionUnitType.Absolute : DimensionUnitType.RelativeToParent;
      visual.Height = horizontal ? 0 : ScrollBarThickness;
      visual.HeightUnits = horizontal ? DimensionUnitType.RelativeToParent : DimensionUnitType.Absolute;
    }

    if (scrollBar.Visual.GetGraphicalUiElementByName(ScrollBarTrackContainerName) is { } trackContainer)
    {
      if (horizontal) trackContainer.Width = -2 * ScrollBarThickness;
      else trackContainer.Height = -2 * ScrollBarThickness;
    }
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
    if (_renderer == null || _canvasElement == null || _noGraphLabel == null) return; // not built yet

    bool hasGraph = data is { IsEmpty: false };
    _canvasElement.Visible = hasGraph;
    _noGraphLabel.IsVisible = !hasGraph;
    _bounds = null;
    HideScrollBars();

    if (data == null || !hasGraph)
    {
      _simulation = null;
      return;
    }

    _renderer.SetGraph(data);

    // Edges as node index pairs, which is what layout and physics work with.
    var indexById = new Dictionary<string, int>(data.Nodes.Count);
    for (int i = 0; i < data.Nodes.Count; i++) indexById[data.Nodes[i].Id] = i;
    (int From, int To)[] edges = data.Edges.Select(edge => (indexById[edge.FromId], indexById[edge.ToId])).ToArray();

    // Starting layout, then let the physics settle before the graph is first shown, so the
    // view is fitted to the final arrangement and nothing jumps on screen.
    Vector2[] positions = GraphVisLayout.Place(_renderer.Radii, edges, _style.NodeGap * _style.LinkIdealGapFactor);
    _simulation = new GraphVisSimulation(positions, _renderer.Radii, edges, _style);
    _simulation.Settle(_style.SimulationSettleSteps);
    _fittedSize = null; // new graph: fit it on the next update
  }

  /// <summary>
  /// Advances the physics and applies navigation input. Call every frame while the visualizer is
  /// visible; cheap once the nodes have come to rest and nothing is pressed.
  /// </summary>
  /// <param name="deltaSeconds">Elapsed time since the last frame, in seconds.</param>
  public void Update(float deltaSeconds)
  {
    if (_simulation == null || _renderer == null || _canvasElement == null) return;

    _simulation.Step(deltaSeconds);

    var viewportSize = new Vector2(_canvasElement.AbsoluteWidth, _canvasElement.AbsoluteHeight);
    if (viewportSize.X <= 0 || viewportSize.Y <= 0) return; // not laid out yet
    _viewportSize = viewportSize;

    _bounds = GraphVisBounds.AroundCircles(
      _simulation.Positions.Select((position, index) => (position, _renderer.Radii[index])));
    if (_bounds is not { } bounds) return;

    // Fit the whole graph when it is new or the viewport size changed (window resize).
    if (_fittedSize != viewportSize)
    {
      _interaction.Reset(GraphVisCamera.Fit(bounds, viewportSize, _style.ViewPadding, _style.MinZoom, _style.MaxFitZoom));
      _fittedSize = viewportSize;
    }

    _interaction.Update(ReadInput(_canvasElement, deltaSeconds), bounds, viewportSize);
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
  /// Draws the graph. Called by Gum every frame while the canvas is visible.
  /// </summary>
  /// <param name="rect">Canvas element rectangle, in window pixels.</param>
  /// <param name="canvas">Canvas (window space).</param>
  private void Render(SKRect rect, SKCanvas canvas)
  {
    if (_renderer == null || _simulation == null || _fittedSize == null) return; // nothing fitted to draw yet
    _renderer.Draw(canvas, rect, _interaction.Camera, _simulation.Positions);
  }
}

/// <summary>
/// Canvas element of the graph viewport. Uses the keyboard (WASD panning) while the pointer is
/// over it, so gameplay ignores the keyboard meanwhile.
/// </summary>
/// <param name="renderable">Renderable drawing the graph.</param>
internal sealed class GraphVisCanvasElement(IRenderable renderable) : InteractiveGue(renderable), IClaimsKeyboardWhileHovered;
