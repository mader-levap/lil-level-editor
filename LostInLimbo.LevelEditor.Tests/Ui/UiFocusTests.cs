using LostInLimbo.LevelEditor.Ui;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace LostInLimbo.LevelEditor.Tests.Ui;

/// <summary>
/// Headless tests for <see cref="UiFocus"/>: the pure focus queries and the
/// ancestor check used to release focus when a UI overlay is hidden.
/// These tests never initialize the engine; they only manipulate UI element trees.
/// </summary>
public sealed class UiFocusTests
{
  /// <summary>
  /// Verifies that a missing service registry reports no text-input focus
  /// (e.g. before the UI system has been created).
  /// </summary>
  [Fact]
  public void IsTextInputFocused_WithNullServices_ReturnsFalse()
  {
    // Act: Query focus without any service registry.
    bool focused = UiFocus.IsTextInputFocused((Stride.Core.IServiceRegistry?)null);

    // Assert: Nothing can be focused without the UI system.
    Assert.False(focused);
  }

  /// <summary>
  /// Verifies that a null focused element reports no text-input focus.
  /// </summary>
  [Fact]
  public void IsTextInputFocused_WithNullElement_ReturnsFalse()
  {
    // Act: Query focus for "no focused element".
    bool focused = UiFocus.IsTextInputFocused((UIElement?)null);

    // Assert: No element means no text focus.
    Assert.False(focused);
  }

  /// <summary>
  /// Verifies that a focused non-EditText element does not count as text-input focus,
  /// so other focused controls would not block the camera.
  /// </summary>
  [Fact]
  public void IsTextInputFocused_WithNonEditTextElement_ReturnsFalse()
  {
    // Arrange: A focused element that is not a text field.
    UIElement focusedElement = new TextBlock();

    // Act: Query focus for that element.
    bool focused = UiFocus.IsTextInputFocused(focusedElement);

    // Assert: Only EditText counts as text input.
    Assert.False(focused);
  }

  /// <summary>
  /// Verifies that a freshly created EditText, which has no active selection,
  /// does not count as text-input focus.
  /// </summary>
  [Fact]
  public void IsTextInputFocused_WithInactiveEditText_ReturnsFalse()
  {
    // Arrange: A text field without an active selection.
    EditText editText = new();

    // Act: Query focus for that element.
    bool focused = UiFocus.IsTextInputFocused(editText);

    // Assert: An inactive text field must not block the camera.
    Assert.False(focused);
  }

  /// <summary>
  /// Verifies that an element reports being inside a panel it is a child of.
  /// </summary>
  [Fact]
  public void IsInside_WithChildInPanel_ReturnsTrue()
  {
    // Arrange: A panel containing a text field.
    Grid panel = new();
    EditText editText = new();
    panel.Children.Add(editText);

    // Act: Check whether the child lies inside the panel.
    bool inside = UiFocus.IsInside(editText, panel);

    // Assert: The child is inside its panel.
    Assert.True(inside);
  }

  /// <summary>
  /// Verifies that an element reports being inside itself, so releasing focus
  /// also works when the panel itself (not a descendant) is the focused element.
  /// </summary>
  [Fact]
  public void IsInside_WithSameElement_ReturnsTrue()
  {
    // Arrange: A single element used as both arguments.
    Grid panel = new();

    // Act: Check whether the element is inside itself.
    bool inside = UiFocus.IsInside(panel, panel);

    // Assert: An element is inside itself.
    Assert.True(inside);
  }

  /// <summary>
  /// Verifies that an unrelated element is not reported as being inside a panel.
  /// </summary>
  [Fact]
  public void IsInside_WithUnrelatedElement_ReturnsFalse()
  {
    // Arrange: Two independent elements.
    Grid panel = new();
    EditText editText = new EditText();

    // Act: Check an element that was never added to the panel.
    bool inside = UiFocus.IsInside(editText, panel);

    // Assert: The element is not inside the panel.
    Assert.False(inside);
  }

  /// <summary>
  /// Verifies that a nested descendant is found through the whole visual parent chain.
  /// </summary>
  [Fact]
  public void IsInside_WithNestedDescendant_ReturnsTrue()
  {
    // Arrange: A panel, an intermediate stack panel and a nested text field.
    Grid panel = new();
    StackPanel intermediate = new();
    EditText editText = new EditText();
    panel.Children.Add(intermediate);
    intermediate.Children.Add(editText);

    // Act: Check the deeply nested element against the outer panel.
    bool inside = UiFocus.IsInside(editText, panel);

    // Assert: The whole ancestor chain is honored.
    Assert.True(inside);
  }

  /// <summary>
  /// Verifies that null inputs never report containment,
  /// so a missing focused element or panel cannot release focus by accident.
  /// </summary>
  [Fact]
  public void IsInside_WithNullInputs_ReturnsFalse()
  {
    // Arrange: A panel and a null element.
    Grid panel = new();

    // Act: Check null element against a real panel and a real element against null.
    bool insideNullElement = UiFocus.IsInside(null, panel);
    bool insideNullAncestor = UiFocus.IsInside(panel, null);

    // Assert: Null on either side means "not inside".
    Assert.False(insideNullElement);
    Assert.False(insideNullAncestor);
  }

  /// <summary>
  /// Verifies that a press never releases when no text field holds capture.
  /// Non-EditText focus and inactive-EditText focus both map to "no capture"
  /// (covered by <see cref="IsTextInputFocused_WithNonEditTextElement_ReturnsFalse"/> and
  /// <see cref="IsTextInputFocused_WithInactiveEditText_ReturnsFalse"/>).
  /// </summary>
  [Fact]
  public void ShouldReleaseTextInputFocusOnPress_WithNullTextCaptureElement_ReturnsFalse()
  {
    // Arrange: no capture; the press hit an unrelated panel.
    Grid panel = new();

    // Act: Evaluate the press.
    bool shouldRelease = UiFocus.ShouldReleaseTextInputFocusOnPress(null, panel);

    // Assert: Without capture there is nothing to release.
    Assert.False(shouldRelease);
  }

  /// <summary>
  /// Verifies that a press on the 3D viewport (no UI element under the cursor)
  /// releases the captured text field, returning keyboard input to gameplay.
  /// </summary>
  [Fact]
  public void ShouldReleaseTextInputFocusOnPress_WithNoHitElement_ReturnsTrue()
  {
    // Arrange: a text field holds capture; the press hit no UI element.
    EditText editText = new();

    // Act: Evaluate the press.
    bool shouldRelease = UiFocus.ShouldReleaseTextInputFocusOnPress(editText, null);

    // Assert: A viewport click outside the field releases capture.
    Assert.True(shouldRelease);
  }

  /// <summary>
  /// Verifies that a press on unrelated UI (another panel or control) releases the captured field.
  /// </summary>
  [Fact]
  public void ShouldReleaseTextInputFocusOnPress_WithHitOutsideTextCaptureElement_ReturnsTrue()
  {
    // Arrange: a text field holds capture; the press hit an unrelated panel.
    EditText editText = new();
    Grid panel = new();

    // Act: Evaluate the press.
    bool shouldRelease = UiFocus.ShouldReleaseTextInputFocusOnPress(editText, panel);

    // Assert: A click outside the field releases capture.
    Assert.True(shouldRelease);
  }

  /// <summary>
  /// Verifies that a press on the captured text field itself keeps capture,
  /// so clicking the field to place the caret never blurs it.
  /// </summary>
  [Fact]
  public void ShouldReleaseTextInputFocusOnPress_WithHitOnTextCaptureElement_ReturnsFalse()
  {
    // Arrange: a text field holds capture; the press hit that same field.
    EditText editText = new();

    // Act: Evaluate the press.
    bool shouldRelease = UiFocus.ShouldReleaseTextInputFocusOnPress(editText, editText);

    // Assert: Clicking the field itself keeps capture.
    Assert.False(shouldRelease);
  }
}
