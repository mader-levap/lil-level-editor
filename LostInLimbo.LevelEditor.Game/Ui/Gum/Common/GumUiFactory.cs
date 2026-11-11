using System;
using System.IO;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Primitives;
using Gum.GueDeriving;
using Gum.Wireframe;
using Stride.Core.Diagnostics;

namespace LostInLimbo.LevelEditor.Ui.Gum.Common;

/// <summary>
/// Shared factory helpers for code-only Gum UI (the Gum counterpart of <c>UiBuilderBase</c>).
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

  /// <summary>Alpha of translucent panel backgrounds (matches the former UGS panels).</summary>
  public const byte PanelBackgroundAlpha = 64;

  /// <summary>Default horizontal margin between a panel edge and its text, in pixels.</summary>
  public const float TextMargin = 5;

  /// <summary>Title font size relative to the default font size.</summary>
  public const float TitleFontSizeFactor = 1.25f;

  /// <summary>Translucent black used behind panels.</summary>
  public static UiRgba PanelBackgroundColor { get; } = UiRgba.Black with { Alpha = PanelBackgroundAlpha };

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
    ApplyColor(background, PanelBackgroundColor);
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
  /// Creates a bold title label, larger than normal text and centered horizontally in its
  /// parent (also inside a top-to-bottom stack).
  /// </summary>
  /// <param name="text">Title text.</param>
  /// <returns>Label.</returns>
  public static Label CreateTitleLabel(string text)
  {
    Label title = CreateLabel(text, UiRgba.White);
    if (title.TextComponent is TextRuntime textRuntime)
    {
      textRuntime.IsBold = true;
      textRuntime.FontSize = (int)MathF.Round(textRuntime.FontSize * TitleFontSizeFactor);
    }

    title.Anchor(Anchor.Top); // centers horizontally; the stack still controls the vertical position
    return title;
  }

  /// <summary>
  /// Creates one form row: a fixed-width white label followed by a control, both centered
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

    Label label = CreateLabel(labelText, UiRgba.White);
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
