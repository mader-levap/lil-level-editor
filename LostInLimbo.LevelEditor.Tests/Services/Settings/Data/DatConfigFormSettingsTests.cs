using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Services.Settings.Data;
using LostInLimbo.LevelGen.Data.Config.Graph;

namespace LostInLimbo.LevelEditor.Tests.Services.Settings.Data;

/// <summary>
/// Headless tests for <see cref="DatConfigFormSettings"/>.
/// </summary>
public sealed class DatConfigFormSettingsTests
{
  /// <summary>
  /// Verifies that an inactive form overrides nothing, even with a builder picked.
  /// </summary>
  [Fact]
  public void ToOverrides_Inactive_ReturnsNone()
  {
    // Arrange: Inactive form with a builder picked.
    var settings = new DatConfigFormSettings { Active = false, GraphBuilder = EnGraphBuilder.Linear };

    // Act + Assert: No overrides.
    Assert.Same(LevelGenConfigOverrides.None, settings.ToOverrides());
  }

  /// <summary>
  /// Verifies that an active form overrides the picked builder.
  /// </summary>
  [Fact]
  public void ToOverrides_Active_ReturnsBuilder()
  {
    // Arrange: Active form.
    var settings = new DatConfigFormSettings { Active = true, GraphBuilder = EnGraphBuilder.Simple };

    // Act + Assert: Builder overridden.
    Assert.Equal(new LevelGenConfigOverrides(EnGraphBuilder.Simple), settings.ToOverrides());
  }
}
