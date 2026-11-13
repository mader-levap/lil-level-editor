using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using LostInLimbo.LevelEditor.Services.Settings.Data;
using LostInLimbo.LevelGen.Data.Config;

namespace LostInLimbo.LevelEditor.Services.Settings;

/// <summary>
/// Converts editor settings to and from JSON text.
/// </summary>
/// <remarks>
/// <para>
/// Follows the library's JSON conventions (camelCase names, indented output, enums as member
/// names, case-insensitive reading), with two deliberate differences: unknown members are
/// ignored, so older and newer editor builds can share one file, and missing sections keep their
/// defaults.
/// </para>
/// <para>Only strings cross this boundary; file reading and writing stay with the callers.</para>
/// </remarks>
public static class EditorSettingsSerializer
{
  /// <summary>Serializer options shared by reading and writing, so the format stays consistent.</summary>
  private static readonly JsonSerializerOptions Options = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
  };

  /// <summary>
  /// Serializes settings into JSON.
  /// </summary>
  /// <param name="settings">Settings to write; the properties are validated first.</param>
  /// <returns>JSON text.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="settings"/> is null.</exception>
  /// <exception cref="ArgumentException">The saved properties violate their invariants.</exception>
  public static string Serialize(DatEditorSettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);
    Validate(settings);
    return JsonSerializer.Serialize(settings, Options);
  }

  /// <summary>
  /// Deserializes JSON into settings and validates them.
  /// </summary>
  /// <param name="json">JSON text written by <see cref="Serialize"/> or by hand.</param>
  /// <returns>Settings; members missing from the JSON keep their defaults.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
  /// <exception cref="ArgumentException">
  /// The JSON is empty or malformed, has a different settings version, or holds invalid
  /// properties (including a different properties config version).
  /// </exception>
  public static DatEditorSettings Deserialize(string json)
  {
    ArgumentNullException.ThrowIfNull(json);
    if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON must not be empty.", nameof(json));

    DatEditorSettings? settings;
    try
    {
      settings = JsonSerializer.Deserialize<DatEditorSettings>(json, Options);
    }
    catch (JsonException exception)
    {
      throw new ArgumentException($"JSON is not valid editor settings: {exception.Message}", nameof(json), exception);
    }

    if (settings == null) throw new ArgumentException("JSON deserialized to null.", nameof(json));
    if (settings.SettingsVersion != DatEditorSettings.CurrentVersion)
    {
      throw new ArgumentException(
        $"Settings version {settings.SettingsVersion} is different than the supported version {DatEditorSettings.CurrentVersion}.",
        nameof(json));
    }

    Validate(settings);
    return settings;
  }

  /// <summary>
  /// Checks the parts of the settings that have invariants.
  /// </summary>
  /// <param name="settings">Settings to check.</param>
  /// <exception cref="ArgumentException">A section is null, or the properties are invalid.</exception>
  private static void Validate(DatEditorSettings settings)
  {
    // An explicit JSON null would bypass the defaults.
    DatLevelGenSettings levelGen = settings.LevelGen
      ?? throw new ArgumentException("Level generation settings must not be null.", nameof(settings));
    if (levelGen.Properties == null) throw new ArgumentException("Saved properties must not be null.", nameof(settings));
    if (levelGen.Configuration == null) throw new ArgumentException("Saved configuration form must not be null.", nameof(settings));

    if (levelGen.Properties.ConfigVersion != CnfLevelGeneration.CurrentConfigVersion)
    {
      throw new ArgumentException(
        $"Saved properties have config version {levelGen.Properties.ConfigVersion}; the supported version is {CnfLevelGeneration.CurrentConfigVersion}.",
        nameof(settings));
    }

    try
    {
      levelGen.Properties.Validate();
    }
    catch (ArgumentException exception)
    {
      throw new ArgumentException($"Saved properties are invalid: {exception.Message}", nameof(settings), exception);
    }

    if (!Enum.IsDefined(levelGen.Configuration.GraphBuilder))
      throw new ArgumentException($"Unknown graph builder {levelGen.Configuration.GraphBuilder}.", nameof(settings));
  }
}
