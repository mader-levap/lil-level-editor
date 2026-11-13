using System;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Parameters of the node physics (<see cref="GraphVisSimulation"/>) and the spacing layouts aim
/// for. Lengths are in world units, which equal screen pixels at zoom 1.
/// </summary>
/// <remarks>
/// Immutable: layout builders offer their own default version (see
/// <c>GraphLayoutBuilder.DefaultPhysics</c>) and callers derive overrides with <c>with</c>.
/// </remarks>
public sealed record GraphVisPhysics
{
  /// <summary>Physics used unless a layout builder or caller supplies another.</summary>
  public static GraphVisPhysics Default { get; } = new();

  /// <summary>Free space any two node borders must keep; closer nodes push each other away.</summary>
  public float NodeGap { get; init; } = 30f;

  /// <summary>
  /// Free space the starting layout aims for between two connected nodes, as a multiple of
  /// <see cref="NodeGap"/>. Lies between "too close" (1) and "too far" (<see cref="LinkRestGapFactor"/>).
  /// </summary>
  public float LinkIdealGapFactor { get; init; } = 2f;

  /// <summary>
  /// Free space between two connected nodes beyond which they pull each other closer, as a
  /// multiple of <see cref="NodeGap"/>.
  /// </summary>
  public float LinkRestGapFactor { get; init; } = 3f;

  /// <summary>Share of a "too close" distance corrected per simulation step (0..1).</summary>
  public float RepelStrength { get; init; } = 0.3f;

  /// <summary>Share of a "too far" distance corrected per simulation step (0..1); 0 turns edge pull off.</summary>
  public float AttractStrength { get; init; } = 0.1f;

  /// <summary>Length of one simulation step, in seconds.</summary>
  public float StepSeconds { get; init; } = 1f / 60f;

  /// <summary>Most simulation steps run per update, so a slow frame cannot snowball.</summary>
  public int MaxStepsPerUpdate { get; init; } = 4;

  /// <summary>Most overlap-removal passes per simulation step.</summary>
  public int OverlapPasses { get; init; } = 4;

  /// <summary>The simulation falls asleep once no node moved farther than this in a step.</summary>
  public float SleepDistance { get; init; } = 0.05f;

  /// <summary>Most simulation steps run on a new graph before it is first shown.</summary>
  public int SettleSteps { get; init; } = 600;

  /// <summary>Free space starting layouts aim for between two connected nodes (<see cref="NodeGap"/> × <see cref="LinkIdealGapFactor"/>).</summary>
  public float LinkIdealGap => NodeGap * LinkIdealGapFactor;

  /// <summary>
  /// Checks that the values can drive a simulation.
  /// </summary>
  /// <exception cref="ArgumentOutOfRangeException">A strength is outside 0..1, a gap or factor is negative, the step length is not positive, or a count is negative.</exception>
  public void Validate()
  {
    if (!(NodeGap >= 0)) throw new ArgumentOutOfRangeException(nameof(NodeGap), NodeGap, "Node gap must not be negative.");
    if (!(LinkIdealGapFactor >= 0)) throw new ArgumentOutOfRangeException(nameof(LinkIdealGapFactor), LinkIdealGapFactor, "Factor must not be negative.");
    if (!(LinkRestGapFactor >= 0)) throw new ArgumentOutOfRangeException(nameof(LinkRestGapFactor), LinkRestGapFactor, "Factor must not be negative.");
    if (!(RepelStrength is >= 0 and <= 1)) throw new ArgumentOutOfRangeException(nameof(RepelStrength), RepelStrength, "Strength must be between 0 and 1.");
    if (!(AttractStrength is >= 0 and <= 1)) throw new ArgumentOutOfRangeException(nameof(AttractStrength), AttractStrength, "Strength must be between 0 and 1.");
    if (!(StepSeconds > 0)) throw new ArgumentOutOfRangeException(nameof(StepSeconds), StepSeconds, "Step length must be positive.");
    if (MaxStepsPerUpdate < 0) throw new ArgumentOutOfRangeException(nameof(MaxStepsPerUpdate), MaxStepsPerUpdate, "Count must not be negative.");
    if (OverlapPasses < 0) throw new ArgumentOutOfRangeException(nameof(OverlapPasses), OverlapPasses, "Count must not be negative.");
    if (!(SleepDistance >= 0)) throw new ArgumentOutOfRangeException(nameof(SleepDistance), SleepDistance, "Distance must not be negative.");
    if (SettleSteps < 0) throw new ArgumentOutOfRangeException(nameof(SettleSteps), SettleSteps, "Count must not be negative.");
  }
}
