using System.Collections.Generic;
using System.Globalization;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Ui.Gum.LevelGen;
using LostInLimbo.LevelGen.Data.Result.Stats;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Ui.Gum.Info;

/// <summary>
/// One label/value row of the level information card.
/// </summary>
/// <param name="Label">Row label, for example <c>"Status:"</c>.</param>
/// <param name="Value">Row value.</param>
public sealed record LevelInfoRow(string Label, string Value);

/// <summary>
/// The facts about a generated world that the information card shows. Extracted from the
/// library's world by the panel, so the formatting stays testable without building a world.
/// </summary>
/// <param name="Phase">Generation phase the world is in.</param>
/// <param name="RunInputs">
/// Properties and generated configuration the run was started with, source of the run settings
/// the card shows (for now the text seed); null if unknown (for example after a clear).
/// </param>
/// <param name="NumericSeed">Numeric seed the library actually used (reproduces the run).</param>
/// <param name="Nodes">Graph node count.</param>
/// <param name="Edges">Graph edge count.</param>
/// <param name="Steps">Map generation steps.</param>
public sealed record LevelWorldFacts(EnPhase Phase, LevelGenRunInputs? RunInputs, long NumericSeed, int Nodes, int Edges, long Steps);

/// <summary>
/// Builds the rows of the level information card.
/// </summary>
public static class LevelInfoSummary
{
  /// <summary>Value shown when nothing was generated yet.</summary>
  public const string NoLevelYet = "No level generated yet.";

  /// <summary>Value shown while a run is in progress.</summary>
  public const string InProgress = "Generation in progress.";

  /// <summary>Value shown when a run ended without a world (cancelled or failed).</summary>
  public const string NoLevelAvailable = "No level available.";

  /// <summary>
  /// Builds the rows for the current generation state.
  /// </summary>
  /// <param name="result">Current generation result.</param>
  /// <param name="failureReason">Failure description, if any; shown as an extra row when not blank.</param>
  /// <param name="world">Facts about the available world, or null if no world is available.</param>
  /// <returns>Rows in display order.</returns>
  public static IReadOnlyList<LevelInfoRow> Build(EnGenResult result, string? failureReason, LevelWorldFacts? world)
  {
    var rows = new List<LevelInfoRow>
    {
      new("Status:", result.ToString()),
    };

    if (!string.IsNullOrWhiteSpace(failureReason))
    {
      rows.Add(new("Reason:", failureReason));
    }

    if (world == null)
    {
      rows.Add(new("Level:", NoWorldText(result)));
      return rows;
    }

    rows.Add(new("Phase:", world.Phase.ToString()));
    rows.Add(new("Text seed:", LevelGenSeedFormat.TextSeed(world.RunInputs?.Props.TextSeed)));
    rows.Add(new("Numeric seed:", world.NumericSeed.ToString(CultureInfo.InvariantCulture)));
    rows.Add(new("Graph nodes:", world.Nodes.ToString()));
    rows.Add(new("Graph edges:", world.Edges.ToString()));
    rows.Add(new("Map steps:", world.Steps.ToString()));
    return rows;
  }

  /// <summary>
  /// Explains why no world is shown.
  /// </summary>
  /// <param name="result">Current generation result.</param>
  /// <returns>Explanation text.</returns>
  private static string NoWorldText(EnGenResult result) => result switch
  {
    EnGenResult.None => NoLevelYet,
    EnGenResult.Running => InProgress,
    _ => NoLevelAvailable,
  };
}
