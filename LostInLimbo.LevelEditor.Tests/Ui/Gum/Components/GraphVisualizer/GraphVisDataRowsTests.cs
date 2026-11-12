using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Headless tests for <see cref="GraphVisDataRows"/> (which data panel pairs share one row).
/// </summary>
public sealed class GraphVisDataRowsTests
{
  /// <summary>Row width used by the tests.</summary>
  private const float Width = 200f;

  /// <summary>Column gap used by the tests.</summary>
  private const float Gap = 10f;

  /// <summary>
  /// Verifies short pairs share rows with the key column as wide as their widest key.
  /// </summary>
  [Fact]
  public void Arrange_ShortPairs_AllSingleRowWithWidestKeyColumn()
  {
    // Act: Three short pairs.
    GraphVisDataRowLayout layout = GraphVisDataRows.Arrange([new(30, 40), new(80, 20), new(50, 60)], Width, Gap);

    // Assert: All on one row each, column fits the widest key.
    Assert.Equal(80f, layout.KeyColumnWidth);
    Assert.Equal([true, true, true], layout.IsSingleRow);
  }

  /// <summary>
  /// Verifies a long value goes below its key while the others stay single-row.
  /// </summary>
  [Fact]
  public void Arrange_LongValue_GoesBelowKey()
  {
    // Act: Middle pair's value is wider than the row.
    GraphVisDataRowLayout layout = GraphVisDataRows.Arrange([new(30, 40), new(30, 250), new(40, 60)], Width, Gap);

    // Assert: Only the middle pair is stacked.
    Assert.Equal(40f, layout.KeyColumnWidth);
    Assert.Equal([true, false, true], layout.IsSingleRow);
  }

  /// <summary>
  /// Verifies one long key does not widen the column when that would push other values off the row.
  /// </summary>
  [Fact]
  public void Arrange_LongKey_StacksItRatherThanLosingOthers()
  {
    // Arrange: Column 150 would fit only the long-key pair (value 30); column 40 fits the other two.
    GraphVisDataPairSize[] pairs = [new(40, 120), new(150, 30), new(35, 140)];

    // Act: Lay out.
    GraphVisDataRowLayout layout = GraphVisDataRows.Arrange(pairs, Width, Gap);

    // Assert: The narrow column wins; the long key is stacked.
    Assert.Equal(40f, layout.KeyColumnWidth);
    Assert.Equal([true, false, true], layout.IsSingleRow);
  }

  /// <summary>
  /// Verifies that with equally many single rows the narrower column is chosen.
  /// </summary>
  [Fact]
  public void Arrange_Tie_PrefersNarrowerColumn()
  {
    // Act: Column 60 fits only the first pair, column 20 only the second (its value is long).
    GraphVisDataRowLayout layout = GraphVisDataRows.Arrange([new(60, 50), new(20, 150)], Width, Gap);

    // Assert: Column 20.
    Assert.Equal(20f, layout.KeyColumnWidth);
    Assert.Equal([false, true], layout.IsSingleRow);
  }

  /// <summary>
  /// Verifies a pair that exactly fills the row is single-row, and one pixel more is not.
  /// </summary>
  /// <param name="valueWidth">Value width.</param>
  /// <param name="expected">Whether single-row.</param>
  [Theory]
  [InlineData(140f, true)]
  [InlineData(141f, false)]
  public void Arrange_ExactFit_IsSingleRow(float valueWidth, bool expected)
  {
    // Act: Key 50 + gap 10 + value.
    GraphVisDataRowLayout layout = GraphVisDataRows.Arrange([new(50, valueWidth)], Width, Gap);

    // Assert: Fits up to the row width.
    Assert.Equal(expected, layout.IsSingleRow[0]);
  }

  /// <summary>
  /// Verifies nothing fitting and no pairs give an empty column.
  /// </summary>
  [Fact]
  public void Arrange_NothingFits_AllStacked()
  {
    // Act + Assert: Every value too long.
    GraphVisDataRowLayout layout = GraphVisDataRows.Arrange([new(30, 300), new(40, 400)], Width, Gap);
    Assert.Equal(0f, layout.KeyColumnWidth);
    Assert.Equal([false, false], layout.IsSingleRow);

    // Act + Assert: No pairs.
    GraphVisDataRowLayout empty = GraphVisDataRows.Arrange([], Width, Gap);
    Assert.Empty(empty.IsSingleRow);
  }

  /// <summary>
  /// Verifies invalid arguments are rejected.
  /// </summary>
  [Fact]
  public void Arrange_WithInvalidArguments_Throws()
  {
    // Act + Assert: Null pairs, negative gap.
    Assert.Throws<ArgumentNullException>(() => GraphVisDataRows.Arrange(null!, Width, Gap));
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphVisDataRows.Arrange([], Width, -1f));
  }
}
