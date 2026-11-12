using System;
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
/// Turns per-frame input into view changes: drag to pan, wheel to zoom about the pointer, keys to
/// pan, and scroll bar values. Owns the camera.
/// </summary>
/// <remarks>
/// A drag starts only when the button goes down over the viewport, and then continues while the
/// button is held, even outside the viewport. Wheel and keys act only while the pointer is over
/// the viewport. After every change the camera is clamped so the graph cannot be lost, and the
/// user cannot zoom out further than the zoom that shows the whole graph.
/// </remarks>
public sealed class GraphVisInteraction
{
  /// <summary>Limits and speeds.</summary>
  private readonly GraphVisStyle _style;

  /// <summary>Whether the primary button was held in the previous frame (to detect presses).</summary>
  private bool _wasPrimaryDown;

  /// <summary>Pointer position in the previous frame of a drag.</summary>
  private Vector2 _lastPointer;

  /// <summary>
  /// Creates the interaction with the identity camera.
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
  public bool IsPanning { get; private set; }

  /// <summary>
  /// Replaces the view (for example after fitting a new graph) and ends a drag in progress.
  /// </summary>
  /// <param name="camera">New view.</param>
  public void Reset(GraphVisCamera camera)
  {
    Camera = camera;
    IsPanning = false;
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
    GraphVisCamera camera = Camera;

    // Drag: starts with a press over the viewport, follows the pointer while held.
    bool pressed = input.IsPrimaryDown && !_wasPrimaryDown;
    _wasPrimaryDown = input.IsPrimaryDown;
    if (pressed && input.IsPointerOver)
    {
      IsPanning = true;
      _lastPointer = input.Pointer;
    }
    else if (!input.IsPrimaryDown)
    {
      IsPanning = false;
    }

    if (IsPanning)
    {
      camera = camera.PanBy(input.Pointer - _lastPointer);
      _lastPointer = input.Pointer;
    }

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

    if (camera == Camera) return false;
    Camera = camera.ClampTo(bounds, viewportSize, _style.ViewPadding);
    return true;
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
