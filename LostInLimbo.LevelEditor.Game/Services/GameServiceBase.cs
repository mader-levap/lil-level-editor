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
  private Logger? _logger;

  /// <summary>
  /// Logger for your services. Use in same way as any other <c>Log</c> elsewhere.
  /// </summary>
  protected Logger Log
  {
    get
    {
      if (_logger != null) return _logger;

      // Lazily create logger, named after the concrete service class like the editor's static loggers.
      _logger = GlobalLogger.GetLogger(GetType().Name);
      return _logger;
    }
  }
}
