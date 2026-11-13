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
  /// <param name="props">Properties used in config generation.</param>
  /// <param name="overrides">Configuration fields forced over the generated configuration; null for none.</param>
  /// <returns>True if start succeeded, otherwise false.</returns>
  /// <remarks>Resuming a paused run ignores both arguments (the run keeps its configuration).</remarks>
  bool Start(PropMain props, LevelGenConfigOverrides? overrides = null);

  /// <summary>
  /// Try to advance one step in level generation.
  /// </summary>
  /// <param name="props">Properties used in config generation.</param>
  /// <param name="overrides">Configuration fields forced over the generated configuration; null for none.</param>
  /// <returns>True if step succeeded, otherwise false.</returns>
  /// <remarks>Resuming a paused run ignores both arguments (the run keeps its configuration).</remarks>
  bool Step(PropMain props, LevelGenConfigOverrides? overrides = null);

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
  /// Try to discard a non-running session (typically a paused one, which can't be cancelled),
  /// returning to the never-ran state. Never blocks.
  /// </summary>
  /// <returns>True if the session was cleared, otherwise false (for example, while running).</returns>
  bool Clear();

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

  /// <summary>
  /// Get the properties and generated configuration of the current or last run, as they were
  /// when the run was started from scratch (a resumed run keeps them).
  /// </summary>
  /// <returns>Run inputs, or null if no run was started (or the session was cleared).</returns>
  /// <remarks>The numeric seed actually used is <see cref="GenerationStatus.Seed"/>.</remarks>
  LevelGenRunInputs? RunInputs();
}

/// <summary>
/// Level generation service. Acts as wrapper for <c>LevelGenSession</c>.
/// </summary>
public sealed class LevelGenService : GameServiceBase, ILevelGenService
{
  /// <summary>Level generation session.</summary>
  private readonly LevelGenSession _levelGenSession = new();

  /// <summary>Properties and configuration of the run last started from scratch; null before the first run and after <see cref="Clear"/>.</summary>
  private LevelGenRunInputs? _runInputs;

  #region Main methods

  /// <inheritdoc />
  public bool Start(PropMain props, LevelGenConfigOverrides? overrides = null)
  {
    var status = _levelGenSession.Status();
    if (status.Result == EnGenResult.Running) return false; // already running

    if (status.Result == EnGenResult.Paused) return ResumeGen(EnRunKind.All);
    return StartGen(EnRunKind.All, props, overrides ?? LevelGenConfigOverrides.None);
  }

  /// <inheritdoc />
  public bool Step(PropMain props, LevelGenConfigOverrides? overrides = null)
  {
    var status = _levelGenSession.Status();
    if (status.Result == EnGenResult.Running) return false; // already running

    if (status.Result == EnGenResult.Paused) return ResumeGen(EnRunKind.Step);
    return StartGen(EnRunKind.Step, props, overrides ?? LevelGenConfigOverrides.None);
  }

  /// <inheritdoc />
  public bool Pause()
  {
    var status = _levelGenSession.Status();
    if (status.Result != EnGenResult.Running) return false; // not running

    return _levelGenSession.Pause();
  }

  /// <inheritdoc />
  public bool Cancel()
  {
    var status = _levelGenSession.Status();
    if (status.Result != EnGenResult.Running) return false; // not running

    return _levelGenSession.Cancel();
  }

  /// <inheritdoc />
  public bool Clear()
  {
    var status = _levelGenSession.Status();
    if (status.Result == EnGenResult.Running) return false; // running runs are cancelled, not cleared

    // Zero timeout: never wait. A non-running session has no run in flight, so it clears at once.
    bool cleared = _levelGenSession.Clear(TimeSpan.Zero);
    if (cleared) _runInputs = null;
    return cleared;
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

  /// <summary>
  /// Get the properties and generated configuration of the current or last run, as they were
  /// when the run was started from scratch (a resumed run keeps them).
  /// </summary>
  /// <returns>Run inputs, or null if no run was started (or the session was cleared).</returns>
  public LevelGenRunInputs? RunInputs()
  {
    return _runInputs;
  }

  #endregion

  #region Internals

  /// <summary>
  /// Start generation from scratch.
  /// </summary>
  /// <param name="runKind">Run kind.</param>
  /// <param name="props">Properties used in config generation.</param>
  /// <param name="overrides">Configuration fields forced over the generated configuration.</param>
  /// <returns>True if start succeeded, otherwise false.</returns>
  private bool StartGen(EnRunKind runKind, PropMain props, LevelGenConfigOverrides overrides)
  {
    CnfLevelGeneration config = GenerateConfig(props, overrides);
    bool started;
    try
    {
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
    else
    {
      _runInputs = new LevelGenRunInputs(props, config);
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
      resumed = _levelGenSession.Resume(runKind);
    }
    catch (Exception exception)
    {
      // Resume refused by the session: no run was claimed, so the button can be enabled again immediately.
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
  /// Generate config from properties, then apply the overrides.
  /// </summary>
  /// <param name="props">Properties used in config generation.</param>
  /// <param name="overrides">Configuration fields forced over the generated configuration.</param>
  /// <returns>Config for level generation.</returns>
  private static CnfLevelGeneration GenerateConfig(PropMain props, LevelGenConfigOverrides overrides)
  {
    CnfLevelGeneration config = LevelGenConfig.Execute(props);
    return overrides.Apply(config);
  }

  #endregion
}
