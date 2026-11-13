# Lost in Limbo level editor

## Project overview

`LostInLimbo.LevelEditor` is a **Stride 4.4.2** desktop application that serves as the level editor, level generator front-end and level viewer for the game codenamed *Lost in Limbo*.

It does not generate levels itself. Generation logic lives in the engine-neutral library `LostInLimbo.LevelGen` (sibling repository `../lil-level-gen`). This project's job is to:

- Configure and trigger generation (seed, configuration).
- Turn generated level **data** into Stride entities, components and scenes.
- Let the user inspect, edit and preview the result in 3D.

The solution targets **.NET 10** (`net10.0-windows7.0` for Game/Tests — required by Gum.Stride's GPU Skia path; launcher `net10.0-windows7.0`).

## Repository layout

```text
lil-level-editor/
├ LostInLimbo.LevelEditor.slnx           - Solution (all projects; Windows is startup project).
├ AGENTS.md                              - This file.
├ README.md, TODO.md                     - Project notes and current tasks.
├ Bin/                                   - Build output (gitignored).
├ LostInLimbo.LevelEditor.Game/          - Stride game package: all engine code, scripts and assets.
│  ├ LostInLimbo.LevelEditor.Game.sdpkg  - Stride package manifest (asset/resource folders).
│  ├ Assets/                             - Stride assets, YAML-serialized (.sdscene, .sdmat, .sdtex, ...).
│  ├ Input/                              - Input handling.
│  ├ Main/                               - Main start point.
│  └ Resources/                          - Raw imported files, usually binary (.dds textures, ...).
│  └ Services/                           - Services.
│  └ Ui/                                 - All scripts related to user interface.
├ LostInLimbo.LevelEditor.Tests/         - xUnit test project (namespace LostInLimbo.LevelEditor.Tests).
│  ├ LostInLimbo.LevelEditor.Tests.csproj - References the Game project; net10.0-windows7.0.
└ LostInLimbo.LevelEditor.Windows/       - Thin WinExe launcher (Program entry point only).
   ├ LostInLimbo.LevelEditorApp.cs       - Creates and runs Stride's Game.
   └ Resources/Icon.ico, app.manifest    - Windows-only packaging files.
```

- **LostInLimbo.LevelEditor.Game** holds everything game-related. New features (scripts, components, scenes, UI) go here.
- **LostInLimbo.LevelEditor.Tests** holds xUnit tests for editor code.
- **LostInLimbo.LevelEditor.Windows** is only the launcher. Do not touch it.
- Game output goes to `Bin/Windows/<Configuration>/`.
- Do not add generated or machine-specific files (`Bin/`, `obj/`, `*.user`, `.vs/`) to the repository.

## Basic commands

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

Before considering a task complete:

- Run `dotnet build LostInLimbo.LevelEditor.slnx`.
- Run `dotnet test LostInLimbo.LevelEditor.slnx`.
- Fix all warnings introduced by the change when practical. Ignore warnings that are related to Stride engine; we can't do much about these.
- The app launches and the affected scene/feature works (when practical to verify).

## Architecture rules

- **One-way dependency:** this repository consumes `LostInLimbo.LevelGen`; the library must never reference this project, Stride, or any engine.
- **No generation logic here.** Rooms, corridors, chunk layout, placement rules and randomness live in `LostInLimbo.LevelGen`. This repo only feeds it a seed/context and consumes its output.
- **Determinism stays in the library.** The editor supplies `WorldGenerationContext`-style inputs (seed, map config etc); it must not compensate for or re-order generation results.
- **Isolate the conversion layer.** Keep code that maps level data to Stride objects (`Entity`, `Transform`, models, colliders) in a dedicated, clearly named area (e.g. a scene builder). Level-data types must not spread through UI/input code, and engine types must not leak into generation decisions.
- **Engine-specific code is allowed and expected here** (unlike in `lil-level-gen`): scene manipulation, input, rendering, Game Studio integration and asset work are this project's purpose.
- **Input conventions.** Gameplay scripts read keyboard input through `GameInput` (capture-gated); mouse, gamepad and touch are read from `Input` directly. UI is code-only Gum (`Ui/Gum/`). Use `GumFocus` for keyboard-capture (`IsKeyboardCaptured`: focused text box, open dropdown list, or a hovered element implementing `IClaimsKeyboardWhileHovered`) and pointer-over queries; Gum text boxes blur on outside click by themselves (`LosesFocusWhenClickedOff`); overlays release focus they hold when hidden.

## LevelGen package workflow

The editor consumes `LostInLimbo.LevelGen` as a **local NuGet package**, not as a `ProjectReference`:

- Pack the library from the sibling repo:

  ```bash
  dotnet pack ../lil-level-gen/src/LostInLimbo.LevelGen/LostInLimbo.LevelGen.csproj -o <local-feed>
  ```

- Reference it from `LostInLimbo.LevelEditor.Game.csproj` via `PackageReference` resolving from a local feed configured in `nuget.config`.
- Do **not** add a `ProjectReference` into `../lil-level-gen`; the repositories must stay independently buildable.
- After changing the library, re-pack (and bump its version when appropriate) before building the editor; NuGet caches aggressively.
- **Ask before** bumping the `LostInLimbo.LevelGen` version or changing how it is consumed.

## Code style

### General coding rules

- Use C# file-scoped namespaces (`namespace LostInLimbo.LevelEditor;`).
- Write null-aware code and do not silence nullable warnings with `null!` without a documented reason. Exception: filling class fields that are initialized later (not in constructor), typically in `Start()`.
  Note that Stride 4.4 assemblies are nullable-oblivious (no annotations), so the compiler cannot warn about engine API misuse — only our own code is checked.
- Use `sealed` for classes not intended to be inherited.
- Prefer `record` for immutable data-transfer types.
- Prefer `IReadOnlyList<T>`/arrays in APIs.
- Avoid public mutable collections where practical.
- Observe standard C# conventions about naming of variables, classes etc.
- Use explicit, descriptive names: `BuildChunkScene`, `LevelGenAdapter`.
- Avoid abbreviations except widely understood ones such as `Id`, `X`, `Y`, and `Z`.
- Avoid magic numbers; use named constants, options objects, configuration records or inspector properties.
- Do not use `async void`.
- Do not use `DateTime.Now`, machine-specific paths or environment-dependent behavior in editor logic.
- Validate inputs; do not swallow exceptions; return empty collections rather than `null`.
- Use `#region <very short description>` for longer code files where multiple logically distinct functionalities lives.

### Scripting conventions

Reference implementation: `LostInLimbo.LevelEditor.Game/Input/BasicCameraController.cs`.

- Gameplay/editor behavior lives in components deriving from `SyncScript` (or `AsyncScript` for long-running work), overriding `Start()` and `Update()`.
- Scripts operate on `Entity.Transform`, `Input`, and the scene; keep `Update()` cheap. Do **not** perform blocking file I/O, network calls, or generation inside `Update()` — run them async or trigger them from a menu/action.
- Expose tunable values as public properties with `[Display(name, group)]` (and `[DataMember]` where needed) so they appear in the Game Studio inspector with sensible labels.
- Use `Stride.Core.Mathematics` (`Vector3`, `Quaternion`, `Matrix`, `MathUtil`) — not `System.Numerics` — so values interoperate with transforms.
- Use `Game.UpdateTime.Elapsed` for frame-rate-independent movement, as `BasicCameraController` does.
- Runtime-generated content must be cleaned up when it is replaced or unloaded (no leaked entities/components when regenerating a chunk).

### Documentation

- Code should be well-commented.
- Add XML documentation comments to all classes, types and members. Exception: overridden methods.

## Stride engine

### General

- A lot of work will have to be done by user by nature of 3D game application.
- When you do not know what to do in Stride, browse official documentation and tutorials and search for similar problems on the Internet. Do deep dives in engine code only as a last resort, when everything else fails.
  - Stride Community Toolkit: https://stride3d.github.io/stride-community-toolkit/index.html
  - Gum UI: https://docs.flatredball.com/gum
- Prefer writing a script and asking the user to hook it up in Game Studio.

### Assets and Game Studio

- `.sd*` files under `Assets/` are **YAML serialized by Stride** (`SerializedVersion`, `Id` GUIDs, `ref!!` references). Prefer editing them through `stride studio` (Game Studio) over hand-editing.
- Never change an asset's `Id` GUID or a `SerializedVersion`. References between assets are GUID+name based; rewriting IDs silently breaks scenes and prefabs.
- Hand-editing is acceptable for small, reviewable changes (names, positions, numbers) when Game Studio is impractical — preserve indentation and the exact serialization style, and re-open the asset in Game Studio afterwards to confirm it loads.
- Asset renames/moves should be done in Game Studio so it rewrites references; if done by hand, search the whole `Assets/` tree for old GUIDs/names first.
- New assets live in `Assets/`; raw source files (textures, models to import) in `Resources/`. Both folders are declared in the `.sdpkg` — do not change `AssetFolders`/`ResourceFolders` without reason.

## Gum

### Directories

```text
Ui/Gum/
├ App/                                   - All panels, screens and overlays specific to application.
├ Common/                                - Common classes.
├ Components/                            - Our custom components.
└ Theme/                                 - Theme data.
```

If our own custom components are needed, place them in `/Ui/Gum/Components/<name>`.

### Component conventions

Reference implementation: `Ui/Gum/Components/GraphVisualizer/`.

- Split a component into plain, engine-free logic (geometry, layout, state machines fed a per-frame input record) with tests, and a thin Gum shell (`Gum*` classes) that only reads Gum/Stride and draws.
- Components know only their own generic model; callers translate app data with an adapter in `Ui/Gum/App/` (e.g. `LevelGraphVisAdapter`).
- Custom drawing: a renderable deriving from `SkiaGum.Renderables.RenderableShapeBase` inside an `InteractiveGue`. The canvas arrives in window space and unclipped: translate and clip yourself. Keep Skia objects (paints, fonts) for the component's lifetime and dispose them in the component's `Dispose`; use `SKPathBuilder` + `Detach()` for paths.
- Poll pointer input from `GumService.Default.Cursor` each frame; `Cursor.VisualOver == element` is the "pointer over" test. An element that uses keys while hovered implements `IClaimsKeyboardWhileHovered`.
- Scroll bars: use `GumScrollBarSizing.ApplyThickness` (15 px, square arrow buttons) for standalone `ScrollBar`s and for a `ScrollViewer`'s `VerticalScrollBar`.

## Tests

Tests reference the Game project directly (`ProjectReference`); Stride packages flow transitively. The Stride asset compiler does not run for test builds (it is gated on executable projects), so tests only need the compiled assemblies.

Rules:
- Tests must not initialize the engine, load scenes or require graphics — keep them fast and headless. Engine-independent editor logic (adapters, serializers, conversion helpers) should be written as plain, injectable classes so it *can* be tested.
- If a bug is known, create a test first, run it to make sure the test catches the bug, then fix the bug.
- Comment tests. Use Arrange/Act/Assert convention, for example `// Assert: Value is correct.`

### Live testing

You can do live testing (opening app on your own and doing app tests), but ask for permission first before hijacking mouse/keyboard. Permission lasts until end of session or revocation.

Notes and quirks:
- After starting app wait for 5 seconds. App starts quickly.
- Window resize is janky (and sometimes outright buggy). Wait second or so after resize before taking screenshot.

## Debugging

### Crashes

Environment is set up for crashes. See `DOTNET_DbgMiniDumpName` to know where full dumps are.

When you run the app yourself and it may crash, start it like this (PowerShell) so the dump gets written completely:

```powershell
$p = Start-Process dotnet -ArgumentList 'run','--project','LostInLimbo.LevelEditor.Windows' -PassThru -RedirectStandardError <scratchpad>\stderr.txt
$p.WaitForExit()
```

- Do **not** pipe the app's output (for example `dotnet run ... | Select-Object -Last 15`). Doing that once produced a truncated dump (about 1 GB instead of about 1.5 GB) that `dotnet-dump` could not open.
- Wait for the process to exit before you touch the dump: the runtime writes it before the process exits.
- `stderr.txt` contains the managed stack trace of an unhandled exception. A native crash has details only in the dump.
- Check the dump with `dotnet-dump analyze <dump> -c "pe" -c "exit"`. If it fails with "did not contain a system info stream", the dump is truncated.
- Analyze a dump before rebuilding, so the `.pdb` files in `Bin/Windows/<Configuration>/` still match it.

## Other

- **Never commit or push.** The user reviews and commits manually. Human has last word.

## When uncertain

Stop and ask before implementing changes that involve:

- A new NuGet package (for this repo or LevelGen).
- A target-framework or Stride-version change.
- A `LostInLimbo.LevelGen` API or version change, or any change to how it is consumed.
- Non-trivial hand-edits of serialized `.sd*` assets, asset GUID changes, or `.sdpkg` structure changes.
- A breaking public-API change in editor code.
- Anything nondeterministic in generation-related behavior.
