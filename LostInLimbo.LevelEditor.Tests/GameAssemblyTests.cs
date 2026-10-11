using System.Reflection;

namespace LostInLimbo.LevelEditor.Tests;

/// <summary>
/// Smoke tests verifying that the game package assembly and its Stride dependencies
/// can be loaded from a plain test host (no engine initialization).
/// </summary>
public sealed class GameAssemblyTests
{
  /// <summary>Assembly name of the game package under test.</summary>
  private const string GameAssemblyName = "LostInLimbo.LevelEditor.Game";

  [Fact]
  public void BasicCameraController_IsDeclaredInGameAssembly()
  {
    // Assert: A known game script lives in the game package assembly.
    Assert.Equal(GameAssemblyName, typeof(BasicCameraController).Assembly.GetName().Name);
  }

  [Fact]
  public void GameAssembly_HasExpectedVersionFormat()
  {
    // Arrange: Get version.
    var version = typeof(BasicCameraController).Assembly.GetName().Version;

    // Assert: Version is d.d.d (informational version may append "+<commit>").
    Assert.NotNull(version);
    Assert.Matches(@"^\d+\.\d+\.\d+", version.ToString());
  }

  [Fact]
  public void GameAssembly_StrideDependenciesAreCopiedToTestOutput()
  {
    // Arrange: Get Stride references.
    var strideReferences = typeof(BasicCameraController).Assembly
        .GetReferencedAssemblies()
        .Where(reference => reference.Name != null && reference.Name.StartsWith("Stride.", StringComparison.Ordinal))
        .ToArray();

    // Assert: Referenced Stride assemblies resolve in the test host,
    // so engine-dependent editor code can be exercised without launching the game.
    Assert.NotEmpty(strideReferences);
    Assert.All(strideReferences, reference => Assert.NotNull(Assembly.Load(reference)));
  }
}
