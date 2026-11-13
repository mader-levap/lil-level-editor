using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer.Data;

/// <summary>
/// Headless tests for <see cref="GraphVisData"/> (validation of the visualizer's input model).
/// </summary>
public sealed class GraphVisDataTests
{
  /// <summary>
  /// Creates a node with the given identifier and markers.
  /// </summary>
  /// <param name="id">Node identifier.</param>
  /// <param name="markerIds">Identifiers of the node's markers.</param>
  /// <returns>Node.</returns>
  private static GraphVisNode Node(string id, params string[] markerIds) => new()
  {
    Id = id,
    Markers = markerIds.Select(markerId => new GraphVisMarker(markerId, markerId)).ToArray(),
  };

  /// <summary>
  /// Creates an edge.
  /// </summary>
  /// <param name="id">Edge identifier.</param>
  /// <param name="from">Start node identifier.</param>
  /// <param name="to">End node identifier.</param>
  /// <returns>Edge.</returns>
  private static GraphVisEdge Edge(string id, string from, string to) => new() { Id = id, FromId = from, ToId = to };

  /// <summary>
  /// Verifies a well-formed graph is accepted and exposes its content.
  /// </summary>
  [Fact]
  public void Constructor_WithValidGraph_KeepsNodesAndEdges()
  {
    // Arrange: Two nodes joined by one edge.
    GraphVisNode[] nodes = [Node("a", "m1"), Node("b")];
    GraphVisEdge[] edges = [Edge("e", "a", "b")];

    // Act: Build the graph.
    var data = new GraphVisData(nodes, edges);

    // Assert: Content is kept in order and nodes can be found by identifier.
    Assert.Equal(nodes, data.Nodes);
    Assert.Equal(edges, data.Edges);
    Assert.False(data.IsEmpty);
    Assert.Same(nodes[1], data.FindNode("b"));
    Assert.Null(data.FindNode("missing"));
  }

  /// <summary>
  /// Verifies the shared empty graph reports itself as empty.
  /// </summary>
  [Fact]
  public void Empty_HasNothingToDraw()
  {
    // Assert: No nodes, no edges.
    Assert.True(GraphVisData.Empty.IsEmpty);
    Assert.Empty(GraphVisData.Empty.Edges);
  }

  /// <summary>
  /// Verifies node defaults: size 1, no color, solid outline, no markers or properties.
  /// </summary>
  [Fact]
  public void Node_Defaults_AreNeutral()
  {
    // Act: Create a node with only an identifier.
    var node = new GraphVisNode { Id = "a" };

    // Assert: Neutral defaults.
    Assert.Equal(GraphVisNode.DefaultRelativeSize, node.RelativeSize);
    Assert.Null(node.Color);
    Assert.False(node.IsDashed);
    Assert.Empty(node.Markers);
    Assert.Empty(node.Properties);
  }

  /// <summary>
  /// Verifies blank node identifiers are rejected.
  /// </summary>
  /// <param name="id">Blank identifier.</param>
  [Theory]
  [InlineData("")]
  [InlineData("  ")]
  public void Constructor_WithBlankNodeId_Throws(string id)
  {
    // Act + Assert: Blank identifier is rejected.
    Assert.Throws<ArgumentException>(() => new GraphVisData([Node(id)], []));
  }

  /// <summary>
  /// Verifies duplicate node identifiers are rejected.
  /// </summary>
  [Fact]
  public void Constructor_WithDuplicateNodeId_Throws()
  {
    // Act + Assert: Two nodes sharing an identifier are rejected.
    Assert.Throws<ArgumentException>(() => new GraphVisData([Node("a"), Node("a")], []));
  }

  /// <summary>
  /// Verifies relative sizes that cannot be drawn are rejected.
  /// </summary>
  /// <param name="relativeSize">Invalid relative size.</param>
  [Theory]
  [InlineData(0f)]
  [InlineData(-1f)]
  [InlineData(float.NaN)]
  [InlineData(float.PositiveInfinity)]
  public void Constructor_WithInvalidRelativeSize_Throws(float relativeSize)
  {
    // Arrange: Node with an unusable size.
    var node = new GraphVisNode { Id = "a", RelativeSize = relativeSize };

    // Act + Assert: The graph is rejected.
    Assert.Throws<ArgumentException>(() => new GraphVisData([node], []));
  }

  /// <summary>
  /// Verifies marker identifiers must be unique across the whole graph, not just within a node.
  /// </summary>
  [Fact]
  public void Constructor_WithDuplicateMarkerIdAcrossNodes_Throws()
  {
    // Act + Assert: The same marker identifier in two nodes is rejected.
    Assert.Throws<ArgumentException>(() => new GraphVisData([Node("a", "m"), Node("b", "m")], []));
  }

  /// <summary>
  /// Verifies duplicate edge identifiers are rejected.
  /// </summary>
  [Fact]
  public void Constructor_WithDuplicateEdgeId_Throws()
  {
    // Act + Assert: Two edges sharing an identifier are rejected.
    Assert.Throws<ArgumentException>(
      () => new GraphVisData([Node("a"), Node("b")], [Edge("e", "a", "b"), Edge("e", "b", "a")]));
  }

  /// <summary>
  /// Verifies edges must connect two different, existing nodes.
  /// </summary>
  /// <param name="from">Start node identifier.</param>
  /// <param name="to">End node identifier.</param>
  [Theory]
  [InlineData("missing", "a")]
  [InlineData("a", "missing")]
  [InlineData("a", "a")]
  public void Constructor_WithBrokenEdge_Throws(string from, string to)
  {
    // Act + Assert: Dangling and self-connecting edges are rejected.
    Assert.Throws<ArgumentException>(() => new GraphVisData([Node("a"), Node("b")], [Edge("e", from, to)]));
  }

  /// <summary>
  /// Verifies an edge may name markers that do not exist; they simply highlight nothing.
  /// </summary>
  [Fact]
  public void Constructor_WithUnknownHighlightMarker_IsAccepted()
  {
    // Arrange: Edge highlighting a marker no node has.
    GraphVisEdge edge = Edge("e", "a", "b") with { HighlightMarkerIds = ["nowhere"] };

    // Act: Build the graph.
    var data = new GraphVisData([Node("a"), Node("b")], [edge]);

    // Assert: The graph is accepted.
    Assert.Single(data.Edges);
  }

  /// <summary>
  /// Verifies null collections are rejected.
  /// </summary>
  [Fact]
  public void Constructor_WithNullCollections_Throws()
  {
    // Act + Assert: Null nodes or edges are rejected.
    Assert.Throws<ArgumentNullException>(() => new GraphVisData(null!, []));
    Assert.Throws<ArgumentNullException>(() => new GraphVisData([], null!));
  }
}
