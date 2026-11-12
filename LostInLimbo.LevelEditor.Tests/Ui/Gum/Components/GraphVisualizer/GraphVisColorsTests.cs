using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Headless tests for <see cref="GraphVisColors"/> (darkening and stable, distinct key colors).
/// </summary>
public sealed class GraphVisColorsTests
{
  /// <summary>
  /// Finds two different keys that hash to the same palette slot.
  /// </summary>
  /// <returns>Colliding keys, in ordinal order.</returns>
  private static (string First, string Second) FindCollidingKeys()
  {
    var seen = new Dictionary<uint, string>();
    for (int i = 0; ; i++)
    {
      string key = $"group{i:D4}"; // zero-padded, so generation order is ordinal order
      uint slot = GraphVisColors.StableHash(key) % (uint)GraphVisColors.Palette.Count;
      if (seen.TryGetValue(slot, out string? earlier)) return (earlier, key);
      seen[slot] = key;
    }
  }

  /// <summary>
  /// Verifies darkening scales the color channels and keeps alpha.
  /// </summary>
  [Fact]
  public void Darken_ScalesChannelsAndKeepsAlpha()
  {
    // Act: Halve the brightness of a translucent color.
    UiRgba darker = GraphVisColors.Darken(new UiRgba(200, 100, 50, 128), 0.5f);

    // Assert: Channels halved, alpha unchanged.
    Assert.Equal(new UiRgba(100, 50, 25, 128), darker);
  }

  /// <summary>
  /// Verifies the ends of the brightness range: black and unchanged.
  /// </summary>
  [Fact]
  public void Darken_AtRangeEnds_GivesBlackOrSameColor()
  {
    // Arrange: Any color.
    var color = new UiRgba(10, 20, 30);

    // Assert: 0 gives black, 1 keeps the color.
    Assert.Equal(UiRgba.Black, GraphVisColors.Darken(color, 0f));
    Assert.Equal(color, GraphVisColors.Darken(color, 1f));
  }

  /// <summary>
  /// Verifies a brightness outside 0..1 is rejected.
  /// </summary>
  /// <param name="brightness">Invalid brightness.</param>
  [Theory]
  [InlineData(-0.1f)]
  [InlineData(1.1f)]
  [InlineData(float.NaN)]
  public void Darken_WithInvalidBrightness_Throws(float brightness)
  {
    // Act + Assert: Out-of-range brightness is rejected.
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphVisColors.Darken(UiRgba.White, brightness));
  }

  /// <summary>
  /// Verifies the hash is a fixed function of the text (known FNV-1a values), so colors survive
  /// restarts; <c>string.GetHashCode</c> would differ per process.
  /// </summary>
  [Fact]
  public void StableHash_MatchesKnownValues()
  {
    // Assert: Empty string gives the FNV offset basis; "a" (bytes 0x61, 0x00) its known hash.
    Assert.Equal(2166136261u, GraphVisColors.StableHash(string.Empty));
    // FNV-1a wraps around on overflow by design.
    Assert.Equal(unchecked((2166136261u ^ 0x61) * 16777619u * 16777619u), GraphVisColors.StableHash("a"));
  }

  /// <summary>
  /// Verifies palette colors are all different, opaque and never gray (gray means "no key").
  /// </summary>
  [Fact]
  public void Palette_ColorsAreDistinctOpaqueAndNotGray()
  {
    // Assert: No duplicates, all opaque, every color has unequal channels.
    Assert.Equal(GraphVisColors.Palette.Count, GraphVisColors.Palette.Distinct().Count());
    Assert.All(GraphVisColors.Palette, color =>
    {
      Assert.Equal(byte.MaxValue, color.Alpha);
      Assert.False(color.Red == color.Green && color.Green == color.Blue);
    });
  }

  /// <summary>
  /// Verifies neighbouring palette slots differ strongly, so a key pushed to the next slot by a
  /// collision is still easy to tell apart.
  /// </summary>
  [Fact]
  public void Palette_NeighbouringSlotsAreFarApart()
  {
    // Minimum sum of channel differences between neighbours, found adequate by eye.
    const int minimumDifference = 120;

    // Assert: Every slot differs clearly from the next one.
    for (int i = 0; i < GraphVisColors.Palette.Count; i++)
    {
      UiRgba current = GraphVisColors.Palette[i];
      UiRgba next = GraphVisColors.Palette[(i + 1) % GraphVisColors.Palette.Count];
      int difference = Math.Abs(current.Red - next.Red) + Math.Abs(current.Green - next.Green) + Math.Abs(current.Blue - next.Blue);
      Assert.True(difference >= minimumDifference, $"Slots {i} and {i + 1} differ by only {difference}.");
    }
  }

  /// <summary>
  /// Verifies a key gets the same color regardless of which other (non-colliding) keys are
  /// present and of the input order.
  /// </summary>
  [Fact]
  public void AssignColors_SameKey_GetsSameColorAcrossCalls()
  {
    // Act: Assign colors for a key alone and for the same key listed after another one.
    UiRgba alone = GraphVisColors.AssignColors(["castle"])["castle"];
    UiRgba expected = GraphVisColors.Palette[(int)(GraphVisColors.StableHash("castle") % (uint)GraphVisColors.Palette.Count)];
    IReadOnlyDictionary<string, UiRgba> reversed = GraphVisColors.AssignColors(["castle", "castle"]);

    // Assert: The color is the palette slot of the key's hash; duplicates collapse.
    Assert.Equal(expected, alone);
    Assert.Equal(expected, Assert.Single(reversed).Value);
  }

  /// <summary>
  /// Verifies two keys hashing to the same slot still get different colors, the same way for
  /// either input order.
  /// </summary>
  [Fact]
  public void AssignColors_CollidingKeys_GetDistinctColorsIndependentOfInputOrder()
  {
    // Arrange: Two keys with the same palette slot.
    (string first, string second) = FindCollidingKeys();

    // Act: Assign in both input orders.
    IReadOnlyDictionary<string, UiRgba> forward = GraphVisColors.AssignColors([first, second]);
    IReadOnlyDictionary<string, UiRgba> backward = GraphVisColors.AssignColors([second, first]);

    // Assert: Colors differ, the ordinal-first key keeps its hash slot, order does not matter.
    Assert.NotEqual(forward[first], forward[second]);
    Assert.Equal(GraphVisColors.AssignColors([first])[first], forward[first]);
    Assert.Equal(forward[first], backward[first]);
    Assert.Equal(forward[second], backward[second]);
  }

  /// <summary>
  /// Verifies all colors within one graph are distinct while the palette is large enough.
  /// </summary>
  [Fact]
  public void AssignColors_UpToPaletteSize_AllColorsDistinct()
  {
    // Arrange: As many keys as there are palette colors.
    string[] keys = Enumerable.Range(0, GraphVisColors.Palette.Count).Select(i => $"group{i}").ToArray();

    // Act: Assign colors.
    IReadOnlyDictionary<string, UiRgba> colors = GraphVisColors.AssignColors(keys);

    // Assert: Every key has its own color.
    Assert.Equal(keys.Length, colors.Values.Distinct().Count());
  }

  /// <summary>
  /// Verifies more keys than palette colors still all get a color (colors repeat).
  /// </summary>
  [Fact]
  public void AssignColors_BeyondPaletteSize_StillColorsEveryKey()
  {
    // Arrange: More keys than palette colors.
    string[] keys = Enumerable.Range(0, GraphVisColors.Palette.Count + 5).Select(i => $"group{i}").ToArray();

    // Act: Assign colors.
    IReadOnlyDictionary<string, UiRgba> colors = GraphVisColors.AssignColors(keys);

    // Assert: Every key is colored from the palette.
    Assert.Equal(keys.Length, colors.Count);
    Assert.All(colors.Values, color => Assert.Contains(color, GraphVisColors.Palette));
  }
}
