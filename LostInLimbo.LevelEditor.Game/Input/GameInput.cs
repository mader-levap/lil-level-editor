using System;
using Stride.Input;

namespace LostInLimbo.LevelEditor.Input;

/// <summary>
/// Gameplay-facing keyboard input: reads raw key state through injected predicates and
/// suppresses it while the keyboard is captured, so gameplay scripts can read keys
/// without knowing what performs the capture.
/// </summary>
/// <remarks>
/// The capture predicate is supplied by the composition root when the instance is
/// registered as a service (see <c>LostInLimboEditor.PrepareContext</c>), keeping this
/// type free of any knowledge of the capturing side. Capture is evaluated on every read,
/// so a key held across a capture transition stops or resumes effect immediately.
/// Only keyboard queries are gated: mouse, gamepad and touch input are not claimed by
/// keyboard capture and are read from <see cref="InputManager"/> directly.
/// Any future keyboard query added here must apply the same gate; raw
/// <see cref="InputManager"/> keyboard reads are reserved for UI code.
/// </remarks>
public sealed class GameInput
{
  /// <summary>Predicate reporting whether a keyboard device is present; never gated by capture.</summary>
  private readonly Func<bool> _hasKeyboard;

  /// <summary>Predicate reporting the raw down-state of a key; suppressed while the keyboard is captured.</summary>
  private readonly Func<Keys, bool> _isKeyDown;

  /// <summary>Predicate reporting whether the keyboard is currently captured (e.g. for typing).</summary>
  private readonly Func<bool> _isKeyboardCaptured;

  /// <summary>
  /// Initializes a new instance of the <see cref="GameInput"/> class.
  /// </summary>
  /// <param name="hasKeyboard">Reports raw keyboard device presence; not gated by capture.</param>
  /// <param name="isKeyDown">Reports the raw down-state of a key.</param>
  /// <param name="isKeyboardCaptured">Reports whether the keyboard is currently captured.</param>
  /// <exception cref="ArgumentNullException">Any predicate is <see langword="null"/>.</exception>
  public GameInput(Func<bool> hasKeyboard, Func<Keys, bool> isKeyDown, Func<bool> isKeyboardCaptured)
  {
    ArgumentNullException.ThrowIfNull(hasKeyboard);
    ArgumentNullException.ThrowIfNull(isKeyDown);
    ArgumentNullException.ThrowIfNull(isKeyboardCaptured);

    _hasKeyboard = hasKeyboard;
    _isKeyDown = isKeyDown;
    _isKeyboardCaptured = isKeyboardCaptured;
  }

  /// <summary>
  /// Gets a value indicating whether a keyboard device is present.
  /// </summary>
  /// <remarks>Device presence is a property of the hardware and is never gated by capture.</remarks>
  public bool HasKeyboard => _hasKeyboard();

  /// <summary>
  /// Determines whether the given key is currently held down, as gameplay may see it.
  /// </summary>
  /// <param name="key">The key to test.</param>
  /// <returns>
  /// <see langword="true"/> when the key is physically down and the keyboard is not captured;
  /// otherwise <see langword="false"/>.
  /// </returns>
  public bool IsKeyDown(Keys key)
  {
    // Capture claims every key: typed input belongs to the capturing side, not to gameplay.
    return !_isKeyboardCaptured() && _isKeyDown(key);
  }
}
