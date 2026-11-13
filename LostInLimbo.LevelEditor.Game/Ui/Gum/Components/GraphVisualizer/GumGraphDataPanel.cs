using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Data column of the graph visualizer: a title naming what is selected and a scrollable list
/// of the selection's properties, key in muted text and value in normal text.
/// </summary>
/// <remarks>
/// <para>Part of <see cref="GumGraphVisualizer"/>, which tells it what to show.</para>
/// <para>Pairs whose key and value fit side by side share one row, with keys and values in
/// aligned columns; the others show the value below the key, wrapped to the width. Which pairs
/// share a row is decided by <see cref="GraphVisDataRows"/> from the measured text widths.</para>
/// </remarks>
public sealed class GumGraphDataPanel
{
  /// <summary>Title shown when cleared (no graph to describe).</summary>
  public const string NothingSelectedTitle = "Nothing selected";

  /// <summary>Height reserved for the title above the property list, in pixels.</summary>
  private const float TitleHeight = 28f;

  /// <summary>Vertical space between two pairs, in pixels.</summary>
  private const float PairSpacing = 4f;

  /// <summary>Free space between the key column and the values of single-row pairs, in pixels.</summary>
  private const float ColumnGap = 10f;

  /// <summary>Indent of rows whose key is in the indented keys, in pixels.</summary>
  private const float Indent = 12f;

  /// <summary>How much smaller the property text is than normal text, in font size units (the title keeps its size).</summary>
  private const int PropertyFontSizeReduction = 1;

  /// <summary>Rows currently in the property list (one per pair), in display order.</summary>
  private readonly List<ContainerRuntime> _rows = [];

  /// <summary>Title label; null until built.</summary>
  private Label? _title;

  /// <summary>Scrollable property list; null until built.</summary>
  private ScrollViewer? _scrollViewer;

  /// <summary>Root container; null until <see cref="Build"/> is called.</summary>
  public ContainerRuntime? Root { get; private set; }

  /// <summary>
  /// Builds the visuals, showing <see cref="NothingSelectedTitle"/>.
  /// </summary>
  /// <returns><see cref="Root"/>, to be added to a parent and sized and positioned by the caller.</returns>
  /// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
  /// <exception cref="InvalidOperationException">The panel was already built.</exception>
  public ContainerRuntime Build()
  {
    if (Root != null) throw new InvalidOperationException("Graph data panel was already built.");

    var root = new ContainerRuntime();

    _title = GumUiFactory.CreateTitleLabel(NothingSelectedTitle);
    root.AddChild(_title);

    _scrollViewer = new ScrollViewer
    {
      Y = TitleHeight,
      Width = 0,
      WidthUnits = DimensionUnitType.RelativeToParent,
      Height = -TitleHeight,
      HeightUnits = DimensionUnitType.RelativeToParent,
      IsVisible = false,
    };

    // Same thickness as the drawing area's scroll bars. The scroll viewer narrows its content by
    // the bar's actual width whenever the bar is shown, so the list gains the freed space.
    if (_scrollViewer.VerticalScrollBar is { } verticalScrollBar) GumScrollBarSizing.ApplyThickness(verticalScrollBar);
    root.AddChild(_scrollViewer.Visual);

    Root = root;
    return root;
  }

  /// <summary>
  /// Shows the properties of a selected element.
  /// </summary>
  /// <param name="title">What is selected (for example "Node").</param>
  /// <param name="properties">Key/value data, shown in its order.</param>
  /// <param name="indentedKeys">Keys whose rows are indented by <see cref="Indent"/>.</param>
  /// <exception cref="ArgumentNullException">An argument is null.</exception>
  public void Show(string title, GraphVisProperties properties, IReadOnlySet<string> indentedKeys)
  {
    ArgumentNullException.ThrowIfNull(title);
    ArgumentNullException.ThrowIfNull(properties);
    ArgumentNullException.ThrowIfNull(indentedKeys);
    if (_title == null || _scrollViewer == null) return; // not built yet

    RemoveRows();
    _title.Text = title;
    _scrollViewer.IsVisible = true;

    // Labels at their natural (one-line) width, so they can be measured.
    var pairs = properties
      .Select(pair => (
        Key: CreatePropertyLabel(pair.Key, EditorTheme.Palette.TextMuted),
        Value: CreatePropertyLabel(pair.Value, EditorTheme.Palette.TextPrimary),
        Indent: indentedKeys.Contains(pair.Key) ? Indent : 0f))
      .ToArray();

    // An indented key reaches further right, so the indent counts as part of its width.
    GraphVisDataPairSize[] sizes = pairs
      .Select(pair => new GraphVisDataPairSize(pair.Indent + pair.Key.Visual.AbsoluteWidth, pair.Value.Visual.AbsoluteWidth))
      .ToArray();
    GraphVisDataRowLayout layout = GraphVisDataRows.Arrange(sizes, AvailableRowWidth(_scrollViewer), ColumnGap);

    for (int i = 0; i < pairs.Length; i++)
    {
      ContainerRuntime row = layout.IsSingleRow[i]
        ? CreateSingleRow(pairs[i].Key, pairs[i].Value, pairs[i].Indent, layout.KeyColumnWidth)
        : CreateStackedRow(pairs[i].Key, pairs[i].Value, pairs[i].Indent);
      if (i > 0) row.Y = PairSpacing; // stacked: Y is the gap above
      _scrollViewer.AddChild(row);
      _rows.Add(row);
    }

    _scrollViewer.VerticalScrollBarValue = 0; // start at the top for each new selection
  }

  /// <summary>
  /// Shows <see cref="NothingSelectedTitle"/> and no properties.
  /// </summary>
  public void Clear()
  {
    if (_title == null || _scrollViewer == null) return; // not built yet

    RemoveRows();
    _title.Text = NothingSelectedTitle;
    _scrollViewer.IsVisible = false;
  }

  /// <summary>
  /// Width a row may use: the list's inner width, minus the vertical scroll bar even while it is
  /// hidden (it may appear once the rows are added, and rows must not change then).
  /// </summary>
  /// <param name="scrollViewer">Property list.</param>
  /// <returns>Width, in pixels.</returns>
  private static float AvailableRowWidth(ScrollViewer scrollViewer)
  {
    float width = scrollViewer.ClipContainer.AbsoluteWidth;
    if (scrollViewer.VerticalScrollBar is { IsVisible: false } bar) width -= bar.Visual.AbsoluteWidth;
    return Math.Max(0, width);
  }

  /// <summary>
  /// Creates a key or value label in the property font, which is a little smaller than normal
  /// text so more fits the narrow column.
  /// </summary>
  /// <param name="text">Text.</param>
  /// <param name="color">Text color.</param>
  /// <returns>Label at its natural width.</returns>
  private static Label CreatePropertyLabel(string text, UiRgba color)
  {
    Label label = GumUiFactory.CreateLabel(text, color);
    if (label.TextComponent is TextRuntime textRuntime) textRuntime.FontSize -= PropertyFontSizeReduction;
    return label;
  }

  /// <summary>
  /// Creates a row holding key and value side by side; the value starts after the key column.
  /// </summary>
  /// <param name="key">Key label (natural width).</param>
  /// <param name="value">Value label (natural width).</param>
  /// <param name="indent">Indent of the key, in pixels (inside the key column).</param>
  /// <param name="keyColumnWidth">Width of the key column, in pixels.</param>
  /// <returns>Row.</returns>
  private static ContainerRuntime CreateSingleRow(Label key, Label value, float indent, float keyColumnWidth)
  {
    ContainerRuntime row = CreateRowContainer();
    key.X = indent;
    row.AddChild(key);
    value.X = keyColumnWidth + ColumnGap;
    row.AddChild(value);
    return row;
  }

  /// <summary>
  /// Creates a row holding the key with the value below it, wrapped to the width.
  /// </summary>
  /// <param name="key">Key label.</param>
  /// <param name="value">Value label.</param>
  /// <param name="indent">Indent of key and value, in pixels.</param>
  /// <returns>Row.</returns>
  private static ContainerRuntime CreateStackedRow(Label key, Label value, float indent)
  {
    ContainerRuntime row = CreateRowContainer();
    row.X = indent;
    row.Width = -indent;
    row.ChildrenLayout = ChildrenLayout.TopToBottomStack;
    MakeWrapping(key);
    row.AddChild(key);
    MakeWrapping(value);
    row.AddChild(value);
    return row;
  }

  /// <summary>
  /// Creates an empty row as wide as the list and as tall as its content.
  /// </summary>
  /// <returns>Row container.</returns>
  private static ContainerRuntime CreateRowContainer()
  {
    return new ContainerRuntime
    {
      Width = 0,
      WidthUnits = DimensionUnitType.RelativeToParent,
      Height = 0,
      HeightUnits = DimensionUnitType.RelativeToChildren,
    };
  }

  /// <summary>
  /// Makes a label span its parent's width and wrap longer text onto more lines.
  /// </summary>
  /// <param name="label">Label.</param>
  private static void MakeWrapping(Label label)
  {
    label.Width = 0;
    label.WidthUnits = DimensionUnitType.RelativeToParent;

    // The text element must follow the label's width (instead of its own text) to wrap.
    if (label.TextComponent is GraphicalUiElement textElement)
    {
      textElement.Width = 0;
      textElement.WidthUnits = DimensionUnitType.RelativeToParent;
    }
  }

  /// <summary>
  /// Removes all rows from the property list.
  /// </summary>
  private void RemoveRows()
  {
    if (_scrollViewer == null) return;
    foreach (ContainerRuntime row in _rows) _scrollViewer.RemoveChild(row);
    _rows.Clear();
  }
}
