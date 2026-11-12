using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Live physics of the graph visualizer: nodes that are too close push each other away, connected
/// nodes that are too far apart pull each other closer, and circles never overlap. Heavier
/// (larger) nodes move less than lighter ones.
/// </summary>
/// <remarks>
/// <para>
/// Between "too close" and "too far" there is no force, so a good starting layout keeps its
/// shape and the simulation falls asleep quickly; it costs nothing while asleep.
/// </para>
/// <para>Pure and deterministic: no randomness, fixed time steps.</para>
/// </remarks>
public sealed class GraphVisSimulation
{
  /// <summary>Look and physics constants.</summary>
  private readonly GraphVisStyle _style;

  /// <summary>Node centers.</summary>
  private readonly Vector2[] _positions;

  /// <summary>Node centers at the start of the current step, to measure how far nodes moved.</summary>
  private readonly Vector2[] _previous;

  /// <summary>Node radii.</summary>
  private readonly float[] _radii;

  /// <summary>Node masses (circle areas up to a constant factor).</summary>
  private readonly float[] _masses;

  /// <summary>Edges as pairs of node indices.</summary>
  private readonly (int From, int To)[] _edges;

  /// <summary>Simulated time not yet turned into steps, in seconds.</summary>
  private float _pendingSeconds;

  /// <summary>
  /// Creates the simulation, awake.
  /// </summary>
  /// <param name="positions">Starting node centers, in node order (copied).</param>
  /// <param name="radii">Node radii, in node order.</param>
  /// <param name="edges">Edges as pairs of node indices.</param>
  /// <param name="style">Physics constants.</param>
  /// <exception cref="ArgumentNullException">An argument is null.</exception>
  /// <exception cref="ArgumentException"><paramref name="positions"/> and <paramref name="radii"/> differ in length.</exception>
  /// <exception cref="ArgumentOutOfRangeException">An edge refers to a node index that does not exist.</exception>
  public GraphVisSimulation(
    IReadOnlyList<Vector2> positions, IReadOnlyList<float> radii, IReadOnlyList<(int From, int To)> edges, GraphVisStyle style)
  {
    ArgumentNullException.ThrowIfNull(positions);
    ArgumentNullException.ThrowIfNull(radii);
    ArgumentNullException.ThrowIfNull(edges);
    _style = style ?? throw new ArgumentNullException(nameof(style));
    if (positions.Count != radii.Count)
      throw new ArgumentException("Positions and radii must have the same length.", nameof(radii));

    _positions = [.. positions];
    _previous = new Vector2[_positions.Length];
    _radii = [.. radii];
    _masses = _radii.Select(radius => radius * radius).ToArray();

    foreach ((int from, int to) in edges)
    {
      if (from < 0 || from >= _positions.Length) throw new ArgumentOutOfRangeException(nameof(edges), from, "Edge refers to a missing node.");
      if (to < 0 || to >= _positions.Length) throw new ArgumentOutOfRangeException(nameof(edges), to, "Edge refers to a missing node.");
    }

    _edges = [.. edges.Where(edge => edge.From != edge.To)];
  }

  /// <summary>Current node centers, in node order.</summary>
  public IReadOnlyList<Vector2> Positions => _positions;

  /// <summary>Whether nothing is moving any more; <see cref="Step"/> does nothing until <see cref="Wake"/>.</summary>
  public bool IsAsleep { get; private set; }

  /// <summary>
  /// Index of a node that physics must not move (the node being dragged), or null. Its
  /// neighbours take the whole displacement, as if it were infinitely heavy.
  /// </summary>
  public int? PinnedNode { get; set; }

  /// <summary>
  /// Makes the simulation run again.
  /// </summary>
  public void Wake()
  {
    IsAsleep = false;
  }

  /// <summary>
  /// Moves a node to a position (for dragging) and wakes the simulation.
  /// </summary>
  /// <param name="node">Node index.</param>
  /// <param name="position">New center.</param>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node index.</exception>
  public void MoveNode(int node, Vector2 position)
  {
    if (node < 0 || node >= _positions.Length) throw new ArgumentOutOfRangeException(nameof(node), node, "Not a node index.");
    _positions[node] = position;
    Wake();
  }

  /// <summary>
  /// Advances the simulation by elapsed time, in fixed steps, so the result does not depend on
  /// the frame rate. Does nothing while asleep.
  /// </summary>
  /// <param name="deltaSeconds">Elapsed time since the last call, in seconds.</param>
  public void Step(float deltaSeconds)
  {
    if (IsAsleep || !(deltaSeconds > 0)) return;

    _pendingSeconds += deltaSeconds;
    for (int step = 0; step < _style.SimulationMaxStepsPerUpdate && _pendingSeconds >= _style.SimulationStepSeconds; step++)
    {
      _pendingSeconds -= _style.SimulationStepSeconds;
      StepOnce();
      if (IsAsleep) break;
    }

    // After a long pause (or when asleep) do not try to catch up later.
    if (IsAsleep || _pendingSeconds > _style.SimulationStepSeconds) _pendingSeconds = 0;
  }

  /// <summary>
  /// Runs steps until the simulation falls asleep or a step limit is reached. Used to settle a
  /// new graph before it is first shown.
  /// </summary>
  /// <param name="maxSteps">Most steps to run.</param>
  /// <returns>True if the simulation is asleep afterwards.</returns>
  public bool Settle(int maxSteps)
  {
    for (int step = 0; step < maxSteps && !IsAsleep; step++) StepOnce();
    return IsAsleep;
  }

  /// <summary>
  /// Runs one fixed step: soft forces, then hard overlap removal, then the sleep check.
  /// </summary>
  private void StepOnce()
  {
    Array.Copy(_positions, _previous, _positions.Length);

    PushApart(_style.NodeGap, _style.SimulationRepelStrength);
    PullTogether();

    // Hard rule: whatever the forces did, circles must not overlap.
    for (int pass = 0; pass < _style.SimulationOverlapPasses; pass++)
    {
      if (!PushApart(0f, 1f)) break;
    }

    float largestMove = 0;
    for (int i = 0; i < _positions.Length; i++)
    {
      largestMove = Math.Max(largestMove, Vector2.Distance(_positions[i], _previous[i]));
    }

    if (largestMove < _style.SimulationSleepDistance) IsAsleep = true;
  }

  /// <summary>
  /// Pushes apart every pair of nodes whose borders are closer than a gap.
  /// </summary>
  /// <param name="gap">Free space required between two node borders.</param>
  /// <param name="strength">Share of the missing distance corrected in this call (0..1).</param>
  /// <returns>True if any pair was too close.</returns>
  private bool PushApart(float gap, float strength)
  {
    bool any = false;
    for (int i = 0; i < _positions.Length; i++)
    {
      for (int j = i + 1; j < _positions.Length; j++)
      {
        float wanted = _radii[i] + _radii[j] + gap;
        Vector2 offset = _positions[j] - _positions[i];
        float distance = offset.Length();
        if (distance >= wanted) continue;

        any = true;
        // Nodes on the same spot have no direction to part in; pick a fixed one.
        Vector2 direction = distance > 0 ? offset / distance : Vector2.UnitX;
        Separate(i, j, direction * ((wanted - distance) * strength));
      }
    }

    return any;
  }

  /// <summary>
  /// Pulls together connected nodes that are farther apart than the rest length.
  /// </summary>
  private void PullTogether()
  {
    foreach ((int from, int to) in _edges)
    {
      float rest = _radii[from] + _radii[to] + _style.NodeGap * _style.LinkRestGapFactor;
      Vector2 offset = _positions[to] - _positions[from];
      float distance = offset.Length();
      if (distance <= rest) continue;

      // Negative separation: the two nodes approach each other.
      Separate(from, to, offset / distance * (-(distance - rest) * _style.SimulationAttractStrength));
    }
  }

  /// <summary>
  /// Changes the distance between two nodes, sharing the movement inversely to mass: the lighter
  /// node moves more. A pinned node does not move at all.
  /// </summary>
  /// <param name="first">First node.</param>
  /// <param name="second">Second node.</param>
  /// <param name="separation">Total change of the vector from the first to the second node.</param>
  private void Separate(int first, int second, Vector2 separation)
  {
    float firstShare;
    if (PinnedNode == first) firstShare = 0f;
    else if (PinnedNode == second) firstShare = 1f;
    else firstShare = _masses[second] / (_masses[first] + _masses[second]);

    _positions[first] -= separation * firstShare;
    _positions[second] += separation * (1f - firstShare);
  }
}
