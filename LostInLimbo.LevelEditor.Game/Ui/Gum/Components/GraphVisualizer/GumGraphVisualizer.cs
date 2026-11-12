using System;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;
using Stride.Input;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Reusable Gum graph visualizer: a panel with the drawing area on the left and a data panel for
/// the selection on the right.
/// </summary>
/// <remarks>
/// Knows only the generic <see cref="GraphVisData"/> model; callers translate their own data into
/// it. Callers add <see cref="Root"/> to a parent, size it and feed it with <see cref="SetGraph"/>.
/// </remarks>
public sealed class GumGraphVisualizer : IDisposable
{
  /// <summary>Padding between the panel edges and its parts, and between the parts, in pixels.</summary>
  private const float Padding = 8f;

  /// <summary>Width of the data panel, in pixels.</summary>
  private const float DataPanelWidth = 260f;

  /// <summary>Data panel title while a node is selected.</summary>
  private const string NodeTitle = "Node";

  /// <summary>Data panel title while an edge is selected.</summary>
  private const string EdgeTitle = "Edge";

  /// <summary>Drawing area.</summary>
  private readonly GumGraphViewport _viewport;

  /// <summary>Data column showing the selection's properties.</summary>
  private readonly GumGraphDataPanel _dataPanel = new();

  /// <summary>Graph shown; null when there is none.</summary>
  private GraphVisData? _data;

  /// <summary>Selection the data panel currently shows.</summary>
  private GraphVisHit _shownSelection = GraphVisHit.None;

  /// <summary>
  /// Creates the visualizer. Call <see cref="Build"/> to create its visuals.
  /// </summary>
  /// <param name="isKeyDown">Reports the raw down-state of a key (for keyboard panning).</param>
  /// <param name="style">Look of the graph; null uses <see cref="GraphVisStyle.Default"/>.</param>
  /// <exception cref="ArgumentNullException"><paramref name="isKeyDown"/> is null.</exception>
  public GumGraphVisualizer(Func<Keys, bool> isKeyDown, GraphVisStyle? style = null)
  {
    _viewport = new GumGraphViewport(style ?? GraphVisStyle.Default, isKeyDown);
  }

  /// <summary>Root container; null until <see cref="Build"/> is called.</summary>
  public ContainerRuntime? Root { get; private set; }

  /// <summary>
  /// Builds the visuals, showing "no graph".
  /// </summary>
  /// <returns><see cref="Root"/>, to be added to a parent and sized and positioned by the caller.</returns>
  /// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
  /// <exception cref="InvalidOperationException">The visualizer was already built.</exception>
  public ContainerRuntime Build()
  {
    if (Root != null) throw new InvalidOperationException("Graph visualizer was already built.");

    ContainerRuntime root = GumUiFactory.CreatePanel();

    // Drawing area: everything left of the data panel.
    ContainerRuntime viewport = _viewport.Build();
    viewport.X = Padding;
    viewport.Y = Padding;
    viewport.Width = -(DataPanelWidth + 3 * Padding);
    viewport.WidthUnits = DimensionUnitType.RelativeToParent;
    viewport.Height = -2 * Padding;
    viewport.HeightUnits = DimensionUnitType.RelativeToParent;
    root.AddChild(viewport);

    // Data panel: fixed-width column on the right.
    ContainerRuntime dataPanel = _dataPanel.Build();
    dataPanel.Anchor(Anchor.TopRight);
    dataPanel.X = -Padding;
    dataPanel.Y = Padding;
    dataPanel.Width = DataPanelWidth;
    dataPanel.WidthUnits = DimensionUnitType.Absolute;
    dataPanel.Height = -2 * Padding;
    dataPanel.HeightUnits = DimensionUnitType.RelativeToParent;
    root.AddChild(dataPanel);

    Root = root;
    return root;
  }

  /// <summary>
  /// Shows a graph. Clears the selection.
  /// </summary>
  /// <param name="data">Graph to show; null or an empty graph shows "No graph data available".</param>
  public void SetGraph(GraphVisData? data)
  {
    _data = data is { IsEmpty: false } ? data : null;
    _viewport.SetGraph(data);
    _shownSelection = GraphVisHit.None;
    _dataPanel.Clear();
  }

  /// <summary>
  /// Advances the node physics, applies navigation and selection input and keeps the data panel
  /// in step with the selection. Call every frame while the visualizer is visible.
  /// </summary>
  /// <param name="deltaSeconds">Elapsed time since the last frame, in seconds.</param>
  public void Update(float deltaSeconds)
  {
    _viewport.Update(deltaSeconds);

    GraphVisHit selection = _viewport.Selection;
    if (selection == _shownSelection) return;
    _shownSelection = selection;
    ShowSelection(selection);
  }

  /// <summary>
  /// Releases the drawing resources; the visualizer shows nothing afterwards. The visuals stay in
  /// the Gum tree; remove <see cref="Root"/> separately. Safe to call twice.
  /// </summary>
  public void Dispose()
  {
    _viewport.Dispose();
    _data = null;
    _shownSelection = GraphVisHit.None;
    _dataPanel.Clear();
  }

  /// <summary>
  /// Fills the data panel with the properties of the selected node or edge.
  /// </summary>
  /// <param name="selection">Selection.</param>
  private void ShowSelection(GraphVisHit selection)
  {
    switch (selection.Kind)
    {
      case EnGraphVisHitKind.Node when _data != null && selection.Index < _data.Nodes.Count:
        GraphVisNode node = _data.Nodes[selection.Index];
        _dataPanel.Show(NodeTitle, node.Properties, node.IndentedPropertyKeys);
        break;

      case EnGraphVisHitKind.Edge when _data != null && selection.Index < _data.Edges.Count:
        GraphVisEdge edge = _data.Edges[selection.Index];
        _dataPanel.Show(EdgeTitle, edge.Properties, edge.IndentedPropertyKeys);
        break;

      default:
        _dataPanel.Clear();
        break;
    }
  }
}
