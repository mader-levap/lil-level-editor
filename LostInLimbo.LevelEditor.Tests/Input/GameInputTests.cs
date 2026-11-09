using LostInLimbo.LevelEditor.Input;
using Stride.Input;

namespace LostInLimbo.LevelEditor.Tests.Input;

/// <summary>
/// Headless tests for <see cref="GameInput"/>: the keyboard-capture gate that lets
/// gameplay scripts read keys without knowing what captures the keyboard.
/// These tests never initialize the engine; the raw reads are injected predicates.
/// </summary>
public sealed class GameInputTests
{
  /// <summary>
  /// Verifies that an uncaptured keyboard reports the raw key state unchanged.
  /// </summary>
  [Fact]
  public void IsKeyDown_WhenKeyboardNotCaptured_ReturnsRawState()
  {
    // Arrange: raw reads report W down and A up; the keyboard is not captured.
    GameInput gameInput = new(
      hasKeyboard: () => true,
      isKeyDown: key => key == Keys.W,
      isKeyboardCaptured: () => false);

    // Act: Read a held key and an unheld key.
    bool wDown = gameInput.IsKeyDown(Keys.W);
    bool aDown = gameInput.IsKeyDown(Keys.A);

    // Assert: Raw state passes through unchanged.
    Assert.True(wDown);
    Assert.False(aDown);
  }

  /// <summary>
  /// Verifies that a captured keyboard suppresses a physically held key,
  /// so typing in a text field never moves the camera (the original bug).
  /// </summary>
  [Fact]
  public void IsKeyDown_WhenKeyboardCaptured_SuppressesRawDownState()
  {
    // Arrange: raw reads report W down, but the keyboard is captured for typing.
    GameInput gameInput = new(
      hasKeyboard: () => true,
      isKeyDown: key => key == Keys.W,
      isKeyboardCaptured: () => true);

    // Act: Read the held key while captured.
    bool wDown = gameInput.IsKeyDown(Keys.W);

    // Assert: Capture suppresses the key even though it is physically down.
    Assert.False(wDown);
  }

  /// <summary>
  /// Verifies that device presence is not gated by capture: a plugged-in keyboard
  /// stays visible to gameplay while the keyboard is captured.
  /// </summary>
  [Fact]
  public void HasKeyboard_WhenKeyboardCaptured_StillReportsDevicePresence()
  {
    // Arrange: a keyboard device is present and the keyboard is captured.
    GameInput gameInput = new(
      hasKeyboard: () => true,
      isKeyDown: key => false,
      isKeyboardCaptured: () => true);

    // Act: Query device presence while captured.
    bool hasKeyboard = gameInput.HasKeyboard;

    // Assert: Capture claims keys, not the device itself.
    Assert.True(hasKeyboard);
  }

  /// <summary>
  /// Verifies that a missing keyboard device is reported as absent,
  /// so gameplay can skip keyboard processing on keyboard-less platforms.
  /// </summary>
  [Fact]
  public void HasKeyboard_WhenNoKeyboardDevice_ReturnsFalse()
  {
    // Arrange: no keyboard device is present; nothing is captured.
    GameInput gameInput = new(
      hasKeyboard: () => false,
      isKeyDown: key => true,
      isKeyboardCaptured: () => false);

    // Act: Query device presence.
    bool hasKeyboard = gameInput.HasKeyboard;

    // Assert: The absent device is reported as absent.
    Assert.False(hasKeyboard);
  }

  /// <summary>
  /// Verifies that a missing capture predicate is rejected at construction,
  /// so a misconfigured GameInput fails fast instead of reading unguarded.
  /// </summary>
  [Fact]
  public void Constructor_WhenCapturePredicateIsNull_ThrowsArgumentNullException()
  {
    // Act/Assert: Constructing without a capture predicate throws.
    Assert.Throws<ArgumentNullException>(() => new GameInput(
      hasKeyboard: () => true,
      isKeyDown: key => true,
      isKeyboardCaptured: null!));
  }
}
