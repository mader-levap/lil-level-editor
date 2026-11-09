using Stride.Core;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;

namespace LostInLimbo.LevelEditor.Ui;

/// <summary>
/// Helpers for querying and releasing the text-input focus tracked by Stride's <see cref="UISystem"/>.
/// </summary>
/// <remarks>
/// Stride's UI does have a focus concept: clicking an <see cref="EditText"/> makes it the
/// <see cref="UISystem.FocusedElement"/> (with <see cref="EditText.IsSelectionActive"/> set),
/// and Enter/Escape, clicking another EditText or deactivating the application releases it.
/// Stride deliberately does not blur on clicks outside the field, so this class provides the
/// release paths: <see cref="ReleaseTextInputFocusOnOutsidePress"/> polls mouse presses once per
/// frame (any button; called from <c>UiMain.Update</c>) and <see cref="ReleaseTextInputFocus"/>
/// is the manual path used when UI containing the focused element is hidden.
/// </remarks>
public static class UiFocus
{
  /// <summary>
  /// Gets the UI element that currently holds the focus, or <see langword="null"/> when none does.
  /// </summary>
  /// <param name="services">Service registry used to resolve <see cref="UISystem"/>; may be null.</param>
  /// <returns>
  /// The focused element, or <see langword="null"/> when the UI system is not available yet
  /// (nothing can be focused before it exists) or no element holds the focus.
  /// </returns>
  public static UIElement? GetFocusedElement(IServiceRegistry? services)
  {
    return services?.GetService<UISystem>()?.FocusedElement;
  }

  /// <summary>
  /// Determines whether an active text-input field currently holds the UI focus.
  /// </summary>
  /// <param name="services">Service registry used to resolve <see cref="UISystem"/>; may be null.</param>
  /// <returns><see langword="true"/> when an active <see cref="EditText"/> is focused; otherwise <see langword="false"/>.</returns>
  public static bool IsTextInputFocused(IServiceRegistry? services)
  {
    return IsTextInputFocused(GetFocusedElement(services));
  }

  /// <summary>
  /// Determines whether the given element is an active text-input field.
  /// </summary>
  /// <param name="focusedElement">The element reported as focused; may be null.</param>
  /// <returns><see langword="true"/> when the element is an <see cref="EditText"/> with an active selection; otherwise <see langword="false"/>.</returns>
  public static bool IsTextInputFocused(UIElement? focusedElement)
  {
    return focusedElement is EditText { IsSelectionActive: true };
  }

  /// <summary>
  /// Releases the text-input focus so keyboard input is routed to game scripts again.
  /// </summary>
  /// <param name="services">Service registry used to resolve <see cref="UISystem"/>; may be null.</param>
  /// <remarks>
  /// Does nothing when no <see cref="EditText"/> is focused. Only meaningful to call while the
  /// focused element and the UI system are still fully initialized (e.g. before hiding its panel).
  /// </remarks>
  public static void ReleaseTextInputFocus(IServiceRegistry? services)
  {
    if (GetFocusedElement(services) is EditText { IsSelectionActive: true } editText)
    {
      editText.IsSelectionActive = false;
    }
  }

  /// <summary>
  /// Determines whether a mouse press outside the captured text field should release text-input focus.
  /// </summary>
  /// <param name="textCaptureElement">
  /// The active text field currently holding capture, or <see langword="null"/> when nothing holds
  /// capture (no focus, non-<see cref="EditText"/> focus or an inactive field — all map to null;
  /// see <see cref="IsTextInputFocused(UIElement?)"/>).
  /// </param>
  /// <param name="elementUnderCursor">
  /// The UI element under the cursor at the press, or <see langword="null"/> when the press hit
  /// no UI at all (e.g. the 3D viewport).
  /// </param>
  /// <returns>
  /// <see langword="true"/> when a text field holds capture and the press did not land on it;
  /// otherwise <see langword="false"/>.
  /// </returns>
  public static bool ShouldReleaseTextInputFocusOnPress(UIElement? textCaptureElement, UIElement? elementUnderCursor)
  {
    // Without capture there is nothing to release.
    if (textCaptureElement == null) return false;

    // A press on the captured text field itself (or inside it) keeps the focus,
    // so clicking the field to place the caret never blurs it.
    if (elementUnderCursor != null && IsInside(elementUnderCursor, textCaptureElement)) return false;

    return true;
  }

  /// <summary>
  /// Releases the text-input focus when a mouse button is pressed and the press does not land
  /// on the focused text field itself, so clicking anywhere else (other UI or the 3D viewport)
  /// returns keyboard input to gameplay.
  /// </summary>
  /// <param name="services">Service registry used to resolve <see cref="UISystem"/> and <see cref="InputManager"/>; may be null.</param>
  /// <remarks>
  /// Intended to be polled once per frame (see <c>UiMain.Update</c>). Any mouse button counts,
  /// and only the press edge does, so holding or dragging (e.g. selecting text out of the field)
  /// does not release focus. The element under the cursor reflects the last UI picking pass
  /// (drawn a frame earlier), which is stable while the cursor is stationary; a press evaluates
  /// against that state, making the result independent of when UI event routing runs.
  /// Stride itself never releases focus on outside clicks, which is why this helper exists.
  /// </remarks>
  public static void ReleaseTextInputFocusOnOutsidePress(IServiceRegistry? services)
  {
    if (services?.GetService<UISystem>() is not { } uiSystem) return;

    // Only an active text field holds capture worth releasing.
    if (!IsTextInputFocused(uiSystem.FocusedElement)) return;

    // Any mouse button counts; edge-triggered by InputManager (pressed this frame only).
    if (services.GetService<InputManager>() is not { } input || !input.HasPressedMouseButtons) return;

    if (ShouldReleaseTextInputFocusOnPress(uiSystem.FocusedElement, uiSystem.UIElementUnderMouseCursor))
    {
      ReleaseTextInputFocus(services);
    }
  }

  /// <summary>
  /// Determines whether <paramref name="element"/> is <paramref name="ancestor"/> itself or one of its visual descendants.
  /// </summary>
  /// <param name="element">The element to test; may be null.</param>
  /// <param name="ancestor">The supposed ancestor element; may be null.</param>
  /// <returns><see langword="true"/> when the element lies inside the ancestor; otherwise <see langword="false"/>.</returns>
  public static bool IsInside(UIElement? element, UIElement? ancestor)
  {
    if (element == null || ancestor == null) return false;

    for (UIElement? current = element; current != null; current = current.VisualParent)
    {
      if (ReferenceEquals(current, ancestor)) return true;
    }

    return false;
  }
}
