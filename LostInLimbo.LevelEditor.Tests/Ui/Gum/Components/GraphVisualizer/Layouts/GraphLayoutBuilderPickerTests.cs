using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;
using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Headless tests for <see cref="GraphLayoutBuilderPicker"/>, the <see cref="GraphVisLayout"/>
/// facade and the input validation shared by all <see cref="GraphLayoutBuilder"/>s.
/// </summary>
public sealed class GraphLayoutBuilderPickerTests
{
  /// <summary>Gap between nodes used by the tests.</summary>
  private const float Gap = 30f;

  /// <summary>Every known layout, as theory data.</summary>
  public static TheoryData<EnGraphLayoutBuilder> AllBuilders => [.. Enum.GetValues<EnGraphLayoutBuilder>()];

  /// <summary>
  /// Verifies the picker lists every layout, the default (Stress) first.
  /// </summary>
  [Fact]
  public void All_ListsEveryLayout_StressFirst()
  {
    // Act + Assert: Same values as the enum, in declaration order.
    Assert.Equal(Enum.GetValues<EnGraphLayoutBuilder>(), GraphLayoutBuilderPicker.All);
    Assert.Equal(EnGraphLayoutBuilder.Stress, GraphLayoutBuilderPicker.All[0]);
    Assert.Equal(EnGraphLayoutBuilder.Stress, default(EnGraphLayoutBuilder));
  }

  /// <summary>
  /// Verifies each layout maps to its builder type.
  /// </summary>
  [Fact]
  public void For_KnownLayouts_GivesMatchingBuilder()
  {
    // Act + Assert: Expected types.
    Assert.IsType<StressLayoutBuilder>(GraphLayoutBuilderPicker.For(EnGraphLayoutBuilder.Stress));
    Assert.IsType<RingLayoutBuilder>(GraphLayoutBuilderPicker.For(EnGraphLayoutBuilder.Ring));
  }

  /// <summary>
  /// Verifies every listed layout has a builder and a display name (catches a new enum value
  /// that was not mapped).
  /// </summary>
  /// <param name="builder">Layout.</param>
  [Theory]
  [MemberData(nameof(AllBuilders))]
  public void For_EveryLayout_HasBuilderAndName(EnGraphLayoutBuilder builder)
  {
    // Act + Assert: Builder exists; name is not empty.
    Assert.NotNull(GraphLayoutBuilderPicker.For(builder));
    Assert.False(string.IsNullOrWhiteSpace(GraphLayoutBuilderPicker.DisplayName(builder)));
  }

  /// <summary>
  /// Verifies an undefined layout value is rejected.
  /// </summary>
  [Fact]
  public void For_UnknownLayout_Throws()
  {
    // Act + Assert: Out-of-range enum value.
    Assert.Throws<ArgumentOutOfRangeException>(() => GraphLayoutBuilderPicker.For((EnGraphLayoutBuilder)999));
  }

  /// <summary>
  /// Verifies the facade gives the same positions as the picked builder.
  /// </summary>
  /// <param name="builder">Layout.</param>
  [Theory]
  [MemberData(nameof(AllBuilders))]
  public void GraphVisLayoutPlace_GivesSameAsPickedBuilder(EnGraphLayoutBuilder builder)
  {
    // Arrange: A small loop with a tail.
    float[] radii = [20f, 20f, 30f, 14f];
    (int From, int To)[] edges = [(0, 1), (1, 2), (2, 0), (2, 3)];

    // Act: Place through the facade and through the builder.
    Vector2[] viaFacade = GraphVisLayout.Place(builder, radii, edges, Gap);
    Vector2[] viaBuilder = GraphLayoutBuilderPicker.For(builder).Place(radii, edges, Gap);

    // Assert: Identical.
    Assert.Equal(viaBuilder, viaFacade);
  }

  /// <summary>
  /// Verifies every builder rejects invalid arguments the same way.
  /// </summary>
  /// <param name="builder">Layout.</param>
  [Theory]
  [MemberData(nameof(AllBuilders))]
  public void Place_WithInvalidArguments_Throws(EnGraphLayoutBuilder builder)
  {
    // Arrange: The builder under test and two equal nodes.
    GraphLayoutBuilder layout = GraphLayoutBuilderPicker.For(builder);
    float[] radii = [20f, 20f];

    // Act + Assert: Edge to a missing node, a non-positive gap and null arguments.
    Assert.Throws<ArgumentOutOfRangeException>(() => layout.Place(radii, [(0, 2)], Gap));
    Assert.Throws<ArgumentOutOfRangeException>(() => layout.Place(radii, [(-1, 0)], Gap));
    Assert.Throws<ArgumentOutOfRangeException>(() => layout.Place(radii, [], 0f));
    Assert.Throws<ArgumentNullException>(() => layout.Place(null!, [], Gap));
    Assert.Throws<ArgumentNullException>(() => layout.Place(radii, null!, Gap));
  }

  /// <summary>
  /// Verifies every builder offers default physics that can drive a simulation.
  /// </summary>
  /// <param name="builder">Layout.</param>
  [Theory]
  [MemberData(nameof(AllBuilders))]
  public void DefaultPhysics_EveryLayout_IsValid(EnGraphLayoutBuilder builder)
  {
    // Act: Get the default physics.
    GraphVisPhysics physics = GraphLayoutBuilderPicker.For(builder).DefaultPhysics;

    // Assert: Present and valid.
    Assert.NotNull(physics);
    physics.Validate();
  }

  /// <summary>
  /// Verifies stress uses the standard physics and ring the same without edge pull.
  /// </summary>
  [Fact]
  public void DefaultPhysics_StressIsStandard_RingHasNoEdgePull()
  {
    // Act: Get both defaults.
    GraphVisPhysics stress = GraphLayoutBuilderPicker.For(EnGraphLayoutBuilder.Stress).DefaultPhysics;
    GraphVisPhysics ring = GraphLayoutBuilderPicker.For(EnGraphLayoutBuilder.Ring).DefaultPhysics;

    // Assert: Stress is the default; ring differs only in edge pull, which is off.
    Assert.Equal(GraphVisPhysics.Default, stress);
    Assert.Equal(GraphVisPhysics.Default with { AttractStrength = 0f }, ring);
  }

  /// <summary>
  /// Verifies the layered layout maps to its builder and has no edge pull by default.
  /// </summary>
  [Fact]
  public void Layered_MapsToBuilder_WithoutEdgePull()
  {
    // Act: Look up the layered layout.
    GraphLayoutBuilder builder = GraphLayoutBuilderPicker.For(EnGraphLayoutBuilder.Layered);

    // Assert: Right type; default physics without edge pull.
    Assert.IsType<LayeredLayoutBuilder>(builder);
    Assert.Equal(GraphVisPhysics.Default with { AttractStrength = 0f }, builder.DefaultPhysics);
  }

  /// <summary>
  /// Verifies every builder rejects a root that refers to a missing node.
  /// </summary>
  /// <param name="builder">Layout.</param>
  [Theory]
  [MemberData(nameof(AllBuilders))]
  public void Place_WithInvalidRoot_Throws(EnGraphLayoutBuilder builder)
  {
    // Arrange: The builder under test and two nodes.
    GraphLayoutBuilder layout = GraphLayoutBuilderPicker.For(builder);
    float[] radii = [20f, 20f];

    // Act + Assert: Roots beyond either end of the node range.
    Assert.Throws<ArgumentOutOfRangeException>(() => layout.Place(radii, [], Gap, [2]));
    Assert.Throws<ArgumentOutOfRangeException>(() => layout.Place(radii, [], Gap, [-1]));
  }

  /// <summary>
  /// Verifies the facade passes roots on to the builder.
  /// </summary>
  [Fact]
  public void GraphVisLayoutPlace_PassesRoots()
  {
    // Arrange: Chain 0-1-2 with root 2.
    float[] radii = [20f, 20f, 20f];
    (int From, int To)[] edges = [(0, 1), (1, 2)];

    // Act: Place through the facade and through the builder.
    Vector2[] viaFacade = GraphVisLayout.Place(EnGraphLayoutBuilder.Layered, radii, edges, Gap, [2]);
    Vector2[] viaBuilder = new LayeredLayoutBuilder().Place(radii, edges, Gap, [2]);

    // Assert: Identical, and the root is leftmost.
    Assert.Equal(viaBuilder, viaFacade);
    Assert.True(viaFacade[2].X < viaFacade[1].X && viaFacade[1].X < viaFacade[0].X);
  }
}
