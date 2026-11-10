using LostInLimbo.LevelEditor.Ui.Gum;

namespace LostInLimbo.LevelEditor.Tests.Ui;

/// <summary>
/// Headless tests for <see cref="GumFocus"/> pure decision functions used by the
/// Phase 1 input seams (keyboard capture OR, camera right-look gate).
/// These tests never initialize Gum or the engine.
/// </summary>
public sealed class GumFocusTests
{
  /// <summary>
  /// Verifies that neither UI stack holding text focus leaves the keyboard uncaptured.
  /// </summary>
  [Fact]
  public void IsKeyboardCaptured_WhenNeitherStackFocused_ReturnsFalse()
  {
    // Act: Query capture with no UGS and no Gum text focus.
    bool captured = GumFocus.IsKeyboardCaptured(ugsTextInputFocused: false, gumTextInputFocused: false);

    // Assert: Gameplay keeps the keyboard.
    Assert.False(captured);
  }

  /// <summary>
  /// Verifies that a focused UGS EditText captures the keyboard (existing behaviour).
  /// </summary>
  [Fact]
  public void IsKeyboardCaptured_WhenUgsTextFocused_ReturnsTrue()
  {
    // Act: Only the UGS text field holds focus.
    bool captured = GumFocus.IsKeyboardCaptured(ugsTextInputFocused: true, gumTextInputFocused: false);

    // Assert: UGS focus alone claims the keyboard.
    Assert.True(captured);
  }

  /// <summary>
  /// Verifies that a focused Gum TextBox captures the keyboard (Phase 1 seam).
  /// </summary>
  [Fact]
  public void IsKeyboardCaptured_WhenGumTextFocused_ReturnsTrue()
  {
    // Act: Only the Gum text field holds focus.
    bool captured = GumFocus.IsKeyboardCaptured(ugsTextInputFocused: false, gumTextInputFocused: true);

    // Assert: Gum focus alone claims the keyboard.
    Assert.True(captured);
  }

  /// <summary>
  /// Verifies that both stacks focused still captures (OR, not XOR).
  /// </summary>
  [Fact]
  public void IsKeyboardCaptured_WhenBothStacksFocused_ReturnsTrue()
  {
    // Act: Both UGS and Gum report text focus (transition edge case).
    bool captured = GumFocus.IsKeyboardCaptured(ugsTextInputFocused: true, gumTextInputFocused: true);

    // Assert: Either side is enough to capture.
    Assert.True(captured);
  }

  /// <summary>
  /// Verifies that camera right-look runs when the pointer is not over Gum UI.
  /// </summary>
  [Fact]
  public void ShouldProcessCameraRightLook_WhenPointerNotOverUi_ReturnsTrue()
  {
    // Act: Pointer is over the 3D viewport (no Gum UI).
    bool process = GumFocus.ShouldProcessCameraRightLook(pointerOverUi: false);

    // Assert: Right-drag look is allowed.
    Assert.True(process);
  }

  /// <summary>
  /// Verifies that camera right-look is suppressed while the pointer is over Gum UI.
  /// </summary>
  [Fact]
  public void ShouldProcessCameraRightLook_WhenPointerOverUi_ReturnsFalse()
  {
    // Act: Pointer is over a Gum element (WindowOver non-null).
    bool process = GumFocus.ShouldProcessCameraRightLook(pointerOverUi: true);

    // Assert: Right-drag must not rotate the camera over UI.
    Assert.False(process);
  }
}
