using System;
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

      // Lazily create logger.
      // FullName is null only for open generic types, which cannot be instantiated; fall back anyway.
      Type type = GetType();
      _logger = GlobalLogger.GetLogger(type.FullName ?? type.Name);
      return _logger;
    }
  }
}
