using Stride.Core;
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
/// Clicking somewhere else deliberately does not release the focus; callers that hide UI
/// containing the focused element must release it themselves via <see cref="ReleaseTextInputFocus"/>.
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
