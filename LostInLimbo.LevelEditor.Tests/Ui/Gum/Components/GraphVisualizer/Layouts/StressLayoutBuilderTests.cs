using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;
using Stride.Core.Mathematics;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Headless tests for <see cref="StressLayoutBuilder"/> (stress layout of the starting positions).
/// Input validation shared by all builders is tested in <see cref="GraphLayoutBuilderPickerTests"/>.
/// </summary>
public sealed class StressLayoutBuilderTests
{
  /// <summary>Builder under test.</summary>
  private static readonly StressLayoutBuilder Builder = new();

  /// <summary>Node radius used by most tests.</summary>
  private const float Radius = 20f;

  /// <summary>Free space aimed for between connected nodes.</summary>
  private const float LinkGap = 60f;

  /// <summary>Ideal center distance of two connected nodes of radius <see cref="Radius"/>.</summary>
  private const float IdealLink = 2 * Radius + LinkGap;

  /// <summary>
  /// Creates equal radii.
  /// </summary>
  /// <param name="count">Number of nodes.</param>
  /// <returns>Radii.</returns>
  private static float[] Radii(int count) => Enumerable.Repeat(Radius, count).ToArray();

  /// <summary>
  /// Creates the edges of a chain 0-1-2-...
  /// </summary>
  /// <param name="count">Number of nodes.</param>
  /// <param name="closed">Whether the last node links back to the first (a loop).</param>
  /// <returns>Edges.</returns>
  private static (int From, int To)[] Chain(int count, bool closed = false)
  {
    var edges = Enumerable.Range(0, count - 1).Select(i => (i, i + 1)).ToList();
    if (closed) edges.Add((count - 1, 0));
    return [.. edges];
  }

  /// <summary>
  /// Tells whether two line segments cross each other (proper crossing, shared ends do not count).
  /// </summary>
  /// <param name="a">Start of the first segment.</param>
  /// <param name="b">End of the first segment.</param>
  /// <param name="c">Start of the second segment.</param>
  /// <param name="d">End of the second segment.</param>
  /// <returns>True if the segments cross.</returns>
  private static bool Cross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
  {
    static float Side(Vector2 p, Vector2 q, Vector2 r) => (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
    return Side(a, b, c) * Side(a, b, d) < 0 && Side(c, d, a) * Side(c, d, b) < 0;
  }

  /// <summary>
  /// Verifies no nodes give no positions.
  /// </summary>
  [Fact]
  public void Place_WithoutNodes_IsEmpty()
  {
    // Act + Assert: Nothing to place.
    Assert.Empty(Builder.Place([], [], LinkGap));
  }

  /// <summary>
  /// Verifies a single node gets a finite position.
  /// </summary>
  [Fact]
  public void Place_SingleNode_IsPlaced()
  {
    // Act: Place one node.
    Vector2 position = Assert.Single(Builder.Place([Radius], [], LinkGap));

    // Assert: Finite coordinates.
    Assert.True(float.IsFinite(position.X) && float.IsFinite(position.Y));
  }

  /// <summary>
  /// Verifies the layout is deterministic.
  /// </summary>
  [Fact]
  public void Place_SameInput_GivesSamePositions()
  {
    // Arrange: A loop with a tail.
    (int, int)[] edges = [.. Chain(6, closed: true), (0, 6), (6, 7)];

    // Act + Assert: Two runs agree exactly.
    Assert.Equal(Builder.Place(Radii(8), edges, LinkGap), Builder.Place(Radii(8), edges, LinkGap));
  }

  /// <summary>
  /// Verifies a chain comes out as a nearly straight horizontal line with neighbours at their
  /// ideal distance.
  /// </summary>
  [Fact]
  public void Place_Chain_IsStraightAndEvenlySpaced()
  {
    // Arrange: Chain of six nodes.
    const int count = 6;

    // Act: Place it.
    Vector2[] positions = Builder.Place(Radii(count), Chain(count), LinkGap);

    // Assert: Neighbours are about the ideal distance apart.
    for (int i = 1; i < count; i++)
    {
      Assert.InRange(Vector2.Distance(positions[i - 1], positions[i]), IdealLink * 0.9f, IdealLink * 1.1f);
    }

    // Assert: The ends are almost as far apart as the chain is long (straight), and it lies horizontally.
    Vector2 span = positions[^1] - positions[0];
    Assert.True(span.Length() > IdealLink * (count - 1) * 0.95f);
    Assert.True(MathF.Abs(span.X) > MathF.Abs(span.Y) * 10);
  }

  /// <summary>
  /// Verifies a loop comes out as an open ring: no two of its edges cross.
  /// </summary>
  [Fact]
  public void Place_Loop_HasNoCrossingEdges()
  {
    // Arrange: Loop of eight nodes.
    const int count = 8;
    (int From, int To)[] edges = Chain(count, closed: true);

    // Act: Place it.
    Vector2[] positions = Builder.Place(Radii(count), edges, LinkGap);

    // Assert: No pair of edges crosses.
    for (int i = 0; i < edges.Length; i++)
    {
      for (int j = i + 1; j < edges.Length; j++)
      {
        Assert.False(
          Cross(positions[edges[i].From], positions[edges[i].To], positions[edges[j].From], positions[edges[j].To]),
          $"Edges {i} and {j} cross.");
      }
    }
  }

  /// <summary>
  /// Verifies leaves hanging off the same node (identical graph distances to everything else)
  /// still end up on different spots.
  /// </summary>
  [Fact]
  public void Place_StarLeaves_AreSeparated()
  {
    // Arrange: A hub with five leaves.
    (int, int)[] edges = [(0, 1), (0, 2), (0, 3), (0, 4), (0, 5)];

    // Act: Place it.
    Vector2[] positions = Builder.Place(Radii(6), edges, LinkGap);

    // Assert: No two nodes overlap.
    for (int i = 0; i < positions.Length; i++)
    {
      for (int j = i + 1; j < positions.Length; j++)
      {
        Assert.True(Vector2.Distance(positions[i], positions[j]) > 2 * Radius, $"Nodes {i} and {j} overlap.");
      }
    }
  }

  /// <summary>
  /// Verifies a larger node gets more room from its neighbour than a small one.
  /// </summary>
  [Fact]
  public void Place_LargerNode_IsFartherFromNeighbour()
  {
    // Arrange: Chain small - small - large.
    float[] radii = [Radius, Radius, 3 * Radius];

    // Act: Place it.
    Vector2[] positions = Builder.Place(radii, Chain(3), LinkGap);

    // Assert: Each link is about both radii plus the gap long.
    Assert.Equal(IdealLink, Vector2.Distance(positions[0], positions[1]), IdealLink * 0.05f);
    Assert.Equal(4 * Radius + LinkGap, Vector2.Distance(positions[1], positions[2]), IdealLink * 0.05f);
  }

  /// <summary>
  /// Verifies separate parts of the graph are placed side by side without overlapping, largest
  /// part first (leftmost), including a node without any edge.
  /// </summary>
  [Fact]
  public void Place_SeparateParts_SitSideBySideLargestFirst()
  {
    // Arrange: A pair (nodes 0-1), a chain of three (2-3-4) and a lone node (5).
    (int, int)[] edges = [(0, 1), (2, 3), (3, 4)];

    // Act: Place it.
    Vector2[] positions = Builder.Place(Radii(6), edges, LinkGap);

    // Assert: Left to right: the chain of three, the pair, the lone node, with free space between them.
    float chainRight = new[] { 2, 3, 4 }.Max(i => positions[i].X) + Radius;
    float pairLeft = new[] { 0, 1 }.Min(i => positions[i].X) - Radius;
    float pairRight = new[] { 0, 1 }.Max(i => positions[i].X) + Radius;
    float loneLeft = positions[5].X - Radius;
    Assert.True(pairLeft - chainRight >= LinkGap - 0.01f);
    Assert.True(loneLeft - pairRight >= LinkGap - 0.01f);
  }
}
