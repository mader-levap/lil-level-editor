using System;
using System.IO;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Primitives;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;
using Stride.Core.Diagnostics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Common;

/// <summary>
/// Shared factory helpers for code-only Gum UI.
/// </summary>
/// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
public static class GumUiFactory
{
  /// <summary>Logger for this class.</summary>
  private static readonly Logger Log = GlobalLogger.GetLogger(nameof(GumUiFactory));

  /// <summary>Rows a combo box dropdown shows at most before it scrolls.</summary>
  public const int DropDownMaxVisibleRows = 8;

  /// <summary>Width and height of icon buttons, in pixels.</summary>
  public const float IconButtonSize = 48;

  /// <summary>Width and height of the icon drawn inside an icon button, in pixels.</summary>
  public const float IconSize = 36;

  /// <summary>Default horizontal margin between a panel edge and its text, in pixels.</summary>
  public const float TextMargin = 5;

  /// <summary>Title font size relative to the default font size.</summary>
  public const float TitleFontSizeFactor = 1.25f;

  /// <summary>Length of each faded end of a separator, in percent of the separator's length.</summary>
  public const float SeparatorFadePercent = 20f;

  /// <summary>Separator alpha at its very tips, relative to its color's alpha (faded ends only).</summary>
  public const float SeparatorTipAlphaFactor = 0.2f;

  /// <summary>Usual thickness of separator lines, in pixels.</summary>
  public const float SeparatorThickness = 1f;

  /// <summary>Whole parent, in percent.</summary>
  private const float FullPercent = 100f;

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
  /// The dropdown sizes itself to its items with Gum's built-in
  /// <see cref="ListBoxVisual.MakeHeightSizedToChildren"/> (Gum's default is a fixed 256 px),
  /// up to <see cref="DropDownMaxVisibleRows"/> rows; longer lists scroll. Gum never opens the
  /// list above the combo box (when it does not fit below, Gum slides it up over the combo box),
  /// so that is done on open (see <see cref="PlaceDropDownAboveIfNeeded"/>).
  /// </remarks>
  /// <exception cref="InvalidOperationException">Gum's dropdown list is not a V3 <see cref="ListBoxVisual"/>.</exception>
  public static ComboBox CreateComboBox(float width)
  {
    var comboBox = new ComboBox
    {
      Width = width,
    };
    var listVisual = comboBox.ListBox?.Visual as ListBoxVisual
      ?? throw new InvalidOperationException("Gum ComboBox dropdown is not a V3 ListBoxVisual.");
    listVisual.MakeHeightSizedToChildren();
    comboBox.DropDownOpened += OnComboBoxDropDownOpened;
    return comboBox;
  }

  /// <summary>
  /// Adjusts a just-opened combo box dropdown: removes row keyboard focus, so the selected row
  /// shows Gum's Selected state, caps the list at <see cref="DropDownMaxVisibleRows"/> rows, and
  /// moves it above the combo box when Gum could not open it below (see <see cref="CreateComboBox"/>).
  /// </summary>
  /// <param name="sender">Combo box that opened.</param>
  /// <param name="e">Event data.</param>
  private static void OnComboBoxDropDownOpened(object? sender, EventArgs e)
  {
    if (sender is not ComboBox { ListBox: { } listBox } comboBox) return;
    listBox.DoListItemsHaveFocus = false;
    CapDropDownHeight(listBox); // first: placement needs the final height
    PlaceDropDownAboveIfNeeded(listBox, comboBox);
  }

  /// <summary>
  /// Moves a dropdown list that Gum slid up over its combo box so it ends exactly at the combo
  /// box's top edge. A list opened below the combo box is left alone.
  /// </summary>
  /// <param name="listBox">Dropdown list box (visible, laid out, already capped).</param>
  /// <param name="comboBox">Combo box that owns the list.</param>
  private static void PlaceDropDownAboveIfNeeded(ListBox listBox, ComboBox comboBox)
  {
    GraphicalUiElement list = listBox.Visual;
    GraphicalUiElement box = comboBox.Visual;

    float listTop = list.AbsoluteTop;
    float newTop = DropDownSizing.ListTop(listTop, list.AbsoluteHeight, box.AbsoluteTop, box.AbsoluteTop + box.AbsoluteHeight);
    list.Y += newTop - listTop; // Y grows downward whatever the list's Y units
  }

  /// <summary>
  /// Limits the dropdown's clip container to <see cref="DropDownMaxVisibleRows"/> rows; past that
  /// the list scrolls. The row height is measured here, because it follows the theme font and
  /// rows are laid out only once the list is shown.
  /// </summary>
  /// <param name="listBox">Dropdown list box (visible and laid out).</param>
  private static void CapDropDownHeight(ListBox listBox)
  {
    if (listBox.ListBoxItems.Count == 0) return; // nothing to measure
    if (listBox.Visual is not ListBoxVisual listVisual) return; // checked in CreateComboBox

    float rowHeight = listBox.ListBoxItems[0].Visual.AbsoluteHeight;
    float maxHeight = rowHeight * DropDownMaxVisibleRows;
    if (listVisual.ClipContainerInstance.MaxHeight == maxHeight) return; // already capped (later opens)

    float clipHeightBefore = listVisual.ClipContainerInstance.AbsoluteHeight;
    listVisual.ClipContainerInstance.MaxHeight = maxHeight;

    // On the first open Gum has already measured the list uncapped and switched it to a fixed
    // (absolute) height for the popup; it restores the sized-to-children setting on close, so
    // later opens are measured with the cap. Shrink that fixed height by what the cap removed,
    // otherwise empty bars show above and below the rows.
    float removedHeight = clipHeightBefore - maxHeight;
    if (removedHeight > 0 && listVisual.HeightUnits == DimensionUnitType.Absolute)
    {
      listVisual.Height -= removedHeight;
    }
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

  /// <summary>
  /// Fills a rectangle with a top-to-bottom linear gradient over its whole height.
  /// </summary>
  /// <param name="rectangle">Rectangle to change.</param>
  /// <param name="top">Color at the top edge.</param>
  /// <param name="bottom">Color at the bottom edge.</param>
  public static void ApplyVerticalGradient(RectangleRuntime rectangle, UiRgba top, UiRgba bottom)
  {
    ApplyColor(rectangle, top); // filled, no outline; the fill color is the gradient's start color
    rectangle.UseGradient = true;
    rectangle.Red2 = bottom.Red;
    rectangle.Green2 = bottom.Green;
    rectangle.Blue2 = bottom.Blue;
    rectangle.Alpha2 = bottom.Alpha;

    // From the top edge straight down to the bottom edge.
    rectangle.GradientX1 = 0;
    rectangle.GradientX1Units = GeneralUnitType.PixelsFromSmall;
    rectangle.GradientX2 = 0;
    rectangle.GradientX2Units = GeneralUnitType.PixelsFromSmall;
    rectangle.GradientY1 = 0;
    rectangle.GradientY1Units = GeneralUnitType.PixelsFromSmall;
    rectangle.GradientY2 = 0;
    rectangle.GradientY2Units = GeneralUnitType.PixelsFromLarge;
  }

  /// <summary>
  /// Creates a thin vertical separator line. With <paramref name="fadeEnds"/>, both ends get
  /// more translucent towards the tips (<see cref="SeparatorFadePercent"/> of the length each,
  /// down to <see cref="SeparatorTipAlphaFactor"/> of the alpha).
  /// </summary>
  /// <param name="color">Line color (at full strength, in the middle).</param>
  /// <param name="thickness">Line width, in pixels.</param>
  /// <param name="fadeEnds">Whether the ends fade out.</param>
  /// <returns>
  /// Container of the given width, as tall as its parent and centered in it vertically; set a
  /// negative <c>Height</c> to shorten it at both ends (its pieces follow its height).
  /// </returns>
  /// <remarks>
  /// In a left-to-right stack whose height comes from its children, the separator still counts
  /// for the stack's width, while Gum leaves it out of the stack's height (it depends on that
  /// height), so the other children decide how tall it is.
  /// </remarks>
  public static ContainerRuntime CreateVerticalSeparator(UiRgba color, float thickness, bool fadeEnds)
  {
    var separator = new ContainerRuntime
    {
      Width = thickness,
      WidthUnits = DimensionUnitType.Absolute,
      Height = 0,
      HeightUnits = DimensionUnitType.RelativeToParent,
      YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center,
      YUnits = GeneralUnitType.PixelsFromMiddle,
      ChildrenLayout = ChildrenLayout.TopToBottomStack,
    };

    if (!fadeEnds)
    {
      RectangleRuntime line = CreateSeparatorPiece(FullPercent);
      ApplyColor(line, color);
      separator.AddChild(line);
      return separator;
    }

    UiRgba tip = color.WithAlphaScaled(SeparatorTipAlphaFactor);

    RectangleRuntime top = CreateSeparatorPiece(SeparatorFadePercent);
    ApplyVerticalGradient(top, tip, color);
    separator.AddChild(top);

    RectangleRuntime middle = CreateSeparatorPiece(FullPercent - 2 * SeparatorFadePercent);
    ApplyColor(middle, color);
    separator.AddChild(middle);

    RectangleRuntime bottom = CreateSeparatorPiece(SeparatorFadePercent);
    ApplyVerticalGradient(bottom, color, tip);
    separator.AddChild(bottom);

    return separator;
  }

  /// <summary>
  /// Creates one piece of a separator: full width, a share of the separator's height.
  /// </summary>
  /// <param name="heightPercent">Height, in percent of the separator's height.</param>
  /// <returns>Rectangle (no color yet).</returns>
  private static RectangleRuntime CreateSeparatorPiece(float heightPercent) => new()
  {
    Width = 0,
    WidthUnits = DimensionUnitType.RelativeToParent,
    Height = heightPercent,
    HeightUnits = DimensionUnitType.PercentageOfParent,
  };
}
