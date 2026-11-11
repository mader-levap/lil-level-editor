using System;
using System.IO;

namespace LostInLimbo.LevelEditor.Ui.Gum.Theme;

/// <summary>
/// Font files used by the editor theme and their location on disk.
/// </summary>
/// <remarks>
/// The UI font is Inter (SIL Open Font License, see <c>OFL.txt</c> next to the files). The Game
/// project copies <c>Resources/Fonts/**</c> to <c>GumContent/Fonts/</c> in the output folder;
/// <see cref="EditorTheme"/> registers the files with Gum under <see cref="FamilyName"/>.
/// </remarks>
public static class EditorFonts
{
  /// <summary>Font family name the theme registers and Gum text styles refer to.</summary>
  public const string FamilyName = "Inter";

  /// <summary>Folder (relative to the application base directory) that holds the Inter files.</summary>
  public static readonly string ContentFolder = Path.Combine("GumContent", "Fonts", "Inter");

  /// <summary>Regular cut.</summary>
  public const string RegularFile = "Inter-Regular.ttf";

  /// <summary>Bold cut (used for bold text such as titles).</summary>
  public const string BoldFile = "Inter-Bold.ttf";

  /// <summary>License shipped with the font.</summary>
  public const string LicenseFile = "OFL.txt";

  /// <summary>
  /// Resolves the absolute path of a font folder file in the given base directory.
  /// </summary>
  /// <param name="baseDirectory">Application base directory.</param>
  /// <param name="fileName">File name, for example <see cref="RegularFile"/>.</param>
  /// <returns>Absolute path of the file.</returns>
  /// <exception cref="ArgumentException">An argument is null, empty or whitespace.</exception>
  public static string ResolvePath(string baseDirectory, string fileName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
    ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
    return Path.GetFullPath(Path.Combine(baseDirectory, ContentFolder, fileName));
  }

  /// <summary>
  /// Resolves the absolute path of a font folder file in the running application's base directory.
  /// </summary>
  /// <param name="fileName">File name, for example <see cref="RegularFile"/>.</param>
  /// <returns>Absolute path of the file.</returns>
  public static string ResolvePath(string fileName) => ResolvePath(AppContext.BaseDirectory, fileName);
}
