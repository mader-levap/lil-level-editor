using LostInLimbo.LevelEditor.Services.SystemInfo.Data;

namespace LostInLimbo.LevelEditor.Services.SystemInfo;

/// <summary>
/// Interface for system info service.
/// </summary>
public interface ISystemInfoService
{
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
    _systemInfo = new()
    {
      Name = "Lost in Limbo: level generator/editor",
      Author = "Mader Levap",
      Version = "0.0.1",
      Build = EnBuildType.Development,
    };
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
