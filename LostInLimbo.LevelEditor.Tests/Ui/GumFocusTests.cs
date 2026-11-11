using LostInLimbo.LevelEditor.Ui.Gum;

namespace LostInLimbo.LevelEditor.Tests.Ui;

/// <summary>
/// Headless tests for <see cref="GumFocus"/> pure decision functions (camera right-look gate).
/// These tests never initialize Gum or the engine.
/// </summary>
public sealed class GumFocusTests
{
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
