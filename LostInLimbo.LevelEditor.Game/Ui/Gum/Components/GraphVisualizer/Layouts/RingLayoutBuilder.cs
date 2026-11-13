using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Starting placement of graph nodes on a ring, in node order, starting at the top and going
/// clockwise. Each node gets a share of the ring matching its size, and the ring is large enough
/// that no two nodes come closer than the link gap. Edges are ignored.
/// </summary>
/// <remarks>Deterministic: the same input always gives the same positions.</remarks>
public sealed class RingLayoutBuilder : GraphLayoutBuilder
{
  /// <summary>How much the ring grows per attempt while neighbouring nodes still overlap.</summary>
  private const float RingGrowthFactor = 1.1f;

  /// <summary>Upper bound of ring growth attempts (1.1^64 is far beyond any real need).</summary>
  private const int MaxRingGrowthSteps = 64;

  /// <summary>
  /// Places the nodes on the ring.
  /// </summary>
  /// <param name="radii">Node radii, in node order (validated).</param>
  /// <param name="edges">Edges; ignored by this layout.</param>
  /// <param name="linkGap">Free space kept between neighbouring nodes (validated).</param>
  /// <returns>Node centers, in node order; the ring is centered on the origin.</returns>
  protected override Vector2[] PlaceCore(IReadOnlyList<float> radii, IReadOnlyList<(int From, int To)> edges, float linkGap)
  {
    int count = radii.Count;
    var positions = new Vector2[count];
    if (count <= 1) return positions; // nothing, or a single node at the origin

    // Each node takes an arc as long as its diameter plus the gap.
    float circumference = 0;
    for (int i = 0; i < count; i++) circumference += 2 * radii[i] + linkGap;

    var angles = new float[count];
    float travelled = 0;
    for (int i = 0; i < count; i++)
    {
      float arc = 2 * radii[i] + linkGap;
      // Middle of the node's arc; -90 degrees puts the first node at the top (screen Y grows down).
      angles[i] = (travelled + arc / 2) / circumference * MathUtil.TwoPi - MathUtil.PiOverTwo;
      travelled += arc;
    }

    // A chord is shorter than its arc, so with few nodes the arc-based ring is too tight; grow it
    // until every pair of ring neighbours has its gap.
    float ringRadius = circumference / MathUtil.TwoPi;
    for (int step = 0; step < MaxRingGrowthSteps; step++)
    {
      WriteRingPositions(positions, angles, ringRadius);
      if (NeighboursHaveGap(positions, radii, linkGap)) break;
      ringRadius *= RingGrowthFactor;
    }

    return positions;
  }

  /// <summary>
  /// Writes ring positions for the given angles.
  /// </summary>
  /// <param name="positions">Positions to fill.</param>
  /// <param name="angles">Angle of each node, in radians.</param>
  /// <param name="ringRadius">Ring radius.</param>
  private static void WriteRingPositions(Vector2[] positions, float[] angles, float ringRadius)
  {
    for (int i = 0; i < positions.Length; i++)
    {
      positions[i] = new Vector2(MathF.Cos(angles[i]), MathF.Sin(angles[i])) * ringRadius;
    }
  }

  /// <summary>
  /// Checks that every pair of ring neighbours is far enough apart.
  /// </summary>
  /// <param name="positions">Node centers.</param>
  /// <param name="radii">Node radii.</param>
  /// <param name="gap">Required free space between neighbours.</param>
  /// <returns>True if all neighbours keep the gap.</returns>
  private static bool NeighboursHaveGap(Vector2[] positions, IReadOnlyList<float> radii, float gap)
  {
    for (int i = 0; i < positions.Length; i++)
    {
      int next = (i + 1) % positions.Length;
      if (Vector2.Distance(positions[i], positions[next]) < radii[i] + radii[next] + gap) return false;
    }

    return true;
  }
}
