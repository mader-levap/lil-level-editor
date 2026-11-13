using System;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Render;

/// <summary>
/// Highlight level of every node and edge, easing towards 1 while the element is hovered or
/// selected and back to 0 otherwise, so the highlight never switches on or off instantly.
/// </summary>
/// <remarks>
/// Levels move linearly over <see cref="FadeSeconds"/>; the values handed out are eased
/// (smoothstep), so the change starts and ends softly.
/// </remarks>
public sealed class GraphVisHighlightFade
{
  /// <summary>Linear level of each node (0..1).</summary>
  private float[] _nodeLevels = [];

  /// <summary>Linear level of each edge (0..1).</summary>
  private float[] _edgeLevels = [];

  /// <summary>
  /// Creates the fade with no elements.
  /// </summary>
  /// <param name="fadeSeconds">Time a full fade in or out takes, in seconds.</param>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="fadeSeconds"/> is not positive.</exception>
  public GraphVisHighlightFade(float fadeSeconds)
  {
    if (!(fadeSeconds > 0)) throw new ArgumentOutOfRangeException(nameof(fadeSeconds), fadeSeconds, "Fade time must be positive.");
    FadeSeconds = fadeSeconds;
  }

  /// <summary>Time a full fade in or out takes, in seconds.</summary>
  public float FadeSeconds { get; }

  /// <summary>
  /// Starts over for a graph: all levels 0.
  /// </summary>
  /// <param name="nodeCount">Number of nodes.</param>
  /// <param name="edgeCount">Number of edges.</param>
  /// <exception cref="ArgumentOutOfRangeException">A count is negative.</exception>
  public void Reset(int nodeCount, int edgeCount)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(nodeCount);
    ArgumentOutOfRangeException.ThrowIfNegative(edgeCount);
    _nodeLevels = new float[nodeCount];
    _edgeLevels = new float[edgeCount];
  }

  /// <summary>
  /// Advances the levels by elapsed time: the hovered and the selected element towards 1, all
  /// others towards 0.
  /// </summary>
  /// <param name="deltaSeconds">Elapsed time, in seconds.</param>
  /// <param name="hover">Element under the pointer.</param>
  /// <param name="selection">Selected element.</param>
  /// <returns>True if any level changed (a redraw shows something new).</returns>
  public bool Step(float deltaSeconds, GraphVisHit hover, GraphVisHit selection)
  {
    if (!(deltaSeconds > 0)) return false;

    float change = deltaSeconds / FadeSeconds;
    bool changed = StepLevels(_nodeLevels, EnGraphVisHitKind.Node, change, hover, selection);
    changed |= StepLevels(_edgeLevels, EnGraphVisHitKind.Edge, change, hover, selection);
    return changed;
  }

  /// <summary>
  /// Eased highlight level of a node.
  /// </summary>
  /// <param name="index">Node index.</param>
  /// <returns>Level 0..1; 0 for an unknown index.</returns>
  public float NodeLevel(int index) => Eased(_nodeLevels, index);

  /// <summary>
  /// Eased highlight level of an edge.
  /// </summary>
  /// <param name="index">Edge index.</param>
  /// <returns>Level 0..1; 0 for an unknown index.</returns>
  public float EdgeLevel(int index) => Eased(_edgeLevels, index);

  /// <summary>
  /// Moves the levels of one kind of element.
  /// </summary>
  /// <param name="levels">Levels.</param>
  /// <param name="kind">Element kind of the levels.</param>
  /// <param name="change">Largest change of a level.</param>
  /// <param name="hover">Element under the pointer.</param>
  /// <param name="selection">Selected element.</param>
  /// <returns>True if any level changed.</returns>
  private static bool StepLevels(float[] levels, EnGraphVisHitKind kind, float change, GraphVisHit hover, GraphVisHit selection)
  {
    bool changed = false;
    for (int i = 0; i < levels.Length; i++)
    {
      bool isLit = (hover.Kind == kind && hover.Index == i) || (selection.Kind == kind && selection.Index == i);
      float target = isLit ? 1f : 0f;
      float level = levels[i];
      if (level == target) continue;

      levels[i] = isLit ? Math.Min(target, level + change) : Math.Max(target, level - change);
      changed = true;
    }

    return changed;
  }

  /// <summary>
  /// Reads a level with smoothstep easing.
  /// </summary>
  /// <param name="levels">Levels.</param>
  /// <param name="index">Index.</param>
  /// <returns>Eased level; 0 for an unknown index.</returns>
  private static float Eased(float[] levels, int index)
  {
    if ((uint)index >= (uint)levels.Length) return 0;
    float level = levels[index];
    return level * level * (3f - 2f * level);
  }
}
