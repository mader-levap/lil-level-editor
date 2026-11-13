using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Render;

/// <summary>
/// Axis-aligned rectangle in world units, used as the extent of a graph.
/// </summary>
/// <param name="Min">Corner with the smallest coordinates.</param>
/// <param name="Max">Corner with the largest coordinates.</param>
public readonly record struct GraphVisBounds(Vector2 Min, Vector2 Max)
{
  /// <summary>Width and height.</summary>
  public Vector2 Size => Max - Min;

  /// <summary>Center point.</summary>
  public Vector2 Center => (Min + Max) / 2f;

  /// <summary>
  /// Computes the rectangle enclosing a set of circles.
  /// </summary>
  /// <param name="circles">Circle centers and radii.</param>
  /// <returns>Enclosing rectangle, or null if there are no circles.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="circles"/> is null.</exception>
  public static GraphVisBounds? AroundCircles(IEnumerable<(Vector2 Center, float Radius)> circles)
  {
    ArgumentNullException.ThrowIfNull(circles);

    GraphVisBounds? bounds = null;
    foreach ((Vector2 center, float radius) in circles)
    {
      var extent = new Vector2(radius);
      Vector2 min = center - extent;
      Vector2 max = center + extent;
      bounds = bounds is { } current
        ? new GraphVisBounds(Vector2.Min(current.Min, min), Vector2.Max(current.Max, max))
        : new GraphVisBounds(min, max);
    }

    return bounds;
  }
}

/// <summary>
/// Screen axis of the graph viewport.
/// </summary>
public enum EnGraphVisAxis
{
  /// <summary>Left to right.</summary>
  Horizontal,

  /// <summary>Top to bottom.</summary>
  Vertical,
}

/// <summary>
/// Scroll bar values for one axis, in viewport pixels. The scroll bar minimum is always 0.
/// </summary>
/// <param name="Maximum">Largest value: content length minus viewport length; 0 when the graph fits.</param>
/// <param name="ViewportSize">Viewport length (thumb size relative to the content).</param>
/// <param name="Value">Current value: 0 when the graph's start (plus padding) is at the near edge.</param>
public readonly record struct GraphVisScrollRange(float Maximum, float ViewportSize, float Value)
{
  /// <summary>
  /// Smallest overflow worth scrolling, in pixels. Less is invisible, and float rounding leaves a
  /// fitted graph that much "too long" on the axis it fills, which would show a needless scroll bar.
  /// </summary>
  public const float MinScrollableLength = 0.5f;

  /// <summary>Whether the graph is longer than the viewport on this axis (by at least <see cref="MinScrollableLength"/>), so a scroll bar is useful.</summary>
  public bool IsNeeded => Maximum > 0;
}

/// <summary>
/// View of the graph: maps world units to viewport pixels as
/// <c>screen = world * Zoom + Translation</c>. Immutable; every operation returns a new camera.
/// </summary>
/// <param name="Zoom">Zoom factor (positive); 1 shows one world unit as one pixel.</param>
/// <param name="Translation">Viewport position of the world origin, in pixels.</param>
/// <remarks>
/// Use <see cref="Identity"/> as the starting value: <c>default</c> has zoom 0, which cannot be
/// inverted.
/// </remarks>
public readonly record struct GraphVisCamera(float Zoom, Vector2 Translation)
{
  /// <summary>Camera showing world units as pixels, world origin at the viewport's top-left corner.</summary>
  public static GraphVisCamera Identity { get; } = new(1f, Vector2.Zero);

  /// <summary>
  /// Converts a world point to viewport pixels.
  /// </summary>
  /// <param name="world">World point.</param>
  /// <returns>Viewport point.</returns>
  public Vector2 WorldToScreen(Vector2 world) => world * Zoom + Translation;

  /// <summary>
  /// Converts a viewport point to world units.
  /// </summary>
  /// <param name="screen">Viewport point, in pixels.</param>
  /// <returns>World point.</returns>
  public Vector2 ScreenToWorld(Vector2 screen) => (screen - Translation) / Zoom;

  /// <summary>
  /// Moves the view.
  /// </summary>
  /// <param name="screenDelta">How far the graph moves on screen, in pixels.</param>
  /// <returns>Moved camera.</returns>
  public GraphVisCamera PanBy(Vector2 screenDelta) => this with { Translation = Translation + screenDelta };

  /// <summary>
  /// Zooms while keeping the world point under a viewport point (usually the mouse) in place.
  /// </summary>
  /// <param name="screenPoint">Viewport point that stays fixed, in pixels.</param>
  /// <param name="factor">Zoom multiplier (positive); above 1 zooms in.</param>
  /// <param name="minZoom">Smallest allowed zoom.</param>
  /// <param name="maxZoom">Largest allowed zoom.</param>
  /// <returns>Zoomed camera; unchanged when the zoom is already at the limit.</returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="factor"/> or <paramref name="minZoom"/> is not positive, or
  /// <paramref name="maxZoom"/> is below <paramref name="minZoom"/>.
  /// </exception>
  public GraphVisCamera ZoomAbout(Vector2 screenPoint, float factor, float minZoom, float maxZoom)
  {
    if (!(factor > 0)) throw new ArgumentOutOfRangeException(nameof(factor), factor, "Zoom factor must be positive.");
    RequireZoomLimits(minZoom, maxZoom);

    float zoom = Math.Clamp(Zoom * factor, minZoom, maxZoom);
    Vector2 anchor = ScreenToWorld(screenPoint);
    return new GraphVisCamera(zoom, screenPoint - anchor * zoom);
  }

  /// <summary>
  /// Creates a camera that shows a whole rectangle, centered in the viewport.
  /// </summary>
  /// <param name="bounds">World rectangle to show.</param>
  /// <param name="viewportSize">Viewport size, in pixels.</param>
  /// <param name="padding">Free space kept at every viewport edge, in pixels.</param>
  /// <param name="minZoom">Smallest allowed zoom.</param>
  /// <param name="maxZoom">Largest allowed zoom (limits how much a small graph is magnified).</param>
  /// <returns>Camera centered on the rectangle; if the zoom limit prevents fitting, the center is still shown.</returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="minZoom"/> is not positive or <paramref name="maxZoom"/> is below it.
  /// </exception>
  public static GraphVisCamera Fit(GraphVisBounds bounds, Vector2 viewportSize, float padding, float minZoom, float maxZoom)
  {
    RequireZoomLimits(minZoom, maxZoom);

    Vector2 size = bounds.Size;
    float availableWidth = viewportSize.X - 2 * padding;
    float availableHeight = viewportSize.Y - 2 * padding;

    // A degenerate axis (single point, or no room) does not constrain the zoom.
    float zoom = maxZoom;
    if (size.X > 0 && availableWidth > 0) zoom = Math.Min(zoom, availableWidth / size.X);
    if (size.Y > 0 && availableHeight > 0) zoom = Math.Min(zoom, availableHeight / size.Y);
    zoom = Math.Clamp(zoom, minZoom, maxZoom);

    return new GraphVisCamera(zoom, viewportSize / 2f - bounds.Center * zoom);
  }

  /// <summary>
  /// Limits panning so the graph cannot be lost. On each axis: a graph smaller than the viewport
  /// stays fully inside it; a larger graph always covers it.
  /// </summary>
  /// <param name="bounds">World rectangle of the graph.</param>
  /// <param name="viewportSize">Viewport size, in pixels.</param>
  /// <param name="padding">Free space kept at every viewport edge, in pixels.</param>
  /// <returns>Camera with the translation moved into the allowed range.</returns>
  public GraphVisCamera ClampTo(GraphVisBounds bounds, Vector2 viewportSize, float padding)
  {
    return this with
    {
      Translation = new Vector2(
        ClampAxis(Translation.X, bounds.Min.X, bounds.Max.X, viewportSize.X, padding),
        ClampAxis(Translation.Y, bounds.Min.Y, bounds.Max.Y, viewportSize.Y, padding)),
    };
  }

  /// <summary>
  /// Computes scroll bar values for one axis. The content is the graph plus padding on both
  /// sides, so the ends of the scroll range match the limits of <see cref="ClampTo"/>.
  /// </summary>
  /// <param name="axis">Axis.</param>
  /// <param name="bounds">World rectangle of the graph.</param>
  /// <param name="viewportSize">Viewport size, in pixels.</param>
  /// <param name="padding">Free space kept at every viewport edge, in pixels.</param>
  /// <returns>Scroll values; <see cref="GraphVisScrollRange.IsNeeded"/> is false when the graph fits (within <see cref="GraphVisScrollRange.MinScrollableLength"/>).</returns>
  public GraphVisScrollRange ScrollRange(EnGraphVisAxis axis, GraphVisBounds bounds, Vector2 viewportSize, float padding)
  {
    (float translation, float worldMin, float worldMax, float viewport) = AxisValues(axis, bounds, viewportSize);
    float startAligned = padding - worldMin * Zoom;
    float endAligned = viewport - padding - worldMax * Zoom;

    float maximum = startAligned - endAligned;
    if (maximum < GraphVisScrollRange.MinScrollableLength) maximum = 0f; // fits, within rounding
    float value = Math.Clamp(startAligned - translation, 0f, maximum);
    return new GraphVisScrollRange(maximum, viewport, value);
  }

  /// <summary>
  /// Moves the view on one axis to a scroll bar value (the inverse of <see cref="ScrollRange"/>).
  /// </summary>
  /// <param name="axis">Axis.</param>
  /// <param name="value">Scroll value; clamped to the scroll range.</param>
  /// <param name="bounds">World rectangle of the graph.</param>
  /// <param name="viewportSize">Viewport size, in pixels.</param>
  /// <param name="padding">Free space kept at every viewport edge, in pixels.</param>
  /// <returns>Camera moved on <paramref name="axis"/> only; unchanged when the graph fits on that axis.</returns>
  public GraphVisCamera ScrolledTo(EnGraphVisAxis axis, float value, GraphVisBounds bounds, Vector2 viewportSize, float padding)
  {
    GraphVisScrollRange range = ScrollRange(axis, bounds, viewportSize, padding);
    if (!range.IsNeeded) return this;

    (_, float worldMin, _, _) = AxisValues(axis, bounds, viewportSize);
    float translation = padding - worldMin * Zoom - Math.Clamp(value, 0f, range.Maximum);
    return this with
    {
      Translation = axis == EnGraphVisAxis.Horizontal
        ? new Vector2(translation, Translation.Y)
        : new Vector2(Translation.X, translation),
    };
  }

  /// <summary>
  /// Picks the values of one axis.
  /// </summary>
  /// <param name="axis">Axis.</param>
  /// <param name="bounds">World rectangle of the graph.</param>
  /// <param name="viewportSize">Viewport size, in pixels.</param>
  /// <returns>Translation, graph extent and viewport length on that axis.</returns>
  private (float Translation, float WorldMin, float WorldMax, float Viewport) AxisValues(
    EnGraphVisAxis axis, GraphVisBounds bounds, Vector2 viewportSize)
  {
    return axis == EnGraphVisAxis.Horizontal
      ? (Translation.X, bounds.Min.X, bounds.Max.X, viewportSize.X)
      : (Translation.Y, bounds.Min.Y, bounds.Max.Y, viewportSize.Y);
  }

  /// <summary>
  /// Clamps the translation on one axis.
  /// </summary>
  /// <param name="translation">Current translation.</param>
  /// <param name="worldMin">Smallest world coordinate of the graph.</param>
  /// <param name="worldMax">Largest world coordinate of the graph.</param>
  /// <param name="viewport">Viewport size on this axis, in pixels.</param>
  /// <param name="padding">Free space at the viewport edges, in pixels.</param>
  /// <returns>Clamped translation.</returns>
  private float ClampAxis(float translation, float worldMin, float worldMax, float viewport, float padding)
  {
    // Translation putting the graph's start at the near edge, and its end at the far edge.
    float startAligned = padding - worldMin * Zoom;
    float endAligned = viewport - padding - worldMax * Zoom;

    // Smaller graph: startAligned < endAligned. Larger graph: the other way round.
    return Math.Clamp(translation, Math.Min(startAligned, endAligned), Math.Max(startAligned, endAligned));
  }

  /// <summary>
  /// Validates zoom limits.
  /// </summary>
  /// <param name="minZoom">Smallest allowed zoom.</param>
  /// <param name="maxZoom">Largest allowed zoom.</param>
  /// <exception cref="ArgumentOutOfRangeException">The limits are not positive or not ordered.</exception>
  private static void RequireZoomLimits(float minZoom, float maxZoom)
  {
    if (!(minZoom > 0)) throw new ArgumentOutOfRangeException(nameof(minZoom), minZoom, "Minimum zoom must be positive.");
    if (!(maxZoom >= minZoom)) throw new ArgumentOutOfRangeException(nameof(maxZoom), maxZoom, "Maximum zoom must not be below the minimum.");
  }
}
