using System;
using System.IO;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Primitives;
using Gum.GueDeriving;
using Gum.Wireframe;
using Stride.Core.Diagnostics;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;

namespace LostInLimbo.LevelEditor.Ui.Gum.Common;

/// <summary>
/// Shared factory helpers for code-only Gum UI.
/// </summary>
/// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
public static class GumUiFactory
{
  /// <summary>Logger for this class.</summary>
  private static readonly Logger Log = GlobalLogger.GetLogger(nameof(GumUiFactory));

  /// <summary>Width and height of icon buttons, in pixels (same as the former UGS buttons).</summary>
  public const float IconButtonSize = 48;

  /// <summary>Width and height of the icon drawn inside an icon button, in pixels.</summary>
  public const float IconSize = 36;

  /// <summary>Default horizontal margin between a panel edge and its text, in pixels.</summary>
  public const float TextMargin = 5;

  /// <summary>Title font size relative to the default font size.</summary>
  public const float TitleFontSizeFactor = 1.25f;

  /// <summary>
  /// Creates a container with a translucent background that fills it.
  /// </summary>
  /// <returns>Container; size and position are set by the caller.</returns>
  /// <remarks>
  /// <c>HasEvents</c> is enabled so the Gum cursor reports the panel in <c>VisualOver</c>;
  /// that is what makes <see cref="GumFocus.IsPointerOverUi"/> block camera right-look over
  /// otherwise non-interactive panels.
  /// </remarks>
  public static ContainerRuntime CreatePanel()
  {
    var panel = new ContainerRuntime
    {
      HasEvents = true,
    };

    var background = new RectangleRuntime();
    background.Dock(Dock.Fill);
    ApplyColor(background, EditorTheme.Palette.PanelBackground);
    panel.AddChild(background);

    return panel;
  }

  /// <summary>
  /// Creates a square push button showing an icon.
  /// </summary>
  /// <param name="iconFile">Icon file name from <see cref="GumIcons"/>.</param>
  /// <param name="toolTip">Tooltip text; also shown as the button text if the icon is missing.</param>
  /// <returns>Button.</returns>
  public static Button CreateIconButton(string iconFile, string toolTip)
  {
    var button = new Button();
    SetUpIconButton(button, iconFile, toolTip);
    return button;
  }

  /// <summary>
  /// Creates a square toggle button showing an icon. Gum's checked state marks it as selected.
  /// </summary>
  /// <param name="iconFile">Icon file name from <see cref="GumIcons"/>.</param>
  /// <param name="toolTip">Tooltip text; also shown as the button text if the icon is missing.</param>
  /// <returns>Toggle button.</returns>
  public static ToggleButton CreateIconToggleButton(string iconFile, string toolTip)
  {
    var button = new ToggleButton();
    SetUpIconButton(button, iconFile, toolTip);
    return button;
  }

  /// <summary>
  /// Adds the icon sprite and tooltip to a button. A missing icon file is logged and the
  /// tooltip text is shown instead, so a broken install still has usable buttons.
  /// </summary>
  /// <param name="button">Button to set up.</param>
  /// <param name="iconFile">Icon file name from <see cref="GumIcons"/>.</param>
  /// <param name="toolTip">Tooltip text.</param>
  private static void SetUpIconButton(ButtonBase button, string iconFile, string toolTip)
  {
    // Gum's default button visuals size height relative to children, which turns Height into
    // extra padding around the content; force a fixed square instead.
    button.Width = IconButtonSize;
    button.WidthUnits = DimensionUnitType.Absolute;
    button.Height = IconButtonSize;
    button.HeightUnits = DimensionUnitType.Absolute;
    button.ToolTip = toolTip;

    string iconPath = GumIcons.ResolvePath(iconFile);
    if (!File.Exists(iconPath))
    {
      Log.Error($"Gum icon not found: {iconPath}");
      SetButtonText(button, toolTip);
      return;
    }

    SetButtonText(button, string.Empty);
    var sprite = new SpriteRuntime
    {
      SourceFileName = iconPath,
      Width = IconSize,
      WidthUnits = DimensionUnitType.Absolute,
      Height = IconSize,
      HeightUnits = DimensionUnitType.Absolute,
    };
    sprite.Anchor(Anchor.Center);
    button.Visual.AddChild(sprite);
  }

  /// <summary>
  /// Changes the icon and tooltip of a button created by <see cref="CreateIconButton"/> or
  /// <see cref="CreateIconToggleButton"/>. A missing icon file is logged and the icon is kept.
  /// </summary>
  /// <param name="button">Icon button to change.</param>
  /// <param name="iconFile">Icon file name from <see cref="GumIcons"/>.</param>
  /// <param name="toolTip">Tooltip text.</param>
  /// <exception cref="ArgumentNullException"><paramref name="button"/> is null.</exception>
  public static void SetIconButtonIcon(ButtonBase button, string iconFile, string toolTip)
  {
    ArgumentNullException.ThrowIfNull(button);
    button.ToolTip = toolTip;

    string iconPath = GumIcons.ResolvePath(iconFile);
    if (!File.Exists(iconPath))
    {
      Log.Error($"Gum icon not found: {iconPath}");
      return;
    }

    foreach (var child in button.Visual.Children)
    {
      if (child is not SpriteRuntime sprite) continue;
      sprite.SourceFileName = iconPath;
      return;
    }

    // Button was created without an icon (its file was missing at creation): show the tooltip text.
    Log.Error($"Gum icon button has no icon sprite; cannot show {iconPath}");
    SetButtonText(button, toolTip);
  }

  /// <summary>
  /// Sets the text of a push or toggle button.
  /// </summary>
  /// <param name="button">Button to change.</param>
  /// <param name="text">New text.</param>
  private static void SetButtonText(ButtonBase button, string text)
  {
    switch (button)
    {
      case ToggleButton toggleButton: toggleButton.Text = text; break;
      case Button pushButton: pushButton.Text = text; break;
    }
  }

  /// <summary>
  /// Creates a label with the given text and color.
  /// </summary>
  /// <param name="text">Label text.</param>
  /// <param name="color">Text color.</param>
  /// <returns>Label.</returns>
  public static Label CreateLabel(string text, UiRgba color)
  {
    var label = new Label
    {
      Text = text,
    };
    ApplyTextColor(label, color);
    return label;
  }

  /// <summary>
  /// Creates a combo box whose open dropdown shows the current selection as a filled row.
  /// </summary>
  /// <param name="width">Width in pixels.</param>
  /// <returns>Combo box; add items and set the selection as usual.</returns>
  /// <remarks>
  /// On open, Gum gives the selected row keyboard focus, and a list row's Focused state wins
  /// over Selected: it has no fill, only the focus bar, which <c>EditorTheme</c> hides. So the
  /// selection was invisible. Clearing the rows' focus right after opening lets Gum show its
  /// Selected state (filled with <c>Primary</c>) instead.
  /// Known limit: arrow keys in the open dropdown focus the newly selected row again (Gum does
  /// that after raising <c>SelectionChanged</c>, so it can't be undone from that event), and the
  /// selection is unmarked until the dropdown is reopened. Accepted (mouse-driven editor).
  /// The dropdown is also resized on open to fit its items (Gum's default is a fixed 128 px),
  /// up to <see cref="DropDownSizing.DefaultMaxVisibleRows"/> rows; longer lists scroll.
  /// </remarks>
  public static ComboBox CreateComboBox(float width)
  {
    var comboBox = new ComboBox
    {
      Width = width,
    };
    comboBox.DropDownOpened += OnComboBoxDropDownOpened;
    return comboBox;
  }

  /// <summary>
  /// Adjusts a just-opened combo box dropdown: removes row keyboard focus, so the selected row
  /// shows Gum's Selected state, and fits the list height to its items (see <see cref="CreateComboBox"/>).
  /// </summary>
  /// <param name="sender">Combo box that opened.</param>
  /// <param name="e">Event data.</param>
  private static void OnComboBoxDropDownOpened(object? sender, EventArgs e)
  {
    if (sender is not ComboBox { ListBox: { } listBox }) return;
    listBox.DoListItemsHaveFocus = false;
    FitDropDownHeight(listBox, ((ComboBox)sender).Visual.AbsoluteTop);
  }

  /// <summary>
  /// Sets the dropdown list height to its rows' height plus the list's own chrome, capped at
  /// <see cref="DropDownSizing.DefaultMaxVisibleRows"/> rows. A list Gum opened above the combo
  /// box is moved so it ends exactly at the combo box's top edge.
  /// </summary>
  /// <param name="listBox">Dropdown list box (visible, laid out; a popup placed in pixels from the top).</param>
  /// <param name="comboBoxTop">Top of the combo box, in window pixels.</param>
  private static void FitDropDownHeight(ListBox listBox, float comboBoxTop)
  {
    if (listBox.ListBoxItems.Count == 0) return; // nothing to measure; keep Gum's default

    GraphicalUiElement list = listBox.Visual;
    float chromeHeight = Math.Max(0, list.AbsoluteHeight - listBox.ClipContainer.AbsoluteHeight);
    float contentHeight = listBox.InnerPanel.AbsoluteHeight;
    float rowHeight = listBox.ListBoxItems[0].Visual.AbsoluteHeight;

    float listTop = list.AbsoluteTop;
    float newHeight = DropDownSizing.Compute(contentHeight, chromeHeight, rowHeight);
    float newTop = DropDownSizing.ListTopAfterResize(listTop, newHeight, comboBoxTop);

    list.HeightUnits = DimensionUnitType.Absolute;
    list.Height = newHeight;
    list.Y += newTop - listTop; // the popup's Y counts pixels from the top
  }

  /// <summary>
  /// Creates a bold title label, larger than normal text and centered horizontally in its
  /// parent (also inside a top-to-bottom stack).
  /// </summary>
  /// <param name="text">Title text.</param>
  /// <returns>Label.</returns>
  public static Label CreateTitleLabel(string text)
  {
    Label title = CreateLabel(text, EditorTheme.Palette.TextPrimary);
    if (title.TextComponent is TextRuntime textRuntime)
    {
      textRuntime.IsBold = true;
      textRuntime.FontSize = (int)MathF.Round(textRuntime.FontSize * TitleFontSizeFactor);
    }

    title.Anchor(Anchor.Top); // centers horizontally; the stack still controls the vertical position
    return title;
  }

  /// <summary>
  /// Creates one form row: a fixed-width label followed by a control, both centered
  /// vertically, so rows built with the same width line up as two columns.
  /// </summary>
  /// <param name="labelText">Label text.</param>
  /// <param name="control">Control shown after the label (input, value label, ...).</param>
  /// <param name="labelWidth">Width of the label column, in pixels.</param>
  /// <returns>Horizontal stack panel.</returns>
  public static StackPanel CreateFormRow(string labelText, FrameworkElement control, float labelWidth)
  {
    var row = new StackPanel
    {
      Orientation = Orientation.Horizontal,
    };

    Label label = CreateLabel(labelText, EditorTheme.Palette.TextPrimary);
    label.Width = labelWidth;
    label.WidthUnits = DimensionUnitType.Absolute;
    label.Anchor(Anchor.CenterVertically);
    row.AddChild(label);

    control.Anchor(Anchor.CenterVertically);
    row.AddChild(control);
    return row;
  }

  /// <summary>
  /// Sets the text color of a label.
  /// </summary>
  /// <param name="label">Label to change.</param>
  /// <param name="color">New text color.</param>
  public static void ApplyTextColor(Label label, UiRgba color)
  {
    if (label.TextComponent is not TextRuntime text) return; // custom visual without a text runtime
    text.Red = color.Red;
    text.Green = color.Green;
    text.Blue = color.Blue;
    text.Alpha = color.Alpha;
  }

  /// <summary>
  /// Sets the fill color of a rectangle.
  /// </summary>
  /// <param name="rectangle">Rectangle to change.</param>
  /// <param name="color">New fill color.</param>
  /// <remarks>
  /// Do not touch stroke channels here: on Skia, <c>RectangleRuntime</c> stays on the legacy
  /// single-slot model unless a stroke renderable is set, so fill and stroke share one color.
  /// Setting <c>StrokeAlpha = 0</c> after the fill made the whole rectangle transparent.
  /// </remarks>
  public static void ApplyColor(RectangleRuntime rectangle, UiRgba color)
  {
    rectangle.IsFilled = true;
    rectangle.FillRed = color.Red;
    rectangle.FillGreen = color.Green;
    rectangle.FillBlue = color.Blue;
    rectangle.FillAlpha = color.Alpha;
    rectangle.StrokeWidth = 0; // no outline (default stroke is a white 1px line)
  }
}
