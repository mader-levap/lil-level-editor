namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Direction in which <see cref="LayeredLayoutBuilder"/> lays out its layers (one layer per
/// number of steps from the root).
/// </summary>
public enum EnLayeredDirection
{
  /// <summary>Chosen per connected part: rows top to bottom, turned to columns left to right when the rows would be taller than wide.</summary>
  Auto = 0,

  /// <summary>Rows, root on top.</summary>
  TopToBottom,

  /// <summary>Columns, root on the left.</summary>
  LeftToRight,
}
