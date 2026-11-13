using System;
using System.Collections.Generic;
using Gum.Forms.Controls;
using LostInLimbo.LevelEditor.Services.LevelGen;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelGen.Data.Config.Graph;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen;

/// <summary>
/// Gum form with configuration fields that override the configuration generated from the
/// properties form. Nothing is overridden until the form is made active; the fields below the
/// Active check box are disabled while it is not.
/// </summary>
/// <remarks>
/// Only builds and reads visuals; the overriding itself is done by
/// <see cref="LevelGenConfigOverrides"/>.
/// </remarks>
public sealed class GumConfigPanel
{
  /// <summary>Form header text.</summary>
  private const string Title = "Configuration";

  /// <summary>Spacing between form rows, in pixels.</summary>
  private const float RowSpacing = 6;

  /// <summary>Width of the form's label column, in pixels.</summary>
  private const float LabelWidth = 90;

  /// <summary>Width of the form's input column, in pixels.</summary>
  private const float InputWidth = 140;

  /// <summary>Width of the form's content (label and input columns), in pixels.</summary>
  public const float ContentWidth = LabelWidth + InputWidth;

  /// <summary>Graph builders in combo box order.</summary>
  private static readonly IReadOnlyList<EnGraphBuilder> GraphBuilders = Enum.GetValues<EnGraphBuilder>();

  /// <summary>Form rows; null until built.</summary>
  private StackPanel? _rows;

  /// <summary>Check box that turns the overrides on.</summary>
  private CheckBox? _activeBox;

  /// <summary>Graph builder choice.</summary>
  private ComboBox? _builderBox;

  /// <summary>
  /// Whether the form rows are shown. Is <see langword="false"/> before <see cref="Build"/>.
  /// </summary>
  public bool IsVisible
  {
    get => _rows?.IsVisible == true;
    set
    {
      if (_rows != null) _rows.IsVisible = value;
    }
  }

  /// <summary>
  /// Builds the form: a centered header above the form rows. The caller adds it to its panel and
  /// positions it.
  /// </summary>
  /// <returns>Stack panel with the header and the form rows.</returns>
  /// <exception cref="InvalidOperationException">The form was already built.</exception>
  public StackPanel Build()
  {
    if (_rows != null) throw new InvalidOperationException("Configuration form was already built.");

    var rows = new StackPanel
    {
      Spacing = RowSpacing,
    };

    // Header centered above the rows (the stack is as wide as its widest row).
    rows.AddChild(GumUiFactory.CreateTitleLabel(Title));

    _activeBox = new CheckBox
    {
      Text = string.Empty,
      IsChecked = false,
    };
    _activeBox.Checked += (_, _) => ApplyActive();
    _activeBox.Unchecked += (_, _) => ApplyActive();
    rows.AddChild(CreateFormRow("Active:", _activeBox));

    _builderBox = GumUiFactory.CreateComboBox(InputWidth);
    var builderItems = _builderBox.Items
      ?? throw new InvalidOperationException("Gum ComboBox has no item list.");
    foreach (EnGraphBuilder builder in GraphBuilders) builderItems.Add(builder.ToString());
    _builderBox.SelectedIndex = 0;
    rows.AddChild(CreateFormRow("Builder:", _builderBox));

    _rows = rows;
    ApplyActive();
    return rows;
  }

  /// <summary>
  /// Releases keyboard focus and closes popups, so a hidden form keeps no input.
  /// </summary>
  public void ReleaseInput()
  {
    if (_builderBox != null) _builderBox.IsDropDownOpen = false;
  }

  /// <summary>
  /// Builds the configuration overrides from the form.
  /// </summary>
  /// <returns>
  /// Overrides; <see cref="LevelGenConfigOverrides.None"/> while the form is not active (or not
  /// built).
  /// </returns>
  public LevelGenConfigOverrides BuildOverrides()
  {
    if (_activeBox?.IsChecked != true || _builderBox == null) return LevelGenConfigOverrides.None;

    int index = _builderBox.SelectedIndex;
    EnGraphBuilder? builder = index >= 0 && index < GraphBuilders.Count ? GraphBuilders[index] : null;
    return new LevelGenConfigOverrides(GraphBuilder: builder);
  }

  /// <summary>
  /// Enables the override fields only while the form is active.
  /// </summary>
  private void ApplyActive()
  {
    bool active = _activeBox?.IsChecked == true;
    if (_builderBox == null) return;
    if (!active) _builderBox.IsDropDownOpen = false;
    _builderBox.IsEnabled = active;
  }

  /// <summary>
  /// Creates one form row with this form's label column width.
  /// </summary>
  /// <param name="labelText">Label text.</param>
  /// <param name="input">Input control.</param>
  /// <returns>Horizontal stack panel.</returns>
  private static StackPanel CreateFormRow(string labelText, FrameworkElement input) =>
    GumUiFactory.CreateFormRow(labelText, input, LabelWidth);
}
