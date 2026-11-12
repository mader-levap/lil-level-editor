using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Input of one frame, as seen by the graph viewport. Filled by the Gum shell from the Gum cursor
/// and the keyboard; plain data so <see cref="GraphVisInteraction"/> can be tested headlessly.
/// </summary>
/// <param name="Pointer">Pointer position relative to the viewport's top-left corner, in pixels.</param>
/// <param name="IsPointerOver">Whether the pointer is over the viewport itself (not over a scroll bar or other UI).</param>
/// <param name="IsPrimaryDown">Whether the primary (left) button is held.</param>
/// <param name="WheelNotches">Mouse wheel movement this frame, in notches; positive zooms in.</param>
/// <param name="PanAxis">
/// Keyboard pan direction: X from -1 (left) to 1 (right), Y from -1 (up) to 1 (down); the direction
/// the view moves, so the graph moves the opposite way.
/// </param>
/// <param name="DeltaSeconds">Elapsed time since the last frame, in seconds.</param>
public readonly record struct GraphVisInputFrame(
  Vector2 Pointer,
  bool IsPointerOver,
  bool IsPrimaryDown,
  float WheelNotches,
  Vector2 PanAxis,
  float DeltaSeconds);

/// <summary>
/// Turns per-frame input into view changes and selection: click to select, drag a node to move
/// it, drag empty space or an edge to pan, wheel to zoom about the pointer, keys to pan, and
/// scroll bar values. Owns the camera and the selection.
/// </summary>
/// <remarks>
/// <para>A press over the viewport starts a gesture. Until the pointer has moved farther than
/// <see cref="GraphVisStyle.DragThreshold"/> it is a click: releasing selects what was under the
/// press (or clears the selection on empty space). Beyond the threshold it becomes a drag: a
/// node pressed on is selected, pinned and follows the pointer (its neighbours react through the
/// simulation); anything else pans the view. A drag continues while the button is held, even
/// outside the viewport.</para>
/// <para>Wheel and keys act only while the pointer is over the viewport. After every view change
/// the camera is clamped so the graph cannot be lost, and the user cannot zoom out further than
/// the zoom that shows the whole graph.</para>
/// </remarks>
public sealed class GraphVisInteraction
{
  /// <summary>
  /// What the held primary button is doing.
  /// </summary>
  private enum EnGesture
  {
    /// <summary>Button not held, or pressed outside the viewport.</summary>
    None,

    /// <summary>Pressed over the viewport, not moved beyond the drag threshold yet (may still be a click).</summary>
    Pending,

    /// <summary>Dragging the view.</summary>
    Panning,

    /// <summary>Dragging a node.</summary>
    DraggingNode,
  }

  /// <summary>Limits and speeds.</summary>
  private readonly GraphVisStyle _style;

  /// <summary>Node physics of the current graph; null when there is none (navigation still works).</summary>
  private GraphVisSimulation? _simulation;

  /// <summary>Node radii of the current graph.</summary>
  private IReadOnlyList<float> _radii = [];

  /// <summary>Edge shapes of the current graph.</summary>
  private IReadOnlyList<GraphVisEdgeShape> _edges = [];

  /// <summary>Whether the primary button was held in the previous frame (to detect presses).</summary>
  private bool _wasPrimaryDown;

  /// <summary>Current gesture of the primary button.</summary>
  private EnGesture _gesture;

  /// <summary>Pointer position at the press that started the gesture.</summary>
  private Vector2 _pressPointer;

  /// <summary>Element under the pointer at the press.</summary>
  private GraphVisHit _pressHit = GraphVisHit.None;

  /// <summary>Pointer position in the previous frame of a pan.</summary>
  private Vector2 _lastPointer;

  /// <summary>Offset from the pointer (in world units) to the center of the dragged node, so it does not jump.</summary>
  private Vector2 _grabOffset;

  /// <summary>
  /// Creates the interaction with the identity camera and no graph.
  /// </summary>
  /// <param name="style">Limits and speeds.</param>
  /// <exception cref="ArgumentNullException"><paramref name="style"/> is null.</exception>
  public GraphVisInteraction(GraphVisStyle style)
  {
    _style = style ?? throw new ArgumentNullException(nameof(style));
  }

  /// <summary>Current view.</summary>
  public GraphVisCamera Camera { get; private set; } = GraphVisCamera.Identity;

  /// <summary>Whether the view is being dragged.</summary>
  public bool IsPanning => _gesture == EnGesture.Panning;

  /// <summary>Index of the node being dragged, or null.</summary>
  public int? DraggedNode => _gesture == EnGesture.DraggingNode ? _pressHit.Index : null;

  /// <summary>Selected node or edge, or <see cref="GraphVisHit.None"/>.</summary>
  public GraphVisHit Selection { get; private set; } = GraphVisHit.None;

  /// <summary>
  /// Node or edge under the pointer after the last <see cref="Update"/> (the dragged node while
  /// dragging; nothing while panning), or <see cref="GraphVisHit.None"/>.
  /// </summary>
  public GraphVisHit Hover { get; private set; } = GraphVisHit.None;

  /// <summary>
  /// Sets the graph that clicks and drags act on. Clears the selection and ends a gesture in progress.
  /// </summary>
  /// <param name="simulation">Node physics (gives positions; dragged nodes are moved through it); null for no graph.</param>
  /// <param name="radii">Node radii, in node order.</param>
  /// <param name="edges">Edge shapes as drawn, in edge order.</param>
  /// <exception cref="ArgumentNullException"><paramref name="radii"/> or <paramref name="edges"/> is null.</exception>
  /// <exception cref="ArgumentException">The radii do not match the simulation's nodes.</exception>
  public void SetGraph(GraphVisSimulation? simulation, IReadOnlyList<float> radii, IReadOnlyList<GraphVisEdgeShape> edges)
  {
    ArgumentNullException.ThrowIfNull(radii);
    ArgumentNullException.ThrowIfNull(edges);
    if (simulation != null && simulation.Positions.Count != radii.Count)
      throw new ArgumentException("Every node needs a radius.", nameof(radii));

    EndGesture();
    _simulation = simulation;
    _radii = simulation == null ? [] : radii;
    _edges = simulation == null ? [] : edges;
    Selection = GraphVisHit.None;
    Hover = GraphVisHit.None;
  }

  /// <summary>
  /// Whether the view still shows the whole graph as fitted by <see cref="FitView"/>: true until
  /// the user pans, zooms or scrolls. A fitted view is fitted again when the viewport is resized.
  /// </summary>
  public bool IsFitted { get; private set; }

  /// <summary>
  /// Replaces the view and ends a gesture in progress. The selection stays.
  /// </summary>
  /// <param name="camera">New view (not considered fitted).</param>
  public void Reset(GraphVisCamera camera)
  {
    Camera = camera;
    IsFitted = false;
    EndGesture();
  }

  /// <summary>
  /// Fits the whole graph into the viewport (for a new graph) and ends a gesture in progress.
  /// </summary>
  /// <param name="bounds">World rectangle of the graph.</param>
  /// <param name="viewportSize">Viewport size, in pixels.</param>
  public void FitView(GraphVisBounds bounds, Vector2 viewportSize)
  {
    Reset(GraphVisCamera.Fit(bounds, viewportSize, _style.ViewPadding, _style.MinZoom, _style.MaxFitZoom));
    IsFitted = true;
  }

  /// <summary>
  /// Adapts the view to a new viewport size: a fitted view is fitted again; otherwise the zoom
  /// and the world point at the viewport center are kept (then clamped), so the user's place in
  /// the graph survives a window resize.
  /// </summary>
  /// <param name="bounds">World rectangle of the graph.</param>
  /// <param name="oldViewportSize">Viewport size before the resize, in pixels.</param>
  /// <param name="newViewportSize">Viewport size after the resize, in pixels.</param>
  public void ResizeView(GraphVisBounds bounds, Vector2 oldViewportSize, Vector2 newViewportSize)
  {
    if (IsFitted)
    {
      FitView(bounds, newViewportSize);
      return;
    }

    Vector2 worldCenter = Camera.ScreenToWorld(oldViewportSize / 2f);
    var camera = new GraphVisCamera(Camera.Zoom, newViewportSize / 2f - worldCenter * Camera.Zoom);
    Camera = camera.ClampTo(bounds, newViewportSize, _style.ViewPadding);
  }

  /// <summary>
  /// Applies one frame of input.
  /// </summary>
  /// <param name="input">Input of this frame.</param>
  /// <param name="bounds">World rectangle of the graph.</param>
  /// <param name="viewportSize">Viewport size, in pixels.</param>
  /// <returns>True if the view changed.</returns>
  public bool Update(GraphVisInputFrame input, GraphVisBounds bounds, Vector2 viewportSize)
  {
    GraphVisCamera camera = UpdateGesture(input, Camera);

    if (input.IsPointerOver)
    {
      // Wheel: zoom about the pointer, no further out than the whole graph.
      if (input.WheelNotches != 0)
      {
        float factor = MathF.Pow(_style.ZoomStep, input.WheelNotches);
        camera = camera.ZoomAbout(input.Pointer, factor, MinZoomFor(bounds, viewportSize), _style.MaxZoom);
      }

      // Keys: the view moves along the axis, so the graph moves the other way.
      Vector2 axis = input.PanAxis;
      if (axis != Vector2.Zero)
      {
        if (axis.LengthSquared() > 1) axis.Normalize(); // diagonals are not faster
        camera = camera.PanBy(-axis * _style.KeyboardPanSpeed * input.DeltaSeconds);
      }
    }

    bool viewChanged = camera != Camera;
    if (viewChanged)
    {
      Camera = camera.ClampTo(bounds, viewportSize, _style.ViewPadding);
      IsFitted = false; // the user moved the view
    }
    Hover = HoverFor(input);
    return viewChanged;
  }

  /// <summary>
  /// Decides what counts as hovered after this frame's input: the dragged node while dragging,
  /// nothing while panning or with the pointer elsewhere, otherwise the element under the pointer.
  /// </summary>
  /// <param name="input">Input of this frame.</param>
  /// <returns>Hovered element.</returns>
  private GraphVisHit HoverFor(GraphVisInputFrame input)
  {
    return _gesture switch
    {
      EnGesture.DraggingNode => _pressHit,
      EnGesture.Panning => GraphVisHit.None,
      _ => input.IsPointerOver ? HitAt(input.Pointer) : GraphVisHit.None,
    };
  }

  /// <summary>
  /// Moves the view to a scroll bar value.
  /// </summary>
  /// <param name="axis">Axis of the scroll bar.</param>
  /// <param name="value">Scroll bar value.</param>
  /// <param name="bounds">World rectangle of the graph.</param>
  /// <param name="viewportSize">Viewport size, in pixels.</param>
  public void ScrollTo(EnGraphVisAxis axis, float value, GraphVisBounds bounds, Vector2 viewportSize)
  {
    Camera = Camera.ScrolledTo(axis, value, bounds, viewportSize, _style.ViewPadding);
    IsFitted = false;
  }

  /// <summary>
  /// Finds the node or edge under a viewport point.
  /// </summary>
  /// <param name="pointer">Point relative to the viewport's top-left corner, in pixels.</param>
  /// <returns>Element under the point, or <see cref="GraphVisHit.None"/>.</returns>
  public GraphVisHit HitAt(Vector2 pointer)
  {
    if (_simulation == null) return GraphVisHit.None;

    // The tolerance is in screen pixels beside the drawn line, so it stays the same on screen at any zoom.
    float tolerance = _style.EdgeWidth / 2f + _style.EdgeHitTolerance / Camera.Zoom;
    return GraphVisHitTest.Find(Camera.ScreenToWorld(pointer), _simulation.Positions, _radii, _edges, tolerance);
  }

  /// <summary>
  /// Advances the primary-button gesture by one frame: click, node drag or pan.
  /// </summary>
  /// <param name="input">Input of this frame.</param>
  /// <param name="camera">View so far this frame.</param>
  /// <returns>View after panning.</returns>
  private GraphVisCamera UpdateGesture(GraphVisInputFrame input, GraphVisCamera camera)
  {
    bool pressed = input.IsPrimaryDown && !_wasPrimaryDown;
    _wasPrimaryDown = input.IsPrimaryDown;

    if (!input.IsPrimaryDown)
    {
      // Released without moving far: a click selects what was under the press.
      if (_gesture == EnGesture.Pending) Selection = _pressHit;
      EndGesture();
      return camera;
    }

    if (pressed && input.IsPointerOver)
    {
      _gesture = EnGesture.Pending;
      _pressPointer = input.Pointer;
      _pressHit = HitAt(input.Pointer);
    }

    if (_gesture == EnGesture.Pending && Vector2.Distance(input.Pointer, _pressPointer) > _style.DragThreshold)
    {
      if (_pressHit.Kind == EnGraphVisHitKind.Node && _simulation != null)
      {
        _gesture = EnGesture.DraggingNode;
        Selection = _pressHit;
        _simulation.PinnedNode = _pressHit.Index;
        _grabOffset = _simulation.Positions[_pressHit.Index] - camera.ScreenToWorld(_pressPointer);
      }
      else
      {
        // Pan from the press point, so the movement below the threshold is not lost.
        _gesture = EnGesture.Panning;
        _lastPointer = _pressPointer;
      }
    }

    switch (_gesture)
    {
      case EnGesture.Panning:
        camera = camera.PanBy(input.Pointer - _lastPointer);
        _lastPointer = input.Pointer;
        break;

      case EnGesture.DraggingNode when _simulation != null:
        Vector2 target = camera.ScreenToWorld(input.Pointer) + _grabOffset;
        if (target != _simulation.Positions[_pressHit.Index]) _simulation.MoveNode(_pressHit.Index, target);
        break;
    }

    return camera;
  }

  /// <summary>
  /// Ends the current gesture and releases a dragged node to the physics.
  /// </summary>
  private void EndGesture()
  {
    if (_gesture == EnGesture.DraggingNode && _simulation != null)
    {
      _simulation.PinnedNode = null;
      _simulation.Wake();
    }

    _gesture = EnGesture.None;
    _pressHit = GraphVisHit.None;
  }

  /// <summary>
  /// Smallest zoom the user may reach: the zoom fit-to-view would choose for the whole graph.
  /// </summary>
  /// <param name="bounds">World rectangle of the graph.</param>
  /// <param name="viewportSize">Viewport size, in pixels.</param>
  /// <returns>Smallest zoom.</returns>
  private float MinZoomFor(GraphVisBounds bounds, Vector2 viewportSize)
  {
    float fitZoom = GraphVisCamera.Fit(bounds, viewportSize, _style.ViewPadding, _style.MinZoom, _style.MaxFitZoom).Zoom;

    // Never above the current zoom: if the graph shrank since (nodes moved closer), zooming out
    // must not jump inwards.
    return Math.Max(_style.MinZoom, Math.Min(fitZoom, Camera.Zoom));
  }
}
