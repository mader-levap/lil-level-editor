using LostInLimbo.LevelGen.Data.Config;
using LostInLimbo.LevelGen.Data.Prop;

namespace LostInLimbo.LevelEditor.Services.LevelGen;

/// <summary>
/// What a generation run was started from: the properties and the configuration generated from
/// them. Kept together so they always belong to the same run.
/// </summary>
/// <remarks>
/// Both are immutable records (init-only properties, no collections), so holding references is
/// safe. The numeric seed actually used is in <c>GenerationStatus.Seed</c>.
/// </remarks>
/// <param name="Props">Properties the run was started with.</param>
/// <param name="Config">Configuration generated from <paramref name="Props"/> and passed to the library.</param>
public sealed record LevelGenRunInputs(PropMain Props, CnfLevelGeneration Config);
