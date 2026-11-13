using System;
using System.Collections.Generic;
using System.Globalization;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Control rows of the graph visualizer, below its data panel: a layout picker (combo box over
/// <see cref="EnGraphLayoutBuilder"/>) above an edge pull slider with its value.
/// </summary>
/// <remarks>
/// <para>Part of <see cref="GumGraphVisualizer"/>. The panel only reports what the user did
/// (<see cref="LayoutPicked"/>, <see cref="LayoutRepicked"/>, <see cref="EdgePullChanged"/>);
/// the visualizer applies it and writes the resulting strength back with
/// <see cref="ShowEdgePull"/>.</para>
/// <para>Picking the selected layout again with the mouse raises <see cref="LayoutRepicked"/>
/// (Gum itself raises no selection change then; see <see cref="ComboBoxRepickDetector"/>).</para>
/// </remarks>
public sealed class GumGraphControlPanel
{
  /// <summary>Caption of the layout picker.</summary>
  private const string LayoutPickerCaption = "Layout:";

  /// <summary>Caption of the edge pull slider.</summary>
  private const string EdgePullCaption = "Edge pull:";

  /// <summary>Largest edge pull strength the slider offers (share of a "too far" distance corrected per step).</summary>
  public const float MaxEdgePullStrength = 0.3f;

  /// <summary>Number format of the edge pull value shown beside the slider.</summary>
  private const string EdgePullFormat = "0.00";

  /// <summary>Height of one control row, in pixels.</summary>
  private const float RowHeight = 32f;

  /// <summary>Vertical space between two rows, in pixels.</summary>
  private const float RowSpacing = 8f;

  /// <summary>Number of rows (layout, edge pull).</summary>
  private const int RowCount = 2;

  /// <summary>Width of a row's caption, in pixels.</summary>
  private const float CaptionWidth = 96f;

  /// <summary>Width of the value shown right of the edge pull slider, in pixels.</summary>
  private const float ValueLabelWidth = 40f;

  /// <summary>Height of the whole panel, in pixels: all rows with the spacing between them.</summary>
  public const float PanelHeight = RowCount * RowHeight + (RowCount - 1) * RowSpacing;

  /// <summary>Layout picker; null until built.</summary>
  private ComboBox? _layoutPicker;

  /// <summary>Edge pull slider; null until built.</summary>
  private Slider? _edgePullSlider;

  /// <summary>Edge pull value shown beside the slider; null until built.</summary>
  private Label? _edgePullValue;

  /// <summary>Whether code is writing to the controls (their change events are then ignored).</summary>
  private bool _isSyncingControls;

  /// <summary>Raised when the user picks a layout other than the selected one.</summary>
  public event EventHandler<EnGraphLayoutBuilder>? LayoutPicked;

  /// <summary>Raised when the user picks the selected layout again, asking to start it afresh.</summary>
  public event EventHandler? LayoutRepicked;

  /// <summary>Raised when the user moves the edge pull slider; carries the strength, within 0 and <see cref="MaxEdgePullStrength"/>.</summary>
  public event EventHandler<float>? EdgePullChanged;

  /// <summary>Root container; null until <see cref="Build"/> is called.</summary>
  public ContainerRuntime? Root { get; private set; }

  /// <summary>
  /// Builds the visuals.
  /// </summary>
  /// <param name="layout">Layout selected at first.</param>
  /// <param name="edgePull">Edge pull strength shown at first.</param>
  /// <returns><see cref="Root"/>, <see cref="PanelHeight"/> high, to be added to a parent and positioned and given a width by the caller.</returns>
  /// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
  /// <exception cref="InvalidOperationException">The panel was already built.</exception>
  public ContainerRuntime Build(EnGraphLayoutBuilder layout, float edgePull)
  {
    if (Root != null) throw new InvalidOperationException("Graph control panel was already built.");

    var root = new ContainerRuntime
    {
      Height = PanelHeight,
      HeightUnits = DimensionUnitType.Absolute,
    };

    root.AddChild(CreateRow(0, LayoutPickerCaption, CreateLayoutPicker(layout)));
    root.AddChild(CreateRow(1, EdgePullCaption, CreateEdgePullControl()));
    ShowEdgePull(edgePull);

    Root = root;
    return root;
  }

  /// <summary>
  /// Writes an edge pull strength to the slider and its value label, without raising
  /// <see cref="EdgePullChanged"/>.
  /// </summary>
  /// <param name="strength">Strength to show.</param>
  public void ShowEdgePull(float strength)
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
  /// Closes the layout picker's dropdown list if it is open (call when the panel is hidden, so an
  /// invisible list does not keep the keyboard).
  /// </summary>
  public void CloseLayoutPicker()
  {
    if (_layoutPicker != null) _layoutPicker.IsDropDownOpen = false;
  }

  #region Building

  /// <summary>
  /// Creates a row: a caption on the left and a control filling the rest.
  /// </summary>
  /// <param name="index">Row index, 0 at the top.</param>
  /// <param name="caption">Caption text.</param>
  /// <param name="control">Control; its width and horizontal position are set here.</param>
  /// <returns>Row container, as wide as the panel.</returns>
  private static ContainerRuntime CreateRow(int index, string caption, GraphicalUiElement control)
  {
    var row = new ContainerRuntime
    {
      Y = index * (RowHeight + RowSpacing),
      Width = 0,
      WidthUnits = DimensionUnitType.RelativeToParent,
      Height = RowHeight,
      HeightUnits = DimensionUnitType.Absolute,
    };

    Label captionLabel = GumUiFactory.CreateLabel(caption, EditorTheme.Palette.TextPrimary);
    captionLabel.Width = CaptionWidth;
    captionLabel.WidthUnits = DimensionUnitType.Absolute;
    captionLabel.Anchor(Anchor.CenterVertically);
    row.AddChild(captionLabel);

    control.Anchor(Anchor.CenterVertically); // resets X and Y, so the offset comes after
    control.X = CaptionWidth;
    control.Width = -CaptionWidth;
    control.WidthUnits = DimensionUnitType.RelativeToParent;
    row.AddChild(control);
    return row;
  }

  /// <summary>
  /// Creates the layout picker: a combo box listing every <see cref="EnGraphLayoutBuilder"/>.
  /// </summary>
  /// <param name="layout">Layout selected at first.</param>
  /// <returns>The combo box's visual.</returns>
  private GraphicalUiElement CreateLayoutPicker(EnGraphLayoutBuilder layout)
  {
    ComboBox picker = GumUiFactory.CreateComboBox(0);
    var items = picker.Items ?? throw new InvalidOperationException("Gum ComboBox has no item list.");
    foreach (EnGraphLayoutBuilder builder in GraphLayoutBuilderPicker.All) items.Add(GraphLayoutBuilderPicker.DisplayName(builder));
    picker.SelectedIndex = IndexOf(layout);
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
      if (repick.Closed(picker.SelectedIndex)) LayoutRepicked?.Invoke(this, EventArgs.Empty);
    };

    _layoutPicker = picker;
    return picker.Visual;
  }

  /// <summary>
  /// Creates the edge pull control: a slider from 0 to <see cref="MaxEdgePullStrength"/> with the
  /// current value shown on its right.
  /// </summary>
  /// <returns>Container holding slider and value.</returns>
  private ContainerRuntime CreateEdgePullControl()
  {
    var container = new ContainerRuntime
    {
      Height = RowHeight,
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
    return container;
  }

  #endregion

  #region User input

  /// <summary>
  /// Reports a picked layout.
  /// </summary>
  /// <param name="index">Picked index in <see cref="GraphLayoutBuilderPicker.All"/>; ignored when out of range.</param>
  private void OnLayoutPicked(int index)
  {
    IReadOnlyList<EnGraphLayoutBuilder> all = GraphLayoutBuilderPicker.All;
    if (index < 0 || index >= all.Count) return;
    LayoutPicked?.Invoke(this, all[index]);
  }

  /// <summary>
  /// Feeds a push or click on a layout list item to the re-pick detector and reports a re-pick if
  /// that completes one.
  /// </summary>
  /// <param name="repick">Detector of the layout picker.</param>
  private void OnLayoutItemActivated(ComboBoxRepickDetector repick)
  {
    if (repick.ItemActivated()) LayoutRepicked?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>
  /// Shows and reports a slider move made by the user.
  /// </summary>
  /// <param name="value">Slider value.</param>
  private void OnEdgePullChanged(double value)
  {
    if (_isSyncingControls) return; // our own write, not the user

    float strength = Math.Clamp((float)value, 0f, MaxEdgePullStrength);
    if (_edgePullValue != null) _edgePullValue.Text = FormatEdgePull(strength);
    EdgePullChanged?.Invoke(this, strength);
  }

  #endregion

  #region Helpers

  /// <summary>
  /// Formats an edge pull strength for the value label.
  /// </summary>
  /// <param name="strength">Strength.</param>
  /// <returns>Text such as "0.10".</returns>
  private static string FormatEdgePull(float strength) => strength.ToString(EdgePullFormat, CultureInfo.InvariantCulture);

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

  #endregion
}
