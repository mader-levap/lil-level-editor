using System;
using LostInLimbo.LevelGen.Api;
using LostInLimbo.LevelGen.Data.Config;
using LostInLimbo.LevelGen.Data.Prop;
using LostInLimbo.LevelGen.Data.Result;
using LostInLimbo.LevelGen.Data.Status;

namespace LostInLimbo.LevelEditor.Services.LevelGen;

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
  /// Try to advance one step in level generation.
  /// </summary>
  /// <returns>True if step succeeded, otherwise false.</returns>
  bool Step();

  /// <summary>
  /// Try to pause level generation.
  /// </summary>
  /// <returns>True if pause succeeded, otherwise false.</returns>
  bool Pause();

  /// <summary>
  /// Try to cancel level generation.
  /// </summary>
  /// <returns>True if cancel succeeded, otherwise false.</returns>
  bool Cancel();

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
/// Level generation service. Acts as wrapper for <c>LevelGenSession</c>.
/// </summary>
public sealed class LevelGenService : GameServiceBase, ILevelGenService
{
  /// <summary>Level generation session.</summary>
  private readonly LevelGenSession _levelGenSession = new();

  #region Main methods

  /// <summary>
  /// Try to start level generation.
  /// </summary>
  /// <returns>True if start succeeded, otherwise false.</returns>
  public bool Start()
  {
    var status = _levelGenSession.Status();
    if (status.Result == EnGenResult.Running) return false; // already running

    if (status.Result == EnGenResult.Paused) return ResumeGen(EnRunKind.All);
    return StartGen(EnRunKind.All);
  }

  /// <summary>
  /// Try to advance one step in level generation.
  /// </summary>
  /// <returns>True if step succeeded, otherwise false.</returns>
  public bool Step()
  {
    var status = _levelGenSession.Status();
    if (status.Result == EnGenResult.Running) return false; // already running

    if (status.Result == EnGenResult.Paused) return ResumeGen(EnRunKind.Step);
    return StartGen(EnRunKind.Step);
  }

  /// <summary>
  /// Try to pause level generation.
  /// </summary>
  /// <returns>True if pause succeeded, otherwise false.</returns>
  public bool Pause()
  {
    var status = _levelGenSession.Status();
    if (status.Result != EnGenResult.Running) return false; // not running

    return _levelGenSession.Pause();
  }

  /// <summary>
  /// Try to cancel level generation.
  /// </summary>
  /// <returns>True if cancel succeeded, otherwise false.</returns>
  public bool Cancel()
  {
    var status = _levelGenSession.Status();
    if (status.Result != EnGenResult.Running) return false; // not running

    return _levelGenSession.Cancel();
  }

  #endregion

  #region Helpers

  /// <summary>
  /// Get status of generation.
  /// </summary>
  /// <returns>Status of generation.</returns>
  public GenerationStatus Status()
  {
    return _levelGenSession.Status();
  }

  /// <summary>
  /// Get generated world.
  /// </summary>
  /// <returns>Generated world or null if world is not available.</returns>
  public LilWorld? World()
  {
    return _levelGenSession.World();
  }

  #endregion

  #region Internals

  /// <summary>
  /// Start generation from scratch.
  /// </summary>
  /// <param name="runKind">Run kind.</param>
  /// <returns>True if start succeeded, otherwise false.</returns>
  private bool StartGen(EnRunKind runKind)
  {
    CnfLevelGeneration config = GenerateConfig();
    bool started;
    try
    {
      Log.Info("LevelGen: starting...");
      started = _levelGenSession.Start(runKind, config);
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
  /// Resume generation from paused state.
  /// </summary>
  /// <param name="runKind">Run kind.</param>
  /// <returns>True if resume succeeded, otherwise false.</returns>
  private bool ResumeGen(EnRunKind runKind)
  {
    bool resumed;
    try
    {
      Log.Info("LevelGen: resuming...");
      resumed = _levelGenSession.Resume(runKind);
    }
    catch (Exception exception)
    {
      // Invalid config: no run was claimed, so the button can be enabled again immediately.
      Log.Error($"LevelGen: Could not resume generation - {exception}");
      return false;
    }

    if (!resumed)
    {
      // Another run already holds the claim, or the worker thread failed to launch.
      Log.Warning("LevelGen: Generation was not resumed.");
    }
    return resumed;
  }

  /// <summary>
  /// Generate config. For now use hardcoded props.
  /// </summary>
  /// <returns>Generated config for level generation.</returns>
  private static CnfLevelGeneration GenerateConfig()
  {
    PropMain propMain = new();
    CnfLevelGeneration config = LevelGenConfig.Execute(propMain);
    return config;
  }

  #endregion
}
