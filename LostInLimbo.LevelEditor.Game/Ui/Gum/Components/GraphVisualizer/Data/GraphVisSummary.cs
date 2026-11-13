using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;

/// <summary>
/// Basic statistics of a whole graph, shown by the data panel while nothing is selected.
/// </summary>
public static class GraphVisSummary
{
  /// <summary>Key of the node count.</summary>
  public const string NodesKey = "Nodes";

  /// <summary>Key of the edge count.</summary>
  public const string EdgesKey = "Edges";

  /// <summary>Key of the marker count (all markers of all nodes).</summary>
  public const string MarkersKey = "Markers";

  /// <summary>
  /// Builds the statistics of a graph as data panel properties.
  /// </summary>
  /// <param name="data">Graph.</param>
  /// <returns>Node, edge and marker counts, in display order.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
  public static GraphVisProperties Properties(GraphVisData data)
  {
    ArgumentNullException.ThrowIfNull(data);

    // Properties are listed in display order.
    return new GraphVisProperties(
    [
      new(NodesKey, Count(data.Nodes.Count)),
      new(EdgesKey, Count(data.Edges.Count)),
      new(MarkersKey, Count(data.Nodes.Sum(node => node.Markers.Count))),
    ]);
  }

  /// <summary>
  /// Formats a count.
  /// </summary>
  /// <param name="count">Count.</param>
  /// <returns>Count text.</returns>
  private static string Count(int count) => count.ToString(CultureInfo.InvariantCulture);
}
