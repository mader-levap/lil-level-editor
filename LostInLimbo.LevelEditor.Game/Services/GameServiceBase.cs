using Stride.Core.Diagnostics;

namespace LostInLimbo.LevelEditor.Services;

/// <summary>
/// Base class for all custom game services.
/// </summary>
/// <remarks>
/// Currently, provides only logging.
/// </remarks>
public abstract class GameServiceBase
{
  /// <summary>Internal logger instance.</summary>
  private Logger? logger;
  
  /// <summary>
  /// Logger for your services. Use in same way as any other <c>Log</c> elsewhere.
  /// </summary>
  protected Logger Log
  {
    get
    {
      if (logger != null) return logger;

      // Lazily create logger.
      var className = GetType().FullName;
      logger = GlobalLogger.GetLogger(className!);
      return logger;
    }
  }  
}