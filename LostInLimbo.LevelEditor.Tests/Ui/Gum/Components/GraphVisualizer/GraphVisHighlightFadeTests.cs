using LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Headless tests for <see cref="GraphVisHighlightFade"/> (eased hover/selection highlight levels).
/// </summary>
public sealed class GraphVisHighlightFadeTests
{
  /// <summary>Tolerance for floating-point comparisons.</summary>
  private const float Tolerance = 0.0001f;

  /// <summary>Fade time used by the tests.</summary>
  private const float FadeSeconds = 0.2f;

  /// <summary>
  /// Creates a fade over three nodes and two edges.
  /// </summary>
  /// <returns>Fade.</returns>
  private static GraphVisHighlightFade Create()
  {
    var fade = new GraphVisHighlightFade(FadeSeconds);
    fade.Reset(3, 2);
    return fade;
  }

  /// <summary>
  /// Verifies a hovered node fades in gradually with easing and reaches full level after the fade time.
  /// </summary>
  [Fact]
  public void Step_Hover_FadesInGraduallyAndEased()
  {
    // Arrange: Nothing lit.
    GraphVisHighlightFade fade = Create();

    // Act: Half the fade time with node 1 hovered.
    bool changed = fade.Step(FadeSeconds / 2f, GraphVisHit.ForNode(1), GraphVisHit.None);
    float half = fade.NodeLevel(1);
    fade.Step(FadeSeconds, GraphVisHit.ForNode(1), GraphVisHit.None);

    // Assert: Halfway is 0.5 (smoothstep is symmetric), then full; other elements untouched.
    Assert.True(changed);
    Assert.Equal(0.5f, half, Tolerance);
    Assert.Equal(1f, fade.NodeLevel(1), Tolerance);
    Assert.Equal(0f, fade.NodeLevel(0));
    Assert.Equal(0f, fade.EdgeLevel(1));
  }

  /// <summary>
  /// Verifies easing: a quarter of the way in time is less than a quarter of the level.
  /// </summary>
  [Fact]
  public void Step_EarlyInFade_StartsSoftly()
  {
    // Arrange + Act: A quarter of the fade time on edge 0.
    GraphVisHighlightFade fade = Create();
    fade.Step(FadeSeconds / 4f, GraphVisHit.ForEdge(0), GraphVisHit.None);

    // Assert: Smoothstep(0.25) = 0.15625.
    Assert.Equal(0.15625f, fade.EdgeLevel(0), Tolerance);
  }

  /// <summary>
  /// Verifies that leaving an element fades it out, while the selected element stays lit.
  /// </summary>
  [Fact]
  public void Step_HoverLeaves_FadesOutButSelectionStays()
  {
    // Arrange: Node 0 selected and node 2 hovered, both fully lit.
    GraphVisHighlightFade fade = Create();
    fade.Step(FadeSeconds, GraphVisHit.ForNode(2), GraphVisHit.ForNode(0));

    // Act: Pointer leaves; a full fade time passes.
    fade.Step(FadeSeconds, GraphVisHit.None, GraphVisHit.ForNode(0));

    // Assert: Hovered node dark again, selected node still lit.
    Assert.Equal(0f, fade.NodeLevel(2), Tolerance);
    Assert.Equal(1f, fade.NodeLevel(0), Tolerance);
  }

  /// <summary>
  /// Verifies a settled fade reports no change, so nothing needs redrawing.
  /// </summary>
  [Fact]
  public void Step_WhenSettled_ReportsNoChange()
  {
    // Arrange: Node 0 fully lit.
    GraphVisHighlightFade fade = Create();
    fade.Step(FadeSeconds, GraphVisHit.ForNode(0), GraphVisHit.None);

    // Act: Same state again.
    bool changed = fade.Step(FadeSeconds, GraphVisHit.ForNode(0), GraphVisHit.None);

    // Assert: Nothing moved.
    Assert.False(changed);
  }

  /// <summary>
  /// Verifies a reset clears all levels and unknown indices read as 0.
  /// </summary>
  [Fact]
  public void Reset_ClearsLevels()
  {
    // Arrange: Edge 1 lit.
    GraphVisHighlightFade fade = Create();
    fade.Step(FadeSeconds, GraphVisHit.ForEdge(1), GraphVisHit.None);

    // Act: Reset for a smaller graph.
    fade.Reset(1, 1);

    // Assert: Dark, and out-of-range indices are 0.
    Assert.Equal(0f, fade.EdgeLevel(0));
    Assert.Equal(0f, fade.EdgeLevel(1));
    Assert.Equal(0f, fade.NodeLevel(-1));
  }

  /// <summary>
  /// Verifies invalid arguments are rejected.
  /// </summary>
  [Fact]
  public void Constructor_And_Reset_RejectInvalidArguments()
  {
    // Act + Assert: Non-positive fade time, negative counts.
    Assert.Throws<ArgumentOutOfRangeException>(() => new GraphVisHighlightFade(0f));
    Assert.Throws<ArgumentOutOfRangeException>(() => Create().Reset(-1, 0));
  }
}
