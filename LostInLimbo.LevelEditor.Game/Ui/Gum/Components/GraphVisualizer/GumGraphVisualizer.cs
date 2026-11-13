using System;
using System.Collections.Generic;
using System.Globalization;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
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

  /// <summary>Caption of the edge pull slider.</summary>
  private const string EdgePullCaption = "Edge pull:";

  /// <summary>Largest edge pull strength the slider offers (share of a "too far" distance corrected per step).</summary>
  public const float MaxEdgePullStrength = 0.3f;

  /// <summary>Number format of the edge pull value shown beside the slider.</summary>
  private const string EdgePullFormat = "0.00";

  /// <summary>Height of one control row below the data panel, in pixels.</summary>
  private const float PickerRowHeight = 32f;

  /// <summary>Width of a control row's caption, in pixels.</summary>
  private const float PickerCaptionWidth = 96f;

  /// <summary>Width of the value shown right of the edge pull slider, in pixels.</summary>
  private const float ValueLabelWidth = 40f;

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

  /// <summary>Control rows below the data panel (layout, edge pull); empty until built, or when <see cref="ShowLayoutPicker"/> is off.</summary>
  private readonly List<ContainerRuntime> _controlRows = [];

  /// <summary>Layout picker; null until built, or when <see cref="ShowLayoutPicker"/> is off.</summary>
  private ComboBox? _layoutPicker;

  /// <summary>Edge pull slider; null until built, or when <see cref="ShowLayoutPicker"/> is off.</summary>
  private Slider? _edgePullSlider;

  /// <summary>Edge pull value shown beside the slider; null until built, or when <see cref="ShowLayoutPicker"/> is off.</summary>
  private Label? _edgePullValue;

  /// <summary>Whether code is writing to the controls (their change events are then ignored).</summary>
  private bool _isSyncingControls;

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
      // Control rows at the bottom of the right column, layout above edge pull; the data panel
      // gives up the room.
      AddControlRow(root, CreatePickerRow(LayoutPickerCaption, CreateLayoutPicker()));
      AddControlRow(root, CreatePickerRow(EdgePullCaption, CreateEdgePullControl()));
      dataPanel.Height -= _controlRows.Count * (PickerRowHeight + Padding);
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
    foreach (ContainerRuntime row in _controlRows) row.Width = width;
    if (_viewportContainer != null) _viewportContainer.Width = -(width + 3 * Padding);
  }

  #region Layout and physics controls

  /// <summary>
  /// Adds a control row below the data panel, under the rows added before. Rows grow upwards
  /// from the bottom edge, so the existing rows move up by one row.
  /// </summary>
  /// <param name="root">Visualizer root.</param>
  /// <param name="row">Row from <see cref="CreatePickerRow"/>.</param>
  private void AddControlRow(ContainerRuntime root, ContainerRuntime row)
  {
    foreach (ContainerRuntime above in _controlRows) above.Y -= PickerRowHeight + Padding;
    root.AddChild(row);
    _controlRows.Add(row);
  }

  /// <summary>
  /// Creates a control row: a caption on the left and a control filling the rest.
  /// </summary>
  /// <param name="caption">Caption text.</param>
  /// <param name="control">Control; its width and horizontal position are set here.</param>
  /// <returns>Row container, anchored to the bottom right; its width follows the data panel's.</returns>
  private static ContainerRuntime CreatePickerRow(string caption, GraphicalUiElement control)
  {
    var row = new ContainerRuntime
    {
      WidthUnits = DimensionUnitType.Absolute,
      Height = PickerRowHeight,
      HeightUnits = DimensionUnitType.Absolute,
    };
    row.Anchor(Anchor.BottomRight); // resets X and Y, so the offsets come after
    row.X = -Padding;
    row.Y = -Padding;

    Label captionLabel = GumUiFactory.CreateLabel(caption, EditorTheme.Palette.TextPrimary);
    captionLabel.Width = PickerCaptionWidth;
    captionLabel.WidthUnits = DimensionUnitType.Absolute;
    captionLabel.Anchor(Anchor.CenterVertically);
    row.AddChild(captionLabel);

    control.Anchor(Anchor.CenterVertically); // resets X and Y, so the offset comes after
    control.X = PickerCaptionWidth;
    control.Width = -PickerCaptionWidth;
    control.WidthUnits = DimensionUnitType.RelativeToParent;
    row.AddChild(control);
    return row;
  }

  /// <summary>
  /// Creates the layout picker: a combo box listing every <see cref="EnGraphLayoutBuilder"/>,
  /// with <see cref="DefaultLayoutBuilder"/> selected. Picking another layout switches to it;
  /// picking the current one again (with the mouse) resets it.
  /// </summary>
  /// <returns>The combo box's visual.</returns>
  private GraphicalUiElement CreateLayoutPicker()
  {
    ComboBox picker = GumUiFactory.CreateComboBox(0);
    var items = picker.Items ?? throw new InvalidOperationException("Gum ComboBox has no item list.");
    foreach (EnGraphLayoutBuilder builder in GraphLayoutBuilderPicker.All) items.Add(GraphLayoutBuilderPicker.DisplayName(builder));
    picker.SelectedIndex = IndexOf(DefaultLayoutBuilder);
    picker.SelectionChanged += (_, _) => OnLayoutPicked(picker.SelectedIndex);

    // Gum raises no selection change when the selected item is picked again; detect it from the
    // dropdown's own events instead. The list box may be created on first open, so its item
    // events are subscribed there (once per list box). Both push and click are watched: Gum may
    // close the list on either, and its own handler can run before ours.
    var repick = new ComboBoxRepickDetector();
    ListBox? subscribedListBox = null;
    picker.DropDownOpened += (_, _) =>
    {
      repick.Opened(picker.SelectedIndex);
      if (picker.ListBox is not { } listBox || ReferenceEquals(listBox, subscribedListBox)) return;
      listBox.ItemPushed += (_, _) => OnLayoutItemActivated(repick);
      listBox.ItemClicked += (_, _) => OnLayoutItemActivated(repick);
      subscribedListBox = listBox;
    };
    picker.DropDownClosed += (_, _) =>
    {
      if (repick.Closed(picker.SelectedIndex)) OnLayoutRepicked();
    };

    _layoutPicker = picker;
    return picker.Visual;
  }

  /// <summary>
  /// Feeds a push or click on a layout list item to the re-pick detector and resets the layout
  /// if that completes a re-pick.
  /// </summary>
  /// <param name="repick">Detector of the layout picker.</param>
  private void OnLayoutItemActivated(ComboBoxRepickDetector repick)
  {
    if (repick.ItemActivated()) OnLayoutRepicked();
  }

  /// <summary>
  /// Starts the current layout afresh (default physics, new layout, fitted view, no selection),
  /// as if another layout had been picked and then this one again.
  /// </summary>
  private void OnLayoutRepicked()
  {
    _viewport.ResetLayout();
    ShowEdgePull(_viewport.Physics.AttractStrength);
    _shownSelection = GraphVisHit.None;
    _dataPanel.Clear();
  }

  /// <summary>
  /// Creates the edge pull control: a slider from 0 to <see cref="MaxEdgePullStrength"/> with the
  /// current value shown on its right, starting at the viewport's physics.
  /// </summary>
  /// <returns>Container holding slider and value.</returns>
  private ContainerRuntime CreateEdgePullControl()
  {
    var container = new ContainerRuntime
    {
      Height = PickerRowHeight,
      HeightUnits = DimensionUnitType.Absolute,
    };

    var slider = new Slider
    {
      Minimum = 0,
      Maximum = MaxEdgePullStrength,
    };
    slider.Anchor(Anchor.CenterVertically);
    slider.Width = -ValueLabelWidth;
    slider.WidthUnits = DimensionUnitType.RelativeToParent;
    slider.ValueChanged += (_, _) => OnEdgePullChanged(slider.Value);
    container.AddChild(slider);
    _edgePullSlider = slider;

    Label value = GumUiFactory.CreateLabel(string.Empty, EditorTheme.Palette.TextPrimary);
    value.Anchor(Anchor.Right);
    container.AddChild(value);
    _edgePullValue = value;

    ShowEdgePull(_viewport.Physics.AttractStrength);
    return container;
  }

  /// <summary>
  /// Writes an edge pull strength to the slider and its value label without applying it.
  /// </summary>
  /// <param name="strength">Strength to show.</param>
  private void ShowEdgePull(float strength)
  {
    _isSyncingControls = true;
    try
    {
      if (_edgePullSlider != null) _edgePullSlider.Value = strength;
      if (_edgePullValue != null) _edgePullValue.Text = FormatEdgePull(strength);
    }
    finally
    {
      _isSyncingControls = false;
    }
  }

  /// <summary>
  /// Formats an edge pull strength for the value label.
  /// </summary>
  /// <param name="strength">Strength.</param>
  /// <returns>Text such as "0.10".</returns>
  private static string FormatEdgePull(float strength) => strength.ToString(EdgePullFormat, CultureInfo.InvariantCulture);

  /// <summary>
  /// Applies a slider move live: the nodes adapt at once; layout, view and selection stay.
  /// </summary>
  /// <param name="value">Slider value.</param>
  private void OnEdgePullChanged(double value)
  {
    if (_isSyncingControls) return; // our own write, not the user

    float strength = Math.Clamp((float)value, 0f, MaxEdgePullStrength);
    _viewport.SetEdgePullStrength(strength);
    if (_edgePullValue != null) _edgePullValue.Text = FormatEdgePull(strength);
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
  /// Lays the graph out again with the picked layout and its default physics (the edge pull
  /// slider follows); this resets the view and the selection.
  /// </summary>
  /// <param name="index">Picked index in <see cref="GraphLayoutBuilderPicker.All"/>; ignored when out of range.</param>
  private void OnLayoutPicked(int index)
  {
    IReadOnlyList<EnGraphLayoutBuilder> all = GraphLayoutBuilderPicker.All;
    if (index < 0 || index >= all.Count) return;

    _viewport.SetLayoutBuilder(all[index]);
    ShowEdgePull(_viewport.Physics.AttractStrength);
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
