using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;
using LostInLimbo.LevelEditor.Ui.Gum.Editor;
using LostInLimbo.LevelGen.Data.Result;
using LostInLimbo.LevelGen.Data.Status;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;

namespace LostInLimbo.LevelEditor.Ui.Gum.Info;

/// <summary>
/// Gum level information overlay: a translucent card with the generation status and, when a
/// world is available, its phase, seeds, graph size and map steps; below it, a large panel
/// visualizing the level graph.
/// </summary>
/// <remarks>
/// Refreshes when shown, and while visible when the generation result changes. It does not
/// poll the world every frame, because <see cref="ILevelGenService.World"/> returns a deep
/// copy. Runs cannot start while this card is open (overlays are exclusive), so a result
/// change is the only way the shown data can go stale.
/// </remarks>
public sealed class GumLevelInfoPanel : IOverlayView
{
  /// <summary>Padding inside the card, in pixels.</summary>
  private const float PanelPadding = 12;

  /// <summary>Spacing between rows, in pixels.</summary>
  private const float RowSpacing = 4;

  /// <summary>Width of the label column, in pixels.</summary>
  private const float LabelWidth = 130;

  /// <summary>Height of the lower area holding the graph panel, in percent of the window.</summary>
  private const float GraphAreaHeightPercent = 55f;

  /// <summary>Width of the graph panel, in percent of the window.</summary>
  private const float GraphAreaWidthPercent = 80f;

  /// <summary>Whole window, in percent.</summary>
  private const float FullPercent = 100f;

  /// <summary>Free space below the graph panel, in pixels.</summary>
  private const float GraphBottomMargin = 12f;

  /// <summary>Card title.</summary>
  private const string Title = "Level information";

  /// <summary>Level generation service.</summary>
  private readonly ILevelGenService _levelGenService;

  /// <summary>Graph visualization panel.</summary>
  private readonly GumGraphVisualizer _graphVisualizer = new();

  /// <summary>Overlay root (full window, no events of its own).</summary>
  private ContainerRuntime? _root;

  /// <summary>Container holding the row visuals.</summary>
  private ContainerRuntime? _rowsContainer;

  /// <summary>Row visuals currently shown.</summary>
  private readonly List<StackPanel> _rowPanels = [];

  /// <summary>Rows currently shown (to skip rebuilding when nothing changed).</summary>
  private IReadOnlyList<LevelInfoRow> _shownRows = [];

  /// <summary>Generation result at the last refresh; null forces a refresh.</summary>
  private EnGenResult? _shownResult;

  /// <summary>
  /// Creates the panel. Call <see cref="Build"/> to create its visuals.
  /// </summary>
  /// <param name="levelGenService">Level generation service.</param>
  /// <exception cref="ArgumentNullException"><paramref name="levelGenService"/> is null.</exception>
  public GumLevelInfoPanel(ILevelGenService levelGenService)
  {
    _levelGenService = levelGenService ?? throw new ArgumentNullException(nameof(levelGenService));
  }

  /// <summary>Whether the overlay is currently shown.</summary>
  public bool IsVisible => _root?.Visible == true;

  /// <summary>
  /// Builds the overlay (hidden) and adds it to the Gum root.
  /// </summary>
  /// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
  /// <exception cref="InvalidOperationException">The panel was already built.</exception>
  public void Build()
  {
    if (_root != null) throw new InvalidOperationException("Level information panel was already built.");

    // Full-window root without events, so empty areas still let the camera look around.
    var root = new ContainerRuntime
    {
      HasEvents = false,
      Visible = false,
      Width = 0,
      WidthUnits = DimensionUnitType.RelativeToParent,
      Height = 0,
      HeightUnits = DimensionUnitType.RelativeToParent,
    };

    // Upper area: the rest of the window above the graph panel; the card is centered in it.
    var upperArea = new ContainerRuntime
    {
      HasEvents = false,
      Width = 0,
      WidthUnits = DimensionUnitType.RelativeToParent,
      Height = FullPercent - GraphAreaHeightPercent,
      HeightUnits = DimensionUnitType.PercentageOfParent,
    };

    ContainerRuntime card = GumUiFactory.CreatePanel();
    card.Anchor(Anchor.Center);
    card.Width = PanelPadding; // right padding added to the children's extent
    card.WidthUnits = DimensionUnitType.RelativeToChildren;
    card.Height = PanelPadding; // bottom padding added to the children's extent
    card.HeightUnits = DimensionUnitType.RelativeToChildren;

    var content = new ContainerRuntime
    {
      X = PanelPadding,
      Y = PanelPadding,
      ChildrenLayout = ChildrenLayout.TopToBottomStack,
      StackSpacing = RowSpacing * 2,
      Width = 0,
      WidthUnits = DimensionUnitType.RelativeToChildren,
      Height = 0,
      HeightUnits = DimensionUnitType.RelativeToChildren,
    };

    content.AddChild(GumUiFactory.CreateTitleLabel(Title));

    _rowsContainer = new ContainerRuntime
    {
      ChildrenLayout = ChildrenLayout.TopToBottomStack,
      StackSpacing = RowSpacing,
      Width = 0,
      WidthUnits = DimensionUnitType.RelativeToChildren,
      Height = 0,
      HeightUnits = DimensionUnitType.RelativeToChildren,
    };
    content.AddChild(_rowsContainer);

    card.AddChild(content);
    upperArea.AddChild(card);
    root.AddChild(upperArea);

    // Lower area: the graph panel, bottom center.
    var lowerArea = new ContainerRuntime
    {
      HasEvents = false,
      Width = GraphAreaWidthPercent,
      WidthUnits = DimensionUnitType.PercentageOfParent,
      Height = GraphAreaHeightPercent,
      HeightUnits = DimensionUnitType.PercentageOfParent,
    };
    lowerArea.Anchor(Anchor.Bottom);
    ContainerRuntime graphPanel = _graphVisualizer.Build();
    graphPanel.Width = 0;
    graphPanel.WidthUnits = DimensionUnitType.RelativeToParent;
    graphPanel.Height = -GraphBottomMargin;
    graphPanel.HeightUnits = DimensionUnitType.RelativeToParent;
    lowerArea.AddChild(graphPanel);
    root.AddChild(lowerArea);

    root.AddToRoot();
    _root = root;
  }

  /// <inheritdoc />
  public void SetVisible(bool visible)
  {
    if (_root == null) return;
    _root.Visible = visible;
    if (visible) Refresh();
  }

  /// <summary>
  /// Refreshes the card if the generation result changed while it is visible. Call every frame;
  /// cheap when nothing changed (one status read, no world copy).
  /// </summary>
  public void Update()
  {
    if (!IsVisible) return;
    if (_levelGenService.Status().Result == _shownResult) return;
    Refresh();
  }

  /// <summary>
  /// Reads status and world and updates the rows if they changed.
  /// </summary>
  private void Refresh()
  {
    GenerationStatus status = _levelGenService.Status();
    LilWorld? world = _levelGenService.World();
    LevelWorldFacts? facts = world == null
      ? null
      : new LevelWorldFacts(
          world.Phase,
          _levelGenService.TextSeed(),
          status.Seed,
          world.Graph.Nodes.Count,
          world.Graph.Edges.Count,
          world.Stats.Steps);

    _shownResult = status.Result;

    // TEMPORARY (Phase 2 of GRAPH_VISUALIZATION.md): a demo graph stands in for the level graph.
    _graphVisualizer.SetGraph(world == null ? null : GraphVisDemoData.Create());

    IReadOnlyList<LevelInfoRow> rows = LevelInfoSummary.Build(status.Result, status.FailureReason, facts);
    if (rows.SequenceEqual(_shownRows)) return; // nothing to redraw
    ShowRows(rows);
  }

  /// <summary>
  /// Replaces the shown rows.
  /// </summary>
  /// <param name="rows">Rows to show.</param>
  private void ShowRows(IReadOnlyList<LevelInfoRow> rows)
  {
    if (_rowsContainer == null) return;

    foreach (StackPanel rowPanel in _rowPanels) _rowsContainer.RemoveChild(rowPanel);
    _rowPanels.Clear();

    foreach (LevelInfoRow row in rows)
    {
      Label value = GumUiFactory.CreateLabel(row.Value, EditorTheme.Palette.TextPrimary);
      StackPanel rowPanel = GumUiFactory.CreateFormRow(row.Label, value, LabelWidth);
      _rowsContainer.AddChild(rowPanel);
      _rowPanels.Add(rowPanel);
    }

    _shownRows = rows;
  }
}
