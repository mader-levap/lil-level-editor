using LostInLimbo.LevelGen.Data.Prop;

namespace LostInLimbo.LevelEditor.Ui.Gum.LevelGen;

/// <summary>
/// Turns the values of the level generation form into <see cref="PropMain"/>.
/// </summary>
/// <remarks>
/// Values are copied as entered; validation and any interpretation (for example a blank seed
/// meaning "use current time") stay in <c>LostInLimbo.LevelGen</c>.
/// </remarks>
public static class LevelGenPropsBuilder
{
  /// <summary>
  /// Builds generation properties from form values.
  /// </summary>
  /// <param name="seedText">Seed text box content; blank or whitespace becomes <see langword="null"/> (time seed).</param>
  /// <param name="difficulty">Difficulty value of the selected preset.</param>
  /// <param name="manyThemes">Many-themes check box state.</param>
  /// <returns>Properties based on <see cref="PropMain.Default"/> with the form values applied.</returns>
  public static PropMain Build(string? seedText, int difficulty, bool manyThemes)
  {
    return PropMain.Default with
    {
      TextSeed = string.IsNullOrWhiteSpace(seedText) ? null : seedText,
      Difficulty = difficulty,
      ManyThemes = manyThemes,
    };
  }
}
