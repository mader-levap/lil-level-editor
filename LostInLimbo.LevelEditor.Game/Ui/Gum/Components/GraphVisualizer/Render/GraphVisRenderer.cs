using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;
using SkiaGum.Renderables;
using SkiaSharp;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Render;

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
  /// <param name="Offset">Position of the dot's center relative to the node center.</param>
  /// <param name="LabelWidth">Measured width of <paramref name="Label"/>.</param>
  /// <param name="MarkerIds">Identifiers of the markers the dot stands for (several when collapsed).</param>
  private readonly record struct PreparedDot(string Label, Vector2 Offset, float LabelWidth, IReadOnlyList<string> MarkerIds);

  /// <summary>
  /// One node, ready to draw.
  /// </summary>
  /// <param name="Radius">Circle radius.</param>
  /// <param name="Fill">Fill color.</param>
  /// <param name="Outline">Outline color.</param>
  /// <param name="IsDashed">Whether the outline is dashed.</param>
  /// <param name="MarkerColor">Color of marker dots and labels, readable on <paramref name="Fill"/>.</param>
  /// <param name="Dots">Marker dots.</param>
  /// <param name="BaseFill">Fill color before brightening (source of <paramref name="Fill"/>).</param>
  /// <param name="BaseOutline">Outline color before brightening (source of <paramref name="Outline"/>).</param>
  /// <param name="IsEmphasized">Whether the fill is a radial glow (lighter center).</param>
  /// <param name="GlowCenter">Glow color at the center of an emphasized node (fades to <paramref name="Fill"/>).</param>
  /// <param name="Badge">Text of the badge on the rim, or null.</param>
  /// <param name="BadgeWidth">Measured width of <paramref name="Badge"/>.</param>
  private readonly record struct PreparedNode(
    float Radius, SKColor Fill, SKColor Outline, bool IsDashed, SKColor MarkerColor, PreparedDot[] Dots, UiRgba BaseFill, UiRgba BaseOutline,
    bool IsEmphasized, SKColor GlowCenter, string? Badge, float BadgeWidth);

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
  /// <param name="Bend">How far the edge bulges from the straight line (0: straight); see <see cref="GraphVisGeometry.ParallelEdgeBends"/>.</param>
  /// <param name="HighlightMarkerIds">Markers highlighted while the edge is selected.</param>
  /// <param name="BaseColor">Line color before brightening (source of <paramref name="Color"/>).</param>
  private readonly record struct PreparedEdge(
    int FromIndex, int ToIndex, SKColor Color, bool ArrowAtFrom, bool ArrowAtTo, bool IsDashed, string? Label, float LabelWidth,
    float Bend, IReadOnlySet<string> HighlightMarkerIds, UiRgba BaseColor);

  #endregion

  /// <summary>
  /// Share of the font size by which a text baseline sits below the vertical middle of the text,
  /// used to center digits and capitals vertically on a point.
  /// </summary>
  private const float BaselineDropFactor = 0.36f;

  /// <summary>Rim width of the plate behind an edge label when the edge is not selected.</summary>
  private const float EdgeLabelRimWidth = 1f;

  /// <summary>Look of the graph.</summary>
  private readonly GraphVisStyle _style;

  /// <summary>Typeface loaded from the editor font file; null when the file is missing (Skia's default is used).</summary>
  private readonly SKTypeface? _ownedTypeface;

  /// <summary>Font of marker labels.</summary>
  private readonly SKFont _markerFont;

  /// <summary>Font of edge labels.</summary>
  private readonly SKFont _edgeLabelFont;

  /// <summary>Font of node badges.</summary>
  private readonly SKFont _badgeFont;

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
    _badgeFont = new SKFont(typeface, style.BadgeFontSize);
    _dash = SKPathEffect.CreateDash([style.DashLength, style.DashGap], 0);
  }

  /// <summary>Radius of each node of the current graph, in node order.</summary>
  public IReadOnlyList<float> Radii { get; private set; } = [];

  /// <summary>Shape of each edge of the current graph as drawn (nodes and bend), in edge order; used for hit testing.</summary>
  public IReadOnlyList<GraphVisEdgeShape> EdgeShapes { get; private set; } = [];

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

      UiRgba fill = node.Color ?? _style.DefaultNodeColor;
      UiRgba outline = GraphVisColors.Darken(fill, _style.NodeOutlineBrightness);
      float radius = GraphVisGeometry.NodeRadius(node.RelativeSize, _style);
      string? badge = string.IsNullOrEmpty(node.Badge) ? null : node.Badge;
      _nodes[i] = new PreparedNode(
        radius,
        EditorTheme.ToSkColor(fill),
        EditorTheme.ToSkColor(outline),
        node.IsDashed,
        EditorTheme.ToSkColor(GraphVisColors.ReadableOn(fill, _style.MarkerColor, _style.MarkerColorOnDark)),
        PrepareDots(node, radius),
        fill,
        outline,
        node.IsEmphasized,
        EditorTheme.ToSkColor(GlowCenterColor(fill, 0f)),
        badge,
        badge == null ? 0 : _badgeFont.MeasureText(badge));
    }

    // Several edges between the same two nodes are drawn as arcs side by side.
    (int From, int To)[] indexPairs = data.Edges.Select(edge => (indexById[edge.FromId], indexById[edge.ToId])).ToArray();
    float[] bends = GraphVisGeometry.ParallelEdgeBends(indexPairs, _style.ParallelEdgeSpacing);

    _edges = new PreparedEdge[data.Edges.Count];
    for (int i = 0; i < _edges.Length; i++)
    {
      GraphVisEdge edge = data.Edges[i];
      string? label = string.IsNullOrEmpty(edge.Label) ? null : edge.Label;
      UiRgba color = edge.Color ?? _style.DefaultEdgeColor;
      _edges[i] = new PreparedEdge(
        indexPairs[i].From,
        indexPairs[i].To,
        EditorTheme.ToSkColor(color),
        edge.ArrowAtFrom,
        edge.ArrowAtTo,
        edge.IsDashed,
        label,
        label == null ? 0 : _edgeLabelFont.MeasureText(label),
        bends[i],
        new HashSet<string>(edge.HighlightMarkerIds),
        color);
    }

    Radii = _nodes.Select(node => node.Radius).ToArray();
    EdgeShapes = _edges.Select(edge => new GraphVisEdgeShape(edge.FromIndex, edge.ToIndex, edge.Bend)).ToArray();
  }

  /// <summary>
  /// Decides and places the marker dots of a node.
  /// </summary>
  /// <param name="node">Node.</param>
  /// <param name="radius">Node radius.</param>
  /// <returns>Dots with their positions relative to the node center.</returns>
  private PreparedDot[] PrepareDots(GraphVisNode node, float radius)
  {
    IReadOnlyList<GraphVisMarkerDot> dots = GraphVisGeometry.MarkerDots(node.Markers, _style.MaxMarkersShown);
    if (dots.Count == 0) return [];

    float[] widths = dots.Select(dot => _markerFont.MeasureText(dot.Label)).ToArray();
    var metrics = new GraphVisMarkerMetrics(
      _style.MarkerRadius,
      _style.MarkerLabelRise,
      _markerFont.Metrics.CapHeight,
      _style.MarkerRowGap,
      _style.MarkerMinSpacing,
      _style.MarkerLabelGap);

    // Markers should stay clear of the outline, not only of the circle's outer edge.
    float availableRadius = Math.Max(radius - _style.NodeOutlineWidth, _style.MarkerRadius);
    IReadOnlyList<Vector2> offsets = GraphVisGeometry.MarkerPositions(widths, availableRadius, metrics);

    var prepared = new PreparedDot[dots.Count];
    for (int i = 0; i < prepared.Length; i++) prepared[i] = new PreparedDot(dots[i].Label, offsets[i], widths[i], dots[i].MarkerIds);
    return prepared;
  }

  /// <summary>
  /// Draws the current graph.
  /// </summary>
  /// <param name="canvas">Canvas as Gum hands it out (window space, unclipped).</param>
  /// <param name="rect">Drawing area, in window pixels.</param>
  /// <param name="camera">View of the graph inside the drawing area.</param>
  /// <param name="positions">Node centers in world units, in node order.</param>
  /// <param name="selection">Selected node or edge, drawn highlighted.</param>
  /// <param name="fade">Hover/selection brightening level of each node and edge.</param>
  public void Draw(
    SKCanvas canvas, SKRect rect, GraphVisCamera camera, IReadOnlyList<Vector2> positions, GraphVisHit selection, GraphVisHighlightFade fade)
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

      int selectedNode = selection.Kind == EnGraphVisHitKind.Node && selection.Index < _nodes.Length ? selection.Index : -1;
      int selectedEdge = selection.Kind == EnGraphVisHitKind.Edge && selection.Index < _edges.Length ? selection.Index : -1;
      IReadOnlySet<string>? highlightedMarkers = selectedEdge >= 0 ? _edges[selectedEdge].HighlightMarkerIds : null;

      if (selectedEdge >= 0) DrawEdgeGlow(canvas, _edges[selectedEdge], positions);
      for (int i = 0; i < _edges.Length; i++) DrawEdge(canvas, Brightened(_edges[i], fade.EdgeLevel(i)), positions);
      for (int i = 0; i < _nodes.Length; i++) DrawNode(canvas, Brightened(_nodes[i], fade.NodeLevel(i)), positions[i]);
      if (selectedNode >= 0) DrawSelectionRing(canvas, _nodes[selectedNode], positions[selectedNode]);
      for (int i = 0; i < _nodes.Length; i++) DrawMarkers(canvas, _nodes[i], positions[i], highlightedMarkers);
      for (int i = 0; i < _nodes.Length; i++) DrawBadge(canvas, _nodes[i], positions[i]); // over the selection ring
      for (int i = 0; i < _edges.Length; i++) DrawEdgeLabel(canvas, _edges[i], positions, i == selectedEdge);
    }

    canvas.Restore();
  }

  /// <summary>
  /// Gives a node brighter fill and outline colors for its hover/selection level. Marker colors
  /// stay, so markers and texts are unaffected.
  /// </summary>
  /// <param name="node">Node.</param>
  /// <param name="level">Highlight level (0..1).</param>
  /// <returns>Node to draw.</returns>
  private PreparedNode Brightened(PreparedNode node, float level)
  {
    if (level <= 0) return node;
    float amount = _style.HoverBrightening * level;
    return node with
    {
      Fill = EditorTheme.ToSkColor(GraphVisColors.Brighten(node.BaseFill, amount)),
      Outline = EditorTheme.ToSkColor(GraphVisColors.Brighten(node.BaseOutline, amount)),
      GlowCenter = EditorTheme.ToSkColor(GlowCenterColor(node.BaseFill, amount)), // the glow lights up with the node
    };
  }

  /// <summary>
  /// Color at the center of an emphasized node's glow.
  /// </summary>
  /// <param name="baseFill">Node fill before brightening.</param>
  /// <param name="hoverAmount">Hover/selection brightening applied to the whole node (0 for none).</param>
  /// <returns>Glow center color.</returns>
  private UiRgba GlowCenterColor(UiRgba baseFill, float hoverAmount) =>
    GraphVisColors.Brighten(GraphVisColors.Brighten(baseFill, _style.EmphasisGlowBrightening), hoverAmount);

  /// <summary>
  /// Gives an edge a brighter line and arrowhead color for its hover/selection level. Use the
  /// result for the line only; the label keeps the original colors.
  /// </summary>
  /// <param name="edge">Edge.</param>
  /// <param name="level">Highlight level (0..1).</param>
  /// <returns>Edge to draw.</returns>
  private PreparedEdge Brightened(PreparedEdge edge, float level)
  {
    if (level <= 0) return edge;
    return edge with { Color = EditorTheme.ToSkColor(GraphVisColors.Brighten(edge.BaseColor, _style.HoverBrightening * level)) };
  }

  /// <summary>
  /// Draws the highlight glow behind a selected edge: a wider line in the highlight color along
  /// the whole edge, arrowheads included.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="edge">Selected edge.</param>
  /// <param name="positions">Node centers.</param>
  private void DrawEdgeGlow(SKCanvas canvas, PreparedEdge edge, IReadOnlyList<Vector2> positions)
  {
    if (CurveOf(edge, positions) is not { } curve) return; // a straight edge is a curve with bend 0

    using var builder = new SKPathBuilder();
    builder.MoveTo(curve.Start.X, curve.Start.Y);
    builder.QuadTo(curve.Control.X, curve.Control.Y, curve.End.X, curve.End.Y);
    using SKPath path = builder.Detach();

    _stroke.Color = EditorTheme.ToSkColor(_style.HighlightColor);
    _stroke.StrokeWidth = _style.EdgeWidth + 2f * _style.EdgeHighlightSpread;
    _stroke.StrokeCap = SKStrokeCap.Round;
    _stroke.StrokeJoin = SKStrokeJoin.Round;
    canvas.DrawPath(path, _stroke);

    // Arrowheads get the same rim: outlined with twice the spread, the fill drawn later covers the inner half.
    _stroke.StrokeWidth = 2f * _style.EdgeHighlightSpread;
    if (edge.ArrowAtFrom) StrokeArrowHead(canvas, curve.Start, curve.Start + curve.StartTangent);
    if (edge.ArrowAtTo) StrokeArrowHead(canvas, curve.End, curve.End - curve.EndTangent);

    _stroke.StrokeCap = SKStrokeCap.Butt;
    _stroke.StrokeJoin = SKStrokeJoin.Miter;
  }

  /// <summary>
  /// Outlines an arrowhead with the current stroke paint.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="tip">Arrow tip.</param>
  /// <param name="from">Point the arrow comes from.</param>
  private void StrokeArrowHead(SKCanvas canvas, Vector2 tip, Vector2 from)
  {
    if (ArrowHeadPath(tip, from) is not { } path) return;
    using (path) canvas.DrawPath(path, _stroke);
  }

  /// <summary>
  /// Builds the triangle of an arrowhead.
  /// </summary>
  /// <param name="tip">Arrow tip.</param>
  /// <param name="from">Point the arrow comes from.</param>
  /// <returns>Closed path (dispose it), or null when the direction is undefined.</returns>
  private SKPath? ArrowHeadPath(Vector2 tip, Vector2 from)
  {
    if (GraphVisGeometry.ArrowHead(tip, from, _style.ArrowLength, _style.ArrowHalfWidth) is not { } arrow) return null;

    using var builder = new SKPathBuilder();
    builder.MoveTo(arrow.Tip.X, arrow.Tip.Y);
    builder.LineTo(arrow.BaseLeft.X, arrow.BaseLeft.Y);
    builder.LineTo(arrow.BaseRight.X, arrow.BaseRight.Y);
    builder.Close();
    return builder.Detach();
  }

  /// <summary>
  /// Draws the highlight ring around a selected node.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="node">Selected node.</param>
  /// <param name="center">Node center.</param>
  private void DrawSelectionRing(SKCanvas canvas, PreparedNode node, Vector2 center)
  {
    _stroke.Color = EditorTheme.ToSkColor(_style.HighlightColor);
    _stroke.StrokeWidth = _style.SelectionRingWidth;
    canvas.DrawCircle(center.X, center.Y, node.Radius + _style.SelectionRingGap + _style.SelectionRingWidth / 2f, _stroke);
  }

  /// <summary>
  /// Computes the visible border-to-border part of a straight edge.
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
  /// Computes the visible border-to-border part of a curved edge (one of several between two nodes).
  /// </summary>
  /// <param name="edge">Edge.</param>
  /// <param name="positions">Node centers.</param>
  /// <returns>Curve, or null when the nodes touch or overlap.</returns>
  private GraphVisCurve? CurveOf(PreparedEdge edge, IReadOnlyList<Vector2> positions)
  {
    return GraphVisGeometry.EdgeCurve(
      positions[edge.FromIndex], _nodes[edge.FromIndex].Radius,
      positions[edge.ToIndex], _nodes[edge.ToIndex].Radius,
      edge.Bend);
  }

  /// <summary>
  /// Draws the line and arrowheads of an edge: straight, or an arc when it bends.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="edge">Edge.</param>
  /// <param name="positions">Node centers.</param>
  private void DrawEdge(SKCanvas canvas, PreparedEdge edge, IReadOnlyList<Vector2> positions)
  {
    if (edge.Bend != 0)
    {
      DrawCurvedEdge(canvas, edge, positions);
      return;
    }

    if (SegmentOf(edge, positions) is not { } segment) return;

    if (GraphVisGeometry.LineBetweenArrows(segment, edge.ArrowAtFrom, edge.ArrowAtTo, _style.ArrowLength) is { } line)
    {
      SetEdgeStroke(edge);
      canvas.DrawLine(line.Start.X, line.Start.Y, line.End.X, line.End.Y, _stroke);
      _stroke.PathEffect = null;
    }

    _fill.Color = edge.Color;
    if (edge.ArrowAtFrom) DrawArrowHead(canvas, segment.Start, segment.End);
    if (edge.ArrowAtTo) DrawArrowHead(canvas, segment.End, segment.Start);
  }

  /// <summary>
  /// Draws a curved edge with arrowheads following the curve's direction at its ends.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="edge">Edge (with a non-zero bend).</param>
  /// <param name="positions">Node centers.</param>
  private void DrawCurvedEdge(SKCanvas canvas, PreparedEdge edge, IReadOnlyList<Vector2> positions)
  {
    if (CurveOf(edge, positions) is not { } curve) return;

    if (GraphVisGeometry.CurveBetweenArrows(curve, edge.ArrowAtFrom, edge.ArrowAtTo, _style.ArrowLength) is { } line)
    {
      using var builder = new SKPathBuilder();
      builder.MoveTo(line.Start.X, line.Start.Y);
      builder.QuadTo(line.Control.X, line.Control.Y, line.End.X, line.End.Y);
      using SKPath path = builder.Detach();

      SetEdgeStroke(edge);
      canvas.DrawPath(path, _stroke);
      _stroke.PathEffect = null;
    }

    _fill.Color = edge.Color;
    if (edge.ArrowAtFrom) DrawArrowHead(canvas, curve.Start, curve.Start + curve.StartTangent);
    if (edge.ArrowAtTo) DrawArrowHead(canvas, curve.End, curve.End - curve.EndTangent);
  }

  /// <summary>
  /// Sets up the stroke paint for an edge line. Reset <c>_stroke.PathEffect</c> afterwards.
  /// </summary>
  /// <param name="edge">Edge.</param>
  private void SetEdgeStroke(PreparedEdge edge)
  {
    _stroke.Color = edge.Color;
    _stroke.StrokeWidth = _style.EdgeWidth;
    _stroke.PathEffect = edge.IsDashed ? _dash : null;
  }

  /// <summary>
  /// Draws a filled arrowhead with the current fill color.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="tip">Arrow tip.</param>
  /// <param name="from">Point the arrow comes from.</param>
  private void DrawArrowHead(SKCanvas canvas, Vector2 tip, Vector2 from)
  {
    if (ArrowHeadPath(tip, from) is not { } path) return;
    using (path) canvas.DrawPath(path, _fill);
  }

  /// <summary>
  /// Draws the text over an edge on a round plate.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="edge">Edge.</param>
  /// <param name="positions">Node centers.</param>
  /// <param name="isSelected">Whether the edge is selected (the plate gets a highlighted rim).</param>
  private void DrawEdgeLabel(SKCanvas canvas, PreparedEdge edge, IReadOnlyList<Vector2> positions, bool isSelected)
  {
    if (edge.Label == null) return;

    // Halfway along the edge: on the arc for bent edges, so plates of parallel edges sit apart.
    Vector2? labelPoint = edge.Bend != 0 ? CurveOf(edge, positions)?.Middle : SegmentOf(edge, positions)?.Middle;
    if (labelPoint is not { } middle) return;

    DrawTextPlate(
      canvas,
      middle,
      new TextPlateLook(edge.Label, edge.LabelWidth, _edgeLabelFont, _style.EdgeLabelPlatePadding),
      EditorTheme.ToSkColor(_style.EdgeLabelPlateColor),
      isSelected ? EditorTheme.ToSkColor(_style.HighlightColor) : edge.Color,
      isSelected ? _style.SelectionRingWidth : EdgeLabelRimWidth,
      EditorTheme.ToSkColor(_style.EdgeLabelTextColor));
  }

  /// <summary>
  /// Draws a node's badge: its text on a round plate centered on the node's rim at 2 o'clock.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="node">Node.</param>
  /// <param name="center">Node center.</param>
  /// <remarks>Colors are fixed; the badge is not brightened with its node.</remarks>
  private void DrawBadge(SKCanvas canvas, PreparedNode node, Vector2 center)
  {
    if (node.Badge == null) return;

    DrawTextPlate(
      canvas,
      center + GraphVisGeometry.BadgeOffset(node.Radius),
      new TextPlateLook(node.Badge, node.BadgeWidth, _badgeFont, _style.BadgePlatePadding),
      EditorTheme.ToSkColor(_style.BadgeFillColor),
      EditorTheme.ToSkColor(_style.BadgeRimColor),
      _style.BadgeRimWidth,
      EditorTheme.ToSkColor(_style.BadgeTextColor));
  }

  /// <summary>
  /// Text drawn on a round plate, with what is needed to size the plate.
  /// </summary>
  /// <param name="Text">Text.</param>
  /// <param name="TextWidth">Measured width of <paramref name="Text"/> in <paramref name="Font"/>.</param>
  /// <param name="Font">Font of the text.</param>
  /// <param name="Padding">Free space between the text and the plate's rim.</param>
  private readonly record struct TextPlateLook(string Text, float TextWidth, SKFont Font, float Padding);

  /// <summary>
  /// Draws a short text centered on a filled, rimmed round plate.
  /// </summary>
  /// <param name="canvas">Canvas, in world units.</param>
  /// <param name="center">Plate center.</param>
  /// <param name="look">Text, font and padding.</param>
  /// <param name="plateColor">Plate fill color.</param>
  /// <param name="rimColor">Plate rim color.</param>
  /// <param name="rimWidth">Plate rim width.</param>
  /// <param name="textColor">Text color.</param>
  private void DrawTextPlate(
    SKCanvas canvas, Vector2 center, TextPlateLook look, SKColor plateColor, SKColor rimColor, float rimWidth, SKColor textColor)
  {
    float fontSize = look.Font.Size;
    float plateRadius = GraphVisGeometry.TextPlateRadius(look.TextWidth, fontSize, look.Padding);

    _fill.Color = plateColor;
    canvas.DrawCircle(center.X, center.Y, plateRadius, _fill);
    _stroke.Color = rimColor;
    _stroke.StrokeWidth = rimWidth;
    canvas.DrawCircle(center.X, center.Y, plateRadius, _stroke);

    _fill.Color = textColor;
    float baseline = center.Y + fontSize * BaselineDropFactor;
    canvas.DrawText(look.Text, center.X, baseline, SKTextAlign.Center, look.Font, _fill);
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
    if (node.IsEmphasized)
    {
      // Radial glow: lighter center fading to the plain fill before the rim (few nodes, so a
      // shader per frame is cheap).
      using SKShader glow = SKShader.CreateRadialGradient(
        new SKPoint(center.X, center.Y),
        node.Radius,
        [node.GlowCenter, node.Fill],
        [0f, 1f - _style.EmphasisGlowSolidShare],
        SKShaderTileMode.Clamp);
      _fill.Shader = glow;
      canvas.DrawCircle(center.X, center.Y, node.Radius, _fill);
      _fill.Shader = null;
    }
    else
    {
      canvas.DrawCircle(center.X, center.Y, node.Radius, _fill);
    }

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
  /// <param name="highlightedMarkers">Identifiers of highlighted markers (of the selected edge), or null.</param>
  /// <remarks>
  /// A highlighted dot (also a collapsed dot standing for any highlighted marker) sits on a plate
  /// in the highlight color covering the dot and its label, drawn in the bright-node marker color.
  /// </remarks>
  private void DrawMarkers(SKCanvas canvas, PreparedNode node, Vector2 center, IReadOnlySet<string>? highlightedMarkers)
  {
    // Plates first, so a plate never covers a neighbouring marker.
    float padding = _style.MarkerHighlightPadding;
    _fill.Color = EditorTheme.ToSkColor(_style.HighlightColor);
    foreach (PreparedDot dot in node.Dots)
    {
      if (!IsHighlighted(dot, highlightedMarkers)) continue;

      Vector2 position = center + dot.Offset;
      float halfWidth = Math.Max(dot.LabelWidth / 2f, _style.MarkerRadius) + padding;
      var plate = new SKRect(
        position.X - halfWidth,
        position.Y - _style.MarkerLabelRise - _markerFont.Metrics.CapHeight - padding,
        position.X + halfWidth,
        position.Y + _style.MarkerRadius + padding);
      canvas.DrawRoundRect(plate, padding * 2f, padding * 2f, _fill);
    }

    foreach (PreparedDot dot in node.Dots)
    {
      Vector2 position = center + dot.Offset;
      _fill.Color = IsHighlighted(dot, highlightedMarkers) ? EditorTheme.ToSkColor(_style.MarkerColor) : node.MarkerColor;
      canvas.DrawCircle(position.X, position.Y, _style.MarkerRadius, _fill);
      canvas.DrawText(dot.Label, position.X, position.Y - _style.MarkerLabelRise, SKTextAlign.Center, _markerFont, _fill);
    }
  }

  /// <summary>
  /// Tells whether a marker dot stands for any highlighted marker.
  /// </summary>
  /// <param name="dot">Marker dot.</param>
  /// <param name="highlightedMarkers">Identifiers of highlighted markers, or null.</param>
  /// <returns>True if the dot is highlighted.</returns>
  private static bool IsHighlighted(PreparedDot dot, IReadOnlySet<string>? highlightedMarkers)
  {
    return highlightedMarkers is { Count: > 0 } && dot.MarkerIds.Any(highlightedMarkers.Contains);
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
    _badgeFont.Dispose();
    _ownedTypeface?.Dispose();
  }
}
