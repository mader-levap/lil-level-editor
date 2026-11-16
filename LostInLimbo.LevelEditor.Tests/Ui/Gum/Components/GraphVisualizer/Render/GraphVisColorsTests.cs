using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Render;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer.Render;

/// <summary>
/// Headless tests for <see cref="GraphVisColors"/> (darkening and distinct key colors).
/// </summary>
public sealed class GraphVisColorsTests
{
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
  /// Verifies brightening mixes the color with white, keeps alpha, and covers the range ends.
  /// </summary>
  [Fact]
  public void Brighten_MixesWithWhiteAndKeepsAlpha()
  {
    // Arrange: A translucent color.
    var color = new UiRgba(0, 100, 255, 128);

    // Act + Assert: Half way to white; 0 keeps the color, 1 gives white with the same alpha.
    Assert.Equal(new UiRgba(128, 178, 255, 128), GraphVisColors.Brighten(color, 0.5f));
    Assert.Equal(color, GraphVisColors.Brighten(color, 0f));
    Assert.Equal(new UiRgba(255, 255, 255, 128), GraphVisColors.Brighten(color, 1f));
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphVisColors.Brighten(color, 1.1f));
  }

  /// <summary>
  /// Verifies text is dark on bright backgrounds and light on dark ones; saturated blue counts
  /// as dark and saturated yellow as bright, although both have a channel at full value.
  /// </summary>
  /// <param name="red">Background red.</param>
  /// <param name="green">Background green.</param>
  /// <param name="blue">Background blue.</param>
  /// <param name="expectDark">Whether dark text is expected.</param>
  [Theory]
  [InlineData(255, 255, 255, true)]
  [InlineData(211, 211, 211, true)]
  [InlineData(255, 255, 0, true)]
  [InlineData(0, 0, 0, false)]
  [InlineData(32, 96, 223, false)]
  [InlineData(0, 0, 255, false)]
  public void ReadableOn_PicksDarkOrLightByBackgroundBrightness(byte red, byte green, byte blue, bool expectDark)
  {
    // Act: Pick a text color for the background.
    UiRgba picked = GraphVisColors.ReadableOn(new UiRgba(red, green, blue), UiRgba.Black, UiRgba.White);

    // Assert: Dark text on bright backgrounds, light text on dark ones.
    Assert.Equal(expectDark ? UiRgba.Black : UiRgba.White, picked);
  }

  /// <summary>Tolerance for floating-point comparisons.</summary>
  private const float Tolerance = 0.001f;

  /// <summary>Perceptual difference from which colors look very distinct.</summary>
  private const float VeryDistinct = 20f;

  /// <summary>Perceptual difference from which colors still look distinct.</summary>
  private const float Distinct = 15f;

  /// <summary>
  /// Smallest perceptual difference a palette color must keep from the colors that already have
  /// a meaning in the view (background, ungrouped gray, selection highlight).
  /// </summary>
  private const float ClearOfReservedColors = 12f;

  /// <summary>Number of leading palette colors that must be very distinct from each other.</summary>
  private const int VeryDistinctPrefix = 7;

  /// <summary>
  /// Asserts every pair among the first colors of the palette differs by at least a minimum.
  /// </summary>
  /// <param name="count">Number of leading palette colors to check.</param>
  /// <param name="minimum">Smallest allowed perceptual difference.</param>
  private static void AssertPairsDiffer(int count, float minimum)
  {
    for (int i = 0; i < count; i++)
    {
      for (int j = i + 1; j < count; j++)
      {
        float difference = GraphVisColors.PerceptualDifference(GraphVisColors.Palette[i], GraphVisColors.Palette[j]);
        Assert.True(difference >= minimum, $"Slots {i} and {j} differ by only {difference:0.0}.");
      }
    }
  }

  /// <summary>
  /// Verifies the perceptual difference on known values: 0 for equal colors, 100 between black
  /// and white, the same in both directions, and alpha ignored.
  /// </summary>
  [Fact]
  public void PerceptualDifference_KnownValues()
  {
    // Arrange: Two different colors.
    var red = new UiRgba(255, 0, 0);
    var blue = new UiRgba(0, 0, 255);

    // Assert: Known values, symmetry, alpha ignored.
    Assert.Equal(0f, GraphVisColors.PerceptualDifference(red, red), Tolerance);
    Assert.Equal(100f, GraphVisColors.PerceptualDifference(UiRgba.Black, UiRgba.White), 0.01f);
    Assert.Equal(GraphVisColors.PerceptualDifference(red, blue), GraphVisColors.PerceptualDifference(blue, red), Tolerance);
    Assert.Equal(0f, GraphVisColors.PerceptualDifference(red, new UiRgba(255, 0, 0, 10)), Tolerance);
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
  /// Verifies the first palette colors all look very different from each other, so a graph with
  /// a usual number of groups never shows look-alike colors.
  /// </summary>
  [Fact]
  public void Palette_FirstSevenAreVeryDistinct()
  {
    // Assert: Every pair among the leading colors is very distinct.
    AssertPairsDiffer(VeryDistinctPrefix, VeryDistinct);
  }

  /// <summary>
  /// Verifies all palette colors still look different from each other.
  /// </summary>
  [Fact]
  public void Palette_AllColorsAreDistinct()
  {
    // Assert: Every pair of the whole palette is distinct.
    AssertPairsDiffer(GraphVisColors.Palette.Count, Distinct);
  }

  /// <summary>
  /// Verifies no palette color resembles the background, the color of ungrouped nodes or the
  /// selection highlight.
  /// </summary>
  [Fact]
  public void Palette_StaysClearOfBackgroundGrayAndHighlight()
  {
    // Arrange: Colors that already mean something in the view.
    GraphVisStyle style = GraphVisStyle.Default;
    UiRgba[] reserved = [style.BackgroundColor, style.DefaultNodeColor, style.HighlightColor];

    // Assert: Every palette color keeps its distance from each of them.
    for (int i = 0; i < GraphVisColors.Palette.Count; i++)
    {
      foreach (UiRgba color in reserved)
      {
        float difference = GraphVisColors.PerceptualDifference(GraphVisColors.Palette[i], color);
        Assert.True(difference >= ClearOfReservedColors, $"Slot {i} is only {difference:0.0} from {color}.");
      }
    }
  }

  /// <summary>
  /// Verifies keys take palette slots in ordinal order, independent of input order, and
  /// duplicates collapse.
  /// </summary>
  [Fact]
  public void AssignColors_TakesSlotsInOrdinalOrder()
  {
    // Act: Assign colors to the generator's group names, shuffled and with a duplicate.
    IReadOnlyDictionary<string, UiRgba> colors = GraphVisColors.AssignColors(["g3", "g1", "g2", "g1"]);

    // Assert: g1, g2, g3 get slots 0, 1, 2.
    Assert.Equal(3, colors.Count);
    Assert.Equal(GraphVisColors.Palette[0], colors["g1"]);
    Assert.Equal(GraphVisColors.Palette[1], colors["g2"]);
    Assert.Equal(GraphVisColors.Palette[2], colors["g3"]);
  }

  /// <summary>
  /// Verifies a key gets the same color across calls when the set of keys is the same.
  /// </summary>
  [Fact]
  public void AssignColors_SameKeys_GetSameColorsAcrossCalls()
  {
    // Act: Assign the same keys twice, in different input orders.
    IReadOnlyDictionary<string, UiRgba> forward = GraphVisColors.AssignColors(["castle", "lobby"]);
    IReadOnlyDictionary<string, UiRgba> backward = GraphVisColors.AssignColors(["lobby", "castle"]);

    // Assert: Same colors both times.
    Assert.Equal(forward["castle"], backward["castle"]);
    Assert.Equal(forward["lobby"], backward["lobby"]);
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
