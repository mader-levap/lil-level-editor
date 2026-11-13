using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer.Data;

/// <summary>
/// Headless tests for <see cref="GraphVisSummary"/> (whole-graph statistics of the data panel).
/// </summary>
public sealed class GraphVisSummaryTests
{
  /// <summary>
  /// Verifies the node, edge and marker counts, in display order.
  /// </summary>
  [Fact]
  public void Properties_CountNodesEdgesAndMarkers()
  {
    // Arrange: Three nodes with 2, 0 and 1 markers; two edges.
    var data = new GraphVisData(
      [
        new GraphVisNode { Id = "a", Markers = [new GraphVisMarker("m1", "Start"), new GraphVisMarker("m2", "Exit")] },
        new GraphVisNode { Id = "b" },
        new GraphVisNode { Id = "c", Markers = [new GraphVisMarker("m3", "Sol")] },
      ],
      [new GraphVisEdge { Id = "ab", FromId = "a", ToId = "b" }, new GraphVisEdge { Id = "bc", FromId = "b", ToId = "c" }]);

    // Act: Build the statistics.
    IReadOnlyDictionary<string, string> properties = GraphVisSummary.Properties(data);

    // Assert: Counts in display order.
    Assert.Equal(
      [
        new KeyValuePair<string, string>(GraphVisSummary.NodesKey, "3"),
        new KeyValuePair<string, string>(GraphVisSummary.EdgesKey, "2"),
        new KeyValuePair<string, string>(GraphVisSummary.MarkersKey, "3"),
      ],
      properties);
  }

  /// <summary>
  /// Verifies a null graph is rejected.
  /// </summary>
  [Fact]
  public void Properties_NullGraph_Throws()
  {
    // Act + Assert: No graph.
    Assert.Throws<ArgumentNullException>(() => GraphVisSummary.Properties(null!));
  }
}
