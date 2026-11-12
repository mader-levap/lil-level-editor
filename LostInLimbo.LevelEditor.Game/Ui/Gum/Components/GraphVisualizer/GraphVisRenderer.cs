using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;
using SkiaGum.Renderables;
using SkiaSharp;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Gum renderable that hands Gum's draw call to a callback, which is how the visualizer gets a
/// Skia canvas to draw on.
/// </summary>
/// <remarks>
/// SkiaGum's own canvas renderable is internal, so this derives from the public shape base. Gum
/// passes the canvas in window space, unclipped, together with the element's rectangle in window
/// pixels; the callback must translate and clip itself.
/// </remarks>
internal sealed class GraphVisCanvasRenderable : RenderableShapeBase
{
  /// <summary>Draw callback.</summary>
  private readonly Action<SKRect, SKCanvas> _draw;

  /// <summary>
  /// Creates the renderable.
  /// </summary>
  /// <param name="draw">Callback receiving the element rectangle (window pixels) and the canvas.</param>
  /// <exception cref="ArgumentNullException"><paramref name="draw"/> is null.</exception>
  public GraphVisCanvasRenderable(Action<SKRect, SKCanvas> draw)
  {
    _draw = draw ?? throw new ArgumentNullException(nameof(draw));
  }

  public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
  {
    _draw(boundingRect, canvas);
  }
}

/// <summary>
/// Draws a graph on a Skia canvas: edges, nodes, markers and texts. Holds the Skia resources and
/// the per-graph values that do not change between frames (radii, colors, measured labels).
/// </summary>
/// <remarks>Not covered by unit tests (needs Skia); the math it relies on is in <see cref="GraphVisGeometry"/>.</remarks>
internal sealed class GraphVisRenderer : IDisposable
{
  #region Prepared data

  /// <summary>
  /// One marker dot, ready to draw.
  /// </summary>
  /// <param name="Label">Text above the dot.</param>
  /// <param name="OffsetX">Horizontal offset of the dot from the node center.</param>
  private readonly record struct PreparedDot(string Label, float OffsetX);

  /// <summary>
  /// One node, ready to draw.
  /// </summary>
  /// <param name="Radius">Circle radius.</param>
  /// <param name="Fill">Fill color.</param>
  /// <param name="Outline">Outline color.</param>
  /// <param name="IsDashed">Whether the outline is dashed.</param>
  /// <param name="MarkerColor">Color of marker dots and labels, readable on <paramref name="Fill"/>.</param>
  /// <param name="Dots">Marker dots.</param>
  private readonly record struct PreparedNode(float Radius, SKColor Fill, SKColor Outline, bool IsDashed, SKColor MarkerColor, PreparedDot[] Dots);

  /// <summary>
  /// One edge, ready to draw.
  /// </summary>
  /// <param name="FromIndex">Index of the start node.</param>
  /// <param name="ToIndex">Index of the end node.</param>
  /// <param name="Color">Line color.</param>
  /// <param name="ArrowAtFrom">Whether an arrowhead sits at the start.</param>
  /// <param name="ArrowAtTo">Whether an arrowhead sits at the end.</param>
  /// <param name="IsDashed">Whether the line is dashed.</param>
  /// <param name="Label">Text over the line, or null.</param>
  /// <param name="LabelWidth">Measured width of <paramref name="Label"/>.</param>
  private readonly record struct PreparedEdge(
    int FromIndex, int ToIndex, SKColor Color, bool ArrowAtFrom, bool ArrowAtTo, bool IsDashed, string? Label, float LabelWidth);

  #endregion

  /// <summary>
  /// Share of the font size by which a text baseline sits below the vertical middle of the text,
  /// used to center digits and capitals vertically on a point.
  /// </summary>
  private const float BaselineDropFactor = 0.36f;

  /// <summary>Look of the graph.</summary>
  private readonly GraphVisStyle _style;

  /// <summary>Typeface loaded from the editor font file; null when the file is missing (Skia's default is used).</summary>
  private readonly SKTypeface? _ownedTypeface;

  /// <summary>Font of marker labels.</summary>
  private readonly SKFont _markerFont;

  /// <summary>Font of edge labels.</summary>
  private readonly SKFont _edgeLabelFont;

  /// <summary>Paint for filled shapes and text.</summary>
  private readonly SKPaint _fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };

  /// <summary>Paint for outlines and lines.</summary>
  private readonly SKPaint _stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };

  /// <summary>Dash pattern of dashed outlines and lines.</summary>
  private readonly SKPathEffect _dash;

  /// <summary>Nodes of the current graph.</summary>
  private PreparedNode[] _nodes = [];

  /// <summary>Edges of the current graph.</summary>
  private PreparedEdge[] _edges = [];

  /// <summary>
  /// Creates the renderer and its Skia resources.
  /// </summary>
  /// <param name="style">Look of the graph.</param>
  /// <exception cref="ArgumentNullException"><paramref name="style"/> is null.</exception>
  public GraphVisRenderer(GraphVisStyle style)
  {
    _style = style ?? throw new ArgumentNullException(nameof(style));

    // Same font file the Gum theme registers, so canvas text matches the rest of the UI.
    string fontPath = EditorFonts.ResolvePath(EditorFonts.RegularFile);
    _ownedTypeface = File.Exists(fontPath) ? SKTypeface.FromFile(fontPath) : null;
    SKTypeface typeface = _ownedTypeface ?? SKTypeface.Default;

    _markerFont = new SKFont(typeface, style.MarkerFontSize);
    _edgeLabelFont = new SKFont(typeface, style.EdgeLabelFontSize);
    _dash = SKPathEffect.CreateDash([style.DashLength, style.DashGap], 0);
  }

  /// <summary>Radius of each node of the current graph, in node order.</summary>
  public IReadOnlyList<float> Radii { get; private set; } = [];

  /// <summary>
  /// Prepares a graph for drawing: resolves colors and radii and measures labels once.
  /// </summary>
  /// <param name="data">Graph to draw.</param>
  /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
  public void SetGraph(GraphVisData data)
  {
    ArgumentNullException.ThrowIfNull(data);

    var indexById = new Dictionary<string, int>(data.Nodes.Count);
    _nodes = new PreparedNode[data.Nodes.Count];
    for (int i = 0; i < _nodes.Length; i++)
    {
      GraphVisNode node = data.Nodes[i];
      indexById[node.Id] = i;

      var fill = node.Color ?? _style.DefaultNodeColor;
      _nodes[i] = new PreparedNode(
        GraphVisGeometry.NodeRadius(node.RelativeSize, _style),
        EditorTheme.ToSkColor(fill),
        EditorTheme.ToSkColor(GraphVisColors.Darken(fill, _style.NodeOutlineBrightness)),
        node.IsDashed,
        EditorTheme.ToSkColor(GraphVisColors.ReadableOn(fill, _style.MarkerColor, _style.MarkerColorOnDark)),
        PrepareDots(node));
    }

    _edges = new PreparedEdge[data.Edges.Count];
    for (int i = 0; i < _edges.Length; i++)
    {
      GraphVisEdge edge = data.Edges[i];
      string? label = string.IsNullOrEmpty(edge.Label) ? null : edge.Label;
      _edges[i] = new PreparedEdge(
        indexById[edge.FromId],
        indexById[edge.ToId],
        EditorTheme.ToSkColor(edge.Color ?? _style.DefaultEdgeColor),
        edge.ArrowAtFrom,
        edge.ArrowAtTo,
        edge.IsDashed,
        label,
        label == null ? 0 : _edgeLabelFont.MeasureText(label));
    }

    Radii = _nodes.Select(node => node.Radius).ToArray();
  }

  /// <summary>
  /// Decides and places the marker dots of a node.
  /// </summary>
  /// <param name="node">Node.</param>
  /// <returns>Dots with their horizontal offsets.</returns>
  private PreparedDot[] PrepareDots(GraphVisNode node)
  {
    IReadOnlyList<GraphVisMarkerDot> dots = GraphVisGeometry.MarkerDots(node.Markers, _style.MaxMarkersShown);
    float[] widths = dots.Select(dot => _markerFont.MeasureText(dot.Label)).ToArray();
    IReadOnlyList<float> offsets = GraphVisGeometry.MarkerOffsets(widths, _style.MarkerMinSpacing, _style.MarkerLabelGap);

    var prepared = new PreparedDot[dots.Count];
    for (int i = 0; i < prepared.Length; i++) prepared[i] = new PreparedDot(dots[i].Label, offsets[i]);
    return prepared;
  }

  /// <summary>
  /// Draws the current graph.
  /// </summary>
  /// <param name="canvas">Canvas as Gum hands it out (window space, unclipped).</param>
  /// <param name="rect">Drawing area, in window pixels.</param>
  /// <param name="camera">View of the graph inside the drawing area.</param>
  /// <param name="positions">Node centers in world units, in node order.</param>
  public void Draw(SKCanvas canvas, SKRect rect, GraphVisCamera camera, IReadOnlyList<Vector2> positions)
  {
    canvas.Save();
    canvas.ClipRect(rect);

    _fill.Color = EditorTheme.ToSkColor(_style.BackgroundColor);
    canvas.DrawRect(rect, _fill);

    // Positions belong to another graph (should not happen): draw the background only.
    if (positions.Count == _nodes.Length)
    {
      // From here on everything is drawn in world units.
      canvas.Translate(rect.Left + camera.Translation.X, rect.Top + camera.Translation.Y);
      canvas.Scale(camera.Zoom);

      foreach (PreparedEdge edge in _edges) DrawEdge(canvas, edge, positions);
      for (int i = 0; i < _nodes.Length; i++) DrawNode(canvas, _nodes[i], positions[i]);
      for (int i = 0; i < _nodes.Length; i++) DrawMarkers(canvas, _nodes[i], positions[i]);
      foreach (PreparedEdge edge in _edges) DrawEdgeLabel(canvas, edge, positions);
    }

    canvas.Restore();
  }

  /// <summary>
  /// Computes the visible border-to-border part of an edge.
  /// </summary>
  /// <param name="edge">Edge.</param>
  /// <param name="positions">Node centers.</param>
  /// <returns>Segment, or null when the nodes touch or overlap.</returns>
  private GraphVisSegment? SegmentOf(PreparedEdge edge, IReadOnlyList<Vector2> positions)
  {
    return GraphVisGeometry.EdgeSegment(
      positions[edge.FromIndex], _nodes[edge.FromIndex].Radius,
      positions[edge.ToIndex], _nodes[edge.ToIndex].Radius);
  }

  /// <summary>
  /// Draws the line and arrowheads of an edge.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="edge">Edge.</param>
  /// <param name="positions">Node centers.</param>
  private void DrawEdge(SKCanvas canvas, PreparedEdge edge, IReadOnlyList<Vector2> positions)
  {
    if (SegmentOf(edge, positions) is not { } segment) return;

    if (GraphVisGeometry.LineBetweenArrows(segment, edge.ArrowAtFrom, edge.ArrowAtTo, _style.ArrowLength) is { } line)
    {
      _stroke.Color = edge.Color;
      _stroke.StrokeWidth = _style.EdgeWidth;
      _stroke.PathEffect = edge.IsDashed ? _dash : null;
      canvas.DrawLine(line.Start.X, line.Start.Y, line.End.X, line.End.Y, _stroke);
      _stroke.PathEffect = null;
    }

    _fill.Color = edge.Color;
    if (edge.ArrowAtFrom) DrawArrowHead(canvas, segment.Start, segment.End);
    if (edge.ArrowAtTo) DrawArrowHead(canvas, segment.End, segment.Start);
  }

  /// <summary>
  /// Draws a filled arrowhead with the current fill color.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="tip">Arrow tip.</param>
  /// <param name="from">Point the arrow comes from.</param>
  private void DrawArrowHead(SKCanvas canvas, Vector2 tip, Vector2 from)
  {
    if (GraphVisGeometry.ArrowHead(tip, from, _style.ArrowLength, _style.ArrowHalfWidth) is not { } arrow) return;

    using var builder = new SKPathBuilder();
    builder.MoveTo(arrow.Tip.X, arrow.Tip.Y);
    builder.LineTo(arrow.BaseLeft.X, arrow.BaseLeft.Y);
    builder.LineTo(arrow.BaseRight.X, arrow.BaseRight.Y);
    builder.Close();
    using SKPath path = builder.Detach();
    canvas.DrawPath(path, _fill);
  }

  /// <summary>
  /// Draws the text over an edge on a round plate.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="edge">Edge.</param>
  /// <param name="positions">Node centers.</param>
  private void DrawEdgeLabel(SKCanvas canvas, PreparedEdge edge, IReadOnlyList<Vector2> positions)
  {
    if (edge.Label == null) return;
    if (SegmentOf(edge, positions) is not { } segment) return;

    Vector2 middle = segment.Middle;
    float plateRadius = Math.Max(edge.LabelWidth, _style.EdgeLabelFontSize) / 2f + _style.EdgeLabelPlatePadding;

    _fill.Color = EditorTheme.ToSkColor(_style.EdgeLabelPlateColor);
    canvas.DrawCircle(middle.X, middle.Y, plateRadius, _fill);
    _stroke.Color = edge.Color;
    _stroke.StrokeWidth = 1f;
    canvas.DrawCircle(middle.X, middle.Y, plateRadius, _stroke);

    _fill.Color = EditorTheme.ToSkColor(_style.EdgeLabelTextColor);
    float baseline = middle.Y + _style.EdgeLabelFontSize * BaselineDropFactor;
    canvas.DrawText(edge.Label, middle.X, baseline, SKTextAlign.Center, _edgeLabelFont, _fill);
  }

  /// <summary>
  /// Draws the circle of a node.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="node">Node.</param>
  /// <param name="center">Node center.</param>
  private void DrawNode(SKCanvas canvas, PreparedNode node, Vector2 center)
  {
    _fill.Color = node.Fill;
    canvas.DrawCircle(center.X, center.Y, node.Radius, _fill);

    // The outline is drawn inside the radius, so the visible circle is exactly the layout circle.
    _stroke.Color = node.Outline;
    _stroke.StrokeWidth = _style.NodeOutlineWidth;
    _stroke.PathEffect = node.IsDashed ? _dash : null;
    canvas.DrawCircle(center.X, center.Y, node.Radius - _style.NodeOutlineWidth / 2f, _stroke);
    _stroke.PathEffect = null;
  }

  /// <summary>
  /// Draws the marker dots of a node with their labels.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="node">Node.</param>
  /// <param name="center">Node center.</param>
  private void DrawMarkers(SKCanvas canvas, PreparedNode node, Vector2 center)
  {
    if (node.Dots.Length == 0) return;

    _fill.Color = node.MarkerColor;
    float dotY = center.Y + _style.MarkerDotDrop;
    foreach (PreparedDot dot in node.Dots)
    {
      float dotX = center.X + dot.OffsetX;
      canvas.DrawCircle(dotX, dotY, _style.MarkerRadius, _fill);
      canvas.DrawText(dot.Label, dotX, dotY - _style.MarkerLabelRise, SKTextAlign.Center, _markerFont, _fill);
    }
  }

  /// <summary>
  /// Releases the Skia resources.
  /// </summary>
  public void Dispose()
  {
    _fill.Dispose();
    _stroke.Dispose();
    _dash.Dispose();
    _markerFont.Dispose();
    _edgeLabelFont.Dispose();
    _ownedTypeface?.Dispose();
  }
}
