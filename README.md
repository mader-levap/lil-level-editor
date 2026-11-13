# Lost in Limbo level editor

This is a level generator front-end, editor and viewer for a future game codenamed Lost in Limbo.

## Stack

- **Language**: C#
- **Game engine**: Stride 4.4.1
- **Other**:
  - **UI library**: Gum

### How to update engine

First download new version in Stride Launcher.

In `LostInLimbo.LevelEditor.Game.csproj` updat all `<PackageReference>` that have `Include="'Stride.*"` to new version. Then rebuild from scratch, run tests, run application.

## Commands

All commands must be issued from the root directory.

Build application:

```bash
dotnet build LostInLimbo.LevelEditor.slnx
```

Building the Game project also runs the **Stride asset compiler**; asset errors surface here, not only at runtime.

Start application:

```bash
dotnet run --project LostInLimbo.LevelEditor.Windows
```

Open **Stride Game Studio** (the asset/scene editor):

```bash
stride studio
```

Run tests:

```bash
dotnet test LostInLimbo.LevelEditor.slnx
```

## Debugging

For development we use Visual Studio Code with the C# Dev Kit extension.

- Always build the application first to make sure the PDBs match. Make sure it is debug build.
- Start the game (from terminal or Game Studio).
- Attach in VS Code: `Ctrl+Shift+D` → select "Attach to running game" → press `F5` → in the process picker find `LostInLimbo.LevelEditor.Windows`.

## Resources

### Our libraries

The editor uses the **LevelGen** library. See `README.md` in that project for information on how to release it.
To update library on this side, after release change its version in `LostInLimbo.LevelEditor.Game.csproj`, for example:
```<PackageReference Include="LostInLimbo.LevelGen" Version="0.0.5" />```
Build and run.

### Other libraries

For user interface we use **Gum**. It is open-source (MIT) game UI: WYSIWYG tool + layout engine + Forms controls, WPF-like API.

## Notes

- UI is code-only Gum (`LostInLimbo.LevelEditor.Game/Ui/Gum/`); Stride's built-in UI is not used. Gum draws after the 3D pass, so post-processing does not affect it.
- UI theme is code-only: `Ui/Gum/Theme/` (`EditorPalette` holds all colors; `EditorTheme.Apply` runs right after Gum init). UI font is **Inter** (SIL Open Font License, `Resources/Fonts/Inter/OFL.txt`).
- UI icons are plain PNGs in `Resources/Graphics/Icons/` (subfolders allowed), copied to `GumContent/Icons/` on build. Do not create Stride texture assets for them.
