using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;
using SkiaGum.Renderables;
using SkiaSharp;
using Stride.Input;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Spike;

/// <summary>
/// SPIKE(graph-vis): throwaway Phase 0 research panel for GRAPH_VISUALIZATION.md. Answers how a
/// Skia canvas behaves inside Gum on Stride (origin, clipping, redraw), how primitives and text
/// look, how pointer/wheel/keyboard input can be read and whether a standalone scroll bar works.
/// Removed at the end of Phase 0; not production code.
/// </summary>
public sealed class GraphVisSpike
{
  #region Constants

  /// <summary>Panel width, in percent of the window.</summary>
  private const float PanelWidthPercent = 80f;

  /// <summary>Panel height, in percent of the window.</summary>
  private const float PanelHeightPercent = 55f;

  /// <summary>Margin between the panel and its canvas, in pixels (makes clipping visible).</summary>
  private const float CanvasMargin = 16f;

  /// <summary>Scroll bar thickness, in pixels.</summary>
  private const float ScrollBarThickness = 20f;

  /// <summary>Number of nodes in the load test.</summary>
  private const int LoadNodes = 200;

  /// <summary>Number of edges in the load test.</summary>
  private const int LoadEdges = 200;

  /// <summary>Smoothing factor of the shown timings (exponential moving average).</summary>
  private const float Smoothing = 0.05f;

  /// <summary>Radius of the draggable test circle, in pixels.</summary>
  private const float DragRadius = 26f;

  #endregion

  /// <summary>Whether the pointer is over the spike canvas; read by the keyboard-capture predicate.</summary>
  public static bool IsPointerOverCanvas { get; private set; }

  /// <summary>Raw (ungated) key state reader.</summary>
  private readonly Func<Keys, bool> _isKeyDown;

  /// <summary>Stride mouse wheel delta reader.</summary>
  private readonly Func<float> _strideWheelDelta;

  /// <summary>Canvas element; null until built.</summary>
  private InteractiveGue? _canvasElement;

  /// <summary>Scroll bar; null until built.</summary>
  private ScrollBar? _scrollBar;

  /// <summary>Gum label used to compare font rendering; null until built.</summary>
  private Label? _referenceLabel;

  /// <summary>Typeface for canvas text; null if the font file is missing.</summary>
  private SKTypeface? _typeface;

  /// <summary>Counters of Gum events raised on the canvas element.</summary>
  private readonly Dictionary<string, int> _eventCounts = new();

  /// <summary>Number of CustomRender calls.</summary>
  private long _renderCalls;

  /// <summary>Number of Update calls.</summary>
  private long _updateCalls;

  /// <summary>Smoothed render time, in milliseconds.</summary>
  private float _renderMilliseconds;

  /// <summary>Smoothed frame time, in milliseconds.</summary>
  private float _frameMilliseconds;

  /// <summary>Total elapsed time, in seconds (drives the load test animation).</summary>
  private float _time;

  /// <summary>Pointer position relative to the canvas element, in pixels.</summary>
  private SKPoint _local;

  /// <summary>Accumulated Gum cursor wheel change.</summary>
  private float _gumWheel;

  /// <summary>Accumulated Stride wheel delta.</summary>
  private float _strideWheel;

  /// <summary>Pan offset moved by WASD and the scroll bar, in pixels.</summary>
  private SKPoint _pan;

  /// <summary>Position of the draggable circle, relative to the canvas element.</summary>
  private SKPoint _dragPosition = new(520, 330);

  /// <summary>Whether the draggable circle is being dragged.</summary>
  private bool _dragging;

  /// <summary>Bounding rectangle Gum passed to the last render.</summary>
  private SKRect _bounds;

  /// <summary>Last cursor state line, built in Update and drawn in render.</summary>
  private string _cursorLine = string.Empty;

  /// <summary>
  /// Creates the spike.
  /// </summary>
  /// <param name="isKeyDown">Raw key state reader (UI code may read keys ungated).</param>
  /// <param name="strideWheelDelta">Stride mouse wheel delta reader.</param>
  public GraphVisSpike(Func<Keys, bool> isKeyDown, Func<float> strideWheelDelta)
  {
    _isKeyDown = isKeyDown ?? throw new ArgumentNullException(nameof(isKeyDown));
    _strideWheelDelta = strideWheelDelta ?? throw new ArgumentNullException(nameof(strideWheelDelta));
  }

  /// <summary>
  /// Builds the spike panel and adds it to the Gum root.
  /// </summary>
  public void Build()
  {
    ContainerRuntime panel = GumUiFactory.CreatePanel();
    panel.Anchor(Anchor.Bottom);
    panel.Width = PanelWidthPercent;
    panel.WidthUnits = DimensionUnitType.PercentageOfParent;
    panel.Height = PanelHeightPercent;
    panel.HeightUnits = DimensionUnitType.PercentageOfParent;

    // S1: the canvas. InteractiveGue (not plain GraphicalUiElement) so it can raise events.
    // Finding: SkiaGum's CanvasRenderable (with its CustomRender event) is internal, so the
    // canvas is our own renderable deriving from the public RenderableShapeBase.
    var canvas = new SpikeCanvasRenderable(Render);
    var canvasElement = new InteractiveGue(canvas)
    {
      HasEvents = true,
      X = CanvasMargin,
      Y = CanvasMargin,
      Width = -2 * CanvasMargin,
      WidthUnits = DimensionUnitType.RelativeToParent,
      Height = -2 * CanvasMargin - ScrollBarThickness,
      HeightUnits = DimensionUnitType.RelativeToParent,
    };
    canvasElement.Push += (_, _) => Count("Push");
    canvasElement.Click += (_, _) => Count("Click");
    canvasElement.Dragging += (_, _) => Count("Dragging");
    canvasElement.RollOn += (_, _) => Count("RollOn");
    canvasElement.RollOff += (_, _) => Count("RollOff");
    canvasElement.RightClick += (_, _) => Count("RightClick");
    canvasElement.MouseWheelScroll += (_, _) => Count("MouseWheelScroll");
    panel.AddChild(canvasElement);
    _canvasElement = canvasElement;

    // S4: standalone Forms scroll bar driving the pan offset.
    var scrollBar = new ScrollBar
    {
      Orientation = Orientation.Horizontal,
      Minimum = 0,
      Maximum = 600,
      ViewportSize = 200,
      SmallChange = 20,
      LargeChange = 100,
    };
    scrollBar.Anchor(Anchor.BottomLeft);
    scrollBar.X = CanvasMargin;
    scrollBar.Y = -CanvasMargin / 2;
    scrollBar.Width = -2 * CanvasMargin;
    scrollBar.WidthUnits = DimensionUnitType.RelativeToParent;
    scrollBar.Height = ScrollBarThickness;
    scrollBar.HeightUnits = DimensionUnitType.Absolute;
    scrollBar.ValueChanged += (_, _) => _pan.X = (float)scrollBar.Value;
    panel.AddChild(scrollBar);
    _scrollBar = scrollBar;

    // S2: Gum label to compare canvas text against.
    Label label = GumUiFactory.CreateLabel("Gum label: Red Key 123 Ag", EditorTheme.Palette.TextPrimary);
    label.X = CanvasMargin + 440;
    label.Y = CanvasMargin + 4;
    panel.AddChild(label);
    _referenceLabel = label;

    string fontPath = EditorFonts.ResolvePath(EditorFonts.RegularFile);
    _typeface = System.IO.File.Exists(fontPath) ? SKTypeface.FromFile(fontPath) : null;

    panel.AddToRoot();
  }

  /// <summary>
  /// Reads input (S3). Call every frame.
  /// </summary>
  /// <param name="deltaSeconds">Elapsed time since the last frame, in seconds.</param>
  public void Update(float deltaSeconds)
  {
    if (_canvasElement == null) return;

    _updateCalls++;
    _time += deltaSeconds;
    _frameMilliseconds += (deltaSeconds * 1000f - _frameMilliseconds) * Smoothing;

    ICursor cursor = GumService.Default.Cursor;
    bool over = cursor.VisualOver == _canvasElement;
    IsPointerOverCanvas = over;

    float cursorX = cursor.XRespectingGumZoomAndBounds();
    float cursorY = cursor.YRespectingGumZoomAndBounds();
    _local = new SKPoint(cursorX - _canvasElement.AbsoluteLeft, cursorY - _canvasElement.AbsoluteTop);

    _gumWheel += cursor.ScrollWheelChange;
    _strideWheel += _strideWheelDelta();

    // Drag test: grab the circle on push, follow the pointer while the button stays down.
    if (cursor.PrimaryPush && over && Distance(_local, _dragPosition) <= DragRadius) _dragging = true;
    if (!cursor.PrimaryDown) _dragging = false;
    if (_dragging) _dragPosition = _local;

    // WASD pan, only while the pointer is over the canvas (raw keys: this is UI code).
    if (over)
    {
      const float panSpeed = 200f;
      if (_isKeyDown(Keys.A)) _pan.X -= panSpeed * deltaSeconds;
      if (_isKeyDown(Keys.D)) _pan.X += panSpeed * deltaSeconds;
      if (_isKeyDown(Keys.W)) _pan.Y -= panSpeed * deltaSeconds;
      if (_isKeyDown(Keys.S)) _pan.Y += panSpeed * deltaSeconds;
    }

    _cursorLine = string.Create(CultureInfo.InvariantCulture,
      $"cursor X/Y={cursor.X}/{cursor.Y} respecting={cursorX:0}/{cursorY:0} local={_local.X:0}/{_local.Y:0} over={over} L={cursor.PrimaryDown} M={cursor.MiddleDown} R={cursor.SecondaryDown}");
  }

  /// <summary>
  /// Counts a Gum event.
  /// </summary>
  /// <param name="name">Event name.</param>
  private void Count(string name)
  {
    _eventCounts[name] = _eventCounts.GetValueOrDefault(name) + 1;
  }

  /// <summary>
  /// Distance between two points.
  /// </summary>
  /// <param name="a">First point.</param>
  /// <param name="b">Second point.</param>
  /// <returns>Distance in pixels.</returns>
  private static float Distance(SKPoint a, SKPoint b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

  /// <summary>
  /// Canvas render callback (S1, S2, S5).
  /// </summary>
  /// <param name="bounds">Bounding rectangle Gum passes for the element.</param>
  /// <param name="canvas">Skia canvas handed out by Gum.</param>
  private void Render(SKRect bounds, SKCanvas canvas)
  {
    _bounds = bounds;
    if (_canvasElement == null) return;
    long start = Stopwatch.GetTimestamp();
    _renderCalls++;

    // S1: state of the canvas exactly as Gum hands it out.
    SKMatrix matrix = canvas.TotalMatrix;
    SKRect localClip = canvas.LocalClipBounds;
    SKRectI deviceClip = canvas.DeviceClipBounds;
    float left = _canvasElement.AbsoluteLeft;
    float top = _canvasElement.AbsoluteTop;
    float width = _canvasElement.AbsoluteWidth;
    float height = _canvasElement.AbsoluteHeight;

    // Markers in the untouched coordinate system: a red cross at (0,0) and a magenta cross at the
    // element's absolute position. Whichever sits on the element's top-left corner tells the origin.
    using var probe = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
    probe.Color = SKColors.Red;
    DrawCross(canvas, 0, 0, probe);
    probe.Color = SKColors.Magenta;
    DrawCross(canvas, left, top, probe);

    // From here on draw in element-local coordinates whatever the incoming matrix was, assuming
    // the incoming translation is either zero (screen space) or the element position (local).
    canvas.Save();
    canvas.Translate(left - matrix.TransX / (matrix.ScaleX == 0 ? 1 : matrix.ScaleX), top - matrix.TransY / (matrix.ScaleY == 0 ? 1 : matrix.ScaleY));
    canvas.ClipRect(new SKRect(0, 0, width, height));

    using var fill = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
    using var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke };
    using var textPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = SKColors.White };
    using var font = new SKFont(_typeface ?? SKTypeface.Default, 14);

    // Element frame.
    stroke.Color = SKColors.LimeGreen;
    stroke.StrokeWidth = 1;
    canvas.DrawRect(new SKRect(0.5f, 0.5f, width - 0.5f, height - 0.5f), stroke);

    DrawLoadTest(canvas, width, height, fill, stroke, textPaint, font);
    DrawPrimitives(canvas, fill, stroke, textPaint);

    // Clipping test: a circle crossing the right edge of the element.
    fill.Color = new SKColor(255, 160, 0, 160);
    canvas.DrawCircle(width, height / 2, 60, fill);

    // Draggable circle.
    fill.Color = _dragging ? SKColors.Yellow : SKColors.Orange;
    canvas.DrawCircle(_dragPosition, DragRadius, fill);
    canvas.DrawText("drag me", _dragPosition.X, _dragPosition.Y + 5, SKTextAlign.Center, font, textPaint);

    // Pointer marker in local coordinates: must sit exactly under the mouse cursor.
    stroke.Color = SKColors.Cyan;
    stroke.StrokeWidth = 1;
    canvas.DrawCircle(_local, 10, stroke);

    // Diagnostics.
    string events = string.Join(" ", _eventCounts);
    string[] lines =
    [
      string.Create(CultureInfo.InvariantCulture, $"render calls={_renderCalls} updates={_updateCalls} frame={_frameMilliseconds:0.00} ms render={_renderMilliseconds:0.000} ms"),
      string.Create(CultureInfo.InvariantCulture, $"matrix scale={matrix.ScaleX:0.###}/{matrix.ScaleY:0.###} trans={matrix.TransX:0.#}/{matrix.TransY:0.#} skew={matrix.SkewX:0.###}/{matrix.SkewY:0.###}"),
      string.Create(CultureInfo.InvariantCulture, $"local clip={localClip.Left:0}/{localClip.Top:0}/{localClip.Right:0}/{localClip.Bottom:0} device clip={deviceClip.Left}/{deviceClip.Top}/{deviceClip.Right}/{deviceClip.Bottom}"),
      string.Create(CultureInfo.InvariantCulture, $"element abs left/top={left:0.#}/{top:0.#} size={width:0.#}x{height:0.#}"),
      string.Create(CultureInfo.InvariantCulture, $"DrawBound rect={_bounds.Left:0.#}/{_bounds.Top:0.#}/{_bounds.Right:0.#}/{_bounds.Bottom:0.#}"),
      _cursorLine,
      string.Create(CultureInfo.InvariantCulture, $"wheel gum={_gumWheel:0.##} stride={_strideWheel:0.##}  pan={_pan.X:0}/{_pan.Y:0} scroll={_scrollBar?.Value:0}"),
      $"events: {events}",
      $"gum label font size={(_referenceLabel?.TextComponent as TextRuntime)?.FontSize} font={(_referenceLabel?.TextComponent as TextRuntime)?.Font} typeface loaded={_typeface != null}",
      $"load test: {LoadNodes} nodes + {LoadEdges} edges + {LoadNodes} texts",
    ];
    fill.Color = new SKColor(0, 0, 0, 170);
    canvas.DrawRect(new SKRect(4, 4, 424, 12 + lines.Length * 18), fill);
    for (int i = 0; i < lines.Length; i++)
    {
      using var small = new SKFont(_typeface ?? SKTypeface.Default, 12);
      canvas.DrawText(lines[i], 10, 22 + i * 18, SKTextAlign.Left, small, textPaint);
    }

    canvas.Restore();

    float elapsed = (float)Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    _renderMilliseconds += (elapsed - _renderMilliseconds) * Smoothing;
  }

  /// <summary>
  /// Draws a small cross.
  /// </summary>
  /// <param name="canvas">Canvas.</param>
  /// <param name="x">Center X.</param>
  /// <param name="y">Center Y.</param>
  /// <param name="paint">Stroke paint.</param>
  private static void DrawCross(SKCanvas canvas, float x, float y, SKPaint paint)
  {
    const float arm = 12;
    canvas.DrawLine(x - arm, y, x + arm, y, paint);
    canvas.DrawLine(x, y - arm, x, y + arm, paint);
  }

  /// <summary>
  /// S2: the primitives the real renderer needs (nodes, dashed outline, arrows, dashed edge, texts, markers).
  /// </summary>
  /// <param name="canvas">Canvas (element-local coordinates).</param>
  /// <param name="fill">Fill paint.</param>
  /// <param name="stroke">Stroke paint.</param>
  /// <param name="textPaint">Text paint.</param>
  private void DrawPrimitives(SKCanvas canvas, SKPaint fill, SKPaint stroke, SKPaint textPaint)
  {
    canvas.Save();
    canvas.Translate(440 - _pan.X, 40 - _pan.Y);

    // Canvas text next to the Gum label, same size, for comparison.
    float gumFontSize = (_referenceLabel?.TextComponent as TextRuntime)?.FontSize ?? 18;
    using var labelFont = new SKFont(_typeface ?? SKTypeface.Default, gumFontSize);
    canvas.DrawText("Skia text: Red Key 123 Ag", 0, 40, SKTextAlign.Left, labelFont, textPaint);

    var nodeA = new SKPoint(60, 130);
    var nodeB = new SKPoint(300, 110);
    var nodeC = new SKPoint(200, 250);
    const float radiusA = 44, radiusB = 30, radiusC = 36;

    // Edges first: solid black with arrows on both ends, dashed dark gray with one arrow.
    stroke.StrokeWidth = 4;
    stroke.Color = SKColors.Black;
    DrawEdge(canvas, nodeA, radiusA, nodeB, radiusB, arrowAtFrom: true, arrowAtTo: true, dashed: false, stroke, fill);
    stroke.Color = new SKColor(70, 70, 70);
    DrawEdge(canvas, nodeB, radiusB, nodeC, radiusC, arrowAtFrom: false, arrowAtTo: true, dashed: true, stroke, fill);

    // Edge label centred on the first edge, with a backing plate for legibility.
    using var edgeFont = new SKFont(_typeface ?? SKTypeface.Default, 14);
    var mid = new SKPoint((nodeA.X + nodeB.X) / 2, (nodeA.Y + nodeB.Y) / 2);
    float labelWidth = edgeFont.MeasureText("2");
    fill.Color = SKColors.White;
    canvas.DrawCircle(mid, labelWidth / 2 + 6, fill);
    using var dark = new SKPaint { IsAntialias = true, Color = SKColors.Black };
    canvas.DrawText("2", mid.X, mid.Y + 5, SKTextAlign.Center, edgeFont, dark);

    // Nodes: light gray default, coloured group, dashed (secret) outline.
    DrawNode(canvas, nodeA, radiusA, new SKColor(211, 211, 211), dashed: false, fill, stroke);
    DrawNode(canvas, nodeB, radiusB, new SKColor(120, 170, 240), dashed: false, fill, stroke);
    DrawNode(canvas, nodeC, radiusC, new SKColor(240, 150, 120), dashed: true, fill, stroke);

    // Markers: dots with a small label above each.
    using var markerFont = new SKFont(_typeface ?? SKTypeface.Default, 10);
    fill.Color = SKColors.Black;
    canvas.DrawCircle(nodeA.X - 16, nodeA.Y + 6, 4, fill);
    canvas.DrawText("Start", nodeA.X - 16, nodeA.Y - 3, SKTextAlign.Center, markerFont, dark);
    fill.Color = SKColors.Gold; // highlighted marker
    canvas.DrawCircle(nodeA.X + 16, nodeA.Y + 6, 5, fill);
    canvas.DrawText("Red Key", nodeA.X + 16, nodeA.Y - 3, SKTextAlign.Center, markerFont, dark);

    canvas.Restore();
  }

  /// <summary>
  /// Draws one node: filled circle with a thick darker outline, optionally dashed.
  /// </summary>
  /// <param name="canvas">Canvas.</param>
  /// <param name="center">Node center.</param>
  /// <param name="radius">Node radius.</param>
  /// <param name="color">Fill color.</param>
  /// <param name="dashed">Whether the outline is dashed.</param>
  /// <param name="fill">Fill paint.</param>
  /// <param name="stroke">Stroke paint.</param>
  private static void DrawNode(SKCanvas canvas, SKPoint center, float radius, SKColor color, bool dashed, SKPaint fill, SKPaint stroke)
  {
    fill.Color = color;
    canvas.DrawCircle(center, radius, fill);

    stroke.Color = new SKColor((byte)(color.Red * 0.6f), (byte)(color.Green * 0.6f), (byte)(color.Blue * 0.6f));
    stroke.StrokeWidth = 5;
    using SKPathEffect? dash = dashed ? SKPathEffect.CreateDash([10, 7], 0) : null;
    stroke.PathEffect = dash;
    canvas.DrawCircle(center, radius, stroke);
    stroke.PathEffect = null;
  }

  /// <summary>
  /// Draws one edge between two node borders with optional arrowheads.
  /// </summary>
  /// <param name="canvas">Canvas.</param>
  /// <param name="from">Source node center.</param>
  /// <param name="fromRadius">Source node radius.</param>
  /// <param name="to">Destination node center.</param>
  /// <param name="toRadius">Destination node radius.</param>
  /// <param name="arrowAtFrom">Whether an arrowhead points at the source.</param>
  /// <param name="arrowAtTo">Whether an arrowhead points at the destination.</param>
  /// <param name="dashed">Whether the line is dashed.</param>
  /// <param name="stroke">Stroke paint (color and width set by the caller).</param>
  /// <param name="fill">Fill paint.</param>
  private static void DrawEdge(SKCanvas canvas, SKPoint from, float fromRadius, SKPoint to, float toRadius,
    bool arrowAtFrom, bool arrowAtTo, bool dashed, SKPaint stroke, SKPaint fill)
  {
    const float arrowLength = 14, arrowHalfWidth = 7;
    float length = Distance(from, to);
    if (length <= 0) return;
    var direction = new SKPoint((to.X - from.X) / length, (to.Y - from.Y) / length);
    var normal = new SKPoint(-direction.Y, direction.X);

    var start = new SKPoint(from.X + direction.X * fromRadius, from.Y + direction.Y * fromRadius);
    var end = new SKPoint(to.X - direction.X * toRadius, to.Y - direction.Y * toRadius);

    // The line stops at the arrow bases so a thick line does not poke through the tips.
    SKPoint lineStart = arrowAtFrom ? new SKPoint(start.X + direction.X * arrowLength, start.Y + direction.Y * arrowLength) : start;
    SKPoint lineEnd = arrowAtTo ? new SKPoint(end.X - direction.X * arrowLength, end.Y - direction.Y * arrowLength) : end;

    using SKPathEffect? dash = dashed ? SKPathEffect.CreateDash([10, 7], 0) : null;
    stroke.PathEffect = dash;
    canvas.DrawLine(lineStart, lineEnd, stroke);
    stroke.PathEffect = null;

    fill.Color = stroke.Color;
    if (arrowAtTo) DrawArrow(canvas, end, direction, normal, arrowLength, arrowHalfWidth, fill);
    if (arrowAtFrom) DrawArrow(canvas, start, new SKPoint(-direction.X, -direction.Y), normal, arrowLength, arrowHalfWidth, fill);
  }

  /// <summary>
  /// Draws a filled triangular arrowhead.
  /// </summary>
  /// <param name="canvas">Canvas.</param>
  /// <param name="tip">Arrow tip.</param>
  /// <param name="direction">Unit direction the arrow points in.</param>
  /// <param name="normal">Unit normal of the edge.</param>
  /// <param name="length">Arrow length.</param>
  /// <param name="halfWidth">Half of the arrow base width.</param>
  /// <param name="fill">Fill paint.</param>
  private static void DrawArrow(SKCanvas canvas, SKPoint tip, SKPoint direction, SKPoint normal, float length, float halfWidth, SKPaint fill)
  {
    var baseCenter = new SKPoint(tip.X - direction.X * length, tip.Y - direction.Y * length);
    using var builder = new SKPathBuilder();
    builder.MoveTo(tip);
    builder.LineTo(baseCenter.X + normal.X * halfWidth, baseCenter.Y + normal.Y * halfWidth);
    builder.LineTo(baseCenter.X - normal.X * halfWidth, baseCenter.Y - normal.Y * halfWidth);
    builder.Close();
    using SKPath path = builder.Detach();
    canvas.DrawPath(path, fill);
  }

  /// <summary>
  /// S5: load test, moving nodes with outlines and texts plus edges, in the right part of the canvas.
  /// </summary>
  /// <param name="canvas">Canvas (element-local coordinates).</param>
  /// <param name="width">Element width.</param>
  /// <param name="height">Element height.</param>
  /// <param name="fill">Fill paint.</param>
  /// <param name="stroke">Stroke paint.</param>
  /// <param name="textPaint">Text paint.</param>
  /// <param name="font">Text font.</param>
  private void DrawLoadTest(SKCanvas canvas, float width, float height, SKPaint fill, SKPaint stroke, SKPaint textPaint, SKFont font)
  {
    float areaLeft = width * 0.62f;
    float areaWidth = width - areaLeft - 20;
    float areaHeight = height - 40;

    SKPoint Position(int index)
    {
      // Deterministic pseudo-positions wobbling over time.
      float baseX = (index * 0.618034f) % 1f;
      float baseY = (index * 0.381966f + 0.13f) % 1f;
      return new SKPoint(
        areaLeft + baseX * areaWidth + MathF.Sin(_time * 1.3f + index) * 8,
        20 + baseY * areaHeight + MathF.Cos(_time * 1.1f + index * 0.7f) * 8);
    }

    stroke.StrokeWidth = 2;
    stroke.Color = new SKColor(0, 0, 0, 200);
    using SKPathEffect dash = SKPathEffect.CreateDash([6, 4], 0);
    for (int i = 0; i < LoadEdges; i++)
    {
      stroke.PathEffect = i % 4 == 0 ? dash : null; // a quarter of the edges dashed
      canvas.DrawLine(Position(i), Position((i * 7 + 3) % LoadNodes), stroke);
    }
    stroke.PathEffect = null;

    using var small = new SKFont(_typeface ?? SKTypeface.Default, 9);
    for (int i = 0; i < LoadNodes; i++)
    {
      SKPoint position = Position(i);
      fill.Color = SKColor.FromHsl(i * 37 % 360, 60, 65);
      canvas.DrawCircle(position, 9, fill);
      stroke.Color = SKColor.FromHsl(i * 37 % 360, 60, 35);
      canvas.DrawCircle(position, 9, stroke);
      canvas.DrawText(i.ToString(CultureInfo.InvariantCulture), position.X, position.Y - 11, SKTextAlign.Center, small, textPaint);
    }
  }
}

/// <summary>
/// SPIKE(graph-vis): renderable that forwards Gum's draw call to a callback.
/// </summary>
internal sealed class SpikeCanvasRenderable : RenderableShapeBase
{
  /// <summary>Draw callback.</summary>
  private readonly Action<SKRect, SKCanvas> _draw;

  /// <summary>
  /// Creates the renderable.
  /// </summary>
  /// <param name="draw">Draw callback receiving the bounding rectangle and the canvas.</param>
  public SpikeCanvasRenderable(Action<SKRect, SKCanvas> draw)
  {
    _draw = draw;
  }

  public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
  {
    _draw(boundingRect, canvas);
  }
}
