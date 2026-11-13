using System;
using Gum.Forms.Controls;
using LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Data;
using LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Helpers;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelGen.Data.Prop;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen;

/// <summary>
/// Gum form with the generation properties (seed, difficulty, many themes). The host places the
/// built rows inside its own panel and reads the properties with <see cref="BuildProps"/>.
/// </summary>
/// <remarks>
/// Only builds and reads visuals; property logic lives in <see cref="LevelGenPropsBuilder"/> and
/// <see cref="DifficultyPresets"/>.
/// </remarks>
public sealed class GumPropertiesPanel
{
  /// <summary>Spacing between form rows, in pixels.</summary>
  private const float RowSpacing = 6;

  /// <summary>Width of the form's label column, in pixels.</summary>
  private const float LabelWidth = 210;

  /// <summary>Width of the form's input column, in pixels.</summary>
  private const float InputWidth = 200;

  /// <summary>Form header text.</summary>
  private const string Title = "Properties";

  /// <summary>Width of the form's content (label and input columns), in pixels.</summary>
  public const float ContentWidth = LabelWidth + InputWidth;

  /// <summary>Form rows; null until built.</summary>
  private StackPanel? _rows;

  /// <summary>Seed input.</summary>
  private TextBox? _seedBox;

  /// <summary>Difficulty preset choice.</summary>
  private ComboBox? _difficultyBox;

  /// <summary>Many-themes check box.</summary>
  private CheckBox? _manyThemesBox;

  /// <summary>
  /// Text of the seed box (null before <see cref="Build"/>). Setting it before the form is
  /// built does nothing.
  /// </summary>
  public string? Seed
  {
    get => _seedBox?.Text;
    set
    {
      if (_seedBox != null) _seedBox.Text = value;
    }
  }

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
    if (_rows != null) throw new InvalidOperationException("Properties form was already built.");

    var rows = new StackPanel
    {
      Spacing = RowSpacing,
    };

    // Header centered above the rows (the stack is as wide as its widest row).
    rows.AddChild(GumUiFactory.CreateTitleLabel(Title));

    _seedBox = new TextBox
    {
      Width = InputWidth,
      Placeholder = "empty = current time",
    };
    rows.AddChild(CreateFormRow("Seed:", _seedBox));

    _difficultyBox = GumUiFactory.CreateComboBox(InputWidth);
    var difficultyItems = _difficultyBox.Items
      ?? throw new InvalidOperationException("Gum ComboBox has no item list.");
    foreach (DifficultyPreset preset in DifficultyPresets.All) difficultyItems.Add(preset.Name);
    _difficultyBox.SelectedIndex = DifficultyPresets.DefaultIndex;
    rows.AddChild(CreateFormRow("Difficulty:", _difficultyBox));

    _manyThemesBox = new CheckBox
    {
      Text = string.Empty,
      IsChecked = PropMain.Default.ManyThemes,
    };
    rows.AddChild(CreateFormRow("Can have many themes:", _manyThemesBox));

    _rows = rows;
    return rows;
  }

  /// <summary>
  /// Releases keyboard focus and closes popups, so a hidden form keeps no input.
  /// </summary>
  public void ReleaseInput()
  {
    if (_seedBox != null) _seedBox.IsFocused = false;
    if (_difficultyBox != null) _difficultyBox.IsDropDownOpen = false;
  }

  /// <summary>
  /// Shows the given properties in the form. A difficulty without a preset selects the first
  /// preset. Does nothing before <see cref="Build"/>.
  /// </summary>
  /// <param name="props">Properties to show.</param>
  /// <exception cref="ArgumentNullException"><paramref name="props"/> is null.</exception>
  public void Apply(PropMain props)
  {
    ArgumentNullException.ThrowIfNull(props);
    if (_seedBox != null) _seedBox.Text = props.TextSeed ?? string.Empty;
    if (_difficultyBox != null) _difficultyBox.SelectedIndex = DifficultyPresets.FindIndex(props.Difficulty);
    if (_manyThemesBox != null) _manyThemesBox.IsChecked = props.ManyThemes;
  }

  /// <summary>
  /// Builds generation properties from the form.
  /// </summary>
  /// <returns>Properties.</returns>
  public PropMain BuildProps()
  {
    int index = _difficultyBox?.SelectedIndex ?? DifficultyPresets.DefaultIndex;
    if (index < 0 || index >= DifficultyPresets.All.Count) index = DifficultyPresets.DefaultIndex;

    return LevelGenPropsBuilder.Build(
      _seedBox?.Text,
      DifficultyPresets.All[index].Value,
      _manyThemesBox?.IsChecked == true);
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
