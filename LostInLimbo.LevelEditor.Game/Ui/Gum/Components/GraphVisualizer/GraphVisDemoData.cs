using System.Collections.Generic;
using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// TEMPORARY (Phase 2 of GRAPH_VISUALIZATION.md): hard-coded graph exercising every visual
/// feature of the visualizer. Removed in Phase 3, when real level data is shown.
/// </summary>
public static class GraphVisDemoData
{
  /// <summary>
  /// Creates the demo graph.
  /// </summary>
  /// <returns>Graph with plain, colored, dashed, small and large nodes, markers (single, several,
  /// collapsed) and edges of every kind.</returns>
  public static GraphVisData Create()
  {
    IReadOnlyDictionary<string, UiRgba> groups = GraphVisColors.AssignColors(["castle", "caves", "garden"]);
    var darkGray = new UiRgba(70, 70, 70);

    GraphVisNode[] nodes =
    [
      new() { Id = "start", RelativeSize = 2, Markers = [new("p-start", "Start")] },
      new() { Id = "hall", RelativeSize = 6, Color = groups["castle"], Markers = [new("p-red", "Red Key"), new("p-blue", "Blue Key")] },
      new() { Id = "tower", RelativeSize = 1, Color = groups["castle"] },
      new() { Id = "cave", RelativeSize = 9, Color = groups["caves"], Markers = [new("p-a", "A"), new("p-b", "B"), new("p-c", "C"), new("p-d", "D"), new("p-e", "E")] },
      new() { Id = "vault", RelativeSize = 3, Color = groups["caves"], IsDashed = true, Markers = [new("p-goal", "Optional Goal")] },
      new() { Id = "garden", RelativeSize = 4, Color = groups["garden"], Markers = [new("p-x", "Lever"), new("p-y", "Button"), new("p-z", "Exit")] },
      new() { Id = "shed", RelativeSize = 0.2f, IsDashed = true },
      new() { Id = "huge", RelativeSize = 40, Color = groups["garden"] },
    ];

    GraphVisEdge[] edges =
    [
      new() { Id = "e1", FromId = "start", ToId = "hall", ArrowAtFrom = true, ArrowAtTo = true },
      new() { Id = "e2", FromId = "hall", ToId = "tower", ArrowAtTo = true },
      new() { Id = "e3", FromId = "hall", ToId = "cave", ArrowAtFrom = true, ArrowAtTo = true, Label = "2", HighlightMarkerIds = ["p-red", "p-blue"] },
      new() { Id = "e4", FromId = "cave", ToId = "vault", ArrowAtFrom = true, ArrowAtTo = true, IsDashed = true, Color = darkGray },
      new() { Id = "e5", FromId = "cave", ToId = "garden", ArrowAtTo = true, Label = "1", HighlightMarkerIds = ["p-c"] },
      new() { Id = "e6", FromId = "garden", ToId = "shed", ArrowAtTo = true, IsDashed = true, Color = darkGray, Label = "12" },
      new() { Id = "e7", FromId = "garden", ToId = "huge" },
      new() { Id = "e8", FromId = "start", ToId = "huge", ArrowAtFrom = true },
    ];

    return new GraphVisData(nodes, edges);
  }
}
