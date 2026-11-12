using System;
using System.Linq;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;
using SkiaSharp;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Drawing area of the graph visualizer: a canvas showing the graph, or a text when there is no
/// graph to show.
/// </summary>
/// <remarks>
/// Part of <see cref="GumGraphVisualizer"/>. The graph is redrawn every frame by Gum through the
/// canvas element; this class only keeps what to draw (graph, node positions, camera).
/// </remarks>
public sealed class GumGraphViewport
{
  /// <summary>Text shown when there is no graph.</summary>
  public const string NoGraphText = "No graph data available";

  /// <summary>Look of the graph.</summary>
  private readonly GraphVisStyle _style;

  /// <summary>Renderer; null until built.</summary>
  private GraphVisRenderer? _renderer;

  /// <summary>Canvas element; null until built.</summary>
  private InteractiveGue? _canvasElement;

  /// <summary>Label shown when there is no graph; null until built.</summary>
  private Label? _noGraphLabel;

  /// <summary>Node centers in world units, in node order.</summary>
  private Vector2[] _positions = [];

  /// <summary>View of the graph.</summary>
  private GraphVisCamera _camera = GraphVisCamera.Identity;

  /// <summary>Viewport size the camera was last fitted to; null forces a fit on the next draw.</summary>
  private Vector2? _fittedSize;

  /// <summary>
  /// Creates the viewport. Call <see cref="Build"/> to create its visuals.
  /// </summary>
  /// <param name="style">Look of the graph.</param>
  /// <exception cref="ArgumentNullException"><paramref name="style"/> is null.</exception>
  public GumGraphViewport(GraphVisStyle style)
  {
    _style = style ?? throw new ArgumentNullException(nameof(style));
  }

  /// <summary>Root container; null until <see cref="Build"/> is called.</summary>
  public ContainerRuntime? Root { get; private set; }

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

    // InteractiveGue (not a plain element) so the canvas can take part in Gum's cursor handling.
    _renderer = new GraphVisRenderer(_style);
    var canvasElement = new InteractiveGue(new GraphVisCanvasRenderable(Render))
    {
      HasEvents = true,
      Visible = false,
    };
    canvasElement.Dock(Dock.Fill);
    root.AddChild(canvasElement);
    _canvasElement = canvasElement;

    Label noGraphLabel = GumUiFactory.CreateLabel(NoGraphText, EditorTheme.Palette.TextMuted);
    noGraphLabel.Anchor(Anchor.Center);
    root.AddChild(noGraphLabel);
    _noGraphLabel = noGraphLabel;

    Root = root;
    return root;
  }

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

    if (data == null || !hasGraph)
    {
      _positions = [];
      return;
    }

    _renderer.SetGraph(data);
    _positions = GraphVisLayout.PlaceOnRing(_renderer.Radii, _style.NodeGap);
    _fittedSize = null; // new graph: fit it on the next draw
  }

  /// <summary>
  /// Draws the graph. Called by Gum every frame while the canvas is visible.
  /// </summary>
  /// <param name="rect">Canvas element rectangle, in window pixels.</param>
  /// <param name="canvas">Canvas (window space).</param>
  private void Render(SKRect rect, SKCanvas canvas)
  {
    if (_renderer == null) return;

    // Fit the whole graph whenever it or the viewport size changed.
    var size = new Vector2(rect.Width, rect.Height);
    if (_fittedSize != size)
    {
      FitToView(size);
      _fittedSize = size;
    }

    _renderer.Draw(canvas, rect, _camera, _positions);
  }

  /// <summary>
  /// Points the camera at the whole graph.
  /// </summary>
  /// <param name="viewportSize">Viewport size, in pixels.</param>
  private void FitToView(Vector2 viewportSize)
  {
    if (_renderer == null) return;

    GraphVisBounds? bounds = GraphVisBounds.AroundCircles(
      _positions.Select((position, index) => (position, _renderer.Radii[index])));
    if (bounds is not { } graphBounds) return;

    _camera = GraphVisCamera.Fit(graphBounds, viewportSize, _style.ViewPadding, _style.MinZoom, _style.MaxFitZoom);
  }
}
