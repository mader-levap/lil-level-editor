using System;
using System.Collections.Generic;
using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;

/// <summary>
/// Small labelled dot shown inside a node (for example a point of interest).
/// </summary>
/// <param name="Id">Identifier, unique across the whole graph; edges refer to it to highlight the marker.</param>
/// <param name="Label">Short text shown above the dot.</param>
public sealed record GraphVisMarker(string Id, string Label);

/// <summary>
/// One node of a visualized graph. Knows nothing about what the node means; callers translate
/// their own data into this shape.
/// </summary>
public sealed record GraphVisNode
{
  /// <summary>Default value of <see cref="RelativeSize"/>.</summary>
  public const float DefaultRelativeSize = 1f;

  /// <summary>Identifier, unique among the nodes of one graph.</summary>
  public required string Id { get; init; }

  /// <summary>
  /// Size relative to other nodes (positive, finite). Drives the drawn radius and how heavy the
  /// node is for the layout.
  /// </summary>
  public float RelativeSize { get; init; } = DefaultRelativeSize;

  /// <summary>Fill color; null uses the style's default node color.</summary>
  public UiRgba? Color { get; init; }

  /// <summary>Whether the outline is dashed.</summary>
  public bool IsDashed { get; init; }

  /// <summary>
  /// Whether layouts that need a starting point (such as the layered layout) begin from this node,
  /// for example the start room. Optional.
  /// </summary>
  /// <remarks>
  /// Several nodes may be marked; one per connected part is the intended use. If one part has
  /// several, layouts use the lowest-index one and place the others like ordinary nodes. Layouts
  /// that need no starting point (Stress, Ring) ignore this flag.
  /// </remarks>
  public bool IsLayoutRoot { get; init; }

  /// <summary>
  /// Drawn more prominently (radial glow fill), for example start and exit rooms. Purely
  /// visual; size and layout are unaffected.
  /// </summary>
  public bool IsEmphasized { get; init; }

  /// <summary>Markers shown inside the node, in display order.</summary>
  public IReadOnlyList<GraphVisMarker> Markers { get; init; } = [];

  /// <summary>Arbitrary key/value data shown in the data panel when the node is selected, in display order.</summary>
  public GraphVisProperties Properties { get; init; } = GraphVisProperties.Empty;

  /// <summary>
  /// Keys of <see cref="Properties"/> shown indented in the data panel (for example the items of
  /// a list whose count is the property before them). Keys that match no property are ignored.
  /// </summary>
  public IReadOnlySet<string> IndentedPropertyKeys { get; init; } = new HashSet<string>();
}

/// <summary>
/// One relationship between two nodes of a visualized graph.
/// </summary>
public sealed record GraphVisEdge
{
  /// <summary>Identifier, unique among the edges of one graph.</summary>
  public required string Id { get; init; }

  /// <summary>Identifier of the node the edge starts at.</summary>
  public required string FromId { get; init; }

  /// <summary>Identifier of the node the edge ends at; must differ from <see cref="FromId"/>.</summary>
  public required string ToId { get; init; }

  /// <summary>Whether an arrowhead is drawn at the <see cref="FromId"/> end.</summary>
  public bool ArrowAtFrom { get; init; }

  /// <summary>Whether an arrowhead is drawn at the <see cref="ToId"/> end.</summary>
  public bool ArrowAtTo { get; init; }

  /// <summary>Whether the line is dashed.</summary>
  public bool IsDashed { get; init; }

  /// <summary>Line color; null uses the style's default edge color.</summary>
  public UiRgba? Color { get; init; }

  /// <summary>Short text shown over the middle of the line; null or empty shows nothing.</summary>
  public string? Label { get; init; }

  /// <summary>
  /// Identifiers of markers highlighted while this edge is selected. Identifiers that match no
  /// marker are ignored.
  /// </summary>
  public IReadOnlyList<string> HighlightMarkerIds { get; init; } = [];

  /// <summary>Arbitrary key/value data shown in the data panel when the edge is selected, in display order.</summary>
  public GraphVisProperties Properties { get; init; } = GraphVisProperties.Empty;

  /// <summary>
  /// Keys of <see cref="Properties"/> shown indented in the data panel (for example the items of
  /// a list whose count is the property before them). Keys that match no property are ignored.
  /// </summary>
  public IReadOnlySet<string> IndentedPropertyKeys { get; init; } = new HashSet<string>();
}

/// <summary>
/// Validated, immutable input of the graph visualizer: nodes and the edges between them.
/// </summary>
public sealed class GraphVisData
{
  /// <summary>Graph without nodes or edges.</summary>
  public static GraphVisData Empty { get; } = new([], []);

  /// <summary>Nodes by identifier.</summary>
  private readonly Dictionary<string, GraphVisNode> _nodesById;

  /// <summary>
  /// Creates the graph and validates it.
  /// </summary>
  /// <param name="nodes">Nodes.</param>
  /// <param name="edges">Edges.</param>
  /// <exception cref="ArgumentNullException">An argument or one of its items is null.</exception>
  /// <exception cref="ArgumentException">
  /// An identifier is blank or duplicated (nodes, edges, markers), a node's relative size is not
  /// positive and finite, or an edge refers to a missing node or connects a node to itself.
  /// </exception>
  public GraphVisData(IReadOnlyList<GraphVisNode> nodes, IReadOnlyList<GraphVisEdge> edges)
  {
    ArgumentNullException.ThrowIfNull(nodes);
    ArgumentNullException.ThrowIfNull(edges);

    _nodesById = new Dictionary<string, GraphVisNode>(nodes.Count);
    var markerIds = new HashSet<string>();
    foreach (GraphVisNode node in nodes)
    {
      ArgumentNullException.ThrowIfNull(node, nameof(nodes));
      RequireId(node.Id, "Node", nameof(nodes));
      if (!_nodesById.TryAdd(node.Id, node))
        throw new ArgumentException($"Duplicate node identifier '{node.Id}'.", nameof(nodes));
      if (!float.IsFinite(node.RelativeSize) || node.RelativeSize <= 0)
        throw new ArgumentException($"Node '{node.Id}' has relative size {node.RelativeSize}; it must be positive and finite.", nameof(nodes));

      foreach (GraphVisMarker marker in node.Markers)
      {
        ArgumentNullException.ThrowIfNull(marker, nameof(nodes));
        RequireId(marker.Id, "Marker", nameof(nodes));
        if (!markerIds.Add(marker.Id))
          throw new ArgumentException($"Duplicate marker identifier '{marker.Id}'.", nameof(nodes));
      }
    }

    var edgeIds = new HashSet<string>();
    foreach (GraphVisEdge edge in edges)
    {
      ArgumentNullException.ThrowIfNull(edge, nameof(edges));
      RequireId(edge.Id, "Edge", nameof(edges));
      if (!edgeIds.Add(edge.Id))
        throw new ArgumentException($"Duplicate edge identifier '{edge.Id}'.", nameof(edges));
      if (!_nodesById.ContainsKey(edge.FromId))
        throw new ArgumentException($"Edge '{edge.Id}' starts at missing node '{edge.FromId}'.", nameof(edges));
      if (!_nodesById.ContainsKey(edge.ToId))
        throw new ArgumentException($"Edge '{edge.Id}' ends at missing node '{edge.ToId}'.", nameof(edges));
      if (edge.FromId == edge.ToId)
        throw new ArgumentException($"Edge '{edge.Id}' connects node '{edge.FromId}' to itself.", nameof(edges));
    }

    Nodes = nodes;
    Edges = edges;
  }

  /// <summary>Nodes, in the order given.</summary>
  public IReadOnlyList<GraphVisNode> Nodes { get; }

  /// <summary>Edges, in the order given.</summary>
  public IReadOnlyList<GraphVisEdge> Edges { get; }

  /// <summary>Whether the graph has no nodes (and therefore nothing to draw).</summary>
  public bool IsEmpty => Nodes.Count == 0;

  /// <summary>
  /// Finds a node by identifier.
  /// </summary>
  /// <param name="id">Node identifier.</param>
  /// <returns>The node, or null if there is none with this identifier.</returns>
  public GraphVisNode? FindNode(string id)
  {
    ArgumentNullException.ThrowIfNull(id);
    return _nodesById.GetValueOrDefault(id);
  }

  /// <summary>
  /// Throws if an identifier is null, empty or whitespace.
  /// </summary>
  /// <param name="id">Identifier to check.</param>
  /// <param name="kind">What the identifier belongs to, for the message.</param>
  /// <param name="parameterName">Name of the offending constructor parameter.</param>
  /// <exception cref="ArgumentException">The identifier is blank.</exception>
  private static void RequireId(string? id, string kind, string parameterName)
  {
    if (string.IsNullOrWhiteSpace(id))
      throw new ArgumentException($"{kind} identifier must not be blank.", parameterName);
  }
}
