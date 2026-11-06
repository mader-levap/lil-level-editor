using System.Reflection;
using System.Linq;
using LostInLimbo.LevelEditor.Main;
using LostInLimbo.LevelEditor.Services.SystemInfo.Data;
using System;

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
  public string GetAuthor();

  /// <summary>
  /// Retrieve version of application.
  /// </summary>
  /// <returns>Version of application.</returns>
  public string GetVersion();

  /// <summary>
  /// Retrieve build type of application.
  /// </summary>
  /// <returns>Build type of application.</returns>
  public EnBuildType GetBuild();
}

/// <summary>
/// System info service.
/// </summary>
public sealed class SystemInfoService : GameServiceBase, ISystemInfoService
{
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
      ?? assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "unknown";
  }

  /// <summary>
  /// Resolves author of application.
  /// </summary>
  /// <param name="assembly">Game assembly.</param>
  /// <returns>Author of application.</returns>
  private static string ResolveAuthor(Assembly assembly)
  {
    return assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "unknown";
  }

  /// <summary>
  /// Resolves version of application.
  /// </summary>
  /// <param name="assembly">Game assembly.</param>
  /// <returns>Version of application.</returns>
  private static string ResolveVersion(Assembly assembly)
  {
    return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
  }

  /// <summary>
  /// Resolves build type enum.
  /// </summary>
  /// <param name="assembly">Game assembly.</param>
  /// <returns>Build type.</returns>
  private static EnBuildType ResolveBuildType(Assembly assembly)
  {
    string buildTypeStr = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(x => x.Key == "BuildType")?.Value ?? "unknown";
    EnBuildType foundBuildType = Enum.TryParse<EnBuildType>(buildTypeStr, ignoreCase: true, out var buildType) ? buildType : EnBuildType.Development;
    if (Enum.IsDefined(buildType)) return foundBuildType;
    return EnBuildType.Development;
  }

  // ///////////////////////////////////////////////////////////////////////////
  // Getters.

  /// <summary>
  /// Retrieve name of application.
  /// </summary>
  /// <returns>Name of application.</returns>
  public string GetName()
  {
    return _systemInfo.Name;
  }

  /// <summary>
  /// Retrieve author of application.
  /// </summary>
  /// <returns>Author of application.</returns>
  public string GetAuthor()
  {
    return _systemInfo.Author;
  }

  /// <summary>
  /// Retrieve version of application.
  /// </summary>
  /// <returns>Version of application.</returns>
  public string GetVersion()
  {
    return _systemInfo.Version;
  }

  /// <summary>
  /// Retrieve build type of application.
  /// </summary>
  /// <returns>Build type of application.</returns>
  public EnBuildType GetBuild()
  {
    return _systemInfo.Build;
  }
}
