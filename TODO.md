# TODO

- Check how to implement generation subsystem in Stride (apparently Stride's equivalent is called services?), so we can have placeholder for code that handles calling generation and processing results. What this subsystem does?
  - Loads json file that contains all data needed for generation. In future, we will have UI to be able to set at least some config.
  - Runs generation asynchronically. Note it won't generate everything at once, only some chunks.
  - After generation we spawn stuff in Strider based on generated data.
  - Rinse, repeat until generator says it is over.
  - Inform user generation is finished.
  - We will likely refine it later, we need proper chunk handling. But that will be later, there is no point in trying to do everything at once.

- Check how to package game so you can run it standalone.

- Add test for RunGenScript once it runs generation system (or rather its mockup).