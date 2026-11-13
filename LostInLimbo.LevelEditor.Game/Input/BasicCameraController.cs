using System;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace LostInLimbo.LevelEditor.Input;

/// <summary>
/// A script that allows moving and rotating an entity through keyboard, mouse and touch input to provide basic camera navigation.
/// </summary>
/// <remarks>
/// The entity can be moved using W, A, S, D, Q and E, arrow keys, a gamepad's left stick or dragging/scaling using multi-touch.
/// Rotation is achieved using the Numpad, the mouse while holding the right mouse button, a gamepad's right stick, or dragging using single-touch.
/// Keyboard input is read through <see cref="GameInput"/>, which applies keyboard capture, so typing never moves the camera;
/// gamepad, mouse and touch input are read directly from the input manager and are not affected by capture.
/// </remarks>
public sealed class BasicCameraController : SyncScript
{
  /// <summary>
  /// Maximum absolute pitch (rotation around the camera's right axis) in radians,
  /// kept just below 90° so the camera can never flip over its own up axis.
  /// </summary>
  private const float MaximumPitch = MathUtil.PiOverTwo * 0.99f;

  /// <summary>World-space up direction; used as the pitch and yaw rotation axis and to derive the camera basis.</summary>
  private Vector3 _upVector;

  /// <summary>Camera-space translation accumulated during the current frame, applied in <see cref="UpdateTransform"/>.</summary>
  private Vector3 _translation;

  /// <summary>Yaw (rotation around the world up axis) accumulated during the current frame, in radians.</summary>
  private float _yaw;

  /// <summary>Pitch accumulated during the current frame, in radians; clamped against <see cref="MaximumPitch"/>.</summary>
  private float _pitch;

  /// <summary>
  /// Gameplay keyboard input resolved from the service registry in <see cref="Start"/>;
  /// applies keyboard capture so typing never moves the camera.
  /// </summary>
  private GameInput _gameInput = default!;

  /// <summary>Gets or sets a value indicating whether gamepad input is processed in addition to keyboard input.</summary>
  [Display("Gamepad", "Input")]
  public bool Gamepad { get; set; } = false;

  /// <summary>Gets or sets the movement speed in units per second for keyboard and gamepad input, per axis.</summary>
  [Display("Keyboard movement speed", "Movement")]
  public Vector3 KeyboardMovementSpeed { get; set; } = new Vector3(5.0f);

  /// <summary>Gets or sets the movement speed applied to touch drag/pinch gestures, per axis.</summary>
  [Display("Touch movement speed", "Movement")]
  public Vector3 TouchMovementSpeed { get; set; } = new Vector3(0.7f, 0.7f, 0.3f);

  /// <summary>Gets or sets the multiplier applied to movement speed while Shift (or the gamepad modifier buttons) is held.</summary>
  [Display("Speed factor", "Movement")]
  public float SpeedFactor { get; set; } = 5.0f;

  /// <summary>Gets or sets the rotation speed for keyboard (Numpad) and gamepad input; X is pitch, Y is yaw.</summary>
  [Display("Keyboard rotation speed", "Rotation")]
  public Vector2 KeyboardRotationSpeed { get; set; } = new Vector2(3.0f);

  /// <summary>Gets or sets the rotation speed for mouse-look while the right mouse button is held; X is yaw, Y is pitch.</summary>
  [Display("Mouse rotation speed", "Rotation")]
  public Vector2 MouseRotationSpeed { get; set; } = new Vector2(1.0f, 1.0f);

  /// <summary>Gets or sets the rotation speed for touch drag gestures; X is yaw, Y is pitch.</summary>
  [Display("Touch rotation speed", "Rotation")]
  public Vector2 TouchRotationSpeed { get; set; } = new Vector2(1.0f, 0.7f);

  /// <summary>
  /// Initializes the default up direction and registers the touch gestures required
  /// by <see cref="ProcessInput"/> on platforms other than the Windows desktop.
  /// </summary>
  public override void Start()
  {
    base.Start();

    // Gameplay keyboard input is provided as a service; fail fast when it is not registered.
    _gameInput = Game.Services.GetService<GameInput>()
      ?? throw new InvalidOperationException("GameInput is not registered; register it in LostInLimboEditor.PrepareContext.");

    // Default up-direction.
    _upVector = Vector3.UnitY;

    // Configure touch input.
    if (!Platform.IsWindowsDesktop)
    {
      Input.Gestures.Add(new GestureConfigDrag());
      Input.Gestures.Add(new GestureConfigComposite());
    }
  }

  /// <summary>Collects input for this frame and applies the resulting movement and rotation to the entity.</summary>
  public override void Update()
  {
    ProcessInput();
    UpdateTransform();
  }

  /// <summary>
  /// Reads keyboard, gamepad, mouse and touch input for the elapsed frame and accumulates
  /// the resulting camera-space <c>translation</c>, <c>yaw</c> and <c>pitch</c>.
  /// Keyboard and gamepad input is scaled by the frame time so movement is frame-rate independent;
  /// mouse and touch input already arrive as per-frame deltas and are used as-is.
  /// Keyboard reads are skipped entirely while the keyboard is captured (see <see cref="GameInput"/>).
  /// </summary>
  private void ProcessInput()
  {
    float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

    // Reset the per-frame accumulators; input from previous frames must not persist.
    _translation = Vector3.Zero;
    _yaw = 0f;
    _pitch = 0f;

    // Keyboard- and gamepad-based movement
    {
      float speed = 1f * deltaTime;

      Vector3 dir = Vector3.Zero;

      // Gamepad: left stick steers forward/strafe, triggers move down/up.
      if (Gamepad && Input.HasGamePad)
      {
        GamePadState padState = Input.DefaultGamePad.State;
        dir.Z += padState.LeftThumb.Y;
        dir.X += padState.LeftThumb.X;

        dir.Y -= padState.LeftTrigger;
        dir.Y += padState.RightTrigger;

        // A, LeftShoulder and RightShoulder act as the "sprint" modifier.
        if ((padState.Buttons & (GamePadButton.A | GamePadButton.LeftShoulder | GamePadButton.RightShoulder)) != 0)
        {
          speed *= SpeedFactor;
        }
      }

      if (_gameInput.HasKeyboard)
      {
        // Forward/backward: W and Up versus S and Down.
        if (_gameInput.IsKeyDown(Keys.W) || _gameInput.IsKeyDown(Keys.Up)) dir.Z += 1;
        if (_gameInput.IsKeyDown(Keys.S) || _gameInput.IsKeyDown(Keys.Down)) dir.Z -= 1;

        // Strafe: A and Left versus D and Right.
        if (_gameInput.IsKeyDown(Keys.A) || _gameInput.IsKeyDown(Keys.Left)) dir.X -= 1;
        if (_gameInput.IsKeyDown(Keys.D) || _gameInput.IsKeyDown(Keys.Right)) dir.X += 1;

        // Vertical movement: Q descends, E ascends.
        if (_gameInput.IsKeyDown(Keys.Q)) dir.Y -= 1;
        if (_gameInput.IsKeyDown(Keys.E)) dir.Y += 1;

        // Shift multiplies the movement speed.
        if (_gameInput.IsKeyDown(Keys.LeftShift) || _gameInput.IsKeyDown(Keys.RightShift))
        {
          speed *= SpeedFactor;
        }

        // Keep combined diagonal input from moving faster than a single direction.
        if (dir.Length() > 1f) dir = Vector3.Normalize(dir);
      }

      // Translation stays in camera space; it is rotated into world space in UpdateTransform.
      _translation += dir * KeyboardMovementSpeed * speed;
    }

    // Keyboard- and gamepad-based rotation
    {
      float speed = 1f * deltaTime;
      Vector2 rotation = Vector2.Zero;
      if (Gamepad && Input.HasGamePad)
      {
        // Right stick: stick Y drives pitch, stick X drives yaw (the stick's X axis is negated to match screen direction).
        GamePadState padState = Input.DefaultGamePad.State;
        rotation.X += padState.RightThumb.Y;
        rotation.Y += -padState.RightThumb.X;
      }

      if (_gameInput.HasKeyboard)
      {
        // Numpad: 2/8 pitch up/down, 4/6 yaw left/right.
        if (_gameInput.IsKeyDown(Keys.NumPad2)) rotation.X += 1;
        if (_gameInput.IsKeyDown(Keys.NumPad8)) rotation.X -= 1;

        if (_gameInput.IsKeyDown(Keys.NumPad4)) rotation.Y += 1;
        if (_gameInput.IsKeyDown(Keys.NumPad6)) rotation.Y -= 1;

        // Clamp combined diagonal rotation like movement.
        if (rotation.Length() > 1f) rotation = Vector2.Normalize(rotation);
      }

      rotation *= KeyboardRotationSpeed * speed;

      _pitch += rotation.X;
      _yaw += rotation.Y;
    }

    // Mouse movement and gestures
    {
      if (Input.HasMouse)
      {
        // Right-look only when the right button is held and the pointer is not over
        // Gum UI (WindowOver), so interacting with panels never rotates the camera.
        bool processRightLook = Input.IsMouseButtonDown(MouseButton.Right)
          && GumFocus.ShouldProcessCameraRightLook(GumFocus.IsPointerOverUi());

        if (processRightLook)
        {
          // While right-dragging, lock and hide the cursor so the delta drives the look direction.
          Input.LockMousePosition();
          Game.IsMouseVisible = false;

          _yaw -= Input.MouseDelta.X * MouseRotationSpeed.X;
          _pitch -= Input.MouseDelta.Y * MouseRotationSpeed.Y;
        }
        else
        {
          Input.UnlockMousePosition();
          Game.IsMouseVisible = true;
        }
      }

      // Touch gestures: a single-finger drag rotates, a two-finger drag/pinch moves.
      foreach (var gestureEvent in Input.GestureEvents)
      {
        switch (gestureEvent.Type)
        {
          case GestureType.Drag:
            var drag = (GestureEventDrag)gestureEvent;
            var dragDistance = drag.DeltaTranslation;
            _yaw -= dragDistance.X * TouchRotationSpeed.X;
            _pitch -= dragDistance.Y * TouchRotationSpeed.Y;
            break;

          case GestureType.Composite:
            var composite = (GestureEventComposite)gestureEvent;
            // X/Y pan across the view plane; pinch is mapped to forward movement via a log curve.
            _translation.X -= composite.DeltaTranslation.X * TouchMovementSpeed.X;
            _translation.Y -= composite.DeltaTranslation.Y * TouchMovementSpeed.Y;
            _translation.Z += MathF.Log(composite.DeltaScale + 1) * TouchMovementSpeed.Z;
            break;
        }
      }
    }
  }

  /// <summary>
  /// Applies the translation and rotation accumulated by <see cref="ProcessInput"/> to
  /// <see cref="Stride.Engine.Entity.Transform"/>.
  /// Translation is rotated from camera space into world space, and pitch is clamped so the
  /// camera cannot look past straight up or straight down.
  /// </summary>
  private void UpdateTransform()
  {
    var rotation = Matrix.RotationQuaternion(Entity.Transform.Rotation);

    // Camera basis: right lies in the horizontal plane (perpendicular to up), up is derived from it.
    var right = Vector3.Cross(rotation.Forward, _upVector);
    var up = Vector3.Cross(right, rotation.Forward);

    right.Normalize();
    up.Normalize();

    // Measure the current pitch from the angle between the view direction and the up axis,
    // then clamp the absolute pitch (current + delta) to ±MaximumPitch and use only the allowed delta.
    var currentPitch = MathUtil.PiOverTwo - MathF.Acos(Vector3.Dot(rotation.Forward, _upVector));
    _pitch = MathUtil.Clamp(currentPitch + _pitch, -MaximumPitch, MaximumPitch) - currentPitch;

    Vector3 finalTranslation = _translation;

    // The camera looks down its local -Z, so invert Z to make "forward" input move the camera forward.
    finalTranslation.Z = -finalTranslation.Z;
    finalTranslation = Vector3.TransformCoordinate(finalTranslation, rotation);

    Entity.Transform.Position += finalTranslation;

    // Pitch around the camera's right axis, then yaw around the world up axis.
    Entity.Transform.Rotation *= Quaternion.RotationAxis(right, _pitch) * Quaternion.RotationAxis(_upVector, _yaw);
  }
}
