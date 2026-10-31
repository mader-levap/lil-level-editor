namespace LostInLimbo.LevelEditor.Ui.LevelGen;

using System;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Events;
using Stride.UI.Controls;
using LostInLimbo.LevelGen.Data.Status;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelGen.Data.Result;

/// <summary>
/// Hooks up the generation to buttons.
/// </summary>
public class RunGenScript : SyncScript
{
  /// <summary>Button used to start level generation.</summary>
  private Button? btnGenerate;

  /// <summary>Level generation service.</summary>
  private ILevelGenService levelGenService = default!;

  /// <summary>
  /// <see langword="true"/> while a run started by <see cref="Start"/> has not yet
  /// reached a terminal state; gates re-entry and drives the poll in <see cref="IsFinished"/>.
  /// </summary>
  private bool awaitingResult = false;

  public override void Start()
  {
    levelGenService = Services.GetService<ILevelGenService>()
      ?? throw new InvalidOperationException("ILevelGenService was not registered.");

    var page = Entity.Get<UIComponent>().Page;
    btnGenerate = page.RootElement.FindVisualChildOfType<Button>("BtnGenerate") 
      ?? throw new InvalidOperationException("UI element 'BtnGenerate' not found.");

    btnGenerate.Click += BtnGenerateClick;
  }

  public override void Update()
  {
    if (!awaitingResult) return; // no run ongoing
    var status = levelGenService.Status();
    if (status.Result == EnGenResult.Running) return; // still running

    // Terminal state reached: hand the button back to the user and report the outcome.
    awaitingResult = false;
    btnGenerate!.IsEnabled = true;

    if (status.Result == EnGenResult.Finished) {
      LilWorld? world = levelGenService.World();
      Log.Info($"LevelGen: Finished. Graph: nodes={world?.Graph.Nodes.Count ?? 0}, edges={world?.Graph.Edges.Count ?? 0}. Map: steps={world?.Stats.Steps ?? 0}).");
    } else if (status.Result == EnGenResult.Error)
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
    btnGenerate!.IsEnabled = false;

    bool started = levelGenService.Start();
    if (!started) { // failed to start generation, re-enable button
      btnGenerate.IsEnabled = true;
      return;
    }

    awaitingResult = true;
  }
}