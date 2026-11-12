using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Headless tests for the engine-free parts of <see cref="GumGraphVisualizer"/>.
/// </summary>
public sealed class GumGraphVisualizerTests
{
  /// <summary>
  /// Verifies the data panel takes its share of the visualizer width, but never less than the
  /// minimum nor more than the maximum (double the minimum).
  /// </summary>
  /// <param name="visualizerWidth">Visualizer width.</param>
  /// <param name="expected">Expected data panel width.</param>
  [Theory]
  [InlineData(0f, 260f)]
  [InlineData(800f, 260f)]
  [InlineData(1040f, 260f)]
  [InlineData(1600f, 400f)]
  [InlineData(2080f, 520f)]
  [InlineData(4000f, 520f)]
  [InlineData(float.NaN, 260f)]
  public void DataPanelWidthFor_IsProportionalBetweenMinimumAndMaximum(float visualizerWidth, float expected)
  {
    // Act: Width for the visualizer width.
    float width = GumGraphVisualizer.DataPanelWidthFor(visualizerWidth);

    // Assert: Share of the width, clamped; maximum is double the minimum.
    Assert.Equal(expected, width);
    Assert.Equal(2 * GumGraphVisualizer.DataPanelMinWidth, GumGraphVisualizer.DataPanelMaxWidth);
  }
}
