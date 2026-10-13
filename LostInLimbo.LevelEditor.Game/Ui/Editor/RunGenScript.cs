namespace LostInLimbo.LevelEditor.Game.Ui.Editor;

using System;
using LostInLimbo.LevelGen.Api;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Events;
using Stride.UI.Controls;
using LostInLimbo.LevelGen.Data.Other;
using LostInLimbo.LevelGen.Data.Config;

/// <summary>
/// Hooks up the generation button: starts a level generation run on click, keeps the button
/// disabled while the run is in flight and reports the terminal status to the Stride console.
/// </summary>
public class RunGenScript : SyncScript
{
  private Button? btnGenerate;

  /// <summary>
  /// <see langword="true"/> while a run started by <see cref="BtnGenerateClick"/> has not yet
  /// reached a terminal state; gates re-entry and drives the poll in <see cref="Update"/>.
  /// </summary>
  private bool awaitingResult;

  public override void Start()
  {
    var page = Entity.Get<UIComponent>().Page;
    btnGenerate = page.RootElement.FindVisualChildOfType<Button>("BtnGenerate")
      ?? throw new InvalidOperationException("UI element 'BtnGenerate' not found.");

    btnGenerate.Click += BtnGenerateClick;
  }

  public override void Update()
  {
    if (!awaitingResult) return;

    var status = LevelGeneration.Status();
    if (status.Result == EnGenResult.Running) return;

    // Terminal state reached: hand the button back to the user and report the outcome.
    awaitingResult = false;
    btnGenerate!.IsEnabled = true;

    if (status.Result == EnGenResult.Finished)
      Log.Info($"LevelGen: Finished (steps: {LevelGeneration.World()?.Stats.Steps ?? 0}).");
    else if (status.Result == EnGenResult.Error)
      Log.Error($"LevelGen: Error - {status.Error}");
    else
      Log.Info($"LevelGen: {status.Result}.");
  }

  /// <summary>
  /// Reacts to the generation button click: disables the button and starts a generation run.
  /// </summary>
  /// <param name="sender">Sender.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void BtnGenerateClick(object? sender, RoutedEventArgs eventArgs)
  {
    if (awaitingResult) return;
    btnGenerate!.IsEnabled = false;

    // For now use custom placeholder config.
    CnfLevelGeneration config = new()
    {
      Debug = new()
      {
        PlaceholderSleep = 3000,
      }
    };

    bool started;
    try
    {
      Log.Info("LevelGen: starting...");
      started = LevelGeneration.Start(EnRunKind.All, config);
    }
    catch (Exception exception)
    {
      // Invalid config: no run was claimed, so the button can be enabled again immediately.
      Log.Error($"LevelGen: Could not start generation - {exception}");
      btnGenerate.IsEnabled = true;
      return;
    }

    if (!started)
    {
      // Another run already holds the claim, or the worker thread failed to launch.
      Log.Warning("LevelGen: Generation was not started; a run is already in flight or the worker failed to launch.");
      btnGenerate.IsEnabled = true;
      return;
    }

    awaitingResult = true;
  }
}