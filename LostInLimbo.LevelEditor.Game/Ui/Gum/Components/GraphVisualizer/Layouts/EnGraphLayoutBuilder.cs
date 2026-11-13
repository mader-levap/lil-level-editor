namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Known graph layout algorithms. The first value is the default. New values show up in the
/// visualizer's layout picker automatically; map them in <see cref="GraphLayoutBuilderPicker.For"/>.
/// </summary>
public enum EnGraphLayoutBuilder
{
  /// <summary>Stress layout (<see cref="StressLayoutBuilder"/>): screen distances follow graph distances.</summary>
  Stress = 0,

  /// <summary>Layered layout (<see cref="LayeredLayoutBuilder"/>): layers by distance from the root, as rows top to bottom or, for deep parts, columns left to right.</summary>
  Layered,

  /// <summary>Ring layout (<see cref="RingLayoutBuilder"/>): nodes on a circle, connected nodes next to each other.</summary>
  Ring,
}
