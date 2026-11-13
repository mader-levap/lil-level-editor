namespace LostInLimbo.LevelEditor.Services.SystemInfo.Data;

/// <summary>
/// System info data.
/// </summary>
/// <remarks>
/// Provides basic information about application.
/// </remarks>
public sealed record DatSystemInfo
{
  #region Defaults

  /// <summary>Default instance.</summary>
  public static DatSystemInfo Default { get; } = new();

  #endregion

  #region Fields

  /// <summary>
  /// Name of application.
  /// </summary>
  public string Name { get; init; } = "UnknownGame";

  /// <summary>
  /// Author of application.
  /// </summary>
  public string Author { get; init; } = "Anonymous";

  /// <summary>
  /// Version of application.
  /// </summary>
  public string Version { get; init; } = "0.0.0";

  /// <summary>
  /// Build of application.
  /// </summary>
  public EnBuildType Build { get; init; } = EnBuildType.Development;

  #endregion
}
