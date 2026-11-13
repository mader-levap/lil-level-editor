using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelGen.Data.Config;
using LostInLimbo.LevelGen.Data.Config.Graph;
using LostInLimbo.LevelGen.Data.Config.Graph.Builder;

namespace LostInLimbo.LevelEditor.Tests.Services.LevelGen;

/// <summary>
/// Headless tests for <see cref="LevelGenConfigOverrides"/>.
/// </summary>
public sealed class LevelGenConfigOverridesTests
{
  /// <summary>
  /// Builds a configuration whose graph builder settings are <paramref name="graphBuilder"/>.
  /// </summary>
  /// <param name="graphBuilder">Graph builder settings.</param>
  /// <returns>Configuration.</returns>
  private static CnfLevelGeneration ConfigWith(CnfGraphBuilder graphBuilder) =>
    CnfLevelGeneration.Default with { Graph = CnfGraph.Default with { GraphBuilder = graphBuilder } };

  /// <summary>
  /// Verifies that no overrides return the generated configuration itself.
  /// </summary>
  [Fact]
  public void Apply_None_ReturnsSameConfig()
  {
    // Arrange: A generated configuration.
    CnfLevelGeneration config = ConfigWith(new CnfGraphBuilder { Builder = EnGraphBuilder.Linear, CnfLinear = CnfGraphBuilderLinear.Default });

    // Act: Apply no overrides.
    CnfLevelGeneration result = LevelGenConfigOverrides.None.Apply(config);

    // Assert: Unchanged instance.
    Assert.Same(config, result);
  }

  /// <summary>
  /// Verifies that forcing a different builder fills its missing options section with the
  /// library default, keeps the generated sections and passes validation.
  /// </summary>
  [Fact]
  public void Apply_DifferentBuilder_FillsMissingSectionWithDefault()
  {
    // Arrange: Generated configuration picked Linear (no Simple section).
    var linear = new CnfGraphBuilderLinear { MaxEdges = 3 };
    CnfLevelGeneration config = ConfigWith(new CnfGraphBuilder { Builder = EnGraphBuilder.Linear, CnfLinear = linear });

    // Act: Force Simple.
    CnfLevelGeneration result = new LevelGenConfigOverrides(EnGraphBuilder.Simple).Apply(config);

    // Assert: Simple with default options, Linear section kept, everything else untouched, valid.
    CnfGraphBuilder builder = result.Graph.GraphBuilder;
    Assert.Equal(EnGraphBuilder.Simple, builder.Builder);
    Assert.Same(CnfGraphBuilderSimple.Default, builder.CnfSimple);
    Assert.Same(linear, builder.CnfLinear);
    Assert.Equal(config with { Graph = result.Graph }, result);
    Assert.Equal(config.Graph with { GraphBuilder = builder }, result.Graph);
    builder.Validate();
  }

  /// <summary>
  /// Verifies that forcing the builder that was generated keeps its generated options.
  /// </summary>
  [Fact]
  public void Apply_SameBuilder_KeepsGeneratedSection()
  {
    // Arrange: Generated configuration picked Simple with non-default options.
    var simple = new CnfGraphBuilderSimple { MaxExtraDegree = 2 };
    CnfLevelGeneration config = ConfigWith(new CnfGraphBuilder { Builder = EnGraphBuilder.Simple, CnfSimple = simple });

    // Act: Force Simple.
    CnfLevelGeneration result = new LevelGenConfigOverrides(EnGraphBuilder.Simple).Apply(config);

    // Assert: Generated options kept; the result equals the input.
    Assert.Same(simple, result.Graph.GraphBuilder.CnfSimple);
    Assert.Equal(config, result);
  }

  /// <summary>
  /// Verifies that forcing the one-node builder (which needs no options section) is valid.
  /// </summary>
  [Fact]
  public void Apply_OneNode_IsValid()
  {
    // Arrange: Generated configuration picked Linear.
    CnfLevelGeneration config = ConfigWith(new CnfGraphBuilder { Builder = EnGraphBuilder.Linear, CnfLinear = CnfGraphBuilderLinear.Default });

    // Act: Force OneNode.
    CnfLevelGeneration result = new LevelGenConfigOverrides(EnGraphBuilder.OneNode).Apply(config);

    // Assert: OneNode, valid.
    Assert.Equal(EnGraphBuilder.OneNode, result.Graph.GraphBuilder.Builder);
    result.Graph.GraphBuilder.Validate();
  }

  /// <summary>
  /// Verifies that a null configuration is rejected.
  /// </summary>
  [Fact]
  public void Apply_NullConfig_Throws()
  {
    // Act + Assert: No configuration.
    Assert.Throws<ArgumentNullException>(() => LevelGenConfigOverrides.None.Apply(null!));
  }
}
