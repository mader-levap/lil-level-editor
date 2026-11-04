using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Services.SystemInfo;
using Stride.Engine;
using Stride.Games;

namespace LostInLimbo.LevelEditor.Main;

public sealed class LostInLimboEditor : Game
{
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
