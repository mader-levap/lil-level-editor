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
}
