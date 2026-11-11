namespace LostInLimbo.LevelEditor.Ui.Gum.Common.Log;

/// <summary>
/// Severity of a status log entry; decides its color.
/// </summary>
public enum EnStatusSeverity
{
  /// <summary>Neutral information.</summary>
  Info,
  /// <summary>Successful outcome.</summary>
  Success,
  /// <summary>Something went partly wrong.</summary>
  Warning,
  /// <summary>Failure.</summary>
  Error,
}
