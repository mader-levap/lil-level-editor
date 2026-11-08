using System.Reflection;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Services.SystemInfo;
using Stride.Engine;
using Stride.Games;

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
