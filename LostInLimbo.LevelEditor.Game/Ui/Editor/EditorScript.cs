namespace LostInLimbo.LevelEditor.Ui.Editor;

using System;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Events;
using Stride.UI.Controls;

/// <summary>
/// Script for main editor UI overlay. This UI is main overlay and contains only some basic data (like app name or version) and icons
/// that show/hide other UI overlays.
/// </summary>
public class EditorScript : StartupScript
{
  /// <summary>Show/hide level gen UI overlay.</summary>
  private Button? btnUiLevelGen;
  /// <summary>Show/hide some other UI overlay.</summary>
  private Button? btnUiSomethingElse;

  public override void Start()
  {
    var page = Entity.Get<UIComponent>().Page;

    btnUiLevelGen = page.RootElement.FindVisualChildOfType<Button>("BtnUiLevelGen") 
      ?? throw new InvalidOperationException("UI element 'BtnUiLevelGen' not found.");
    btnUiSomethingElse = page.RootElement.FindVisualChildOfType<Button>("BtnUiSomethingElse") 
      ?? throw new InvalidOperationException("UI element 'BtnUiSomethingElse' not found.");

    btnUiLevelGen.Click += BtnUiLevelGenClick;
    btnUiSomethingElse.Click += BtnUiSomethingElseClick;
  }

  // Icon buttons handling.

  /// <summary>
  /// Shows or hides UI overlay for level generation.
  /// </summary>
  /// <param name="sender">Sender.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void BtnUiLevelGenClick(object? sender, RoutedEventArgs eventArgs)
  {
    // TODO
    // check status of UI overlay for level generation and show/hide it

    Log.Warning("BtnUiLevelGenClick() called.");
  }

  /// <summary>
  /// Does nothing.
  /// </summary>
  /// <param name="sender">Sender.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void BtnUiSomethingElseClick(object? sender, RoutedEventArgs eventArgs)
  {
    Log.Warning("BtnUiSomethingElseClick() called.");
  }
}