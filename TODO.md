# TODO

## Current
- F5 generates map, as if loop (so modifies text seed) but only single map. If loop is active or some other generation is still ongoing or it ended in error, F5 does nothing.
  Note F5 should work even outside level gen overlay and if you open that overlay, log should show correct additions.


## Later

- Make better difficulty preset.
- Fix issues in level generation library.
- First video for LiL on this, post in discord on my server.

## Do it some eon
- Profiling tests to make sure there are no memory leaks, especially with continuous generation.

- Test other gen buttons, only start and step is done. Rest is untested because right now generation is practically instant and so it is over before we can use pause/stop buttons.
- Resizabe window. Works, but janky. Seems to be engine issue.
- Check how to package the game so you can run it standalone.

- Loading config/props from JSON file.
