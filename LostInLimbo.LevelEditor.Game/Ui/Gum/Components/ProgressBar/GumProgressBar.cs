using System;
using System.Collections.Generic;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Ui.Gum.Common;
using LostInLimbo.LevelEditor.Ui.Gum.Theme;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.ProgressBar;

/// <summary>
/// Reusable Gum progress bar: a track with an eased fill, diagonal bands scrolling inside the
/// fill while animating ("barber pole") and the percentage centered on top.
/// </summary>
/// <remarks>
/// Gum Forms has no progress bar control, so this one is built from plain runtimes. Callers add
/// <see cref="Root"/> to a parent, feed the value with <see cref="SetTarget"/> or
/// <see cref="Reset"/> and call <see cref="Update"/> every frame while it is visible.
/// Math lives in <see cref="ProgressBarMath"/>.
/// </remarks>
public sealed class GumProgressBar
{
  /// <summary>Fill easing rate (see <see cref="ProgressBarMath.Approach"/>), per second.</summary>
  private const float FillEasingRate = 8f;

  /// <summary>Width of one band, in pixels.</summary>
  private const float BandWidth = 10f;

  /// <summary>Distance between the starts of two neighbouring bands, in pixels.</summary>
  private const float BandPeriod = 24f;

  /// <summary>Band tilt, in degrees.</summary>
  private const float BandRotation = 30f;

  /// <summary>Band height relative to the bar height, so tilted bands still cover the bar.</summary>
  private const float BandHeightFactor = 3f;

  /// <summary>Band scroll speed, in pixels per second.</summary>
  private const float BandSpeed = 30f;

  /// <summary>Extra bands beyond the bar width, so tilted bands also cover both ends.</summary>
  private const int ExtraBands = 3;

  /// <summary>Full width of the fill, in percent of the bar.</summary>
  private const float FullPercent = 100f;

  /// <summary>Band rectangles, left to right.</summary>
  private readonly List<RectangleRuntime> _bands = [];

  /// <summary>Clipping container of the fill; null until built.</summary>
  private ContainerRuntime? _fill;

  /// <summary>Percentage label; null until built.</summary>
  private Label? _label;

  /// <summary>Target completion (0..1).</summary>
  private float _target;

  /// <summary>Shown completion (0..1), eased toward <see cref="_target"/>.</summary>
  private float _shown;

  /// <summary>Current band scroll offset, in pixels (0..<see cref="BandPeriod"/>).</summary>
  private float _bandOffset;

  /// <summary>Whether bands scroll.</summary>
  private bool _animating;

  /// <summary>Root container; null until <see cref="Build"/> is called.</summary>
  public ContainerRuntime? Root { get; private set; }

  /// <summary>
  /// Builds the visuals.
  /// </summary>
  /// <param name="width">Bar width, in pixels.</param>
  /// <param name="height">Bar height, in pixels.</param>
  /// <returns><see cref="Root"/>, to be added to a parent and positioned by the caller.</returns>
  /// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
  /// <exception cref="InvalidOperationException">The bar was already built.</exception>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is not positive.</exception>
  public ContainerRuntime Build(float width, float height)
  {
    if (Root != null) throw new InvalidOperationException("Progress bar was already built.");
    if (!(width > 0)) throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
    if (!(height > 0)) throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");

    EditorPalette palette = EditorTheme.Palette;

    var root = new ContainerRuntime
    {
      Width = width,
      WidthUnits = DimensionUnitType.Absolute,
      Height = height,
      HeightUnits = DimensionUnitType.Absolute,
    };

    var track = new RectangleRuntime();
    track.Dock(Dock.Fill);
    GumUiFactory.ApplyColor(track, palette.ProgressTrack);
    root.AddChild(track);

    // Fill: clips the bands, so they only show over the filled part.
    var fill = new ContainerRuntime
    {
      ClipsChildren = true,
      Width = 0,
      WidthUnits = DimensionUnitType.PercentageOfParent,
      Height = 0,
      HeightUnits = DimensionUnitType.RelativeToParent,
    };
    var fillBackground = new RectangleRuntime();
    fillBackground.Dock(Dock.Fill);
    GumUiFactory.ApplyColor(fillBackground, palette.ProgressFill);
    fill.AddChild(fillBackground);

    // Bands: tall tilted rectangles rotated around their centers, starting one period left of
    // the bar, so scrolling right never uncovers the left end.
    int bandCount = (int)MathF.Ceiling(width / BandPeriod) + ExtraBands;
    for (int i = 0; i < bandCount; i++)
    {
      var band = new RectangleRuntime
      {
        Width = BandWidth,
        WidthUnits = DimensionUnitType.Absolute,
        Height = height * BandHeightFactor,
        HeightUnits = DimensionUnitType.Absolute,
        XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center,
        YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center,
        YUnits = GeneralUnitType.PixelsFromMiddle,
        Rotation = BandRotation,
      };
      GumUiFactory.ApplyColor(band, palette.ProgressBand);
      fill.AddChild(band);
      _bands.Add(band);
    }
    root.AddChild(fill);

    Label label = GumUiFactory.CreateLabel(ProgressBarMath.FormatPercent(0f), palette.TextPrimary);
    label.Anchor(Anchor.Center);
    root.AddChild(label);

    _fill = fill;
    _label = label;
    Root = root;
    ApplyVisuals();
    return root;
  }

  /// <summary>
  /// Sets the completion the fill eases toward.
  /// </summary>
  /// <param name="completion">Completion (clamped to 0..1). A lower value than shown snaps.</param>
  public void SetTarget(float completion)
  {
    _target = ProgressBarMath.Clamp01(completion);
  }

  /// <summary>
  /// Shows a completion immediately, without easing.
  /// </summary>
  /// <param name="completion">Completion (clamped to 0..1).</param>
  public void Reset(float completion)
  {
    _target = ProgressBarMath.Clamp01(completion);
    _shown = _target;
    ApplyVisuals();
  }

  /// <summary>
  /// Starts or stops band scrolling (for example, stopped while paused).
  /// </summary>
  /// <param name="animating">Whether bands scroll.</param>
  public void SetAnimating(bool animating)
  {
    _animating = animating;
  }

  /// <summary>
  /// Advances the fill easing and band scrolling. Call every frame while visible.
  /// </summary>
  /// <param name="deltaSeconds">Elapsed time since the last frame, in seconds.</param>
  public void Update(float deltaSeconds)
  {
    if (Root == null) return;

    _shown = ProgressBarMath.Approach(_shown, _target, deltaSeconds, FillEasingRate);
    if (_animating && deltaSeconds > 0)
      _bandOffset = ProgressBarMath.WrapOffset(_bandOffset + BandSpeed * deltaSeconds, BandPeriod);

    ApplyVisuals();
  }

  /// <summary>
  /// Writes the shown value and band offset to the visuals.
  /// </summary>
  private void ApplyVisuals()
  {
    if (_fill == null || _label == null) return;

    // Only touch changed properties: every Gum property change triggers a layout pass.
    float fillWidth = _shown * FullPercent;
    if (_fill.Width != fillWidth) _fill.Width = fillWidth;

    string text = ProgressBarMath.FormatPercent(_shown);
    if (_label.Text != text) _label.Text = text;

    for (int i = 0; i < _bands.Count; i++)
    {
      // Band centers: one period left of the bar, then every period, shifted by the offset.
      float x = (i - 1) * BandPeriod + _bandOffset;
      if (_bands[i].X != x) _bands[i].X = x;
    }
  }
}
