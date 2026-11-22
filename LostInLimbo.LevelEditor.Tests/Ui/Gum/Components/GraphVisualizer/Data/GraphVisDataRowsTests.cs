using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer.Data;

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
  /// Verifies the key column is as wide as the minimum, or as the widest key if that is wider.
  /// </summary>
  /// <param name="minimum">Minimum key column width.</param>
  /// <param name="expected">Key column width.</param>
  [Theory]
  [InlineData(100f, 100f)]
  [InlineData(50f, 80f)]
  public void Arrange_MinimumColumn_IsLowerBound(float minimum, float expected)
  {
    // Act: Widest key is 80.
    GraphVisDataRowLayout layout = GraphVisDataRows.Arrange([new(30, 40), new(80, 20)], Width, Gap, minimum);

    // Assert: The larger of minimum and widest key; both pairs still fit.
    Assert.Equal(expected, layout.KeyColumnWidth);
    Assert.Equal([true, true], layout.IsSingleRow);
  }

  /// <summary>
  /// Verifies a value that no longer fits beside the minimum column goes below its key.
  /// </summary>
  [Fact]
  public void Arrange_MinimumColumn_StacksValueThatNoLongerFits()
  {
    // Act: Column 130 + gap 10 leaves 60 for values.
    GraphVisDataRowLayout layout = GraphVisDataRows.Arrange([new(30, 40), new(30, 70)], Width, Gap, 130f);

    // Assert: The wider value is stacked, the column keeps the minimum.
    Assert.Equal(130f, layout.KeyColumnWidth);
    Assert.Equal([true, false], layout.IsSingleRow);
  }

  /// <summary>
  /// Verifies a row too narrow for the minimum column stacks everything.
  /// </summary>
  [Fact]
  public void Arrange_MinimumColumnWiderThanRow_AllStacked()
  {
    // Act: Column 195 + gap 10 is wider than the row.
    GraphVisDataRowLayout layout = GraphVisDataRows.Arrange([new(30, 40)], Width, Gap, 195f);

    // Assert: No column, value below the key.
    Assert.Equal(0f, layout.KeyColumnWidth);
    Assert.Equal([false], layout.IsSingleRow);
  }

  /// <summary>
  /// Verifies invalid arguments are rejected.
  /// </summary>
  [Fact]
  public void Arrange_WithInvalidArguments_Throws()
  {
    // Act + Assert: Null pairs, negative gap, negative or NaN minimum column.
    Assert.Throws<ArgumentNullException>(() => GraphVisDataRows.Arrange(null!, Width, Gap));
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphVisDataRows.Arrange([], Width, -1f));
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphVisDataRows.Arrange([], Width, Gap, -1f));
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphVisDataRows.Arrange([], Width, Gap, float.NaN));
  }
}
