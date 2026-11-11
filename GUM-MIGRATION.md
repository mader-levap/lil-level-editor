# Gum UI migration

Permanent log of the migration from Stride UGS UI to Gum. Update the
**Status** section every session; keep the plan stable, append decisions.

## Status

| Phase | State | Notes |
|-------|-------|-------|
| 0 — Spike (Gum beside UGS UI) | **Done** | Build + tests + visual check confirmed (render, click, resize, UGS intact) |
| 1 — Input seams (focus/capture/camera) | **Done** | Capture OR (UGS+Gum), camera right-look gated on `VisualOver`; 28/28 tests |
| 2 — Port panels (app bar → toolbar → LevelGen → LevelInfo) | **In progress** | Steps 0–3c done + user-verified; step 4 (LevelInfo) done, awaiting manual check |
| 3 — Switch to Gum-only | Not started | |
| 4 — Remove UGS UI + assets | Not started | |

**Current session:** Phase 2 step 4 (LevelInfo overlay) done (see Session log). Next: user check; then Phase 2 is complete.

**Decisions (locked):**
- Author UI **code-only** first (Gum tool / `.gumx` later, no rework).
- **Add `Gum.Stride`** NuGet (AGENTS.md exception approved).
- **TFM → `net10.0-windows7.0`** now (GPU Skia path; AGENTS.md exception approved).
- **Strangler migration**: Gum alongside UGS UI, port panel-by-panel, delete last.
- **Package version:** `Gum.Stride` **2026.11.2.1** (latest stable; not a preview).
- **Renderer registration:** default `registerSceneRenderer: true` — no `.sdgfxcomp` hand-edit. Gum appends `GumSceneRenderer` at the end of the compositor's `Game` collection (after 3D pass and UGS Group31 stage).
- **Phase 2 difficulty:** ComboBox with named presets mapped to `PropMain.Difficulty`
  (placeholder table Easy 0 / Medium 50 / Hard 100, one constants table).
- **Phase 2 visuals:** stock Gum Forms V3 look; PNG icons only on toolbar/transport buttons
  (no nine-slice rebuild of `BtnSelected`/`BtnUnselected`).
- **Phase 2 host:** `UiMain` builds the Gum UI lazily once `GumService.Default.IsInitialized`;
  services go into Gum panel constructors (no `UiRes`). Overlay switching becomes pure
  `OverlaySwitcher` over `IOverlayView` (UGS adapter bridges until overlays are ported).
- **Reusable status log:** `Ui/Gum/Common/Log/` — pure `StatusLog` model (+ `StatusEntry`,
  `EnStatusSeverity`) and `GumStatusLogView` (panel + ScrollViewer + optional Clear). Future
  screens reuse these instead of building their own log.
- **Focus indicator bars kept for now:** the orange 2px lines under focused controls are Gum's
  V3 `FocusedIndicator` (keyboard/gamepad focus cue), not a bug. Remove them later in the Gum
  tool (`.gumx`) theme, not with per-screen code (user decision, 2026-11-11).
- **Startup splash disabled** in `LostInLimboEditor.PrepareContext` (`SceneSystem.SplashScreenUrl = null`): editor opens immediately, and the splash path loads the compositor asynchronously (Gum must register on the real compositor, not the SceneSystem placeholder).

## Findings (2026-11-10)

- Gum = open-source (MIT) game UI: WYSIWYG tool + layout engine + Forms controls
  (`Button`, `TextBox`, `ComboBox`, `ListBox`, `CheckBox`, `Slider`, `ScrollViewer`,
  `StackPanel`, `Grid`, `Window`, …), WPF-like API.
- Official Stride support: **`Gum.Stride` NuGet** (preview, e.g. `2026.9.3.3-preview.1`).
  - Renders via SkiaSharp; `GumSceneRenderer` joins Stride’s compositor.
  - Input through `Stride.Input`; on Stride, Gum `Update`/`Draw` are automatic.
  - `GumService.Default.Initialize(game)` works with plain Stride `Game`
    (Community Toolkit only in their sample).
  - `net10.0` minimum; GPU Skia path (Oct 2026+) needs `net10.0-windows7.0` for D3D11.
  - Sample: https://github.com/vchelaru/Gum/tree/main/Samples/StrideGum
  - Docs: https://docs.flatredball.com/gum/code/getting-started/setup/adding-initializing-gum/stride.md
- Solves current pain: ComboBox (TODO difficulty), layout math, focus/outside-click,
  disabled/selected styles, scrollable log, post-process-free UI.
- Code-only fully supported; default Forms visuals ship with runtime.

### Integration points (this repo)

1. **Init order:** compositor exists (`SceneSystem.GraphicsCompositor.Game != null`) →
   `GumService.Default.Initialize(this)` (default `registerSceneRenderer: true`) →
   `UseKeyboardDefaults()`. Implemented as lazy once-guard in
   `LostInLimboEditor.EnsureGumInitialized` (called from `LoadContent` and first `Update`).
2. **Renderer placement:** runtime-appended to end of compositor `Game` collection —
   **no asset edit** (confirmed from `GumService.AddSceneRenderer` source).
3. **Keyboard capture:** `GameInput` predicate in `LostInLimboEditor.PrepareContext`
   ORs `UiFocus.IsTextInputFocused` (UGS) with `GumFocus.IsTextInputFocused()` (Gum).
   Gum side: `InteractiveGue.CurrentInputReceiver is TextBoxBase` (focused TextBox takes
   keyboard; non-text focus leaves the receiver null so WASD still works).
4. **Mouse/camera:** camera uses right-drag look only (`BasicCameraController`).
   Gate with `GumFocus.ShouldProcessCameraRightLook(GumFocus.IsPointerOverUi())`.
   Pointer-over uses `Cursor.VisualOver` — Gum obsolete-aliased `WindowOver` → `VisualOver`
   (same member; `VisualOver` only set when `HasEvents` is true under the default
   `VisualOverBehavior.IfHasEventsIsTrue`). Non-interactive panels that must block look
   need `HasEvents = true` when built (Phase 2).
5. **Icons/textures:** existing PNGs under `Resources/` loadable as Gum textures
   (not via Stride `Content.Load<SpriteSheet>`).
6. **Cleanup end-state:** remove `Stride.UI`, `UIComponent`/`Group31`/`UiStage`,
   `BasicFont`, `Btn*.sdsheet`, most of `UiFocus` once UGS UI is gone.
7. **Splash:** disabled at runtime (see Decisions) — if re-enabled, the Update fallback
   in `EnsureGumInitialized` still waits for a real compositor.

## Plan

### Phase 0 — Spike
1. ~~Game.csproj: TFM → `net10.0-windows7.0`; add `Gum.Stride`.~~ **Done** (`2026.11.2.1`;
   Tests + Windows launcher TFMs matched).
2. ~~`LostInLimboEditor`: after compositor ready, `Initialize(this)` + `UseKeyboardDefaults()`.~~
   **Done** (lazy once-guard; splash disabled so compositor loads synchronously).
3. ~~One Gum `Button` + `Label` on root; leave UGS UI untouched.~~ **Done**
   (`Ui/Gum/GumSpikeUi.cs`).
4. ~~Compositor: add `GumSceneRenderer` after forward renderer (confirm asset edit).~~
   **N/A — default `registerSceneRenderer: true` appends at end; no asset edit.**
5. ~~Verify: renders over 3D scene without post FX, click works, resize OK.~~
   **Done** — user confirmed spike renders, click works, resize OK, UGS UI intact.
6. ~~`dotnet build` + `dotnet test` green.~~ **Done.**

**Exit criteria met.**

### Phase 1 — Input seams
1. ~~Gum-backed `IsTextInputFocused` (TextBox focused) → capture predicate.~~ **Done**
   (`GumFocus.IsTextInputFocused` via `CurrentInputReceiver is TextBoxBase`).
2. ~~Suppress camera right-look while cursor is over Gum UI.~~ **Done**
   (`GumFocus.IsPointerOverUi` via `Cursor.VisualOver`; pure `ShouldProcessCameraRightLook`).
3. ~~Keep old `UiFocus` for UGS panels until Phase 4 (OR during transition).~~ **Done**
   (`GumFocus.IsKeyboardCaptured(ugs, gum)` OR in `PrepareContext`).
4. ~~Headless tests for pure decision functions (no Gum init).~~ **Done**
   (`GumFocusTests` — 6 cases; total suite 28/28).

**Exit criteria met.**

### Phase 2 — Port panels
New folder `LostInLimbo.LevelEditor.Game/Ui/Gum/`. Order:
1. App bar (name/version/build colors from `ISystemInfoService`).
2. Editor toolbar (icon buttons, overlay switch).
3. LevelGen overlay (transport + enable matrix; **wire real props**:
   seed, ComboBox difficulty, CheckBox many-themes → `PropMain`;
   status log ScrollViewer with clear/cap).
4. LevelInfo overlay (replace mockup).

Per panel: build → verify → test pure logic → hide UGS equivalent.
Prefer constructor injection over a new static locator.

### Phase 3 — Switch
1. `UiMain`/bootstrap attaches Gum only; remove UGS `UIPage` path.
2. Overlay switching + generation-state UI fully on Gum.
3. Optional: JSON props persistence (TODO.md).

### Phase 4 — Remove UGS
1. Delete `UiBuilderBase`, UGS builders, `UiRes` sprite/font loading, UGS `UiFocus`.
2. Drop `Stride.UI`; clean `UIMain` entity in `MapScene`.
3. Compositor: remove Group31/UiStage if unused (Game Studio preferred).
4. Retire dead assets (`IconQuestion`, sheets/font if unreferenced).
   **Ensure no icon duplication** in the shipped app: delete the `Icon*.sdtex` assets once
   only Gum uses the icons (PNGs copied to `GumContent/Icons/` stay); revisit the csproj
   `*.png` copy rule (e.g. drop unused `icon_question.png`). User request, 2026-11-11.
5. Update `AGENTS.md` input conventions, README Group31 note, `TODO.md`.
6. Full build + test + manual smoke (panels, generation, typing, camera, resize).

## Verification (every phase)

- `dotnet build LostInLimbo.LevelEditor.slnx`
- `dotnet test LostInLimbo.LevelEditor.slnx`
- Manual: show/hide panels, gen start/step, typing doesn’t move camera,
  right-drag over UI doesn’t rotate camera, window resize.
- Phase 1 manual: Gum spike TextBox typing must not move camera; right-drag over
  spike UI must not rotate; right-drag on empty 3D still looks; UGS EditText
  typing still captures.

## Risks

| Risk | Mitigation |
|------|------------|
| Gum.Stride package churn | Pinned `2026.11.2.1` (stable); read migration notes on upgrade |
| GPU Skia path new (Oct 2026) | Phase 0 smoke launch OK; D3D11 adapter present in output |
| `.sdgfxcomp` hand-edit | **Avoided** — runtime append via default `registerSceneRenderer` |
| Dual UI input/focus split | Phase 1 OR-predicate (`GumFocus.IsKeyboardCaptured`); drop UGS per panel fast |
| `Cursor.WindowOver` obsolete | Use `VisualOver` (same member; no CS0618) |
| `ColoredRectangleRuntime` obsolete (CS0618/GUM002) | Use `RectangleRuntime` + `IsFilled` + `FillRed/Green/Blue/Alpha` + `StrokeWidth = 0`; do **not** set `StrokeAlpha` (made the fill transparent) |
| Orange focus bars look like glitches | Gum V3 `FocusedIndicator`; no global switch in 2026.11.2.1 (docs, reflection, GitHub checked). Fix in `.gumx` theme; fallback: per-root helper setting `FocusedIndicatorColor` transparent |
| Forms button default height is RelativeToChildren | Icon buttons set `Width/HeightUnits = Absolute` (else `Height` becomes padding: 48 → 84 px) |
| Texture loading differs from Stride | Small loader helper; reuse `Resources/` PNGs |

## Session log

- **2026-11-10:** Investigation complete; plan drafted; decisions locked
  (code-only, add Gum.Stride, TFM windows7.0, strangler). No code changes.
- **2026-11-10 (Phase 0):** Implemented spike.
  - TFMs: Game/Tests/Windows → `net10.0-windows7.0`; added `Gum.Stride` `2026.11.2.1`.
  - `LostInLimboEditor`: disabled splash; lazy `GumService.Default.Initialize(this)` +
    `UseKeyboardDefaults()` once `GraphicsCompositor.Game` is set; spike UI build.
  - New: `Ui/Gum/GumSpikeUi.cs` (Label + Button on Gum root).
  - `AGENTS.md` TFM notes updated.
  - Verified: `dotnet build` 0 errors (no new CS warnings; only pre-existing NU190x
    transitive advisories); `dotnet test` 22/22; smoke launch stayed alive 10s
    (`StrideGum`/`SkiaGum`/`SkiaGameRendering.Stride.D3D11` in `Bin/Windows/Debug`).
  - User visual confirmation: spike label/button visible, click works, resize works,
    UGS UI intact. **Phase 0 complete.**
- **2026-11-10 (Phase 1):** Input seams.
  - New: `Ui/Gum/GumFocus.cs` — runtime queries (`IsTextInputFocused`,
    `IsPointerOverUi`) + pure decisions (`IsKeyboardCaptured`, `ShouldProcessCameraRightLook`).
  - `LostInLimboEditor.PrepareContext`: capture predicate = UGS OR Gum text focus.
  - `BasicCameraController`: right-look only when right button down AND pointer not over
    Gum UI (`VisualOver`); unlocks mouse while over UI.
  - `GumSpikeUi`: added TextBox for manual capture verification.
  - New: `GumFocusTests` (6 headless cases).
  - Note: `Cursor.WindowOver` is `[Obsolete]` → use `VisualOver` (identical member).
  - Verified: `dotnet build` 0 errors (no CS warnings; only pre-existing NU190x);
    `dotnet test` 28/28; smoke launch alive 10s.
  - **Manual check for user:** type in Gum spike TextBox → WASD must not move camera;
    right-drag over Gum spike UI must not rotate camera; right-drag on 3D still looks;
    typing in UGS fields still captures. Non-interactive panels need `HasEvents = true`
    to block look (Phase 2).
- **2026-11-11 (Phase 2, steps 0–1):** Shared helpers + app bar.
  - Detailed plan: step 0 helpers, 1 app bar, 2 toolbar (`OverlaySwitcher`), 3 LevelGen
    (pure `LevelGenButtonStates`, `DifficultyPresets`, `LevelGenPropsBuilder`, `StatusLog`,
    `LevelGenStatusMessages`), 4 LevelInfo (real counts instead of mockup).
  - Removed `GumSpikeUi` (and its call in `LostInLimboEditor`).
  - New: `Ui/Gum/Common/UiRgba.cs` (engine-neutral color), `Ui/Gum/Common/GumUiFactory.cs`
    (`CreatePanel` = `ContainerRuntime` + translucent rectangle, `HasEvents = true`;
    `CreateLabel`, color helpers via TextRuntime/ColoredRectangleRuntime RGBA ints).
  - New: `Ui/Gum/AppBar/GumAppBar.cs` (+ pure `BuildTypeColors`); built by `UiMain`.
  - `UiMain`: UGS app bar removed; row 0 is now a fixed `GumAppBar.Height` spacer so the
    UGS toolbar/overlays keep their position.
  - Icon loading (`GumIcons`, csproj copy of PNGs) moved to step 2 (first visible use).
  - Verified: `dotnet build` 0 errors (no CS warnings; only NU190x); `dotnet test` 32/32;
    smoke launch alive 12s.
  - **Manual check for user:** app bar shows name/author left, version + colored build right;
    full width on resize; UGS bar gone; UGS buttons sit below the Gum bar; right-drag over
    the bar does not rotate the camera.
  - **User visual check passed:** app bar present, resizes, UGS bar gone, UGS buttons under
    the Gum bar, right-drag over the bar does not rotate the camera.
  - Correction: the step 1 "no CS warnings" note was wrong (an up-to-date incremental build hid
    them). `ColoredRectangleRuntime` raised CS0618/GUM002; fixed in step 2. Check warnings
    with `--no-incremental` from now on.
- **2026-11-11 (Phase 2, step 2):** Editor toolbar.
  - New: `Ui/Gum/Common/GumIcons.cs` (icon names + absolute path resolution); Game.csproj
    copies `Resources/Graphics/Icons/*.png` to `GumContent/Icons/` (`None Update` + Link) —
    flows to `Bin/Windows/<Config>/` and test output.
  - `GumUiFactory`: `CreateIconButton` / `CreateIconToggleButton` (48×48, 36×36 `SpriteRuntime`
    by absolute `SourceFileName`; missing file → logged + tooltip text); panel background now
    `RectangleRuntime`.
  - New: `Ui/Gum/Editor/IOverlayView`, pure `OverlaySwitcher`, temporary `UgsOverlayView`
    (keeps UGS focus release on hide), `GumEditorToolbar` (gears/info `ToggleButton`s; checked
    state re-synced after every click).
  - `UiEditorBuilder` shrunk to hosting the two UGS overlay panels (buttons, `SwitchOverlay`,
    `SelButton` removed); `UiMain` registers UGS adapters and builds the toolbar.
  - New tests: `OverlaySwitcherTests` (9), `GumIconsTests` (9 incl. icons present in output).
  - Verified: full rebuild 0 errors, no CS/GUM warnings (only NU190x + Stride shader X3557);
    `dotnet test` 50/50; smoke launch alive 12s.
  - **Manual check for user:** gears/info icons render on the toolbar; click opens the UGS
    overlay and checks the button; clicking the other switches (one checked at a time); second
    click closes; typing in a UGS field then closing the overlay → WASD moves the camera again;
    right-drag over toolbar does not rotate the camera.
  - **User visual check:** toolbar works (icons, switching, focus release, camera gate), but
    the app bar background was missing. Cause: on Skia, `RectangleRuntime` without a stroke
    renderable uses one shared color slot, so `StrokeAlpha = 0` after the fill made it
    transparent. Fix: removed the stroke line, set `IsFilled = true`.
  - Re-check: background back but with a 1px white outline (default stroke). Fix:
    `StrokeWidth = 0`. Confirmed by screenshot (no outline).
  - Found while checking: toolbar buttons were 48×84 (Forms visual height is RelativeToChildren,
    so `Height = 48` acted as padding around the 36px icon). Fix: `GumUiFactory` forces
    Absolute width/height units; layout dump confirmed 48×48.
  - Note: display runs at 150% and the editor is not DPI-aware; Gum's canvas is the physical
    1280×720 back buffer. Screenshot tooling must be DPI-aware or it crops the right side.
  - Verified: full rebuild 0 errors, no CS/GUM warnings; `dotnet test` 50/50.
  - **User visual check passed** (toolbar + app bar after fixes).
- **2026-11-11 (Phase 2, step 3):** LevelGen overlay on Gum.
  - New pure logic (`Ui/Gum/LevelGen/`): `LevelGenUiState` (button matrix + `PropsVisible`;
    form hidden while Running **and Paused**, since resume keeps the original config),
    `DifficultyPresets` (Easy 0 / Medium 50 / Hard 100; default from `PropMain.Default`),
    `LevelGenPropsBuilder` (blank seed → null; values copied as entered, no trimming),
    `StatusLog` (cap 200, Added/Trimmed/Cleared events, severity colors),
    `LevelGenStatusMessages` (texts moved from UGS; dropped the stray `)` after `steps=N`).
  - New: `GumLevelGenPanel` (full-window root with `HasEvents = false`; transport column
    top-right; props form next to toolbar: seed `TextBox`, difficulty `ComboBox`, many-themes
    `CheckBox`; bottom log `ScrollViewer` (35% height) + Clear). On hide: seed box unfocused,
    dropdown closed. Gum `TextBoxBase.LosesFocusWhenClickedOff` covers outside-click blur, so
    `GumFocus.ReleaseTextInputFocus` from the plan was not needed.
  - `UiEditorBuilder`: UGS LevelGen overlay no longer built (`UiLevelGenBuilder` file stays
    until Phase 4). `UiMain` builds/registers/ticks the Gum panel.
  - New tests: `LevelGenUiStateTests`, `LevelGenPropsTests`, `StatusLogTests` (77/77 total).
  - Verified: full rebuild 0 errors, no CS/GUM warnings; screenshots (posted clicks): overlay
    opens, Start → Finished (green), Step → Paused, form hidden while paused and back after
    finish, Pause/Stop disabled when idle. Posted WM_CHAR did not reach Gum, so typing and
    seed determinism are **not** verified by automation.
  - Note: Gum `ScrollViewer` default visual has an opaque dark background (stock look).
  - **Manual check for user:** same typed seed → same counts twice; difficulty dropdown opens
    and changes; checkbox toggles; Clear empties log; many runs scroll to newest; typing in
    seed box does not move camera; closing overlay while typing → WASD works; right-drag over
    form/log/buttons does not rotate camera, over empty 3D it does.
- **2026-11-11 (Phase 2, step 3b):** Status log extracted as reusable component (user request).
  - Moved model to `Ui/Gum/Common/Log/` (namespace `...Ui.Gum.Common.Log`; `Log` rather than
    `StatusLog` so the namespace doesn't share the class name): `StatusLog.cs`,
    `StatusEntry.cs`, `EnStatusSeverity.cs`. Added `Add(text, severity = Info)` shortcut.
  - New `GumStatusLogView` (IDisposable): builds translucent panel + ScrollViewer + optional
    Clear; shows existing entries on build; follows Added/Trimmed/Cleared; deferred
    scroll-to-end in `Update()`; `Dispose()` unsubscribes. Caller only positions/sizes `Root`.
  - `GumLevelGenPanel`: log code replaced by the view (anchor bottom, 35% height).
  - Tests split: `StatusLogTests` (model) + `LevelGenStatusMessagesTests`; 78/78.
  - Verified: full rebuild 0 errors, no CS/GUM warnings; screenshot: Start ×2 → Clear →
    Start leaves only the last run's two lines; look unchanged.
- **2026-11-11 (Phase 2, step 3c):** Orange lines investigation (user report).
  - Lines under seed box, under ComboBox and under its open dropdown = Gum V3
    `FocusedIndicator` bars (2px, shown on focus; default palette `Colors.Warning` #E8AB19 is the
    likely color). Present on Button/ToggleButton/CheckBox/RadioButton/ComboBox/ListBox/
    ListBoxItem/ScrollViewer/Slider/TextBoxBase visuals.
  - No global off switch in Gum 2026.11.2.1: checked XML docs, reflection over all public types
    of GumCommon/SkiaGum/StrideGum, Gum docs and GitHub (Menu bar was removed upstream by
    editing the visual, PR #6708).
  - **Decision:** keep the bars until the Gum tool theme; no code change.
  - Also noted: newer Gum clears focus when a focused control is hidden (issue #6934); our
    manual unfocus-on-hide stays correct either way.
- **2026-11-11 (Phase 2, step 4):** LevelInfo overlay on Gum (UGS mockup replaced).
  - New `Ui/Gum/Info/`: pure `LevelInfoSummary.Build(result, failureReason, LevelWorldFacts?)`
    → `LevelInfoRow(Label, Value)` list (status, optional reason, then phase/graph nodes/
    edges/map steps, or a "no level" explanation); `GumLevelInfoPanel` (centered translucent
    card, rows rebuilt only when they change; refreshes on show and when `Status().Result`
    changes while visible — `World()` deep-copies, so it is not read every frame).
  - Form row helper moved to `GumUiFactory.CreateFormRow` (shared by LevelGen and LevelInfo).
  - `UiMain` registers the Gum panel for `EnOverlay.LevelInfo`; `UiEditorBuilder` now builds an
    empty UGS grid (removed in Phase 3); `UgsOverlayView` deleted (unused).
    `UiLevelInfoBuilder` file stays until Phase 4.
  - Tests: `LevelInfoSummaryTests`; 86/86.
  - Verified: full rebuild 0 errors, no CS/GUM warnings; screenshots: info before generation
    shows "Status: None / No level generated yet."; after Start shows Finished, Completed,
    nodes/edges/steps.
  - Follow-up (user request): card title centered, bold, 1.25× font size via new
    `GumUiFactory.CreateTitleLabel` (reusable for future screen titles).
