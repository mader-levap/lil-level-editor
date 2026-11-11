using LostInLimbo.LevelEditor.Ui.Gum;

namespace LostInLimbo.LevelEditor.Tests.Ui;

/// <summary>
/// Headless tests for <see cref="GumFocus"/> pure decision functions (camera right-look gate).
/// These tests never initialize Gum or the engine.
/// </summary>
public sealed class GumFocusTests
{
  /// <summary>
  /// Verifies which kinds of Gum keyboard receivers take the keyboard away from gameplay.
  /// Regression: an open ComboBox dropdown (its ListBox is the receiver) used arrow keys
  /// while the camera also moved.
  /// </summary>
  [Theory]
  [InlineData(EnKeyboardReceiverKind.None, false)]
  [InlineData(EnKeyboardReceiverKind.Other, false)]
  [InlineData(EnKeyboardReceiverKind.TextInput, true)]
  [InlineData(EnKeyboardReceiverKind.ListNavigation, true)]
  public void CapturesKeyboard_ReturnsExpected(EnKeyboardReceiverKind kind, bool expected)
  {
    // Act: Decide capture for the receiver kind.
    bool captured = GumFocus.CapturesKeyboard(kind);

    // Assert: Text input and list navigation capture; nothing else does.
    Assert.Equal(expected, captured);
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
    // Act: Pointer is over a Gum element (VisualOver non-null).
    bool process = GumFocus.ShouldProcessCameraRightLook(pointerOverUi: true);

    // Assert: Right-drag must not rotate the camera over UI.
    Assert.False(process);
  }
}
