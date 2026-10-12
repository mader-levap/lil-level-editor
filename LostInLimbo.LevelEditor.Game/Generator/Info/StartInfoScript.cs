namespace LostInLimbo.LevelEditor.Game.Generator.Info;

using LostInLimbo.LevelGen.Api;
using Stride.Engine;

/// <summary>
/// Startup script that prints some data about the LevelGen library to the Stride console. For testing.
/// </summary>
public class StartInfoScript : StartupScript
{
  public override void Start()
  {
    Log.Warning($"LevelGen: Name = {LibraryInfo.Name}, Version = {LibraryInfo.Version}.");
  }
}