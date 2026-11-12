namespace LostInLimbo.LevelEditor.Ui.Gum.Common;

/// <summary>
/// Marks a Gum element that uses the keyboard while the pointer is over it (for example the graph
/// viewport panning with WASD), so gameplay (the 3D camera) must ignore the keyboard meanwhile.
/// </summary>
/// <remarks>
/// Checked by <see cref="GumFocus.IsKeyboardCaptured"/> against the element under the Gum cursor
/// (<c>Cursor.VisualOver</c>), so the element needs <c>HasEvents = true</c>. Hidden elements are
/// never under the cursor, so hiding releases the keyboard by itself.
/// </remarks>
public interface IClaimsKeyboardWhileHovered;
