using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;

namespace LostInLimbo.LevelEditor.Ui.Gum;

/// <summary>
/// Queries and pure decision helpers for Gum focus and pointer-over state, used by the
/// editor's keyboard capture (<c>GameInput</c>) and camera right-look gate.
/// </summary>
/// <remarks>
/// Runtime queries are null-safe before <c>GumService.Default.Initialize</c>. Unit tests
/// never initialize Gum; only the pure <see cref="ShouldProcessCameraRightLook"/> is
/// exercised headlessly.
/// </remarks>
public static class GumFocus
{
  /// <summary>
  /// Determines whether a Gum text-input control currently holds keyboard focus.
  /// </summary>
  /// <returns>
  /// <see langword="true"/> when Gum is initialized and its current input receiver is a
  /// <see cref="TextBoxBase"/> (focused TextBox/PasswordBox take keyboard input);
  /// otherwise <see langword="false"/> (Gum not ready, or focus is not text input).
  /// </returns>
  /// <remarks>
  /// Uses <see cref="InteractiveGue.CurrentInputReceiver"/>, which Gum sets while a text
  /// control is focused — the same signal the TextBox docs recommend checking before
  /// handling Escape/Enter in game code. Non-text focus (e.g. a Button) leaves the
  /// receiver null so gameplay keys (WASD) keep working.
  /// </remarks>
  public static bool IsTextInputFocused()
  {
    if (!GumService.Default.IsInitialized) return false;
    return InteractiveGue.CurrentInputReceiver is TextBoxBase;
  }

  /// <summary>
  /// Determines whether the Gum cursor currently reports an interactive element under it.
  /// </summary>
  /// <returns>
  /// <see langword="true"/> when Gum is initialized and <c>Cursor.WindowOver</c> is non-null;
  /// otherwise <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// Uses <c>Cursor.VisualOver</c> (the non-obsolete name for <c>WindowOver</c>), which
  /// only reports elements with <c>HasEvents</c> under the default
  /// <see cref="Gum.Wireframe.VisualOverBehavior.IfHasEventsIsTrue"/>. Non-interactive
  /// panels that must block camera look need <c>HasEvents = true</c> when they are
  /// built (see <c>GumUiFactory.CreatePanel</c>).
  /// </remarks>
  public static bool IsPointerOverUi()
  {
    if (!GumService.Default.IsInitialized) return false;
    return GumService.Default.Cursor?.VisualOver != null;
  }

  /// <summary>
  /// Decides whether camera right-look should run for this frame.
  /// </summary>
  /// <param name="pointerOverUi">Whether the cursor is over Gum UI (<see cref="IsPointerOverUi"/>).</param>
  /// <returns><see langword="true"/> only when the pointer is not over Gum UI.</returns>
  public static bool ShouldProcessCameraRightLook(bool pointerOverUi)
  {
    return !pointerOverUi;
  }
}
