using System;
using System.Collections.Generic;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Base of the layout algorithms that compute starting positions of graph nodes. The live
/// <see cref="GraphVisSimulation"/> takes over from these positions.
/// </summary>
/// <remarks>
/// <para>Builders must be deterministic (no randomness): the same input always gives the same
/// positions. They must also be stateless, so one instance can be shared (see
/// <see cref="GraphLayoutBuilderPicker"/>).</para>
/// <para><see cref="Place"/> validates the input once for all builders, so they all reject bad
/// input the same way; <see cref="PlaceCore"/> receives only valid input.</para>
/// </remarks>
public abstract class GraphLayoutBuilder
{
  /// <summary>
  /// Physics this layout is meant to be shown with. The visualizer starts the simulation with a
  /// copy of it whenever this layout is chosen; the user may override parts of it afterwards.
  /// </summary>
  public virtual GraphVisPhysics DefaultPhysics => GraphVisPhysics.Default;

  /// <summary>
  /// Places the nodes of a graph.
  /// </summary>
  /// <param name="radii">Node radii, in node order.</param>
  /// <param name="edges">Edges as pairs of node indices (direction does not matter).</param>
  /// <param name="linkGap">Free space aimed for between the borders of two nodes, in pixels.</param>
  /// <param name="roots">
  /// Nodes that layouts with a starting point begin from (for example the start room); null or
  /// empty lets such layouts choose. Several are allowed, one per connected part is the intended
  /// use; if a part has several, the lowest index is used and the others are placed like ordinary
  /// nodes. Other layouts ignore them.
  /// </param>
  /// <returns>Node centers, in node order.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="radii"/> or <paramref name="edges"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">An edge or root refers to a node index that does not exist, or <paramref name="linkGap"/> is not positive.</exception>
  public Vector2[] Place(IReadOnlyList<float> radii, IReadOnlyList<(int From, int To)> edges, float linkGap, IReadOnlyList<int>? roots = null)
  {
    ArgumentNullException.ThrowIfNull(radii);
    ArgumentNullException.ThrowIfNull(edges);
    if (!(linkGap > 0)) throw new ArgumentOutOfRangeException(nameof(linkGap), linkGap, "Link gap must be positive.");

    int count = radii.Count;
    foreach ((int from, int to) in edges)
    {
      if (from < 0 || from >= count) throw new ArgumentOutOfRangeException(nameof(edges), from, "Edge refers to a missing node.");
      if (to < 0 || to >= count) throw new ArgumentOutOfRangeException(nameof(edges), to, "Edge refers to a missing node.");
    }

    roots ??= [];
    foreach (int root in roots)
    {
      if (root < 0 || root >= count) throw new ArgumentOutOfRangeException(nameof(roots), root, "Root refers to a missing node.");
    }

    return PlaceCore(radii, edges, linkGap, roots);
  }

  /// <summary>
  /// Places the nodes of a graph; the input is already validated by <see cref="Place"/>.
  /// </summary>
  /// <param name="radii">Node radii, in node order.</param>
  /// <param name="edges">Edges as pairs of existing node indices (direction does not matter).</param>
  /// <param name="linkGap">Free space aimed for between the borders of two nodes; positive.</param>
  /// <param name="roots">Existing node indices to start from (possibly empty); see <see cref="Place"/>.</param>
  /// <returns>Node centers, in node order.</returns>
  protected abstract Vector2[] PlaceCore(
    IReadOnlyList<float> radii, IReadOnlyList<(int From, int To)> edges, float linkGap, IReadOnlyList<int> roots);
}
