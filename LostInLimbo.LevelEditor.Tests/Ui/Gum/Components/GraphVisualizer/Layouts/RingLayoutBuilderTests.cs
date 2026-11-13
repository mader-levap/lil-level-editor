using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Headless tests for <see cref="RingLayoutBuilder"/> (nodes on a ring).
/// </summary>
public sealed class RingLayoutBuilderTests
{
  /// <summary>Tolerance for floating-point comparisons.</summary>
  private const float Tolerance = 0.01f;

  /// <summary>Gap between nodes used by the tests.</summary>
  private const float Gap = 30f;

  /// <summary>Builder under test.</summary>
  private static readonly RingLayoutBuilder Builder = new();

  /// <summary>
  /// Verifies no nodes give no positions.
  /// </summary>
  [Fact]
  public void Place_WithoutNodes_IsEmpty()
  {
    // Act + Assert: Nothing to place.
    Assert.Empty(Builder.Place([], [], Gap));
  }

  /// <summary>
  /// Verifies a single node sits at the origin.
  /// </summary>
  [Fact]
  public void Place_SingleNode_IsAtOrigin()
  {
    // Act: Place one node.
    Vector2[] positions = Builder.Place([20f], [], Gap);

    // Assert: It is at the origin.
    Assert.Equal(Vector2.Zero, Assert.Single(positions));
  }

  /// <summary>
  /// Verifies no two nodes come closer than the gap, for few and many nodes and for very
  /// different sizes (two nodes are the tightest case: the ring must grow beyond its arc length).
  /// </summary>
  /// <param name="radii">Node radii.</param>
  [Theory]
  [InlineData(new[] { 20f, 20f })]
  [InlineData(new[] { 14f, 70f })]
  [InlineData(new[] { 20f, 20f, 20f })]
  [InlineData(new[] { 70f, 14f, 14f, 14f, 70f, 14f })]
  [InlineData(new[] { 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f, 22f })]
  public void Place_KeepsGapBetweenAllNodes(float[] radii)
  {
    // Act: Place the nodes.
    Vector2[] positions = Builder.Place(radii, [], Gap);

    // Assert: Every pair of circles is at least the gap apart.
    Assert.Equal(radii.Length, positions.Length);
    for (int i = 0; i < positions.Length; i++)
    {
      for (int j = i + 1; j < positions.Length; j++)
      {
        float free = Vector2.Distance(positions[i], positions[j]) - radii[i] - radii[j];
        Assert.True(free >= Gap - Tolerance, $"Nodes {i} and {j} are only {free} apart.");
      }
    }
  }

  /// <summary>
  /// Verifies the first node is at the top of the ring and the ring is centered on the origin.
  /// </summary>
  [Fact]
  public void Place_EqualNodes_StartsAtTopAndIsCentered()
  {
    // Act: Place four equal nodes.
    Vector2[] positions = Builder.Place([20f, 20f, 20f, 20f], [], Gap);

    // Assert: All on one ring around the origin; their centers average to the origin.
    float ringRadius = positions[0].Length();
    Assert.All(positions, position => Assert.Equal(ringRadius, position.Length(), Tolerance));
    Vector2 sum = positions.Aggregate(Vector2.Zero, (total, position) => total + position);
    Assert.Equal(0f, sum.Length(), Tolerance);

    // Assert: Clockwise on screen (Y down) starting in the top-right quadrant: the first node's arc starts at the top.
    Assert.True(positions[0].Y < 0 && positions[0].X > 0);
    Assert.True(positions[1].Y > 0 && positions[1].X > 0);
  }

  /// <summary>
  /// Verifies edges do not change the placement.
  /// </summary>
  [Fact]
  public void Place_IgnoresEdges()
  {
    // Arrange: Mixed radii.
    float[] radii = [14f, 30f, 70f, 22f];

    // Act + Assert: With and without edges the positions agree exactly.
    Assert.Equal(Builder.Place(radii, [], Gap), Builder.Place(radii, [(0, 2), (1, 3)], Gap));
  }

  /// <summary>
  /// Verifies placement is deterministic.
  /// </summary>
  [Fact]
  public void Place_SameInput_GivesSamePositions()
  {
    // Arrange: Mixed radii.
    float[] radii = [14f, 30f, 70f, 22f, 22f];

    // Act + Assert: Two runs agree exactly.
    Assert.Equal(Builder.Place(radii, [], Gap), Builder.Place(radii, [], Gap));
  }
}
