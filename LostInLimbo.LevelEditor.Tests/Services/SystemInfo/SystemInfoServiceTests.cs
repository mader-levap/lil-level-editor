using LostInLimbo.LevelEditor.Services.SystemInfo;
using LostInLimbo.LevelEditor.Services.SystemInfo.Data;

namespace LostInLimbo.LevelEditor.Tests.Services.SystemInfo;

/// <summary>
/// Headless tests for <see cref="SystemInfoService"/>.
/// </summary>
public sealed class SystemInfoServiceTests
{
  /// <summary>
  /// Verifies that build configuration names map to their build types, ignoring case.
  /// </summary>
  /// <param name="value">Build metadata value.</param>
  /// <param name="expected">Expected build type.</param>
  [Theory]
  [InlineData("Debug", EnBuildType.Debug)]
  [InlineData("release", EnBuildType.Release)]
  [InlineData("DEVELOPMENT", EnBuildType.Development)]
  public void ParseBuildType_KnownName_ReturnsBuildType(string value, EnBuildType expected)
  {
    // Act: Parse the configuration name.
    EnBuildType buildType = SystemInfoService.ParseBuildType(value);

    // Assert: Matching build type.
    Assert.Equal(expected, buildType);
  }

  /// <summary>
  /// Verifies that missing, unknown and numeric values fall back to <see cref="EnBuildType.Development"/>.
  /// </summary>
  /// <param name="value">Build metadata value.</param>
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("Profiling")]
  [InlineData("1")]
  [InlineData("42")]
  public void ParseBuildType_UnknownValue_ReturnsDevelopment(string? value)
  {
    // Act: Parse a value that names no build type.
    EnBuildType buildType = SystemInfoService.ParseBuildType(value);

    // Assert: Development fallback (numbers are not names, even when they match a member).
    Assert.Equal(EnBuildType.Development, buildType);
  }

  /// <summary>
  /// Verifies that the running assembly yields non-empty application data.
  /// </summary>
  [Fact]
  public void Constructor_ReadsAssemblyData()
  {
    // Act: Read the Game assembly attributes.
    var service = new SystemInfoService();

    // Assert: Name, author and version are present.
    Assert.False(string.IsNullOrWhiteSpace(service.GetName()));
    Assert.False(string.IsNullOrWhiteSpace(service.GetAuthor()));
    Assert.False(string.IsNullOrWhiteSpace(service.GetVersion()));
  }
}
