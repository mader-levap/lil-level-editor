using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using LostInLimbo.LevelGen.Data.Result.Graph;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.Info;

/// <summary>
/// Translates a generated level graph into the generic model of the graph visualizer. This is the
/// only place that knows both sides: the visualizer never sees level types.
/// </summary>
/// <remarks>
/// Pure and engine-independent. It only describes the graph; it does not reorder, fix or filter
/// what the generator produced.
/// </remarks>
public static class LevelGraphVisAdapter
{
  #region Constants

  /// <summary>Line color of secret passages (dark gray).</summary>
  public static readonly UiRgba SecretEdgeColor = new(70, 70, 70);

  /// <summary>
  /// Relative size used for a node whose <see cref="LilNode.RelSize"/> is not positive, so it is
  /// still drawn (at the visualizer's smallest radius) and the real value stays visible in its data.
  /// </summary>
  public const float SmallestRelativeSize = 0.01f;

  /// <summary>Value shown for an absent group.</summary>
  public const string NoGroupText = "(none)";

  /// <summary>Value shown for an unlimited edge count.</summary>
  public const string UnlimitedText = "unlimited";

  /// <summary>Value shown for an empty list.</summary>
  public const string NoneText = "-";

  /// <summary>Separator between the items of a list value.</summary>
  private const string ListSeparator = ", ";

  #endregion

  /// <summary>
  /// Converts a level graph.
  /// </summary>
  /// <param name="graph">Level graph.</param>
  /// <returns>Graph for the visualizer.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="graph"/> is null.</exception>
  /// <exception cref="ArgumentException">
  /// The level graph is malformed (duplicate identifiers, a passage to a missing place or from a
  /// place to itself); see <see cref="GraphVisData"/>.
  /// </exception>
  public static GraphVisData Convert(LilGraph graph)
  {
    ArgumentNullException.ThrowIfNull(graph);

    // POI texts by identifier, to describe conditions and reach data by name instead of by GUID.
    var poiTexts = new Dictionary<Guid, string>();
    foreach (LilPoi poi in graph.Nodes.SelectMany(node => node.Pois)) poiTexts.TryAdd(poi.Id, PoiLabel(poi));

    IReadOnlyDictionary<string, UiRgba> groupColors = GraphVisColors.AssignColors(
      graph.Nodes.Select(node => node.Style.Group).Where(group => !string.IsNullOrEmpty(group)).Select(group => group!));

    GraphVisNode[] nodes = graph.Nodes.Select(node => ConvertNode(node, groupColors, poiTexts)).ToArray();
    GraphVisEdge[] edges = graph.Edges.Select(edge => ConvertEdge(edge, poiTexts)).ToArray();
    return new GraphVisData(nodes, edges);
  }

  #region Nodes

  /// <summary>
  /// Converts one place.
  /// </summary>
  /// <param name="node">Place.</param>
  /// <param name="groupColors">Color of each group.</param>
  /// <param name="poiTexts">Text of each POI of the graph, by identifier.</param>
  /// <returns>Visualizer node.</returns>
  private static GraphVisNode ConvertNode(
    LilNode node, IReadOnlyDictionary<string, UiRgba> groupColors, IReadOnlyDictionary<Guid, string> poiTexts)
  {
    string? group = string.IsNullOrEmpty(node.Style.Group) ? null : node.Style.Group;

    // Properties are added in display order.
    var properties = new Dictionary<string, string>
    {
      ["Id"] = Id(node.Id),
      ["Relative size"] = node.RelSize.ToString(CultureInfo.InvariantCulture),
      ["Max edges"] = node.MaxEdges < 0 ? UnlimitedText : node.MaxEdges.ToString(CultureInfo.InvariantCulture),
      ["Secret"] = YesNo(node.Secret),
      ["Group"] = group ?? NoGroupText,
      ["POIs"] = node.Pois.Count.ToString(CultureInfo.InvariantCulture),
    };

    // The single POIs are shown indented under their count.
    var indentedKeys = new HashSet<string>();
    for (int i = 0; i < node.Pois.Count; i++)
    {
      LilPoi poi = node.Pois[i];
      string key = $"POI {i + 1}";
      properties[key] = string.IsNullOrWhiteSpace(poi.Name) ? poi.Type.ToString() : $"{poi.Name} ({poi.Type})";
      indentedKeys.Add(key);
    }

    properties["Open reach from"] = Join(node.OpenReach.Select(start => PoiText(start, poiTexts)));
    foreach ((Guid start, List<Guid> solutions) in node.Reach.OrderBy(entry => entry.Key))
    {
      properties[$"Reach from {PoiText(start, poiTexts)} needs"] = Join(solutions.Select(solution => PoiText(solution, poiTexts)));
    }

    return new GraphVisNode
    {
      Id = Id(node.Id),
      RelativeSize = node.RelSize > 0 ? node.RelSize : SmallestRelativeSize,
      Color = group == null ? null : groupColors[group],
      IsDashed = node.Secret,
      IsLayoutRoot = node.Pois.Any(poi => poi.Type == EnPoiType.Start), // layered layout starts at the start room
      Markers = node.Pois.Select(poi => new GraphVisMarker(Id(poi.Id), PoiLabel(poi))).ToArray(),
      Properties = properties,
      IndentedPropertyKeys = indentedKeys,
    };
  }

  /// <summary>
  /// Short text of a POI: its name, or its type when it has no name.
  /// </summary>
  /// <param name="poi">POI.</param>
  /// <returns>Label.</returns>
  private static string PoiLabel(LilPoi poi) => string.IsNullOrWhiteSpace(poi.Name) ? poi.Type.ToString() : poi.Name;

  /// <summary>
  /// Text of a POI referenced by identifier.
  /// </summary>
  /// <param name="poiId">POI identifier.</param>
  /// <param name="poiTexts">Text of each POI of the graph, by identifier.</param>
  /// <returns>The POI's label, or its identifier when the graph has no such POI.</returns>
  private static string PoiText(Guid poiId, IReadOnlyDictionary<Guid, string> poiTexts)
  {
    return poiTexts.TryGetValue(poiId, out string? text) ? text : Id(poiId);
  }

  #endregion

  #region Edges

  /// <summary>
  /// Converts one passage.
  /// </summary>
  /// <param name="edge">Passage.</param>
  /// <param name="poiTexts">Text of each POI of the graph, by identifier.</param>
  /// <returns>Visualizer edge.</returns>
  private static GraphVisEdge ConvertEdge(LilEdge edge, IReadOnlyDictionary<Guid, string> poiTexts)
  {
    // Properties are added in display order.
    var properties = new Dictionary<string, string>
    {
      ["Id"] = Id(edge.Id),
      ["From"] = Id(edge.From),
      ["To"] = Id(edge.To),
      ["Direction"] = edge.Direction.ToString(),
      ["Chokepoint"] = YesNo(edge.Chokepoint),
      ["Secret"] = YesNo(edge.Secret),
      ["Conditions"] = edge.Conditions.Count.ToString(CultureInfo.InvariantCulture),
    };

    // The single conditions are shown indented under their count.
    var indentedKeys = new HashSet<string>();
    for (int i = 0; i < edge.Conditions.Count; i++)
    {
      LilCondition condition = edge.Conditions[i];
      string key = $"Condition {i + 1}";
      properties[key] = $"{condition.Type}: {PoiText(condition.RequiresPoi, poiTexts)}";
      indentedKeys.Add(key);
    }

    return new GraphVisEdge
    {
      Id = Id(edge.Id),
      FromId = Id(edge.From),
      ToId = Id(edge.To),
      // A two-way passage points both ways; a one-way passage only at its destination.
      ArrowAtFrom = edge.Direction == EnPassageDirection.Bidirectional,
      ArrowAtTo = true,
      IsDashed = edge.Secret,
      Color = edge.Secret ? SecretEdgeColor : null,
      Label = edge.Conditions.Count == 0 ? null : edge.Conditions.Count.ToString(CultureInfo.InvariantCulture),
      HighlightMarkerIds = edge.Conditions.Select(condition => Id(condition.RequiresPoi)).Distinct().ToArray(),
      Properties = properties,
      IndentedPropertyKeys = indentedKeys,
    };
  }

  #endregion

  #region Formatting

  /// <summary>
  /// Formats an identifier for the visualizer.
  /// </summary>
  /// <param name="id">Identifier.</param>
  /// <returns>Identifier text.</returns>
  private static string Id(Guid id) => id.ToString();

  /// <summary>
  /// Formats a flag.
  /// </summary>
  /// <param name="value">Flag.</param>
  /// <returns><c>"yes"</c> or <c>"no"</c>.</returns>
  private static string YesNo(bool value) => value ? "yes" : "no";

  /// <summary>
  /// Formats a list.
  /// </summary>
  /// <param name="items">Items.</param>
  /// <returns>Comma-separated items, or <see cref="NoneText"/> when there are none.</returns>
  private static string Join(IEnumerable<string> items)
  {
    string joined = string.Join(ListSeparator, items);
    return joined.Length == 0 ? NoneText : joined;
  }

  #endregion
}
