# TODO

## Current

- Issues:
  - When we are in input like EditText, keys still affect movement.
    - For now do some hacky solution
    - Report it on Stride discord

- UI:
  - Add panel where we can set props. Default values are from `PropMain.Default`. That panel will vanish during generation and will be back once it finishes.
    - How to save to disk? Does Stride have any support? I want last prop setup to survive restart of app. I guess in worst case we can just save/load json file with props from directory where game is running.
  - Add panel with graph visualization.
    - Where it should go?
    - Research how to make canvas that I can draw on freely that can update every tick.


## Later

- Test other gen buttons, only start and step is done. Rest is untested because right now generation is practically instant and so it is over before we can use pause/stop buttons.
- Resizabe window.
- Check how to package the game so you can run it standalone.

- Loading config/props from JSON file.
