# TODO

## Current

- UI:
  - Adjust props form.
  - Make better difficulty preset.
  - Persist last props setup across app restarts.
    - How to save to disk? Does Stride have any support?
    - I guess in worst case we can just save/load json file with props from directory where game is running.
    - If no file present, fill with defaults from `PropMain.Default`.
  - Add panel with graph visualization.
    - Where it should go?
    - Research how to make canvas that I can draw on freely that can update every tick.

## Later

- Test other gen buttons, only start and step is done. Rest is untested because right now generation is practically instant and so it is over before we can use pause/stop buttons.
  - These buttons are set without caring about actual state of generator. Not good.
- Resizabe window. Works, but janky. Seems to be engine issue.
- Check how to package the game so you can run it standalone.

- Loading config/props from JSON file.
