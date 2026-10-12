namespace LostInLimbo.LevelEditor.Game.Generator.Info;

using System;
using LostInLimbo.LevelGen.Api;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Events;
using Stride.UI.Controls;

/// <summary>
/// Hooks up generation button so clicking on it starts generation process.
/// TODO: for now, it just prints data from library in console.
/// </summary>
public class RunGenScript : StartupScript
{
  private Button? btnGenerate;

  public override void Start()
  {
    var page = Entity.Get<UIComponent>().Page;
    btnGenerate = page.RootElement.FindVisualChildOfType<Button>("BtnGenerate")
      ?? throw new InvalidOperationException("UI element 'BtnGenerate' not found.");

    btnGenerate.Click += BtnGenerateClick;
  }

  /// <summary>
  /// React on generation button click.
  /// </summary>
  /// <param name="sender">Sender.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void BtnGenerateClick(object? sender, RoutedEventArgs eventArgs)
  {
    // TODO: in future, here we will start entire level generation machinery that will go on separate thread.
    // We will also need to disable button until generation finishes.
    Log.Warning($"LevelGen: Name = {LibraryInfo.Name}, Version = {LibraryInfo.Version}.");    
  }
}