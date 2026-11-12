# TODO

## Current
- Style stuff:
  - Find files with namespaces not matching directory path and fix them.

## Later

- Add config form where selected config params are manually set and override whatever was generated from props form.
  - Active: if false, this form is not active and won't modify config generated from props form.
  - Builder: combobox, pick one builder. Create list from enum, so any addition in LevelGen library will be automatically shown here.

- Make better difficulty preset.
- Persist state of forms in level generation overlay across app restarts.
  - How to save to disk? Does Stride have any support?
  - I guess in worst case we can just save/load json file with props from directory where game is running.
  - If no file present, fill with defaults from `PropMain.Default`.

- First video for LiL on this, post in discord on my server.

## Do it some eon
- Profiling tests to make sure there are no memory leaks, especially with continuous generation.

- Test other gen buttons, only start and step is done. Rest is untested because right now generation is practically instant and so it is over before we can use pause/stop buttons.
- Resizabe window. Works, but janky. Seems to be engine issue.
- Check how to package the game so you can run it standalone.

- Loading config/props from JSON file.
