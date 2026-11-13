using System.IO;
using LostInLimbo.LevelEditor.Services.Settings;
using LostInLimbo.LevelEditor.Services.Settings.Data;
using LostInLimbo.LevelGen.Data.Config.Graph;
using LostInLimbo.LevelGen.Data.Prop;

namespace LostInLimbo.LevelEditor.Tests.Services.Settings;

/// <summary>
/// Headless tests for <see cref="SettingsService"/>, on a temporary folder deleted afterwards.
/// </summary>
public sealed class SettingsServiceTests : IDisposable
{
  /// <summary>Temporary folder of this test instance.</summary>
  private readonly string _folder = Path.Combine(Path.GetTempPath(), "lil-settings-tests-" + Guid.NewGuid().ToString("N"));

  /// <summary>Settings file path inside a not yet existing sub-folder.</summary>
  private string FilePath => Path.Combine(_folder, "sub", "editor-settings.json");

  /// <summary>Deletes the temporary folder.</summary>
  public void Dispose()
  {
    if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
  }

  /// <summary>
  /// Verifies that a missing file gives the defaults.
  /// </summary>
  [Fact]
  public void Load_MissingFile_ReturnsDefault()
  {
    // Act: Load without a file.
    DatEditorSettings result = new SettingsService(FilePath).Load();

    // Assert: Defaults.
    Assert.Same(DatEditorSettings.Default, result);
  }

  /// <summary>
  /// Verifies that saved settings load back, missing folders are created, and no temporary
  /// file is left behind.
  /// </summary>
  [Fact]
  public void SaveThenLoad_RoundTrips()
  {
    // Arrange: Non-default settings.
    var settings = DatEditorSettings.Default with
    {
      LevelGen = new DatLevelGenSettings
      {
        Properties = PropMain.Default with { TextSeed = "007", Difficulty = 100 },
        Configuration = new DatConfigFormSettings { Active = true, GraphBuilder = EnGraphBuilder.Simple },
      },
    };
    var service = new SettingsService(FilePath);

    // Act: Save, then load with a new service.
    bool saved = service.Save(settings);
    DatEditorSettings result = new SettingsService(FilePath).Load();

    // Assert: Saved, equal, and only the settings file in the folder.
    Assert.True(saved);
    Assert.Equal(settings, result);
    Assert.Equal([FilePath], Directory.GetFiles(Path.GetDirectoryName(FilePath)!));
  }

  /// <summary>
  /// Verifies that a corrupt file gives the defaults and is left untouched.
  /// </summary>
  [Fact]
  public void Load_CorruptFile_ReturnsDefaultAndKeepsFile()
  {
    // Arrange: A file that is not JSON.
    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
    const string content = "{ broken";
    File.WriteAllText(FilePath, content);

    // Act: Load.
    DatEditorSettings result = new SettingsService(FilePath).Load();

    // Assert: Defaults, file unchanged.
    Assert.Same(DatEditorSettings.Default, result);
    Assert.Equal(content, File.ReadAllText(FilePath));
  }

  /// <summary>
  /// Verifies that invalid settings are not saved and the previous file stays.
  /// </summary>
  [Fact]
  public void Save_InvalidSettings_ReturnsFalseAndKeepsFile()
  {
    // Arrange: A valid saved file, then settings with an invalid difficulty.
    var service = new SettingsService(FilePath);
    service.Save(DatEditorSettings.Default);
    string before = File.ReadAllText(FilePath);
    var invalid = DatEditorSettings.Default with
    {
      LevelGen = DatLevelGenSettings.Default with { Properties = PropMain.Default with { Difficulty = -1 } },
    };

    // Act: Try to save.
    bool saved = service.Save(invalid);

    // Assert: Refused, previous file intact.
    Assert.False(saved);
    Assert.Equal(before, File.ReadAllText(FilePath));
  }

  /// <summary>
  /// Verifies that an empty path is rejected.
  /// </summary>
  [Fact]
  public void Constructor_EmptyPath_Throws()
  {
    // Act + Assert: No file to work with.
    Assert.Throws<ArgumentException>(() => new SettingsService(" "));
  }
}
