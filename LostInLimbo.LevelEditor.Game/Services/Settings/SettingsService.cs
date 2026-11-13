using System;
using System.IO;
using LostInLimbo.LevelEditor.Services.Settings.Data;

namespace LostInLimbo.LevelEditor.Services.Settings;

/// <summary>
/// Interface for the editor settings service.
/// </summary>
public interface ISettingsService
{
  /// <summary>
  /// Loads the saved settings.
  /// </summary>
  /// <returns>Saved settings; <see cref="DatEditorSettings.Default"/> if there are none or they cannot be read.</returns>
  DatEditorSettings Load();

  /// <summary>
  /// Saves the settings, replacing the saved ones.
  /// </summary>
  /// <param name="settings">Settings to save.</param>
  /// <returns>True if saved, otherwise false (the failure is logged).</returns>
  bool Save(DatEditorSettings settings);
}

/// <summary>
/// Keeps the editor settings in one JSON file.
/// </summary>
/// <remarks>
/// The file location is given by the caller, so this class holds no machine-specific paths.
/// Saving writes a temporary file next to the target and then replaces the target, so an
/// interrupted save never leaves a half-written file.
/// </remarks>
public sealed class SettingsService : GameServiceBase, ISettingsService
{
  /// <summary>Suffix of the temporary file written during a save.</summary>
  private const string TempSuffix = ".tmp";

  /// <summary>
  /// Creates the service.
  /// </summary>
  /// <param name="filePath">Full path of the settings file.</param>
  /// <exception cref="ArgumentException"><paramref name="filePath"/> is null, empty or whitespace.</exception>
  public SettingsService(string filePath)
  {
    if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Settings file path must not be empty.", nameof(filePath));
    FilePath = Path.GetFullPath(filePath);
  }

  /// <summary>Full path of the settings file.</summary>
  public string FilePath { get; }

  /// <inheritdoc />
  public DatEditorSettings Load()
  {
    if (!File.Exists(FilePath)) return DatEditorSettings.Default; // first start

    try
    {
      return EditorSettingsSerializer.Deserialize(File.ReadAllText(FilePath));
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
    {
      // The file stays as it is until the next save replaces it.
      Log.Warning($"Settings: Could not load \"{FilePath}\", using defaults - {exception.Message}");
      return DatEditorSettings.Default;
    }
  }

  /// <inheritdoc />
  public bool Save(DatEditorSettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);

    string tempPath = FilePath + TempSuffix;
    try
    {
      string json = EditorSettingsSerializer.Serialize(settings);
      string? directory = Path.GetDirectoryName(FilePath);
      if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
      File.WriteAllText(tempPath, json);
      File.Move(tempPath, FilePath, overwrite: true);
      return true;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
    {
      // Saving runs on shutdown: report it, but never crash because of it.
      Log.Error($"Settings: Could not save \"{FilePath}\" - {exception.Message}");
      TryDelete(tempPath);
      return false;
    }
  }

  /// <summary>
  /// Deletes a leftover temporary file, if any; failures are ignored (best-effort cleanup).
  /// </summary>
  /// <param name="path">File to delete.</param>
  private static void TryDelete(string path)
  {
    try
    {
      if (File.Exists(path)) File.Delete(path);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      // Nothing more to do: the save itself was already reported as failed.
    }
  }
}
