using System;
using LostInLimbo.LevelGen.Data.Config;
using LostInLimbo.LevelGen.Data.Config.Graph;
using LostInLimbo.LevelGen.Data.Config.Graph.Builder;

namespace LostInLimbo.LevelEditor.Services.LevelGen;

/// <summary>
/// Configuration fields the user forces on top of the configuration generated from properties.
/// A null field keeps the generated value.
/// </summary>
/// <param name="GraphBuilder">Graph builder to use instead of the generated one, or null to keep it.</param>
public sealed record LevelGenConfigOverrides(EnGraphBuilder? GraphBuilder = null)
{
  /// <summary>No overrides: the generated configuration is used as it is.</summary>
  public static LevelGenConfigOverrides None { get; } = new();

  /// <summary>
  /// Returns <paramref name="config"/> with the overridden fields replaced.
  /// </summary>
  /// <param name="config">Configuration generated from properties.</param>
  /// <returns>
  /// Configuration with the overrides applied; <paramref name="config"/> itself when nothing is
  /// overridden.
  /// </returns>
  /// <remarks>
  /// A forced graph builder needs its own options section. If the generated configuration
  /// picked a different builder, that section is missing, so it is filled with the library's
  /// default options for the forced builder. A section that is already there is kept.
  /// </remarks>
  /// <exception cref="ArgumentNullException"><paramref name="config"/> is null.</exception>
  public CnfLevelGeneration Apply(CnfLevelGeneration config)
  {
    ArgumentNullException.ThrowIfNull(config);
    if (GraphBuilder is not { } builder) return config; // nothing overridden

    CnfGraphBuilder generated = config.Graph.GraphBuilder;
    CnfGraphBuilder forced = generated with
    {
      Builder = builder,
      CnfSimple = builder == EnGraphBuilder.Simple ? generated.CnfSimple ?? CnfGraphBuilderSimple.Default : generated.CnfSimple,
      CnfLinear = builder == EnGraphBuilder.Linear ? generated.CnfLinear ?? CnfGraphBuilderLinear.Default : generated.CnfLinear,
    };
    return config with { Graph = config.Graph with { GraphBuilder = forced } };
  }
}
