using LostInLimbo.LevelEditor.Services.Settings;
using LostInLimbo.LevelEditor.Services.Settings.Data;
using LostInLimbo.LevelGen.Data.Config.Graph;
using LostInLimbo.LevelGen.Data.Prop;

namespace LostInLimbo.LevelEditor.Tests.Services.Settings;

/// <summary>
/// Headless tests for <see cref="EditorSettingsSerializer"/>.
/// </summary>
public sealed class EditorSettingsSerializerTests
{
  /// <summary>Settings with every saved value different from its default.</summary>
  private static readonly DatEditorSettings Sample = DatEditorSettings.Default with
  {
    LevelGen = new DatLevelGenSettings
    {
      Properties = PropMain.Default with { TextSeed = "mordor", Difficulty = 50, ManyThemes = true },
      Configuration = new DatConfigFormSettings { Active = true, GraphBuilder = EnGraphBuilder.Linear },
    },
  };

  /// <summary>
  /// Verifies that writing and reading back keeps every value.
  /// </summary>
  [Fact]
  public void RoundTrip_KeepsAllValues()
  {
    // Act: Write and read back.
    DatEditorSettings result = EditorSettingsSerializer.Deserialize(EditorSettingsSerializer.Serialize(Sample));

    // Assert: Same settings.
    Assert.Equal(Sample, result);
  }

  /// <summary>
  /// Verifies the written format: camelCase names and enums as member names.
  /// </summary>
  [Fact]
  public void Serialize_UsesCamelCaseAndEnumNames()
  {
    // Act: Write.
    string json = EditorSettingsSerializer.Serialize(Sample);

    // Assert: Expected names and the builder as text.
    Assert.Contains("\"settingsVersion\": 1", json);
    Assert.Contains("\"levelGen\":", json);
    Assert.Contains("\"textSeed\": \"mordor\"", json);
    Assert.Contains("\"graphBuilder\": \"Linear\"", json);
  }

  /// <summary>
  /// Verifies that sections and members missing from the file keep their defaults.
  /// </summary>
  [Fact]
  public void Deserialize_MissingMembers_KeepDefaults()
  {
    // Arrange: Only the version and the seed.
    const string json = """{ "settingsVersion": 1, "levelGen": { "properties": { "textSeed": "x" } } }""";

    // Act: Read.
    DatEditorSettings result = EditorSettingsSerializer.Deserialize(json);

    // Assert: Seed read, everything else at defaults.
    Assert.Equal(PropMain.Default with { TextSeed = "x" }, result.LevelGen.Properties);
    Assert.Equal(DatConfigFormSettings.Default, result.LevelGen.Configuration);
  }

  /// <summary>
  /// Verifies that members this editor does not know are ignored (files from newer builds).
  /// </summary>
  [Fact]
  public void Deserialize_UnknownMembers_AreIgnored()
  {
    // Arrange: An unknown section and an unknown member in a known section.
    const string json = """{ "settingsVersion": 1, "futureArea": { "a": 1 }, "levelGen": { "configuration": { "active": true, "extra": 2 } } }""";

    // Act: Read.
    DatEditorSettings result = EditorSettingsSerializer.Deserialize(json);

    // Assert: Known values read.
    Assert.True(result.LevelGen.Configuration.Active);
  }

  /// <summary>
  /// Verifies that unusable files are rejected.
  /// </summary>
  /// <param name="json">File content.</param>
  [Theory]
  [InlineData("")]
  [InlineData("{ not json")]
  [InlineData("""{ "settingsVersion": 2 }""")]
  [InlineData("""{ "settingsVersion": 1, "levelGen": { "properties": { "difficulty": -1 } } }""")]
  [InlineData("""{ "settingsVersion": 1, "levelGen": { "properties": { "configVersion": 99 } } }""")]
  [InlineData("""{ "settingsVersion": 1, "levelGen": { "configuration": { "graphBuilder": 1 } } }""")]
  [InlineData("""{ "settingsVersion": 1, "levelGen": { "configuration": { "graphBuilder": "Nope" } } }""")]
  [InlineData("""{ "settingsVersion": 1, "levelGen": null }""")]
  public void Deserialize_InvalidContent_Throws(string json)
  {
    // Act + Assert: Rejected as an argument error.
    Assert.Throws<ArgumentException>(() => EditorSettingsSerializer.Deserialize(json));
  }
}
