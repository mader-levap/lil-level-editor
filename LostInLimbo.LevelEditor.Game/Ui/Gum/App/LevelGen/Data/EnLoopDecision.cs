namespace LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Data;

/// <summary>
/// What the generation loop does after one of its runs ended.
/// </summary>
public enum EnLoopDecision
{
  /// <summary>The run finished; start the next iteration with the next seed.</summary>
  Continue,
  /// <summary>The run failed; the loop has ended.</summary>
  Failed,
}
