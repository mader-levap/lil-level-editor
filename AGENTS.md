# Lost in Limbo level editor

## Project overview

`LostInLimbo.LevelEditor` is a **Stride 4.4.0** desktop application that serves as the level editor, level generator front-end and level viewer for the game *Lost in Limbo*.

It does not generate levels itself. Generation logic lives in the engine-neutral library `LostInLimbo.LevelGen` (sibling repository `../lil-level-gen`). This project's job is to:

- Configure and trigger generation (seed, map configuration, player position, chunk scheduling).
- Turn generated level **data** into Stride entities, components and scenes.
- Let the user inspect, edit and preview the result in 3D.

The solution targets **.NET 10** (`net10.0`, launcher is `net10.0-windows`).

## Repository layout

```text
lil-level-editor/
├ LostInLimbo.LevelEditor.slnx           - Solution (all projects; Windows is startup project).
├ AGENTS.md                              - This file.
├ README.md, TODO.md                     - Project notes and current tasks.
├ Bin/                                   - Build output (gitignored).
├ LostInLimbo.LevelEditor.Game/          - Stride game package: all engine code, scripts and assets.
│  ├ LostInLimbo.LevelEditor.Game.sdpkg  - Stride package manifest (asset/resource folders).
│  ├ *.cs                                - Scripts (namespace LostInLimbo.LevelEditor).
│  ├ Assets/                             - Stride assets, YAML-serialized (.sdscene, .sdmat, .sdtex, ...).
│  └ Resources/                          - Raw imported files (.dds textures, ...).
├ LostInLimbo.LevelEditor.Tests/         - xUnit test project (namespace LostInLimbo.LevelEditor.Tests).
│  ├ LostInLimbo.LevelEditor.Tests.csproj - References the Game project; net10.0.
│  └ *.cs                                - Tests.
└ LostInLimbo.LevelEditor.Windows/       - Thin WinExe launcher (Program entry point only).
   ├ LostInLimbo.LevelEditorApp.cs       - Creates and runs Stride's Game.
   └ Resources/Icon.ico, app.manifest    - Windows-only packaging files.
```

- **LostInLimbo.LevelEditor.Game** holds everything game-related. New features (scripts, components, scenes, UI) go here.
- **LostInLimbo.LevelEditor.Tests** holds xUnit tests for editor code (see *Code style → Tests*).
- **LostInLimbo.LevelEditor.Windows** is only the launcher. Do not touch it.
- Game output goes to `Bin/Windows/<Configuration>/`.
- Do not add generated or machine-specific files (`Bin/`, `obj/`, `*.user`, `.vs/`) to the repository.

## Basic commands

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

Before considering a task complete:

- Run `dotnet build LostInLimbo.LevelEditor.slnx`.
- Run `dotnet test LostInLimbo.LevelEditor.slnx`.
- Fix all warnings introduced by the change when practical. Ignore warnings that are related to Stride engine, we can't do much about these.
- The app launches and the affected scene/feature works (when practical to verify).

## Architecture rules

- **One-way dependency:** this repository consumes `LostInLimbo.LevelGen`; the library must never reference this project, Stride, or any engine.
- **No generation logic here.** Rooms, corridors, chunk layout, placement rules and randomness live in `LostInLimbo.LevelGen`. This repo only feeds it a seed/context and consumes its output.
- **Determinism stays in the library.** The editor supplies `WorldGenerationContext`-style inputs (seed, map config, player position, known chunk data); it must not compensate for or re-order generation results.
- **Isolate the conversion layer.** Keep code that maps level data to Stride objects (`Entity`, `Transform`, models, colliders) in a dedicated, clearly named area (e.g. a scene/chunk builder). Level-data types must not spread through UI/input code, and engine types must not leak into generation decisions.
- **Engine-specific code is allowed and expected here** (unlike in `lil-level-gen`): scene manipulation, input, rendering, Game Studio integration and asset work are this project's purpose.
- Chunk lifecycle (scheduling, threading, scene construction, unloading) is a shared concern with the future game; keep it structured so it can be reused.

## LevelGen package workflow

The editor consumes `LostInLimbo.LevelGen` as a **local NuGet package**, not as a `ProjectReference`:

- Pack the library from the sibling repo:

  ```bash
  dotnet pack ../lil-level-gen/src/LostInLimbo.LevelGen/LostInLimbo.LevelGen.csproj -o <local-feed>
  ```

- Reference it from `LostInLimbo.LevelEditor.Game.csproj` via `PackageReference` resolving from a local feed configured in `nuget.config`.
- Do **not** add a `ProjectReference` into `../lil-level-gen`; the repositories must stay independently buildable.
- After changing the library, re-pack (and bump its version when appropriate) before building the editor; NuGet caches aggressively.
- The wiring is a pending TODO (see `TODO.md`) — ask the user to agree on the feed location and `nuget.config` before setting it up.
- **Ask before** bumping the `LostInLimbo.LevelGen` version or changing how it is consumed.

## Stride engine

### General

- When you do not know what to do in Stride, browse official documentation and tutorials and search for similar problem on Internet. Do deep dives in engine code only as last resort, when everything else fails.
- Prefer making script and asking user to hook it up in Game Studio.

### Assets and Game Studio

- `.sd*` files under `Assets/` are **YAML serialized by Stride** (`SerializedVersion`, `Id` GUIDs, `ref!!` references). Prefer editing them through `stride studio` (Game Studio) over hand-editing.
- Never change an asset's `Id` GUID or a `SerializedVersion`. References between assets are GUID+name based; rewriting IDs silently breaks scenes and prefabs.
- Hand-editing is acceptable for small, reviewable changes (names, positions, numbers) when Game Studio is impractical — preserve indentation and the exact serialization style, and re-open the asset in Game Studio afterwards to confirm it loads.
- Asset renames/moves should be done in Game Studio so it rewrites references; if done by hand, search the whole `Assets/` tree for old GUIDs/names first.
- New assets live in `Assets/`; raw source files (textures, models to import) in `Resources/`. Both folders are declared in the `.sdpkg` — do not change `AssetFolders`/`ResourceFolders` without reason.

## Code style

### General coding rules

- Use C# file-scoped namespaces (`namespace LostInLimbo.LevelEditor;`).
- Write null-aware code and do not silence nullable warnings with `null!` without a documented reason. Note that Stride 4.4 assemblies are nullable-oblivious (no annotations), so the compiler cannot warn about engine API misuse — only our own code is checked.
- Use `sealed` for classes not intended to be inherited.
- Prefer `record` for immutable data-transfer types.
- Prefer `IReadOnlyList<T>`/arrays in APIs.
- Avoid public mutable collections where practical.
- Use explicit, descriptive names: `BuildChunkScene`, `LevelGenAdapter`.
- Avoid abbreviations except widely understood ones such as `Id`, `X`, `Y`, and `Z`.
- Avoid magic numbers; use named constants, options objects, configuration records or inspector properties.
- Use `CancellationToken` for potentially expensive or cancellable operations.
- Do not use `async void`.
- Do not use `DateTime.Now`, machine-specific paths or environment-dependent behavior in editor logic.
- Validate inputs; do not swallow exceptions; return empty collections rather than `null`.

### Scripting conventions

Reference implementation: `LostInLimbo.LevelEditor.Game/BasicCameraController.cs`.

- Gameplay/editor behavior lives in components deriving from `SyncScript` (or `AsyncScript` for long-running work), overriding `Start()` and `Update()`.
- Scripts operate on `Entity.Transform`, `Input`, and the scene; keep `Update()` cheap. Do **not** perform blocking file I/O, network calls, or generation inside `Update()` — run them async or trigger them from a menu/action.
- Expose tunable values as public properties with `[Display(name, group)]` (and `[DataMember]` where needed) so they appear in the Game Studio inspector with sensible labels.
- Use `Stride.Core.Mathematics` (`Vector3`, `Quaternion`, `Matrix`, `MathUtil`) — not `System.Numerics` — so values interoperate with transforms.
- Use `Game.UpdateTime.Elapsed` for frame-rate-independent movement, as `BasicCameraController` does.
- Runtime-generated content must be cleaned up when it is replaced or unloaded (no leaked entities/components when regenerating a chunk).

### Tests

Tests reference the Game project directly (`ProjectReference`); Stride packages flow transitively. The Stride asset compiler does not run for test builds (it is gated on executable projects), so tests only need the compiled assemblies.

Rules:
- Tests must not initialize the engine, load scenes or require graphics — keep them fast and headless. Engine-independent editor logic (adapters, serializers, conversion helpers) should be written as plain, injectable classes so it *can* be tested.
- If bug is known, create test first, run it to make sure test catches bug, then fix bug.
- Comment tests. Use Arrange/Act/Assert convention, for example `// Assert: Value is correct.`.

### Documentation

- Code should be well-commented.
- Add XML documentation comments to all classes, types and members.

### Other

- **Never commit or push.** The user reviews and commits manually.

## When uncertain

Stop and ask before implementing changes that involve:

- A new NuGet package (for this repo or LevelGen).
- A target-framework or Stride-version change.
- A `LostInLimbo.LevelGen` API or version change, or any change to how it is consumed.
- Non-trivial hand-edits of serialized `.sd*` assets, asset GUID changes, or `.sdpkg` structure changes.
- A breaking public-API change in editor code.
- Anything nondeterministic in generation-related behavior.
