# Lost in Limbo level editor

This is level generator, editor and viewer for future game codenamed Lost in Limbo.

## Commands

All commands must be issued from root directory.

Build application:

```bash
dotnet build LostInLimbo.LevelEditor.slnx
```

Building the Game project also runs the **Stride asset compiler**; asset errors surface here, not only at runtime.

Start application:

```bash
dotnet run --project LostInLimbo.LevelEditor.Windows
```

Open **Stride Game Studio** for user (asset/scene editor):

```bash
stride studio
```

Run tests:

```bash
dotnet test LostInLimbo.LevelEditor.slnx
```

## Debugging

For development we use Visual Studio Code with C# Dev Kit extension.
- Always build application first to make sure PDB match. Make sure it is debug build.
- Start the game (from terminal or Game Studio).
- Attach in VS Code: `Ctrl+Shift+D` -> select "Attach to running game" -> press `F5` → in the process picker find `LostInLimbo.LevelEditor.Windows`.


## Resources

### External libraries

Editor uses **LevelGen** library. See README.md in that project for information how to wire it up.