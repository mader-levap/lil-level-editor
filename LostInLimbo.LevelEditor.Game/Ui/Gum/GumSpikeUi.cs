using Gum;
using Gum.Forms.Controls;

namespace LostInLimbo.LevelEditor.Ui.Gum;

/// <summary>
/// Temporary Phase 0 spike: proves Gum renders over the 3D scene and receives
/// clicks while the existing UGS UI stays in place. Remove once panels are
/// ported (Phase 2+).
/// </summary>
public static class GumSpikeUi
{
  /// <summary>
  /// Builds a small stack (label + button) on the Gum root.
  /// </summary>
  /// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
  public static void Build()
  {
    var stackPanel = new StackPanel
    {
      X = 20,
      Y = 80,
      Spacing = 8,
    };
    stackPanel.AddToRoot();

    var label = new Label
    {
      Text = "Gum spike (Phase 0)",
    };
    stackPanel.AddChild(label);

    var button = new Button
    {
      Text = "Click me",
    };
    button.Click += (_, _) => label.Text = "Gum click works!";
    stackPanel.AddChild(button);
  }
}
