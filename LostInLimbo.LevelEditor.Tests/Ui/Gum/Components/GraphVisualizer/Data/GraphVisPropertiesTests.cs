using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer.Data;

/// <summary>
/// Headless tests for <see cref="GraphVisProperties"/> (ordered key/value data of the data panel).
/// </summary>
public sealed class GraphVisPropertiesTests
{
  /// <summary>
  /// Verifies that enumeration and positional access follow insertion order, also for keys that
  /// would sort or hash differently.
  /// </summary>
  [Fact]
  public void Enumeration_FollowsInsertionOrder()
  {
    // Arrange: Keys deliberately not in alphabetical order.
    var pairs = new[] { Pair("Zeta", "1"), Pair("Alpha", "2"), Pair("Mu", "3"), Pair("Beta", "4") };

    // Act: Build the properties.
    var properties = new GraphVisProperties(pairs);

    // Assert: Same order when enumerated and when indexed by position.
    Assert.Equal(pairs, properties.ToArray());
    Assert.Equal("Mu", properties[2].Key);
    Assert.Equal(new[] { "Zeta", "Alpha", "Mu", "Beta" }, properties.Keys);
    Assert.Equal(new[] { "1", "2", "3", "4" }, properties.Values);
  }

  /// <summary>
  /// Verifies lookup by key.
  /// </summary>
  [Fact]
  public void LookupByKey_FindsValue()
  {
    // Arrange: Two properties.
    var properties = new GraphVisProperties([Pair("Id", "a"), Pair("Secret", "no")]);

    // Assert: Indexer, TryGetValue and ContainsKey agree.
    Assert.Equal("no", properties["Secret"]);
    Assert.True(properties.TryGetValue("Id", out string? value));
    Assert.Equal("a", value);
    Assert.False(properties.ContainsKey("Missing"));
    Assert.Throws<KeyNotFoundException>(() => properties["Missing"]);
  }

  /// <summary>
  /// Verifies that a repeated key is rejected instead of silently replacing the earlier value.
  /// </summary>
  [Fact]
  public void Constructor_DuplicateKey_Throws()
  {
    // Assert: Keys must be unique.
    Assert.Throws<ArgumentException>(() => new GraphVisProperties([Pair("Id", "a"), Pair("Id", "b")]));
  }

  /// <summary>
  /// Verifies that the source can be changed afterwards without affecting the properties.
  /// </summary>
  [Fact]
  public void Constructor_CopiesSource()
  {
    // Arrange: A mutable source.
    var source = new List<KeyValuePair<string, string>> { Pair("Id", "a") };
    var properties = new GraphVisProperties(source);

    // Act: Change the source.
    source.Add(Pair("Extra", "x"));

    // Assert: Properties unchanged.
    Assert.Single(properties);
  }

  /// <summary>
  /// Verifies the shared empty instance.
  /// </summary>
  [Fact]
  public void Empty_HasNoProperties()
  {
    // Assert: Nothing inside.
    Assert.Empty(GraphVisProperties.Empty);
    Assert.Empty(GraphVisProperties.Empty.Keys);
  }

  /// <summary>
  /// Creates a key/value pair.
  /// </summary>
  /// <param name="key">Key.</param>
  /// <param name="value">Value.</param>
  /// <returns>Pair.</returns>
  private static KeyValuePair<string, string> Pair(string key, string value) => new(key, value);
}
