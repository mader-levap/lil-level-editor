# Lost in Limbo level editor

This is a level generator front-end, editor and viewer for a future game codenamed Lost in Limbo.

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

### External libraries

The editor uses the **LevelGen** library. See README.md in that project for information on how to wire it up.