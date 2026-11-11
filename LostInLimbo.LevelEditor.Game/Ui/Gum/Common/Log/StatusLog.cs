using System;
using System.Collections.Generic;

namespace LostInLimbo.LevelEditor.Ui.Gum.Common.Log;

/// <summary>
/// Bounded list of status entries: once <see cref="MaxEntries"/> is exceeded the oldest
/// entries are dropped, so a long editor session cannot grow the log without limit.
/// </summary>
/// <remarks>
/// Pure model (no Gum, Stride or LevelGen); any screen can own one and show it with
/// <see cref="GumStatusLogView"/>. A log may outlive its views or be shown by several.
/// </remarks>
public sealed class StatusLog
{
  /// <summary>Default maximum number of kept entries.</summary>
  public const int DefaultMaxEntries = 200;

  /// <summary>Kept entries, oldest first.</summary>
  private readonly List<StatusEntry> _entries = [];

  /// <summary>
  /// Creates an empty log.
  /// </summary>
  /// <param name="maxEntries">Maximum number of kept entries; must be at least 1.</param>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxEntries"/> is less than 1.</exception>
  public StatusLog(int maxEntries = DefaultMaxEntries)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(maxEntries, 1);
    MaxEntries = maxEntries;
  }

  /// <summary>Maximum number of kept entries.</summary>
  public int MaxEntries { get; }

  /// <summary>Kept entries, oldest first.</summary>
  public IReadOnlyList<StatusEntry> Entries => _entries;

  /// <summary>Raised after an entry is added; the argument is the new entry.</summary>
  public event EventHandler<StatusEntry>? Added;

  /// <summary>Raised after entries are dropped; the argument is how many oldest entries were removed.</summary>
  public event EventHandler<int>? Trimmed;

  /// <summary>Raised after the log is cleared.</summary>
  public event EventHandler? Cleared;

  /// <summary>
  /// Adds an entry, dropping the oldest ones past <see cref="MaxEntries"/>.
  /// </summary>
  /// <param name="entry">Entry to add.</param>
  /// <exception cref="ArgumentNullException"><paramref name="entry"/> is null.</exception>
  public void Add(StatusEntry entry)
  {
    ArgumentNullException.ThrowIfNull(entry);
    _entries.Add(entry);
    Added?.Invoke(this, entry);

    int excess = _entries.Count - MaxEntries;
    if (excess <= 0) return;
    _entries.RemoveRange(0, excess);
    Trimmed?.Invoke(this, excess);
  }

  /// <summary>
  /// Adds an entry built from text and severity.
  /// </summary>
  /// <param name="text">Line text.</param>
  /// <param name="severity">Severity.</param>
  public void Add(string text, EnStatusSeverity severity = EnStatusSeverity.Info) => Add(new StatusEntry(text, severity));

  /// <summary>
  /// Removes all entries.
  /// </summary>
  public void Clear()
  {
    _entries.Clear();
    Cleared?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>
  /// Gets the text color for a severity.
  /// </summary>
  /// <param name="severity">Severity.</param>
  /// <returns>Color (white, light green, orange or red, as in the former UGS log).</returns>
  public static UiRgba ColorFor(EnStatusSeverity severity) => severity switch
  {
    EnStatusSeverity.Success => new UiRgba(144, 238, 144), // Stride Color.LightGreen
    EnStatusSeverity.Warning => new UiRgba(255, 165, 0), // Stride Color.Orange
    EnStatusSeverity.Error => new UiRgba(255, 0, 0), // Stride Color.Red
    _ => UiRgba.White,
  };
}
