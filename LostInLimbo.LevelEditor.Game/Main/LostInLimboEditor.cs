using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Gum;
using LostInLimbo.LevelEditor.Input;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Services.Settings;
using LostInLimbo.LevelEditor.Services.SystemInfo;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

namespace LostInLimbo.LevelEditor.Main;

/// <summary>
/// Level editor entry point and composition root of the application: registers global
/// services, initializes Gum and applies the editor theme.
/// </summary>
/// <remarks>The Gum UI itself is built later by the <c>UiMain</c> script.</remarks>
public sealed class LostInLimboEditor : Game
{
  /// <summary>Folder of the editor's per-user data, under the user's local application data.</summary>
  private static readonly string[] UserDataFolders = ["LostInLimbo", "LevelEditor"];

  /// <summary>File name of the editor settings.</summary>
  private const string SettingsFileName = "editor-settings.json";

  /// <summary>Whether Gum has been initialized against the live compositor.</summary>
  private bool _gumInitialized;

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
  /// Resolves the settings file path: the only place that knows the machine location.
  /// </summary>
  /// <returns>Full path of the settings file in the user's local application data.</returns>
  private static string ResolveSettingsPath()
  {
    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    return Path.Combine([localAppData, .. UserDataFolders, SettingsFileName]);
  }

  /// <summary>
  /// Prepares context.
  /// </summary>
  protected override void PrepareContext()
  {
    base.PrepareContext();

    // Skip the startup splash: an editor should open immediately, and the splash
    // path loads the graphics compositor asynchronously (Gum must register its
    // scene renderer on the real compositor, not the SceneSystem placeholder).
    SceneSystem.SplashScreenUrl = null;

    // Register global services here.
    Services.AddService<ISystemInfoService>(new SystemInfoService());
    Services.AddService<ILevelGenService>(new LevelGenService());
    Services.AddService<ISettingsService>(new SettingsService(ResolveSettingsPath()));

    // Gameplay keyboard input. InputManager does not exist yet during PrepareContext
    // (Game.Initialize creates it later), so it is resolved lazily on every query.
    // The keyboard is captured while a Gum text box or an open dropdown list has it (null-safe before Gum init).
    Services.AddService(new GameInput(
      hasKeyboard: () => Services.GetService<InputManager>()?.HasKeyboard ?? false,
      isKeyDown: key => Services.GetService<InputManager>()?.IsKeyDown(key) ?? false,
      isKeyboardCaptured: GumFocus.IsKeyboardCaptured));
  }

  /// <summary>
  /// Loads content (runs as a script task after game systems, including SceneSystem,
  /// have loaded the compositor).
  /// </summary>
  /// <returns>Base load task.</returns>
  protected override Task LoadContent()
  {
    EnsureGumInitialized();
    return base.LoadContent();
  }

  /// <summary>
  /// Called every tick.
  /// </summary>
  /// <param name="gameTime">Game time.</param>
  protected override void Update(GameTime gameTime)
  {
    // Fallback if LoadContent ran before the compositor was ready (e.g. splash
    // re-enabled later): initialize on the first frame that has a real compositor.
    EnsureGumInitialized();

    base.Update(gameTime);
  }

  /// <summary>
  /// Initializes Gum once the SceneSystem holds a real compositor.
  /// </summary>
  /// <remarks>
  /// SceneSystem constructs an empty placeholder compositor (<c>Game</c> is null) and
  /// replaces it with the loaded asset. Gum's default <c>registerSceneRenderer: true</c>
  /// appends a <c>GumSceneRenderer</c> to the end of the compositor's Game collection
  /// (after the 3D pass), so no asset edit is required.
  /// Stride then ticks Gum Update/Draw every frame through that renderer.
  /// </remarks>
  private void EnsureGumInitialized()
  {
    if (_gumInitialized) return;
    if (SceneSystem.GraphicsCompositor?.Game is null) return;

    GumService.Default.Initialize(this);
    GumService.Default.UseKeyboardDefaults();
    // Theme must be applied before any Forms control is created (UiMain builds the UI later).
    EditorTheme.Apply(EditorPalette.Default);
    // Gum UI itself is built by the UiMain script once Gum reports IsInitialized.
    _gumInitialized = true;
  }

  /// <summary>
  /// Releases the game. Registered services hold no resources that need disposing; UI resources
  /// are released by the <c>UiMain</c> script.
  /// </summary>
  protected override void Destroy()
  {
    base.Destroy();
  }
}
