using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;
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

  /// <summary>Caption of the layout picker.</summary>
  private const string LayoutPickerCaption = "Layout:";

  /// <summary>Height of the layout picker row below the data panel, in pixels.</summary>
  private const float LayoutPickerRowHeight = 32f;

  /// <summary>Width of the layout picker caption, in pixels.</summary>
  private const float LayoutPickerCaptionWidth = 64f;

  /// <summary>Drawing area.</summary>
  private readonly GumGraphViewport _viewport;

  /// <summary>Data column showing the selection's properties.</summary>
  private readonly GumGraphDataPanel _dataPanel = new();

  /// <summary>Graph shown; null when there is none.</summary>
  private GraphVisData? _data;

  /// <summary>Selection the data panel currently shows.</summary>
  private GraphVisHit _shownSelection = GraphVisHit.None;

  /// <summary>Drawing area container; null until built.</summary>
  private ContainerRuntime? _viewportContainer;

  /// <summary>Data panel container; null until built.</summary>
  private ContainerRuntime? _dataPanelContainer;

  /// <summary>Layout picker row container; null until built, or when <see cref="ShowLayoutPicker"/> is off.</summary>
  private ContainerRuntime? _layoutPickerRow;

  /// <summary>Layout picker; null until built, or when <see cref="ShowLayoutPicker"/> is off.</summary>
  private ComboBox? _layoutPicker;

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
  /// Whether a layout picker is shown below the data panel. Read by <see cref="Build"/>; setting
  /// it afterwards has no effect.
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
      // Picker row at the bottom of the right column; the data panel gives up the room.
      ContainerRuntime pickerRow = CreateLayoutPickerRow();
      root.AddChild(pickerRow);
      _layoutPickerRow = pickerRow;
      dataPanel.Height -= LayoutPickerRowHeight + Padding;
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
    if (_layoutPickerRow != null) _layoutPickerRow.Width = width;
    if (_viewportContainer != null) _viewportContainer.Width = -(width + 3 * Padding);
  }

  /// <summary>
  /// Creates the layout picker row: a caption and a combo box listing every
  /// <see cref="EnGraphLayoutBuilder"/>, with <see cref="DefaultLayoutBuilder"/> selected.
  /// </summary>
  /// <returns>Row container, anchored to the bottom right; its width follows the data panel's.</returns>
  private ContainerRuntime CreateLayoutPickerRow()
  {
    var row = new ContainerRuntime
    {
      WidthUnits = DimensionUnitType.Absolute,
      Height = LayoutPickerRowHeight,
      HeightUnits = DimensionUnitType.Absolute,
    };
    row.Anchor(Anchor.BottomRight); // resets X and Y, so the offsets come after
    row.X = -Padding;
    row.Y = -Padding;

    Label caption = GumUiFactory.CreateLabel(LayoutPickerCaption, EditorTheme.Palette.TextPrimary);
    caption.Width = LayoutPickerCaptionWidth;
    caption.WidthUnits = DimensionUnitType.Absolute;
    caption.Anchor(Anchor.CenterVertically);
    row.AddChild(caption);

    ComboBox picker = GumUiFactory.CreateComboBox(0);
    var items = picker.Items ?? throw new InvalidOperationException("Gum ComboBox has no item list.");
    foreach (EnGraphLayoutBuilder builder in GraphLayoutBuilderPicker.All) items.Add(GraphLayoutBuilderPicker.DisplayName(builder));
    picker.SelectedIndex = IndexOf(DefaultLayoutBuilder);
    picker.Anchor(Anchor.CenterVertically);
    picker.X = LayoutPickerCaptionWidth;
    picker.Width = -LayoutPickerCaptionWidth;
    picker.WidthUnits = DimensionUnitType.RelativeToParent;
    picker.SelectionChanged += (_, _) => OnLayoutPicked(picker.SelectedIndex);
    row.AddChild(picker);
    _layoutPicker = picker;

    return row;
  }

  /// <summary>
  /// Finds the position of a layout in <see cref="GraphLayoutBuilderPicker.All"/>.
  /// </summary>
  /// <param name="builder">Layout.</param>
  /// <returns>Index; 0 (the default layout) for an unknown value.</returns>
  private static int IndexOf(EnGraphLayoutBuilder builder)
  {
    IReadOnlyList<EnGraphLayoutBuilder> all = GraphLayoutBuilderPicker.All;
    for (int i = 0; i < all.Count; i++)
    {
      if (all[i] == builder) return i;
    }

    return 0;
  }

  /// <summary>
  /// Lays the graph out again with the picked layout; this resets the view and the selection.
  /// </summary>
  /// <param name="index">Picked index in <see cref="GraphLayoutBuilderPicker.All"/>; ignored when out of range.</param>
  private void OnLayoutPicked(int index)
  {
    IReadOnlyList<EnGraphLayoutBuilder> all = GraphLayoutBuilderPicker.All;
    if (index < 0 || index >= all.Count) return;

    _viewport.SetLayoutBuilder(all[index]);
    _shownSelection = GraphVisHit.None;
    _dataPanel.Clear();
  }

  /// <summary>
  /// Closes the layout picker's dropdown list if it is open (call when the visualizer is hidden,
  /// so an invisible list does not keep the keyboard).
  /// </summary>
  public void CloseLayoutPicker()
  {
    if (_layoutPicker != null) _layoutPicker.IsDropDownOpen = false;
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
