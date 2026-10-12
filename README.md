# Lost in Limbo level editor

This is level generator, editor and viewer for future game codenamed Lost in Limbo.

## Commands

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
stride studio LostInLimbo.LevelEditor.Game/LostInLimbo.LevelEditor.Game.sdpkg
```

Run tests:

```bash
dotnet test LostInLimbo.LevelEditor.slnx
```

## Resources

### External libraries

Editor uses **LevelGen** library. See README.md in that project for information how to wire it up.