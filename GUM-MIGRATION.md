# Gum UI migration

Permanent log of the migration from Stride UGS UI to Gum. Update the
**Status** section every session; keep the plan stable, append decisions.

## Status

| Phase | State | Notes |
|-------|-------|-------|
| 0 — Spike (Gum beside UGS UI) | **Done** | Build + tests + visual check confirmed (render, click, resize, UGS intact) |
| 1 — Input seams (focus/capture/camera) | Not started | |
| 2 — Port panels (app bar → toolbar → LevelGen → LevelInfo) | Not started | |
| 3 — Switch to Gum-only | Not started | |
| 4 — Remove UGS UI + assets | Not started | |

**Current session:** Phase 0 complete (see Session log). Next: Phase 1.

**Decisions (locked):**
- Author UI **code-only** first (Gum tool / `.gumx` later, no rework).
- **Add `Gum.Stride`** NuGet (AGENTS.md exception approved).
- **TFM → `net10.0-windows7.0`** now (GPU Skia path; AGENTS.md exception approved).
- **Strangler migration**: Gum alongside UGS UI, port panel-by-panel, delete last.
- **Package version:** `Gum.Stride` **2026.11.2.1** (latest stable; not a preview).
- **Renderer registration:** default `registerSceneRenderer: true` — no `.sdgfxcomp` hand-edit. Gum appends `GumSceneRenderer` at the end of the compositor's `Game` collection (after 3D pass and UGS Group31 stage).
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
   still `UiFocus.IsTextInputFocused` → switch to Gum focus in Phase 1
   (`FrameworkElement.IsFocused` on `TextBox`). Keep injectable-predicate design;
   during transition OR Gum + UGS predicates.
4. **Mouse/camera:** camera uses right-drag look only (`BasicCameraController`).
   Gate with `GumService.Default.Cursor.WindowOver == null`.
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
1. Gum-backed `IsTextInputFocused` (TextBox focused) → capture predicate.
2. Suppress camera right-look while `Cursor.WindowOver != null`.
3. Keep old `UiFocus` for UGS panels until Phase 4 (OR during transition).
4. Headless tests for pure decision functions (no Gum init).

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
5. Update `AGENTS.md` input conventions, README Group31 note, `TODO.md`.
6. Full build + test + manual smoke (panels, generation, typing, camera, resize).

## Verification (every phase)

- `dotnet build LostInLimbo.LevelEditor.slnx`
- `dotnet test LostInLimbo.LevelEditor.slnx`
- Manual: show/hide panels, gen start/step, typing doesn’t move camera,
  right-drag over UI doesn’t rotate camera, window resize.

## Risks

| Risk | Mitigation |
|------|------------|
| Gum.Stride package churn | Pinned `2026.11.2.1` (stable); read migration notes on upgrade |
| GPU Skia path new (Oct 2026) | Phase 0 smoke launch OK; D3D11 adapter present in output |
| `.sdgfxcomp` hand-edit | **Avoided** — runtime append via default `registerSceneRenderer` |
| Dual UI input/focus split | Phase 1 OR-predicate; drop UGS per panel fast |
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
