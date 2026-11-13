using LostInLimbo.LevelEditor.Ui.Gum.App.Info;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Render;
using LostInLimbo.LevelGen.Data.Result.Graph;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

/// <summary>
/// Headless tests for <see cref="LevelGraphVisAdapter"/> (level graph to visualizer model).
/// </summary>
public sealed class LevelGraphVisAdapterTests
{
  /// <summary>
  /// Creates a place.
  /// </summary>
  /// <param name="relSize">Relative size.</param>
  /// <param name="group">Style group, or null.</param>
  /// <param name="secret">Whether the place is secret.</param>
  /// <param name="pois">POIs inside the place.</param>
  /// <returns>Place with a new identifier.</returns>
  private static LilNode Node(int relSize = 1, string? group = null, bool secret = false, params LilPoi[] pois) => new()
  {
    Id = Guid.NewGuid(),
    RelSize = relSize,
    MaxEdges = -1,
    Secret = secret,
    Style = new LilStyle { Group = group },
    Pois = [.. pois],
  };

  /// <summary>
  /// Creates a POI.
  /// </summary>
  /// <param name="type">POI type.</param>
  /// <param name="name">Name, or null.</param>
  /// <returns>POI with a new identifier.</returns>
  private static LilPoi Poi(EnPoiType type, string? name = null) => new() { Id = Guid.NewGuid(), Type = type, Name = name };

  /// <summary>
  /// Creates a passage.
  /// </summary>
  /// <param name="from">Source place.</param>
  /// <param name="to">Destination place.</param>
  /// <param name="direction">Direction.</param>
  /// <param name="secret">Whether the passage is secret.</param>
  /// <param name="requiredPois">POIs required to pass.</param>
  /// <returns>Passage with a new identifier.</returns>
  private static LilEdge Edge(
    LilNode from, LilNode to, EnPassageDirection direction = EnPassageDirection.Bidirectional, bool secret = false, params LilPoi[] requiredPois) => new()
  {
    Id = Guid.NewGuid(),
    From = from.Id,
    To = to.Id,
    Direction = direction,
    Secret = secret,
    Conditions = requiredPois.Select(poi => new LilCondition { Type = EnConditionType.RequiresSolution, RequiresPoi = poi.Id }).ToList(),
  };

  /// <summary>
  /// Verifies an empty level graph converts to an empty visualizer graph.
  /// </summary>
  [Fact]
  public void Convert_EmptyGraph_IsEmpty()
  {
    // Act: Convert a graph without places.
    GraphVisData data = LevelGraphVisAdapter.Convert(new LilGraph());

    // Assert: Nothing to draw.
    Assert.True(data.IsEmpty);
    Assert.Empty(data.Edges);
  }

  /// <summary>
  /// Verifies the visible traits of a place: identifier, size, secret (dashed) outline.
  /// </summary>
  [Fact]
  public void Convert_Node_MapsIdSizeAndSecret()
  {
    // Arrange: A secret place of size 7.
    LilNode node = Node(relSize: 7, secret: true);

    // Act: Convert.
    GraphVisNode converted = Assert.Single(LevelGraphVisAdapter.Convert(new LilGraph { Nodes = [node] }).Nodes);

    // Assert: Identifier as text, same size, dashed outline, no color without a group.
    Assert.Equal(node.Id.ToString(), converted.Id);
    Assert.Equal(7f, converted.RelativeSize);
    Assert.True(converted.IsDashed);
    Assert.Null(converted.Color);
  }

  /// <summary>
  /// Verifies a place with a non-positive size is still drawable and keeps its real size as data.
  /// </summary>
  /// <param name="relSize">Non-positive relative size.</param>
  [Theory]
  [InlineData(0)]
  [InlineData(-3)]
  public void Convert_NodeWithNonPositiveSize_UsesSmallestSize(int relSize)
  {
    // Act: Convert a place the visualizer could not draw as is.
    GraphVisNode converted = Assert.Single(LevelGraphVisAdapter.Convert(new LilGraph { Nodes = [Node(relSize)] }).Nodes);

    // Assert: Smallest drawable size; the data still shows the generator's value.
    Assert.Equal(LevelGraphVisAdapter.SmallestRelativeSize, converted.RelativeSize);
    Assert.Equal(relSize.ToString(), converted.Properties["Relative size"]);
  }

  /// <summary>
  /// Verifies group colors: same group same color, different groups different colors, no group
  /// (null or empty) no color, and the color is the stable one for the group name.
  /// </summary>
  [Fact]
  public void Convert_Groups_GetStableDistinctColors()
  {
    // Arrange: Two places in one group, one in another, two without a group.
    var graph = new LilGraph { Nodes = [Node(group: "castle"), Node(group: "castle"), Node(group: "caves"), Node(), Node(group: "")] };

    // Act: Convert.
    IReadOnlyList<GraphVisNode> nodes = LevelGraphVisAdapter.Convert(graph).Nodes;

    // Assert: Colors follow the groups.
    Assert.NotNull(nodes[0].Color);
    Assert.Equal(nodes[0].Color, nodes[1].Color);
    Assert.NotEqual(nodes[0].Color, nodes[2].Color);
    Assert.Null(nodes[3].Color);
    Assert.Null(nodes[4].Color);
    Assert.Equal(GraphVisColors.AssignColors(["castle", "caves"])["castle"], nodes[0].Color);
  }

  /// <summary>
  /// Verifies POIs become markers labelled with their name, or their type when unnamed.
  /// </summary>
  [Fact]
  public void Convert_Pois_BecomeMarkersWithNameOrTypeLabel()
  {
    // Arrange: A named, an unnamed and a blank-named POI.
    LilPoi named = Poi(EnPoiType.Solution, "Red Key");
    LilPoi unnamed = Poi(EnPoiType.Start);
    LilPoi blank = Poi(EnPoiType.Exit, "  ");

    // Act: Convert.
    GraphVisNode converted = Assert.Single(
      LevelGraphVisAdapter.Convert(new LilGraph { Nodes = [Node(pois: [named, unnamed, blank])] }).Nodes);

    // Assert: Markers in POI order, identified by the POI identifiers.
    Assert.Equal(
      [new GraphVisMarker(named.Id.ToString(), "Red Key"), new GraphVisMarker(unnamed.Id.ToString(), "Start"), new GraphVisMarker(blank.Id.ToString(), "Exit")],
      converted.Markers);
  }

  /// <summary>
  /// Verifies the data shown for a selected place, including reach data by POI name.
  /// </summary>
  [Fact]
  public void Convert_Node_ListsAllDataAsProperties()
  {
    // Arrange: A start place and a place reachable from it only with the key.
    LilPoi start = Poi(EnPoiType.Start);
    LilPoi key = Poi(EnPoiType.Solution, "Red Key");
    LilNode startNode = Node(pois: [start, key]);
    LilNode target = Node(relSize: 4, group: "castle") with { MaxEdges = 3 };
    target.Reach[start.Id] = [key.Id];
    target.OpenReach.Add(start.Id);

    // Act: Convert.
    IReadOnlyList<GraphVisNode> nodes = LevelGraphVisAdapter.Convert(new LilGraph { Nodes = [startNode, target] }).Nodes;

    // Assert: Start place lists its POIs and has unlimited edges.
    Assert.Equal(LevelGraphVisAdapter.UnlimitedText, nodes[0].Properties["Max edges"]);
    Assert.Equal("2", nodes[0].Properties["POIs"]);
    Assert.Equal("Start", nodes[0].Properties["POI 1"]);
    Assert.Equal("Red Key (Solution)", nodes[0].Properties["POI 2"]);
    Assert.Equal(["POI 1", "POI 2"], nodes[0].IndentedPropertyKeys.Order());
    Assert.Equal(LevelGraphVisAdapter.NoGroupText, nodes[0].Properties["Group"]);
    Assert.Equal(LevelGraphVisAdapter.NoneText, nodes[0].Properties["Open reach from"]);

    // Assert: Target place shows its own data and reach by POI name.
    Assert.Equal(target.Id.ToString(), nodes[1].Properties["Id"]);
    Assert.Equal("4", nodes[1].Properties["Relative size"]);
    Assert.Equal("3", nodes[1].Properties["Max edges"]);
    Assert.Equal("no", nodes[1].Properties["Secret"]);
    Assert.Equal("castle", nodes[1].Properties["Group"]);
    Assert.Equal("Start", nodes[1].Properties["Open reach from"]);
    Assert.Equal("Red Key", nodes[1].Properties["Reach from Start needs"]);
  }

  /// <summary>
  /// Verifies arrows follow the passage direction: both ends for two-way, destination only for one-way.
  /// </summary>
  /// <param name="direction">Passage direction.</param>
  /// <param name="arrowAtFrom">Expected arrow at the source.</param>
  [Theory]
  [InlineData(EnPassageDirection.Bidirectional, true)]
  [InlineData(EnPassageDirection.OneWay, false)]
  public void Convert_EdgeDirection_SetsArrows(EnPassageDirection direction, bool arrowAtFrom)
  {
    // Arrange: Two places joined by one passage.
    LilNode a = Node();
    LilNode b = Node();
    LilEdge edge = Edge(a, b, direction);

    // Act: Convert.
    GraphVisEdge converted = Assert.Single(LevelGraphVisAdapter.Convert(new LilGraph { Nodes = [a, b], Edges = [edge] }).Edges);

    // Assert: Ends and arrows.
    Assert.Equal(edge.Id.ToString(), converted.Id);
    Assert.Equal(a.Id.ToString(), converted.FromId);
    Assert.Equal(b.Id.ToString(), converted.ToId);
    Assert.Equal(arrowAtFrom, converted.ArrowAtFrom);
    Assert.True(converted.ArrowAtTo);
  }

  /// <summary>
  /// Verifies an open, ordinary passage is a plain solid line without text.
  /// </summary>
  [Fact]
  public void Convert_PlainEdge_IsSolidDefaultColoredAndUnlabelled()
  {
    // Arrange: Open passage.
    LilNode a = Node();
    LilNode b = Node();

    // Act: Convert.
    GraphVisEdge converted = Assert.Single(LevelGraphVisAdapter.Convert(new LilGraph { Nodes = [a, b], Edges = [Edge(a, b)] }).Edges);

    // Assert: Solid, default color, no label, nothing to highlight.
    Assert.False(converted.IsDashed);
    Assert.Null(converted.Color);
    Assert.Null(converted.Label);
    Assert.Empty(converted.HighlightMarkerIds);
    Assert.Equal("0", converted.Properties["Conditions"]);
  }

  /// <summary>
  /// Verifies a secret passage is dashed and dark gray.
  /// </summary>
  [Fact]
  public void Convert_SecretEdge_IsDashedDarkGray()
  {
    // Arrange: Secret passage.
    LilNode a = Node();
    LilNode b = Node();

    // Act: Convert.
    GraphVisEdge converted = Assert.Single(
      LevelGraphVisAdapter.Convert(new LilGraph { Nodes = [a, b], Edges = [Edge(a, b, secret: true)] }).Edges);

    // Assert: Dashed, dark gray, flagged in the data.
    Assert.True(converted.IsDashed);
    Assert.Equal(LevelGraphVisAdapter.SecretEdgeColor, converted.Color);
    Assert.Equal("yes", converted.Properties["Secret"]);
  }

  /// <summary>
  /// Verifies conditions: their count is the line text, the required POIs are the markers to
  /// highlight, and each condition is described by POI name.
  /// </summary>
  [Fact]
  public void Convert_EdgeWithConditions_ShowsCountAndPointsAtSolutionMarkers()
  {
    // Arrange: A door needing the red and the blue key.
    LilPoi red = Poi(EnPoiType.Solution, "Red Key");
    LilPoi blue = Poi(EnPoiType.Solution, "Blue Key");
    LilNode a = Node(pois: [red, blue]);
    LilNode b = Node();
    LilEdge edge = Edge(a, b, requiredPois: [red, blue]) with { Chokepoint = true };

    // Act: Convert.
    GraphVisData data = LevelGraphVisAdapter.Convert(new LilGraph { Nodes = [a, b], Edges = [edge] });
    GraphVisEdge converted = Assert.Single(data.Edges);

    // Assert: Count as label; highlight ids match the markers of the required POIs.
    Assert.Equal("2", converted.Label);
    Assert.Equal([red.Id.ToString(), blue.Id.ToString()], converted.HighlightMarkerIds);
    Assert.All(converted.HighlightMarkerIds, id => Assert.Contains(data.Nodes[0].Markers, marker => marker.Id == id));

    // Assert: Data describes the passage and its conditions.
    Assert.Equal("Bidirectional", converted.Properties["Direction"]);
    Assert.Equal("yes", converted.Properties["Chokepoint"]);
    Assert.Equal("2", converted.Properties["Conditions"]);
    Assert.Equal("RequiresSolution: Red Key", converted.Properties["Condition 1"]);
    Assert.Equal("RequiresSolution: Blue Key", converted.Properties["Condition 2"]);
    Assert.Equal(["Condition 1", "Condition 2"], converted.IndentedPropertyKeys.Order());
  }

  /// <summary>
  /// Verifies a malformed level graph is reported, not silently repaired.
  /// </summary>
  [Fact]
  public void Convert_EdgeToMissingNode_Throws()
  {
    // Arrange: Passage leading to a place that is not in the graph.
    LilNode a = Node();
    LilNode missing = Node();

    // Act + Assert: The visualizer model rejects it.
    Assert.Throws<ArgumentException>(() => LevelGraphVisAdapter.Convert(new LilGraph { Nodes = [a], Edges = [Edge(a, missing)] }));
  }

  /// <summary>
  /// Verifies null is rejected.
  /// </summary>
  [Fact]
  public void Convert_Null_Throws()
  {
    // Act + Assert: No graph, no conversion.
    Assert.Throws<ArgumentNullException>(() => LevelGraphVisAdapter.Convert(null!));
  }

  /// <summary>
  /// Verifies the place holding the Start POI is marked as layout root, and only that one.
  /// </summary>
  [Fact]
  public void Convert_StartPlace_IsLayoutRoot()
  {
    // Arrange: A place with a Start POI, one with another POI and one without POIs.
    LilNode start = Node(1, null, false, Poi(EnPoiType.Start));
    LilNode exit = Node(1, null, false, Poi(EnPoiType.Exit));
    LilNode plain = Node();

    // Act: Convert.
    GraphVisData data = LevelGraphVisAdapter.Convert(new LilGraph { Nodes = [start, exit, plain] });

    // Assert: Only the start place is a layout root.
    Assert.Equal([true, false, false], data.Nodes.Select(node => node.IsLayoutRoot));
  }

  /// <summary>
  /// Verifies unnamed optional goals and solutions use the short type names.
  /// </summary>
  [Fact]
  public void Convert_UnnamedOptionalGoalAndSolution_UseShortNames()
  {
    // Arrange: One unnamed optional goal and one unnamed solution.
    LilPoi optional = Poi(EnPoiType.OptionalGoal);
    LilPoi solution = Poi(EnPoiType.Solution);

    // Act: Convert.
    GraphVisNode converted = Assert.Single(
      LevelGraphVisAdapter.Convert(new LilGraph { Nodes = [Node(pois: [optional, solution])] }).Nodes);

    // Assert: Short labels on the markers and in the POI rows.
    Assert.Equal(["OptGoal", "Sol"], converted.Markers.Select(marker => marker.Label));
    Assert.Equal("OptGoal", converted.Properties["POI 1"]);
    Assert.Equal("Sol", converted.Properties["POI 2"]);
  }

  /// <summary>
  /// Verifies unnamed POIs of a type that occurs more than once in the graph are numbered in graph
  /// order, across places, and that the numbers are used where the POIs are referenced.
  /// </summary>
  [Fact]
  public void Convert_RepeatedUnnamedType_IsNumberedInGraphOrder()
  {
    // Arrange: Solutions in two places (two unnamed, one named), one start; the target needs the second unnamed solution.
    LilPoi start = Poi(EnPoiType.Start);
    LilPoi first = Poi(EnPoiType.Solution);
    LilPoi named = Poi(EnPoiType.Solution, "Red Key");
    LilPoi second = Poi(EnPoiType.Solution);
    LilNode a = Node(pois: [start, first]);
    LilNode b = Node(pois: [named, second]);
    LilNode target = Node();
    target.Reach[start.Id] = [second.Id];

    // Act: Convert.
    GraphVisData data = LevelGraphVisAdapter.Convert(new LilGraph { Nodes = [a, b, target] });

    // Assert: Unnamed solutions numbered 1 and 2; the start (alone) and the named one are not.
    Assert.Equal(["Start", "Sol 1"], data.Nodes[0].Markers.Select(marker => marker.Label));
    Assert.Equal(["Red Key", "Sol 2"], data.Nodes[1].Markers.Select(marker => marker.Label));
    Assert.Equal("Sol 2", data.Nodes[1].Properties["POI 2"]);
    Assert.Equal("Sol 2", data.Nodes[2].Properties["Reach from Start needs"]);
  }

  /// <summary>
  /// Verifies the short type names of all POI types.
  /// </summary>
  /// <param name="type">POI type.</param>
  /// <param name="expected">Expected short name.</param>
  [Theory]
  [InlineData(EnPoiType.Start, "Start")]
  [InlineData(EnPoiType.Exit, "Exit")]
  [InlineData(EnPoiType.Goal, "Goal")]
  [InlineData(EnPoiType.OptionalGoal, "OptGoal")]
  [InlineData(EnPoiType.Solution, "Sol")]
  public void ShortTypeName_IsAsExpected(EnPoiType type, string expected)
  {
    // Act + Assert: Short name of the type.
    Assert.Equal(expected, LevelGraphVisAdapter.ShortTypeName(type));
  }
}
