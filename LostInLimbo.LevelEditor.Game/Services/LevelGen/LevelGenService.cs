namespace LostInLimbo.LevelEditor.Services.LevelGen;

using System;
using LostInLimbo.LevelGen.Api;
using LostInLimbo.LevelGen.Data.Config;
using LostInLimbo.LevelGen.Data.Prop;
using LostInLimbo.LevelGen.Data.Result;
using LostInLimbo.LevelGen.Data.Status;

/// <summary>
/// Interface for level generation service.
/// </summary>
public interface ILevelGenService
{
  /// <summary>
  /// Try to start level generation.
  /// </summary>
  /// <returns>True if start succeeded, otherwise false.</returns>
  bool Start();

  /// <summary>
  /// Get status of generation.
  /// </summary>
  /// <returns>Status of generation.</returns>
  GenerationStatus Status();

  /// <summary>
  /// Get generated world.
  /// </summary>
  /// <returns>Generated world or null if world is not available.</returns>
  public LilWorld? World();
}

/// <summary>
/// Level generation service.
/// </summary>
public sealed class LevelGenService : GameServiceBase, ILevelGenService
{
  /// <summary>Level generation session.</summary>
  private readonly LevelGenSession levelGenSession = new();

  /// <summary>
  /// Try to start level generation.
  /// </summary>
  /// <returns>True if start succeeded, otherwise false.</returns>
  public bool Start()
  {
    var status = levelGenSession.Status();
    if (status.Result == EnGenResult.Running) return false; // already running

    // For now use hardcoded props.
    PropMain propMain = new();
    CnfLevelGeneration config = LevelGenConfig.Execute(propMain);
    bool started;
    try
    {
      Log.Info("LevelGen: starting...");
      started = levelGenSession.Start(EnRunKind.All, config);
    }
    catch (Exception exception)
    {
      // Invalid config: no run was claimed, so the button can be enabled again immediately.
      Log.Error($"LevelGen: Could not start generation - {exception}");
      return false;
    }
    
    if (!started)
    {
      // Another run already holds the claim, or the worker thread failed to launch.
      Log.Warning("LevelGen: Generation was not started; a run is already in flight or the worker failed to launch.");
    }
    return started;
  }

  /// <summary>
  /// Get status of generation.
  /// </summary>
  /// <returns>Status of generation.</returns>
  public GenerationStatus Status()
  {
    return levelGenSession.Status();
  }

  /// <summary>
  /// Get generated world.
  /// </summary>
  /// <returns>Generated world or null if world is not available.</returns>
  public LilWorld? World()
  {
    return levelGenSession.World();
  }
}