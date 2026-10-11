using LostInLimbo.LevelGen.Api;

namespace LostInLimbo.LevelEditor.Tests.Smoke;

/// <summary>
/// Smoke tests verifying the LostInLimbo.LevelGen package resolves transitively
/// and its API is callable from the test host.
/// </summary>
public sealed class LevelGenLibraryTests
{
  /// <summary>
  /// Verifies the LevelGen library is referenced and reports its expected name.
  /// </summary>
  [Fact]
  public void Name_ReturnsExpectedName()
  {
    // Assert: Library returns correct name of library.
    Assert.Equal("LostInLimbo.LevelGen", LibraryInfo.Name);
  }
}
