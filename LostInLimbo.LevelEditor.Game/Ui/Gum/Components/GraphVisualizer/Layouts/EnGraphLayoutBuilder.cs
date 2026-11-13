namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Known graph layout algorithms. The first value is the default. New values show up in the
/// visualizer's layout picker automatically; map them in <see cref="GraphLayoutBuilderPicker.For"/>.
/// </summary>
public enum EnGraphLayoutBuilder
{
  /// <summary>Stress layout (<see cref="StressLayoutBuilder"/>): screen distances follow graph distances.</summary>
  Stress = 0,

  /// <summary>Ring layout (<see cref="RingLayoutBuilder"/>): nodes on a circle in node order.</summary>
  Ring,
}
