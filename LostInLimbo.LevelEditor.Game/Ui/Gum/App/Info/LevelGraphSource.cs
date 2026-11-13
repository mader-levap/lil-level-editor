using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelGen.Data.Result.Stats;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.Info;

/// <summary>
/// Identifies the graph a refresh of the information overlay would show. Two equal sources show
/// the same graph, so the visualizer can keep its layout, view and selection.
/// </summary>
/// <remarks>
/// A new run changes the run inputs and seed; resuming or stepping changes the phase or steps;
/// a clear drops the run inputs. Equal inputs and seed reproduce the same world (generation is
/// deterministic), so value equality is safe.
/// </remarks>
/// <param name="Result">Generation result.</param>
/// <param name="RunInputs">Properties and configuration of the run, or null if unknown.</param>
/// <param name="NumericSeed">Numeric seed the library used.</param>
/// <param name="Phase">Phase of the available world, or null if there is no world.</param>
/// <param name="Steps">Map steps of the available world, or null if there is no world.</param>
public sealed record LevelGraphSource(
  EnGenResult Result, LevelGenRunInputs? RunInputs, long NumericSeed, EnPhase? Phase, long? Steps);
