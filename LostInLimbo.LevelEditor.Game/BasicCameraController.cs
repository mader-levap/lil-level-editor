using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace LostInLimbo.LevelEditor;

/// <summary>
/// A script that allows to move and rotate an entity through keyboard, mouse and touch input to provide basic camera navigation.
/// </summary>
/// <remarks>
/// The entity can be moved using W, A, S, D, Q and E, arrow keys, a gamepad's left stick or dragging/scaling using multi-touch.
/// Rotation is achieved using the Numpad, the mouse while holding the right mouse button, a gamepad's right stick, or dragging using single-touch.
/// </remarks>
public class BasicCameraController : SyncScript
{
  /// <summary>
  /// Maximum absolute pitch (rotation around the camera's right axis) in radians,
  /// kept just below 90° so the camera can never flip over its own up axis.
  /// </summary>
  private const float MaximumPitch = MathUtil.PiOverTwo * 0.99f;

  /// <summary>World-space up direction used as the pitch rotation axis.</summary>
  private Vector3 upVector;

  /// <summary>Camera-space translation accumulated during the current frame, applied in <see cref="UpdateTransform"/>.</summary>
  private Vector3 translation;

  /// <summary>Yaw (rotation around the world up axis) accumulated during the current frame, in radians.</summary>
  private float yaw;

  /// <summary>Pitch accumulated during the current frame, in radians; clamped against <see cref="MaximumPitch"/>.</summary>
  private float pitch;

  /// <summary>Gets a value indicating whether gamepad input is processed in addition to keyboard input.</summary>
  public bool Gamepad { get; set; } = false;

  /// <summary>Gets the movement speed in units per second for keyboard and gamepad input, per axis.</summary>
  public Vector3 KeyboardMovementSpeed { get; set; } = new Vector3(5.0f);

  /// <summary>Gets the movement speed applied to touch drag/pinch gestures, per axis.</summary>
  public Vector3 TouchMovementSpeed { get; set; } = new Vector3(0.7f, 0.7f, 0.3f);

  /// <summary>Gets the multiplier applied to movement speed while Shift (or the gamepad modifier buttons) is held.</summary>
  public float SpeedFactor { get; set; } = 5.0f;

  /// <summary>Gets the rotation speed for keyboard (Numpad) and gamepad input; X is pitch, Y is yaw.</summary>
  public Vector2 KeyboardRotationSpeed { get; set; } = new Vector2(3.0f);

  /// <summary>Gets the rotation speed for mouse-look while the right mouse button is held; X is yaw, Y is pitch.</summary>
  public Vector2 MouseRotationSpeed { get; set; } = new Vector2(1.0f, 1.0f);

  /// <summary>Gets the rotation speed for touch drag gestures; X is yaw, Y is pitch.</summary>
  public Vector2 TouchRotationSpeed { get; set; } = new Vector2(1.0f, 0.7f);

  /// <summary>
  /// Initializes the default up direction and registers the touch gestures required
  /// by <see cref="ProcessInput"/> on platforms other than the Windows desktop.
  /// </summary>
  public override void Start()
  {
    base.Start();

    // Default up-direction.
    upVector = Vector3.UnitY;

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
  /// All values are scaled by the frame time so movement is frame-rate independent.
  /// </summary>
  private void ProcessInput()
  {
    float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

    // Reset the per-frame accumulators; input from previous frames must not persist.
    translation = Vector3.Zero;
    yaw = 0f;
    pitch = 0f;

    // Keyboard and Gamepad based movement
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

      if (Input.HasKeyboard)
      {
        // Forward/backward: W and Up versus S and Down.
        if (Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Up)) dir.Z += 1;
        if (Input.IsKeyDown(Keys.S) || Input.IsKeyDown(Keys.Down)) dir.Z -= 1;

        // Strafe: A and Left versus D and Right.
        if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left)) dir.X -= 1;
        if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right)) dir.X += 1;

        // Vertical movement: Q descends, E ascends.
        if (Input.IsKeyDown(Keys.Q)) dir.Y -= 1;
        if (Input.IsKeyDown(Keys.E)) dir.Y += 1;

        // Shift multiplies the movement speed.
        if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
        {
          speed *= SpeedFactor;
        }

        // Keep combined diagonal input from moving faster than a single direction.
        if (dir.Length() > 1f) dir = Vector3.Normalize(dir);
      }

      // Translation stays in camera space; it is rotated into world space in UpdateTransform.
      translation += dir * KeyboardMovementSpeed * speed;
    }

    // Keyboard and Gamepad based Rotation
    {
      float speed = 1f * deltaTime;
      Vector2 rotation = Vector2.Zero;
      if (Gamepad && Input.HasGamePad)
      {
        // Right stick: X drives pitch, Y drives yaw (X is negated to match screen direction).
        GamePadState padState = Input.DefaultGamePad.State;
        rotation.X += padState.RightThumb.Y;
        rotation.Y += -padState.RightThumb.X;
      }

      if (Input.HasKeyboard)
      {
        // Numpad: 2/8 pitch down/up, 4/6 yaw left/right.
        if (Input.IsKeyDown(Keys.NumPad2)) rotation.X += 1;
        if (Input.IsKeyDown(Keys.NumPad8)) rotation.X -= 1;

        if (Input.IsKeyDown(Keys.NumPad4)) rotation.Y += 1;
        if (Input.IsKeyDown(Keys.NumPad6)) rotation.Y -= 1;

        // Clamp combined diagonal rotation like movement.
        if (rotation.Length() > 1f) rotation = Vector2.Normalize(rotation);
      }

      rotation *= KeyboardRotationSpeed * speed;

      pitch += rotation.X;
      yaw += rotation.Y;
    }

    // Mouse movement and gestures
    {
      if (Input.HasMouse)
      {
        if (Input.IsMouseButtonDown(MouseButton.Right))
        {
          // While right-dragging, lock and hide the cursor so the delta drives the look direction.
          Input.LockMousePosition();
          Game.IsMouseVisible = false;

          yaw -= Input.MouseDelta.X * MouseRotationSpeed.X;
          pitch -= Input.MouseDelta.Y * MouseRotationSpeed.Y;
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
            yaw = -dragDistance.X * TouchRotationSpeed.X;
            pitch = -dragDistance.Y * TouchRotationSpeed.Y;
            break;

          case GestureType.Composite:
            var composite = (GestureEventComposite)gestureEvent;
            // X/Y pan across the view plane; pinch is mapped to forward movement via a log curve.
            translation.X = -composite.DeltaTranslation.X * TouchMovementSpeed.X;
            translation.Y = -composite.DeltaTranslation.Y * TouchMovementSpeed.Y;
            translation.Z = MathF.Log(composite.DeltaScale + 1) * TouchMovementSpeed.Z;
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
    var right = Vector3.Cross(rotation.Forward, upVector);
    var up = Vector3.Cross(right, rotation.Forward);

    right.Normalize();
    up.Normalize();

    // Measure the current pitch from the angle between the view direction and the up axis,
    // then clamp the absolute pitch (current + delta) to ±MaximumPitch and use only the allowed delta.
    var currentPitch = MathUtil.PiOverTwo - MathF.Acos(Vector3.Dot(rotation.Forward, upVector));
    pitch = MathUtil.Clamp(currentPitch + pitch, -MaximumPitch, MaximumPitch) - currentPitch;

    Vector3 finalTranslation = translation;

    // The camera looks down its local -Z, so invert Z to make "forward" input move the camera forward.
    finalTranslation.Z = -finalTranslation.Z;
    finalTranslation = Vector3.TransformCoordinate(finalTranslation, rotation);

    Entity.Transform.Position += finalTranslation;

    // Pitch around the camera's right axis, then yaw around the world up axis.
    Entity.Transform.Rotation *= Quaternion.RotationAxis(right, pitch) * Quaternion.RotationAxis(upVector, yaw);
  }
}
