using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Starting placement of graph nodes with a chosen layout algorithm (see
/// <see cref="EnGraphLayoutBuilder"/> and <see cref="GraphLayoutBuilderPicker"/>).
/// </summary>
public static class GraphVisLayout
{
  /// <summary>
  /// Places the nodes of a graph with the given layout.
  /// </summary>
  /// <param name="builder">Layout algorithm.</param>
  /// <param name="radii">Node radii, in node order.</param>
  /// <param name="edges">Edges as pairs of node indices (direction does not matter).</param>
  /// <param name="linkGap">Free space aimed for between the borders of two nodes, in pixels.</param>
  /// <param name="roots">Nodes to start from, for layouts that need a starting point (see <see cref="GraphLayoutBuilder.Place"/>).</param>
  /// <returns>Node centers, in node order.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="radii"/> or <paramref name="edges"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="builder"/> is unknown, an edge or root refers to a missing node, or <paramref name="linkGap"/> is not positive.</exception>
  public static Vector2[] Place(
    EnGraphLayoutBuilder builder, IReadOnlyList<float> radii, IReadOnlyList<(int From, int To)> edges, float linkGap,
    IReadOnlyList<int>? roots = null)
  {
    return GraphLayoutBuilderPicker.For(builder).Place(radii, edges, linkGap, roots);
  }
}
