using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;

namespace LostInLimbo.LevelEditor.Ui.Gum.Common.Log;

/// <summary>
/// Reusable Gum view of a <see cref="StatusLog"/>: a translucent panel with a scrollable list of
/// colored lines (newest at the bottom, auto-scrolled) and an optional Clear button.
/// </summary>
/// <remarks>
/// The view only mirrors the log; whoever owns the log writes to it. Callers position and size
/// <see cref="Root"/>, call <see cref="Update"/> every frame and <see cref="Dispose"/> when the
/// view is removed (so a longer-lived log does not keep the view alive).
/// </remarks>
public sealed class GumStatusLogView : IDisposable
{
  /// <summary>Padding inside the panel, in pixels.</summary>
  private const float PanelPadding = 8;

  /// <summary>Width of the Clear button, in pixels.</summary>
  private const float ClearButtonWidth = 70;

  /// <summary>Shown log.</summary>
  private readonly StatusLog _log;

  /// <summary>Whether a Clear button is shown.</summary>
  private readonly bool _showClearButton;

  /// <summary>Shown labels, oldest first (parallel to <see cref="StatusLog.Entries"/>).</summary>
  private readonly List<Label> _labels = [];

  /// <summary>Scrollable list; null until built.</summary>
  private ScrollViewer? _scrollViewer;

  /// <summary>Whether to scroll to the bottom on the next update (after layout).</summary>
  private bool _scrollToEndPending;

  /// <summary>Whether the view was disposed.</summary>
  private bool _disposed;

  /// <summary>
  /// Creates the view. Call <see cref="Build"/> to create its visuals.
  /// </summary>
  /// <param name="log">Log to show.</param>
  /// <param name="showClearButton">Whether to show a Clear button that empties the log.</param>
  /// <exception cref="ArgumentNullException"><paramref name="log"/> is null.</exception>
  public GumStatusLogView(StatusLog log, bool showClearButton = true)
  {
    _log = log ?? throw new ArgumentNullException(nameof(log));
    _showClearButton = showClearButton;
  }

  /// <summary>Root panel; null until <see cref="Build"/> is called. Caller sets position and size.</summary>
  public ContainerRuntime? Root { get; private set; }

  /// <summary>
  /// Builds the visuals, shows entries already in the log and starts following it.
  /// </summary>
  /// <returns><see cref="Root"/>, to be added to a parent by the caller.</returns>
  /// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
  /// <exception cref="InvalidOperationException">The view was already built.</exception>
  /// <exception cref="ObjectDisposedException">The view was disposed.</exception>
  public ContainerRuntime Build()
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    if (Root != null) throw new InvalidOperationException("Status log view was already built.");

    ContainerRuntime panel = GumUiFactory.CreatePanel();

    float reservedRight = _showClearButton ? 2 * PanelPadding + ClearButtonWidth : PanelPadding;
    _scrollViewer = new ScrollViewer
    {
      X = PanelPadding,
      Y = PanelPadding,
      Width = -(PanelPadding + reservedRight),
      WidthUnits = DimensionUnitType.RelativeToParent,
      Height = -2 * PanelPadding,
      HeightUnits = DimensionUnitType.RelativeToParent,
    };
    panel.AddChild(_scrollViewer.Visual);

    if (_showClearButton)
    {
      var clearButton = new Button
      {
        Text = "Clear",
        Width = ClearButtonWidth,
      };
      clearButton.Anchor(Anchor.TopRight);
      clearButton.X = -PanelPadding;
      clearButton.Y = PanelPadding;
      clearButton.Click += (_, _) => _log.Clear();
      panel.AddChild(clearButton.Visual);
    }

    Root = panel;

    foreach (StatusEntry entry in _log.Entries) AddLabel(entry);
    _log.Added += OnAdded;
    _log.Trimmed += OnTrimmed;
    _log.Cleared += OnCleared;

    return panel;
  }

  /// <summary>
  /// Finishes deferred work (scroll to the newest line once layout has run). Call every frame.
  /// </summary>
  public void Update()
  {
    if (!_scrollToEndPending || _scrollViewer == null) return;
    _scrollViewer.ScrollToBottom();
    _scrollToEndPending = false;
  }

  /// <summary>
  /// Stops following the log. Visuals stay where they are; remove <see cref="Root"/> separately.
  /// </summary>
  public void Dispose()
  {
    if (_disposed) return;
    _log.Added -= OnAdded;
    _log.Trimmed -= OnTrimmed;
    _log.Cleared -= OnCleared;
    _disposed = true;
  }

  /// <summary>
  /// Shows a new entry.
  /// </summary>
  /// <param name="sender">Log.</param>
  /// <param name="entry">New entry.</param>
  private void OnAdded(object? sender, StatusEntry entry) => AddLabel(entry);

  /// <summary>
  /// Removes labels of entries the log dropped.
  /// </summary>
  /// <param name="sender">Log.</param>
  /// <param name="count">Number of dropped (oldest) entries.</param>
  private void OnTrimmed(object? sender, int count) => RemoveOldestLabels(count);

  /// <summary>
  /// Removes all labels.
  /// </summary>
  /// <param name="sender">Log.</param>
  /// <param name="eventArgs">Event arguments.</param>
  private void OnCleared(object? sender, EventArgs eventArgs) => RemoveOldestLabels(_labels.Count);

  /// <summary>
  /// Adds a label for an entry and scrolls to it.
  /// </summary>
  /// <param name="entry">Entry.</param>
  private void AddLabel(StatusEntry entry)
  {
    if (_scrollViewer == null) return;
    Label label = GumUiFactory.CreateLabel(entry.Text, EditorTheme.Palette.ForSeverity(entry.Severity));
    _scrollViewer.AddChild(label);
    _labels.Add(label);
    _scrollViewer.ScrollToBottom();
    _scrollToEndPending = true; // again after layout, once the new line has its height
  }

  /// <summary>
  /// Removes the oldest labels.
  /// </summary>
  /// <param name="count">Number of labels to remove.</param>
  private void RemoveOldestLabels(int count)
  {
    if (_scrollViewer == null) return;
    int toRemove = Math.Min(count, _labels.Count);
    for (int i = 0; i < toRemove; i++) _scrollViewer.RemoveChild(_labels[i]);
    _labels.RemoveRange(0, toRemove);
  }
}
