using System;
using System.Linq;
using System.Reflection;
using LostInLimbo.LevelEditor.Main;
using LostInLimbo.LevelEditor.Services.SystemInfo.Data;

namespace LostInLimbo.LevelEditor.Services.SystemInfo;

/// <summary>
/// Interface for system info service.
/// </summary>
public interface ISystemInfoService
{
  /// <summary>
  /// Retrieve name of application.
  /// </summary>
  /// <returns>Name of application.</returns>
  string GetName();

  /// <summary>
  /// Retrieve author of application.
  /// </summary>
  /// <returns>Author of application.</returns>
  string GetAuthor();

  /// <summary>
  /// Retrieve version of application.
  /// </summary>
  /// <returns>Version of application.</returns>
  string GetVersion();

  /// <summary>
  /// Retrieve build type of application.
  /// </summary>
  /// <returns>Build type of application.</returns>
  EnBuildType GetBuild();
}

/// <summary>
/// System info service.
/// </summary>
public sealed class SystemInfoService : GameServiceBase, ISystemInfoService
{
  /// <summary>Value shown for assembly information that is missing.</summary>
  public const string UnknownValue = "unknown";

  /// <summary>Key of the assembly metadata that holds the build configuration (set in the Game project file).</summary>
  private const string BuildTypeMetadataKey = "BuildType";

  /// <summary>System info.</summary>
  private readonly DatSystemInfo _systemInfo;

  /// <summary>
  /// Constructor.
  /// </summary>
  public SystemInfoService()
  {
    Assembly assembly = typeof(LostInLimboEditor).Assembly;
    _systemInfo = new()
    {
      Name = ResolveName(assembly),
      Author = ResolveAuthor(assembly),
      Version = ResolveVersion(assembly),
      Build = ResolveBuildType(assembly),
    };
  }

  /// <summary>
  /// Resolves name of application.
  /// </summary>
  /// <param name="assembly">Game assembly.</param>
  /// <returns>Name of application.</returns>
  private static string ResolveName(Assembly assembly)
  {
    return assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
      ?? assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? UnknownValue;
  }

  /// <summary>
  /// Resolves author of application.
  /// </summary>
  /// <param name="assembly">Game assembly.</param>
  /// <returns>Author of application.</returns>
  private static string ResolveAuthor(Assembly assembly)
  {
    return assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? UnknownValue;
  }

  /// <summary>
  /// Resolves version of application.
  /// </summary>
  /// <param name="assembly">Game assembly.</param>
  /// <returns>Version of application.</returns>
  private static string ResolveVersion(Assembly assembly)
  {
    return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? UnknownValue;
  }

  /// <summary>
  /// Resolves build type from the assembly's build configuration metadata.
  /// </summary>
  /// <param name="assembly">Game assembly.</param>
  /// <returns>Build type.</returns>
  private static EnBuildType ResolveBuildType(Assembly assembly)
  {
    string? value = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
      .FirstOrDefault(attribute => attribute.Key == BuildTypeMetadataKey)?.Value;
    return ParseBuildType(value);
  }

  /// <summary>
  /// Converts a build configuration name into a build type.
  /// </summary>
  /// <param name="value">Configuration name (for example <c>Debug</c>), case ignored; may be null.</param>
  /// <returns>
  /// Matching build type; <see cref="EnBuildType.Development"/> for a missing, unknown or numeric value.
  /// </returns>
  public static EnBuildType ParseBuildType(string? value)
  {
    // Names only: Enum.TryParse would also accept numbers ("1"), which are not configuration names.
    if (string.IsNullOrWhiteSpace(value) || !char.IsLetter(value[0])) return EnBuildType.Development;
    return Enum.TryParse(value, ignoreCase: true, out EnBuildType buildType) && Enum.IsDefined(buildType)
      ? buildType
      : EnBuildType.Development;
  }

  #region Getters

  /// <inheritdoc />
  public string GetName()
  {
    return _systemInfo.Name;
  }

  /// <inheritdoc />
  public string GetAuthor()
  {
    return _systemInfo.Author;
  }

  /// <inheritdoc />
  public string GetVersion()
  {
    return _systemInfo.Version;
  }

  /// <inheritdoc />
  public EnBuildType GetBuild()
  {
    return _systemInfo.Build;
  }

  #endregion
}
