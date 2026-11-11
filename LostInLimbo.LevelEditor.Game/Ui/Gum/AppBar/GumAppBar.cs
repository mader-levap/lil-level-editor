using System;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using LostInLimbo.LevelEditor.Services.SystemInfo;
using LostInLimbo.LevelEditor.Ui.Gum.Common;

namespace LostInLimbo.LevelEditor.Ui.Gum.AppBar;

/// <summary>
/// Gum app bar: a translucent full-width strip at the top of the window showing app name
/// and author on the left, and version and build type (colored) on the right.
/// </summary>
public sealed class GumAppBar
{
  /// <summary>Height of the app bar in pixels. Other UI starts below this inset.</summary>
  public const float Height = 28;

  /// <summary>Spacing between the right-hand labels, in pixels.</summary>
  private const float RightLabelSpacing = 10;

  /// <summary>System info source.</summary>
  private readonly ISystemInfoService _systemInfo;

  /// <summary>
  /// Creates the app bar. Call <see cref="Build"/> to create its visuals.
  /// </summary>
  /// <param name="systemInfo">System info source.</param>
  /// <exception cref="ArgumentNullException"><paramref name="systemInfo"/> is null.</exception>
  public GumAppBar(ISystemInfoService systemInfo)
  {
    _systemInfo = systemInfo ?? throw new ArgumentNullException(nameof(systemInfo));
  }

  /// <summary>Root visual of the app bar; null until <see cref="Build"/> is called.</summary>
  public ContainerRuntime? Root { get; private set; }

  /// <summary>
  /// Builds the app bar visuals and adds them to the Gum root.
  /// </summary>
  /// <remarks>Call only after <c>GumService.Default.Initialize</c> has returned.</remarks>
  /// <exception cref="InvalidOperationException">The app bar was already built.</exception>
  public void Build()
  {
    if (Root != null) throw new InvalidOperationException("App bar was already built.");

    ContainerRuntime bar = GumUiFactory.CreatePanel();
    bar.X = 0;
    bar.Y = 0;
    bar.Width = 0;
    bar.WidthUnits = DimensionUnitType.RelativeToParent; // full window width
    bar.Height = Height;

    Label appLabel = GumUiFactory.CreateLabel($"{_systemInfo.GetName()} © by {_systemInfo.GetAuthor()}", UiRgba.White);
    appLabel.Anchor(Anchor.Left);
    appLabel.X = GumUiFactory.TextMargin;

    bar.AddChild(appLabel);
    bar.AddChild(CreateRightGroup());
    bar.AddToRoot();
    Root = bar;
  }

  /// <summary>
  /// Creates the right-aligned group with version and build labels.
  /// </summary>
  /// <returns>Horizontal stack container.</returns>
  private ContainerRuntime CreateRightGroup()
  {
    var group = new ContainerRuntime
    {
      ChildrenLayout = ChildrenLayout.LeftToRightStack,
      StackSpacing = RightLabelSpacing,
    };
    group.Anchor(Anchor.Right);
    group.X = -GumUiFactory.TextMargin;
    group.Width = 0;
    group.WidthUnits = DimensionUnitType.RelativeToChildren;
    group.Height = 0;
    group.HeightUnits = DimensionUnitType.RelativeToParent;

    Label versionLabel = GumUiFactory.CreateLabel($"v. {_systemInfo.GetVersion()}", UiRgba.White);
    versionLabel.Anchor(Anchor.CenterVertically);
    group.AddChild(versionLabel);

    var buildType = _systemInfo.GetBuild();
    Label buildLabel = GumUiFactory.CreateLabel(buildType.ToString(), BuildTypeColors.For(buildType));
    buildLabel.Anchor(Anchor.CenterVertically);
    group.AddChild(buildLabel);

    return group;
  }
}
