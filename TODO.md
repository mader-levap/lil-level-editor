# TODO

- We need slightly fancier UI for our generator screen:
  - It is likely there is better way to pool results from generation than checking every tick. Investigate.
  - More buttons for pause/cancel.
  - Buttons need visible disabled feedback.
  - Maybe small window with result?

- Check how to implement generation subsystem in Stride (apparently Stride's equivalent is called services?), so we can have a placeholder for code that handles calling generation and processing results. What does this subsystem do?
  - Loads a JSON file that contains all data needed for generation. In future, we will have UI to be able to set at least some config.
  - Runs generation asynchronously. Note that it won't generate everything at once, only some chunks.
  - After generation, we spawn entities in Stride based on generated data.
  - Rinse and repeat until generator says it is over.
  - Inform user generation is finished.
  - We will likely refine it later, we need proper chunk handling. But that can come later; there is no point in trying to do everything at once.

- Check how to package the game so you can run it standalone.

- Add a test for RunGenScript once it runs generation system (or rather its mockup).

- Loading config from JSON file.