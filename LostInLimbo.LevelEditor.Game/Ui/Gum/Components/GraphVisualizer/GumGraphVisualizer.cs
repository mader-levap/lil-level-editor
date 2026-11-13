using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Render;
using Stride.Input;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Reusable Gum graph visualizer: a panel with the drawing area on the left and a data panel for
/// the selection on the right, with the layout controls (<see cref="GumGraphControlPanel"/>) below it.
/// </summary>
/// <remarks>
/// Knows only the generic <see cref="GraphVisData"/> model; callers translate their own data into
/// it. Callers add <see cref="Root"/> to a parent, size it and feed it with <see cref="SetGraph"/>.
/// </remarks>
public sealed class GumGraphVisualizer : IDisposable
{
  /// <summary>Padding between the panel edges and its parts, and between the parts, in pixels.</summary>
  private const float Padding = 8f;

  /// <summary>Smallest width of the data panel, in pixels.</summary>
  public const float DataPanelMinWidth = 260f;

  /// <summary>Largest width of the data panel, in pixels: double the smallest.</summary>
  public const float DataPanelMaxWidth = 2 * DataPanelMinWidth;

  /// <summary>Share of the visualizer's width given to the data panel, between its smallest and largest width.</summary>
  public const float DataPanelWidthShare = 0.25f;

  /// <summary>Data panel title while a node is selected.</summary>
  private const string NodeTitle = "Node";

  /// <summary>Data panel title while an edge is selected.</summary>
  private const string EdgeTitle = "Edge";

  /// <summary>Data panel title while nothing is selected (the whole graph's statistics are shown).</summary>
  private const string GraphTitle = "Graph";

  /// <summary>No indented keys (the graph statistics are flat).</summary>
  private static readonly IReadOnlySet<string> NoIndentedKeys = new HashSet<string>();

  /// <summary>Drawing area.</summary>
  private readonly GumGraphViewport _viewport;

  /// <summary>Data column showing the selection's properties.</summary>
  private readonly GumGraphDataPanel _dataPanel = new();

  /// <summary>Layout and physics controls below the data panel.</summary>
  private readonly GumGraphControlPanel _controlPanel = new();

  /// <summary>Control panel container; null until built, or when <see cref="ShowLayoutPicker"/> is off.</summary>
  private ContainerRuntime? _controlPanelContainer;

  /// <summary>Graph shown; null when there is none.</summary>
  private GraphVisData? _data;

  /// <summary>Selection the data panel currently shows.</summary>
  private GraphVisHit _shownSelection = GraphVisHit.None;

  /// <summary>Drawing area container; null until built.</summary>
  private ContainerRuntime? _viewportContainer;

  /// <summary>Data panel container; null until built.</summary>
  private ContainerRuntime? _dataPanelContainer;

  /// <summary>Data panel width currently applied, in pixels.</summary>
  private float _appliedDataPanelWidth = DataPanelMinWidth;

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
  /// Layout algorithm used at first (the first <see cref="EnGraphLayoutBuilder"/> value unless
  /// changed). Read by <see cref="Build"/>; setting it afterwards has no effect.
  /// </summary>
  public EnGraphLayoutBuilder DefaultLayoutBuilder { get; set; }

  /// <summary>
  /// Whether the layout picker and the edge pull slider are shown below the data panel. Read by
  /// <see cref="Build"/>; setting it afterwards has no effect.
  /// </summary>
  public bool ShowLayoutPicker { get; set; } = true;

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
    viewport.WidthUnits = DimensionUnitType.RelativeToParent;
    viewport.Height = -2 * Padding;
    viewport.HeightUnits = DimensionUnitType.RelativeToParent;
    root.AddChild(viewport);
    _viewportContainer = viewport;

    // Data panel: column on the right; its width follows the visualizer's (see UpdateDataPanelWidth).
    ContainerRuntime dataPanel = _dataPanel.Build();
    dataPanel.Anchor(Anchor.TopRight);
    dataPanel.X = -Padding;
    dataPanel.Y = Padding;
    dataPanel.WidthUnits = DimensionUnitType.Absolute;
    dataPanel.Height = -2 * Padding;
    dataPanel.HeightUnits = DimensionUnitType.RelativeToParent;
    root.AddChild(dataPanel);
    _dataPanelContainer = dataPanel;

    _viewport.SetLayoutBuilder(DefaultLayoutBuilder);
    if (ShowLayoutPicker)
    {
      // Control panel at the bottom of the right column; the data panel gives up the room.
      ContainerRuntime controlPanel = _controlPanel.Build(DefaultLayoutBuilder, _viewport.Physics.AttractStrength);
      controlPanel.Anchor(Anchor.BottomRight); // resets X and Y, so the offsets come after
      controlPanel.X = -Padding;
      controlPanel.Y = -Padding;
      controlPanel.WidthUnits = DimensionUnitType.Absolute;
      root.AddChild(controlPanel);
      _controlPanelContainer = controlPanel;
      dataPanel.Height -= GumGraphControlPanel.PanelHeight + Padding;

      _controlPanel.LayoutPicked += (_, builder) => OnLayoutPicked(builder);
      _controlPanel.LayoutRepicked += (_, _) => OnLayoutRepicked();
      _controlPanel.EdgePullChanged += (_, strength) => _viewport.SetEdgePullStrength(strength);
    }

    ApplyDataPanelWidth(DataPanelMinWidth);
    Root = root;
    return root;
  }

  /// <summary>
  /// Computes the data panel width for a visualizer width: a fixed share of it, kept between
  /// <see cref="DataPanelMinWidth"/> and <see cref="DataPanelMaxWidth"/>.
  /// </summary>
  /// <param name="visualizerWidth">Width of the whole visualizer, in pixels.</param>
  /// <returns>Data panel width, in pixels.</returns>
  public static float DataPanelWidthFor(float visualizerWidth)
  {
    float proportional = float.IsFinite(visualizerWidth) ? visualizerWidth * DataPanelWidthShare : 0;
    return Math.Clamp(proportional, DataPanelMinWidth, DataPanelMaxWidth);
  }

  /// <summary>
  /// Resizes the data panel when the visualizer's width changed, and lays the shown properties
  /// out again for the new width.
  /// </summary>
  private void UpdateDataPanelWidth()
  {
    if (Root == null) return;

    float width = DataPanelWidthFor(Root.AbsoluteWidth);
    if (MathF.Abs(width - _appliedDataPanelWidth) < 1f) return; // whole pixels only: no layout churn
    ApplyDataPanelWidth(width);
    ShowSelection(_shownSelection); // which pairs fit on one row depends on the width
  }

  /// <summary>
  /// Sets the data panel width and gives the drawing area the rest.
  /// </summary>
  /// <param name="width">Data panel width, in pixels.</param>
  private void ApplyDataPanelWidth(float width)
  {
    _appliedDataPanelWidth = width;
    if (_dataPanelContainer != null) _dataPanelContainer.Width = width;
    if (_controlPanelContainer != null) _controlPanelContainer.Width = width;
    if (_viewportContainer != null) _viewportContainer.Width = -(width + 3 * Padding);
  }

  #region Layout and physics controls

  /// <summary>
  /// Lays the graph out again with the picked layout and its default physics (the edge pull
  /// slider follows); this resets the view and the selection.
  /// </summary>
  /// <param name="builder">Picked layout.</param>
  private void OnLayoutPicked(EnGraphLayoutBuilder builder)
  {
    _viewport.SetLayoutBuilder(builder);
    _controlPanel.ShowEdgePull(_viewport.Physics.AttractStrength);
    _shownSelection = GraphVisHit.None;
    ShowSelection(GraphVisHit.None);
  }

  /// <summary>
  /// Starts the current layout afresh (default physics, new layout, fitted view, no selection),
  /// as if another layout had been picked and then this one again.
  /// </summary>
  private void OnLayoutRepicked()
  {
    _viewport.ResetLayout();
    _controlPanel.ShowEdgePull(_viewport.Physics.AttractStrength);
    _shownSelection = GraphVisHit.None;
    ShowSelection(GraphVisHit.None);
  }

  /// <summary>
  /// Closes the layout picker's dropdown list if it is open (call when the visualizer is hidden,
  /// so an invisible list does not keep the keyboard).
  /// </summary>
  public void CloseLayoutPicker() => _controlPanel.CloseLayoutPicker();

  #endregion

  /// <summary>
  /// Shows a graph. Clears the selection.
  /// </summary>
  /// <param name="data">Graph to show; null or an empty graph shows "No graph data available".</param>
  public void SetGraph(GraphVisData? data)
  {
    _data = data is { IsEmpty: false } ? data : null;
    _viewport.SetGraph(data);
    _shownSelection = GraphVisHit.None;
    ShowSelection(GraphVisHit.None);
  }

  /// <summary>
  /// Advances the node physics, applies navigation and selection input and keeps the data panel
  /// in step with the selection. Call every frame while the visualizer is visible.
  /// </summary>
  /// <param name="deltaSeconds">Elapsed time since the last frame, in seconds.</param>
  public void Update(float deltaSeconds)
  {
    UpdateDataPanelWidth();
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
    CloseLayoutPicker();
    _viewport.Dispose();
    _data = null;
    _shownSelection = GraphVisHit.None;
    _dataPanel.Clear();
  }

  /// <summary>
  /// Fills the data panel with the properties of the selected node or edge, or with the graph's
  /// statistics when nothing is selected.
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
        if (_data != null) _dataPanel.Show(GraphTitle, GraphVisSummary.Properties(_data), NoIndentedKeys);
        else _dataPanel.Clear(); // no graph: nothing to describe
        break;
    }
  }
}
