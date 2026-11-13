using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;

namespace LostInLimbo.LevelEditor.Ui.Gum.Common;

/// <summary>
/// Queries and pure decision helpers for Gum focus and pointer-over state, used by the
/// editor's keyboard capture (<c>GameInput</c>) and camera right-look gate.
/// </summary>
/// <remarks>
/// Runtime queries are null-safe before <c>GumService.Default.Initialize</c>. Unit tests
/// never initialize Gum; only the pure <see cref="CapturesKeyboard"/> and
/// <see cref="ShouldProcessCameraRightLook"/> are exercised headlessly.
/// </remarks>
public static class GumFocus
{
  /// <summary>
  /// Determines whether a Gum control currently needs the keyboard, so gameplay (camera)
  /// must ignore it. Used as the <c>GameInput</c> capture predicate.
  /// </summary>
  /// <returns>
  /// <see langword="true"/> when Gum is initialized and either a control has keyboard focus
  /// (<see cref="IsKeyboardFocused"/>) or the pointer is over an element that uses the keyboard
  /// while hovered (<see cref="IClaimsKeyboardWhileHovered"/>); otherwise <see langword="false"/>.
  /// </returns>
  public static bool IsKeyboardCaptured()
  {
    if (!GumService.Default.IsInitialized) return false;
    return IsKeyboardFocused() || ClaimsKeyboardWhileHovered(GumService.Default.Cursor?.VisualOver);
  }

  /// <summary>
  /// Determines whether a Gum control has keyboard focus (typing or list navigation). Elements
  /// that use the keyboard while hovered must not react to keys meanwhile.
  /// </summary>
  /// <returns>
  /// <see langword="true"/> when Gum is initialized and a text box or an open list (for
  /// example a ComboBox dropdown) receives keyboard input; otherwise <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// Uses <see cref="InteractiveGue.CurrentInputReceiver"/>. Gum sets it to the focused text
  /// box, and to the dropdown's <see cref="ListBox"/> while a ComboBox is open (arrow keys
  /// move the selection); it returns to null when the dropdown closes. Non-text focus (e.g. a
  /// Button) leaves it null, so gameplay keys (WASD, arrows) keep working.
  /// </remarks>
  public static bool IsKeyboardFocused()
  {
    if (!GumService.Default.IsInitialized) return false;
    return CapturesKeyboard(Classify(InteractiveGue.CurrentInputReceiver));
  }

  /// <summary>
  /// Decides whether the element under the pointer takes the keyboard away from gameplay.
  /// </summary>
  /// <param name="visualOver">Element under the Gum cursor, or null.</param>
  /// <returns><see langword="true"/> when it is marked with <see cref="IClaimsKeyboardWhileHovered"/>.</returns>
  public static bool ClaimsKeyboardWhileHovered(object? visualOver) => visualOver is IClaimsKeyboardWhileHovered;

  /// <summary>
  /// Classifies a Gum keyboard receiver.
  /// </summary>
  /// <param name="receiver">Current input receiver, or null.</param>
  /// <returns>Receiver kind.</returns>
  private static EnKeyboardReceiverKind Classify(object? receiver) => receiver switch
  {
    null => EnKeyboardReceiverKind.None,
    TextBoxBase => EnKeyboardReceiverKind.TextInput,
    ListBox { IsVisible: true } => EnKeyboardReceiverKind.ListNavigation,
    _ => EnKeyboardReceiverKind.Other,
  };

  /// <summary>
  /// Decides whether a receiver kind takes the keyboard away from gameplay.
  /// </summary>
  /// <param name="kind">Receiver kind.</param>
  /// <returns><see langword="true"/> for text input and list navigation; otherwise <see langword="false"/>.</returns>
  public static bool CapturesKeyboard(EnKeyboardReceiverKind kind) =>
    kind is EnKeyboardReceiverKind.TextInput or EnKeyboardReceiverKind.ListNavigation;

  /// <summary>
  /// Determines whether the Gum cursor currently reports an interactive element under it.
  /// </summary>
  /// <returns>
  /// <see langword="true"/> when Gum is initialized and <c>Cursor.VisualOver</c> is non-null;
  /// otherwise <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// <c>Cursor.VisualOver</c> only reports elements with <c>HasEvents</c> under the default
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
