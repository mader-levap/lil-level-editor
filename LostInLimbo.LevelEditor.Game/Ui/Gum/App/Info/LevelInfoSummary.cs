using System.Collections.Generic;
using System.Globalization;
using LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Helpers;
using LostInLimbo.LevelGen.Data.Config;
using LostInLimbo.LevelGen.Data.Prop;
using LostInLimbo.LevelGen.Data.Result.Stats;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.Info;

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
/// <param name="NumericSeed">Numeric seed the library actually used (reproduces the run).</param>
/// <param name="Steps">Map generation steps.</param>
public sealed record LevelWorldFacts(EnPhase Phase, long NumericSeed, long Steps);

/// <summary>
/// Builds the rows of the level information card's three columns: generation status, the
/// properties the run was started with, and selected fields of the configuration it used.
/// </summary>
public static class LevelInfoSummary
{
  /// <summary>Title of the status column.</summary>
  public const string StatusTitle = "Level information";

  /// <summary>Title of the properties column.</summary>
  public const string PropertiesTitle = "Properties";

  /// <summary>Title of the configuration column.</summary>
  public const string ConfigurationTitle = "Configuration";

  /// <summary>Value shown when nothing was generated yet.</summary>
  public const string NoLevelYet = "No level generated yet.";

  /// <summary>Value shown while a run is in progress.</summary>
  public const string InProgress = "Generation in progress.";

  /// <summary>Value shown when a run ended without a world (cancelled or failed).</summary>
  public const string NoLevelAvailable = "No level available.";

  /// <summary>Text shown centered in a column that has no rows (no run inputs known).</summary>
  public const string NotAvailable = "Not available.";

  /// <summary>Text for a true flag.</summary>
  public const string Yes = "Yes";

  /// <summary>Text for a false flag.</summary>
  public const string No = "No";

  /// <summary>
  /// Builds the status column rows for the current generation state.
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
    rows.Add(new("Numeric seed:", world.NumericSeed.ToString(CultureInfo.InvariantCulture)));
    rows.Add(new("Map steps:", world.Steps.ToString(CultureInfo.InvariantCulture)));
    return rows;
  }

  /// <summary>
  /// Builds the properties column rows: every field of the properties, in declaration order.
  /// </summary>
  /// <param name="props">Properties the run was started with, or null if unknown.</param>
  /// <returns>Rows in display order; empty when <paramref name="props"/> is null.</returns>
  public static IReadOnlyList<LevelInfoRow> Properties(PropMain? props)
  {
    if (props == null) return [];

    return
    [
      new("Config version:", props.ConfigVersion.ToString(CultureInfo.InvariantCulture)),
      new("Text seed:", LevelGenSeedFormat.TextSeed(props.TextSeed)),
      new("Difficulty:", props.Difficulty.ToString(CultureInfo.InvariantCulture)),
      new("Many themes:", props.ManyThemes ? Yes : No),
    ];
  }

  /// <summary>
  /// Builds the configuration column rows: selected fields of the configuration the run used
  /// (overrides included).
  /// </summary>
  /// <param name="config">Configuration the run used, or null if unknown.</param>
  /// <returns>Rows in display order; empty when <paramref name="config"/> is null.</returns>
  public static IReadOnlyList<LevelInfoRow> Configuration(CnfLevelGeneration? config)
  {
    if (config == null) return [];

    return
    [
      new("Builder:", config.Graph.GraphBuilder.Builder.ToString()),
    ];
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
