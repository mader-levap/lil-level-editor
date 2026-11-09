using System.Reflection;
using LostInLimbo.LevelEditor.Input;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Services.SystemInfo;
using LostInLimbo.LevelEditor.Ui;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

namespace LostInLimbo.LevelEditor.Main;

public sealed class LostInLimboEditor : Game
{
  /// <summary>
  /// Begin run.
  /// </summary>
  protected override void BeginRun()
  {
    base.BeginRun();
    Window.Title = ResolveTitle();
    Window.AllowUserResizing = true;
  }

  /// <summary>
  /// Resolve title of application.
  /// </summary>
  /// <returns>Title of application.</returns>
  private static string ResolveTitle()
  {
    Assembly assembly = typeof(LostInLimboEditor).Assembly;
    string appTitle = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
      ?? assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "unknown";
    return appTitle;
  }

  /// <summary>
  /// Prepares context.
  /// </summary>
  protected override void PrepareContext()
  {
    base.PrepareContext();

    // Register global services here.
    Services.AddService<ISystemInfoService>(new SystemInfoService());
    Services.AddService<ILevelGenService>(new LevelGenService());

    // Gameplay keyboard input. InputManager and UISystem do not exist yet during
    // PrepareContext (they are created later by Game.Initialize and the UI renderer),
    // so both are resolved lazily on every query; capture comes from UiFocus, which
    // only this composition root is allowed to know about.
    Services.AddService(new GameInput(
      hasKeyboard: () => Services.GetService<InputManager>()?.HasKeyboard ?? false,
      isKeyDown: key => Services.GetService<InputManager>()?.IsKeyDown(key) ?? false,
      isKeyboardCaptured: () => UiFocus.IsTextInputFocused(Services)));
  }

  /// <summary>
  /// Called every tick.
  /// </summary>
  /// <param name="gameTime">Game time.</param>
  protected override void Update(GameTime gameTime)
  {
    // Optional: update services before base.Update() on every tick.
    base.Update(gameTime);
  }

  /// <summary>
  /// Dispose stuff here.
  /// </summary>
  protected override void Destroy()
  {
    // Dispose services here if needed.
     base.Destroy();
  }
}
