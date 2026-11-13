using System.Collections.Generic;
using LostInLimbo.LevelGen.Data.Prop;

namespace LostInLimbo.LevelEditor.Ui.Gum.App.LevelGen.Data;

/// <summary>
/// A named difficulty choice shown in the level generation form.
/// </summary>
/// <param name="Name">Display name.</param>
/// <param name="Value">Value written to <see cref="PropMain.Difficulty"/> (0..<see cref="PropMain.MaxDifficulty"/>).</param>
public sealed record DifficultyPreset(string Name, int Value);

/// <summary>
/// The difficulty choices offered by the level generation form.
/// </summary>
/// <remarks>Placeholder table; tune names and values here only.</remarks>
public static class DifficultyPresets
{
  /// <summary>Presets in display order.</summary>
  public static IReadOnlyList<DifficultyPreset> All { get; } =
  [
    new("Easy", 0),
    new("Medium", 50),
    new("Hard", PropMain.MaxDifficulty),
  ];

  /// <summary>
  /// Index of the preset selected by default: the one matching
  /// <see cref="PropMain.Default"/>'s difficulty, or 0 if none matches.
  /// </summary>
  public static int DefaultIndex { get; } = FindIndex(PropMain.Default.Difficulty);

  /// <summary>
  /// Finds the index of the preset with the given value.
  /// </summary>
  /// <param name="value">Difficulty value.</param>
  /// <returns>Index of the matching preset, or 0 if none matches.</returns>
  public static int FindIndex(int value)
  {
    for (int i = 0; i < All.Count; i++)
    {
      if (All[i].Value == value) return i;
    }
    return 0;
  }
}
