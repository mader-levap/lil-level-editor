# TODO

## Current

- UI:
  - ~~Migrate current UI to Gum.~~ Done (all phases, see `GUM-MIGRATION.md`). Custom code-only theme done (dark palette, Inter, no focus bars).
  - ~~Add panel where we can set props. Default values are from `PropMain.Default`. That panel will vanish during generation and will be back once it finishes.~~ Done (Gum LevelGen overlay).
    - ~~Difficulty level... we need combobox.~~ Done (Easy/Medium/Hard presets).
  - Persist last props setup across app restarts (separate task after Gum Phase 4). How to save to disk? Does Stride have any support? I guess in worst case we can just save/load json file with props from directory where game is running.
  - Add panel with graph visualization.
    - Where it should go?
    - Research how to make canvas that I can draw on freely that can update every tick.

## Later

- Test other gen buttons, only start and step is done. Rest is untested because right now generation is practically instant and so it is over before we can use pause/stop buttons.
- Resizabe window.
- Check how to package the game so you can run it standalone.

- Loading config/props from JSON file.
