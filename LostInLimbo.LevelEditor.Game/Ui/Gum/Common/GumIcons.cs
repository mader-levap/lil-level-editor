using System;
using System.IO;

namespace LostInLimbo.LevelEditor.Ui.Gum.Common;

/// <summary>
/// Icon files used by Gum UI and their location on disk.
/// </summary>
/// <remarks>
/// Gum loads images as plain files (not through Stride's content pipeline). The Game project
/// copies <c>Resources/Graphics/Icons/**/*.png</c> to <c>GumContent/Icons/</c> in the output
/// folder, keeping subfolders; the PNGs are the only copy (no Stride texture assets). Icon
/// constants are paths relative to that folder (for example <c>Main/Gears.png</c>). Paths are
/// resolved against the application base directory and passed to Gum as absolute paths, so
/// Gum's own relative-directory setting does not matter.
/// </remarks>
public static class GumIcons
{
  /// <summary>Folder (relative to the application base directory) that holds the icon files.</summary>
  public static readonly string ContentFolder = Path.Combine("GumContent", "Icons");

  /// <summary>Gears icon (level generation overlay).</summary>
  public const string Gears = "Main/Gears.png";
  /// <summary>Info icon (level information overlay).</summary>
  public const string Info = "Main/Info.png";

  /// <summary>Start/resume generation icon.</summary>
  public const string Start = "Gen/Start.png";
  /// <summary>Single generation step icon.</summary>
  public const string Step = "Gen/Step.png";
  /// <summary>Pause generation icon.</summary>
  public const string Pause = "Gen/Pause.png";
  /// <summary>Stop generation icon.</summary>
  public const string Stop = "Gen/Stop.png";
  /// <summary>Start generation loop icon.</summary>
  public const string Loop = "Gen/Loop.png";
  /// <summary>Stop generation loop icon.</summary>
  public const string LoopStop = "Gen/LoopStop.png";

  /// <summary>
  /// Resolves the absolute path of an icon file in the given base directory.
  /// </summary>
  /// <param name="baseDirectory">Application base directory.</param>
  /// <param name="iconFile">Icon file name, for example <see cref="Gears"/>.</param>
  /// <returns>Absolute path of the icon file.</returns>
  /// <exception cref="ArgumentException">An argument is null, empty or whitespace.</exception>
  public static string ResolvePath(string baseDirectory, string iconFile)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
    ArgumentException.ThrowIfNullOrWhiteSpace(iconFile);
    return Path.GetFullPath(Path.Combine(baseDirectory, ContentFolder, iconFile));
  }

  /// <summary>
  /// Resolves the absolute path of an icon file in the running application's base directory.
  /// </summary>
  /// <param name="iconFile">Icon file name, for example <see cref="Gears"/>.</param>
  /// <returns>Absolute path of the icon file.</returns>
  public static string ResolvePath(string iconFile) => ResolvePath(AppContext.BaseDirectory, iconFile);
}
