using System.Reflection;
using System.Threading.Tasks;
using Gum;
using LostInLimbo.LevelEditor.Input;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Services.SystemInfo;
using LostInLimbo.LevelEditor.Ui;
using LostInLimbo.LevelEditor.Ui.Gum;
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
  /// Initializes Gum once the SceneSystem holds a real compositor and builds the spike UI.
  /// </summary>
  /// <remarks>
  /// SceneSystem constructs an empty placeholder compositor (<c>Game</c> is null) and
  /// replaces it with the loaded asset. Gum's default <c>registerSceneRenderer: true</c>
  /// appends a <c>GumSceneRenderer</c> to the end of the compositor's Game collection
  /// (after the 3D pass and UGS Group31 stage), so no asset edit is required.
  /// Stride then ticks Gum Update/Draw every frame through that renderer.
  /// </remarks>
  private void EnsureGumInitialized()
  {
    if (_gumInitialized) return;
    if (SceneSystem.GraphicsCompositor?.Game is null) return;

    GumService.Default.Initialize(this);
    GumService.Default.UseKeyboardDefaults();
    GumSpikeUi.Build();
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
