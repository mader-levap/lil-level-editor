using Stride.Core.Diagnostics;
using Stride.UI.Panels;

namespace LostInLimbo.LevelEditor.Ui;

/// <summary>
/// Base class for all UI builders.
/// </summary>
/// <remarks>
/// Currently, provides only logging.
/// </remarks>
public abstract class UiBuilderBase
{
  /// <summary>Internal logger instance.</summary>
  private Logger? _logger;

  /// <summary>
  /// Logger for your UI builders. Use in same way as any other <c>Log</c> elsewhere.
  /// </summary>
  protected Logger Log
  {
    get
    {
      if (_logger != null) return _logger;

      // Lazily create logger.
      var className = GetType().FullName;
      _logger = GlobalLogger.GetLogger(className!);
      return _logger;
    }
  }

  /// <summary>Build panel.</summary>
  /// <returns>Panel.</returns>
  abstract public Panel Build();

  /// <summary>Call update.</summary>
  virtual public void Update()
  {
    // NO-OP
  }
}
