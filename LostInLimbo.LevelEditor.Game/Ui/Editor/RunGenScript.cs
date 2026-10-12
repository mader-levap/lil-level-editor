namespace LostInLimbo.LevelEditor.Game.Ui.Editor;

using System;
using LostInLimbo.LevelGen.Api;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Events;
using Stride.UI.Controls;

/// <summary>
/// Hooks up the generation button.
/// 
/// TODO For now the click handler only prints library info to the console; it does not start generation yet.
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
  /// Reacts to the generation button click.
  /// </summary>
  /// <param name="sender">Sender.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void BtnGenerateClick(object? sender, RoutedEventArgs eventArgs)
  {
    // TODO: in future, this will start the entire level generation machinery, which will run on a separate thread.
    // We will also need to keep the button disabled until generation finishes.
    Log.Warning($"LevelGen: Name = {LibraryInfo.Name}, Version = {LibraryInfo.Version}.");
  }
}