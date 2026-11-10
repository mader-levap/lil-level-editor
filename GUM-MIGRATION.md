# Gum UI migration

Permanent log of the migration from Stride UGS UI to Gum. Update the
**Status** section every session; keep the plan stable, append decisions.

## Status

| Phase | State | Notes |
|-------|-------|-------|
| 0 — Spike (Gum beside UGS UI) | Not started | |
| 1 — Input seams (focus/capture/camera) | Not started | |
| 2 — Port panels (app bar → toolbar → LevelGen → LevelInfo) | Not started | |
| 3 — Switch to Gum-only | Not started | |
| 4 — Remove UGS UI + assets | Not started | |

**Current session:** plan approved; no code changes yet.

**Decisions (locked):**
- Author UI **code-only** first (Gum tool / `.gumx` later, no rework).
- **Add `Gum.Stride`** NuGet (preview; AGENTS.md exception approved).
- **TFM → `net10.0-windows7.0`** now (GPU Skia path; AGENTS.md exception approved).
- **Strangler migration**: Gum alongside UGS UI, port panel-by-panel, delete last.

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

1. **Init order:** compositor exists → `GumService.Default.Initialize(this,
   registerSceneRenderer: false)` → `UseKeyboardDefaults()` (+ optional gamepad).
2. **Renderer placement:** insert `GumSceneRenderer` as last child of `Game*` in
   `Assets/GraphicsCompositor.sdgfxcomp` (same slot as Group31/UiStage today),
   **after** forward renderer so post FX does not touch UI.
   Hand-edit of `.sdgfxcomp` needs explicit OK (or do it in Game Studio).
3. **Keyboard capture:** `GameInput` predicate in `LostInLimboEditor.PrepareContext`
   currently `UiFocus.IsTextInputFocused` → switch to Gum focus
   (`FrameworkElement.IsFocused` on `TextBox`). Keep injectable-predicate design;
   during transition OR Gum + UGS predicates.
4. **Mouse/camera:** camera uses right-drag look only (`BasicCameraController`).
   Gate with `GumService.Default.Cursor.WindowOver == null`.
5. **Icons/textures:** existing PNGs under `Resources/` loadable as Gum textures
   (not via Stride `Content.Load<SpriteSheet>`).
6. **Cleanup end-state:** remove `Stride.UI`, `UIComponent`/`Group31`/`UiStage`,
   `BasicFont`, `Btn*.sdsheet`, most of `UiFocus` once UGS UI is gone.

## Plan

### Phase 0 — Spike
1. Game.csproj: TFM → `net10.0-windows7.0`; add `Gum.Stride` (pinned preview).
   Tests.csproj: same TFM (ProjectReference to Game).
2. `LostInLimboEditor`: after compositor ready, `Initialize(this,
   registerSceneRenderer: false)` + `UseKeyboardDefaults()`.
3. One Gum `Button` + `Label` on root; leave UGS UI untouched.
4. Compositor: add `GumSceneRenderer` after forward renderer (confirm asset edit).
5. Verify: renders over 3D scene without post FX, click works, resize OK.
6. `dotnet build` + `dotnet test` green.

**Exit:** Gum visible + clickable next to UGS panels; no regressions.

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
| Gum.Stride preview-only | Pin version; read migration notes on upgrade |
| GPU Skia path new (Oct 2026) | Phase 0 validates; fallback = link Gum source |
| `.sdgfxcomp` hand-edit | Game Studio preferred; confirm before edit |
| Dual UI input/focus split | Phase 1 OR-predicate; drop UGS per panel fast |
| Texture loading differs from Stride | Small loader helper; reuse `Resources/` PNGs |

## Session log

- **2026-11-10:** Investigation complete; plan drafted; decisions locked
  (code-only, add Gum.Stride, TFM windows7.0, strangler). No code changes.
