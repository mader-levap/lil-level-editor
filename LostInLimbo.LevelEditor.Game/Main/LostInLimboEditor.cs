using System.Reflection;
using System.Threading.Tasks;
using Gum;
using LostInLimbo.LevelEditor.Input;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Services.SystemInfo;
using LostInLimbo.LevelEditor.Ui.Gum;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

namespace LostInLimbo.LevelEditor.Main;

public sealed class LostInLimboEditor : Game
{
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

    // Gameplay keyboard input. InputManager does not exist yet during PrepareContext
    // (Game.Initialize creates it later), so it is resolved lazily on every query.
    // The keyboard is captured while a Gum text box or an open dropdown list has it (null-safe before Gum init).
    Services.AddService(new GameInput(
      hasKeyboard: () => Services.GetService<InputManager>()?.HasKeyboard ?? false,
      isKeyDown: key => Services.GetService<InputManager>()?.IsKeyDown(key) ?? false,
      // SPIKE(graph-vis): the graph canvas owns the keyboard while the pointer is over it.
      isKeyboardCaptured: () => GumFocus.IsKeyboardCaptured()
        || Ui.Gum.Components.GraphVisualizer.Spike.GraphVisSpike.IsPointerOverCanvas));
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

    // Optional: update services before base.Update() on every tick.
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
  /// Dispose stuff here.
  /// </summary>
  protected override void Destroy()
  {
    // Dispose services here if needed.
     base.Destroy();
  }
}
