using System;
using LostInLimbo.LevelEditor.Services.SystemInfo;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Graphics;

namespace LostInLimbo.LevelEditor.Ui.Main;

/// <summary>
/// User interface resources.
/// </summary>
public static class UiRes
{
  /// <summary>Logger for this class.</summary>
  private static readonly Logger Log = GlobalLogger.GetLogger(nameof(UiRes));

  /// <summary>Script used in UI creation.</summary>
  public static StartupScript Script { get; private set; } = default!;

  /// <summary>System info service.</summary>
  public static ISystemInfoService SystemInfoService { get; private set; } = default!;
  /// <summary>Font used by text.</summary>
  public static SpriteFont Font { get; private set; } = default!;
  /// <summary>Button for unselected overlay.</summary>
  public static SpriteSheet SpriteBtnUnselected { get; private set; } = default!;
  /// <summary>Button for selected overlay.</summary>
  public static SpriteSheet SpriteBtnSelected { get; private set; } = default!;

  /// <summary>
  /// Initialize UI resources.
  /// </summary>
  /// <param name="script">Script used in UI creation.</param>
  /// <exception cref="InvalidOperationException">Failed to get ISystemInfoService.</exception>
  public static void InitResources(StartupScript script)
  {
    Script = script;

    SystemInfoService = Script.Services.GetService<ISystemInfoService>()
      ?? throw new InvalidOperationException("ISystemInfoService was not registered.");

    Font = Script.Content.Load<SpriteFont>("UI/Fonts/BasicFont");
    SpriteBtnUnselected = Script.Content.Load<SpriteSheet>("/LostInLimbo.LevelEditor/Graphics/Ui/BtnUnselected");
    SpriteBtnSelected = Script.Content.Load<SpriteSheet>("/LostInLimbo.LevelEditor/Graphics/Ui/BtnSelected");
  }
}
