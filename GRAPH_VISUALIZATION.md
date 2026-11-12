# Graph visualization

Plan and tracking document for the graph visualization panel. Work is split into phases; **after
each phase we stop and the user verifies** before the next one starts. Update the *Current state*
section, the phase checklists and the logs at the end of every session so work can resume from here.

## Current state

| Item | Value |
|---|---|
| Active phase | Phase 0 (awaiting verification) |
| Last finished phase | none |
| Blockers | none |
| Next action | User tries the spike panel; then remove spike code (see Phase 0 checklist) and start Phase 1 |

| Phase | Title | Status |
|---|---|---|
| 0 | Research spikes | awaiting verification |
| 1 | Data model and pure helpers | not started |
| 2 | Component shell and static rendering | not started |
| 3 | LevelGen adapter and overlay integration | not started |
| 4 | Layout simulation | not started |
| 5 | Navigation | not started |
| 6 | Selection, dragging and data panel | not started |
| 7 | Polish and cleanup | not started |

Statuses: `not started`, `in progress`, `awaiting verification`, `done`.

## Goal

A reusable Gum component, `GumGraphVisualizer`, that draws a node/relationship graph in real time
and lets the user pan, zoom, select and drag. It is used in the level information overlay, in a
prominent area at the bottom centre of the screen, with the existing info card above it. When no
graph is available the panel is still shown, with the text "No graph data available".

Expected size: at most about 200 drawn entities (nodes plus relationships).

### Hard rules

- The component lives in `LostInLimbo.LevelEditor.Game/Ui/Gum/Components/GraphVisualizer/`
  (namespace `LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer`), with all its
  subcomponents, helpers and data structures.
- The component knows nothing about `LilNode`, `LilEdge` or any other LevelGen type. A separate
  adapter outside the component folder translates generation data into the component's model.
- No generation logic and no changes to `LostInLimbo.LevelGen`. No new NuGet package
  (SkiaSharp already arrives through `Gum.Stride`).
- Layout must be deterministic: same graph in, same initial placement out (no time-based random).
- Math, layout, hit testing, colours and the adapter are plain classes with headless xUnit tests.
  Only the thin Gum/Skia shell is untested and verified by eye.
- Usual project rules apply (AGENTS.md): XML docs, no magic numbers, build and test before a
  phase is declared ready, never commit.

## Research so far (done 2026-11-12)

Verified by inspecting the assemblies of `Gum.Stride` 2026.11.2.1 in the NuGet cache. Nothing was
run yet, so on-screen behaviour is unconfirmed.

- **Canvas hook (superseded, see Findings log):** `CanvasRenderable` turned out to be `internal`.
  Original note: `SkiaGum.Renderables.CanvasRenderable` has `event Action<SKCanvas> CustomRender`.
  There is no `CanvasRuntime`; wrap it with `new GraphicalUiElement(canvasRenderable)`. Inside the
  handler any SkiaSharp call works (`DrawCircle`, `DrawPath`, `DrawText`, `SKPathEffect.CreateDash`).
- **Shape elements (fallback):** `PolygonRuntime` (open or closed, `SetPoints`, `IsPointInside`),
  `LineRuntime`, `CircleRuntime`, `ArcRuntime`, `RectangleRuntime`; all share stroke width, dashes
  and colours from `SkiaShapeRuntime`. Each is a full Gum element taking part in layout.
- **Chosen approach:** one canvas element drawing everything in immediate mode. About 200 entities
  per frame is trivial for Skia, and immediate mode avoids a Gum layout pass per moved node
  (see the "every Gum property change triggers a layout pass" note in `GumProgressBar`).
- **Unknown:** whether the `SKCanvas` arrives translated/clipped to the element or in screen space;
  docs do not say. Stride is not named in the Gum shape docs at all.
- **Known rough edge:** `GumUiFactory.cs` notes that `RectangleRuntime` stroke channels misbehave
  on Skia.

Existing code this work builds on:

- `Ui/Gum/App/Info/GumLevelInfoPanel.cs` – the overlay to extend; refreshes only when the
  generation result changes because `ILevelGenService.World()` returns a deep copy.
- `Ui/Gum/Components/ProgressBar/` – pattern for a custom component: `Build()` returns a root
  container, `Update(deltaSeconds)` per frame, math in a separate pure class with tests.
- `Ui/Gum/Common/GumUiFactory.cs` (`CreatePanel`, `CreateLabel`, `CreateFormRow`, `CreateTitleLabel`),
  `Ui/Gum/Common/UiRgba.cs` (engine-neutral colour), `Ui/Gum/Theme/EditorTheme.cs` (palette,
  `UiRgba` to `SKColor` conversion), `Ui/Gum/Theme/EditorFonts.cs`.
- `Input/GameInput.cs` and `Ui/Gum/Common/GumFocus.cs` – keyboard capture; the camera
  (`Input/BasicCameraController.cs`) also uses WASD and right-drag look.
- LevelGen data (`LostInLimbo.LevelGen.Data.Result.Graph`): `LilGraph { Nodes, Edges }`,
  `LilNode { Id, RelSize, MaxEdges, Secret, Style.Group, Pois, Reach, OpenReach }`,
  `LilPoi { Id, Type, Name }`, `LilEdge { Id, From, To, Direction, Chokepoint, Secret, Conditions }`,
  `LilCondition { Type, RequiresPoi }`, `EnPassageDirection { Bidirectional, OneWay }`.

## Design

### Component structure

```text
GumGraphVisualizer            root; Build() returns its container, Update(deltaSeconds) per frame
├ GumGraphViewport            canvas element + horizontal/vertical scroll bars + empty-state label
└ GumGraphDataPanel           right side; key/value rows for the selected node or relationship
```

Public surface of `GumGraphVisualizer` (kept small): `Build(...)`, `SetGraph(GraphVisData?)`
(null or empty shows "No graph data available"), `Update(float deltaSeconds)`, `Root`.

### Files (all in `Ui/Gum/Components/GraphVisualizer/`)

| File | Role | Tested |
|---|---|---|
| `GraphVisData.cs` | Immutable input model: `GraphVisData`, `GraphVisNode`, `GraphVisEdge`, `GraphVisMarker` | yes |
| `GraphVisStyle.cs` | Named constants / options (stroke widths, radii, dash lengths, colours, limits) | – |
| `GraphVisColors.cs` | Darken, highlight and "key to distinct colour" helpers | yes |
| `GraphVisCamera.cs` | Pan/zoom state, world/screen transforms, zoom about a point, clamping, fit-to-view, scroll bar ranges | yes |
| `GraphVisGeometry.cs` | Edge end points on circle borders, arrowheads, label anchor, marker dot placement | yes |
| `GraphVisLayout.cs` | Initial placement and force simulation with per-node mass | yes |
| `GraphVisHitTest.cs` | Node (circle) and edge (distance to segment) picking | yes |
| `GraphVisInteraction.cs` | State machine driven by a per-frame input record: pan, zoom, select, drag | yes |
| `GraphVisRenderer.cs` | Skia draw calls for nodes, edges, markers, texts, highlights | by eye |
| `GumGraphViewport.cs`, `GumGraphDataPanel.cs`, `GumGraphVisualizer.cs` | Gum shell | by eye |

Outside the component: `Ui/Gum/App/Info/LevelGraphVisAdapter.cs` (LevelGen graph to
`GraphVisData`, tested) and changes to `GumLevelInfoPanel.cs`. Tests go to
`LostInLimbo.LevelEditor.Tests/Ui/Gum/GraphVisualizer/`.

### Data model (generic, no level concepts)

- `GraphVisNode`: `Id` (string), `RelativeSize`, `Color` (`UiRgba?`, null means default light gray),
  `IsDashed`, `Markers` (list of `GraphVisMarker { Id, Label }`), `Properties`
  (`IReadOnlyDictionary<string, string>`).
- `GraphVisEdge`: `Id`, `FromId`, `ToId`, `ArrowAtFrom`, `ArrowAtTo`, `IsDashed`, `Color`
  (`UiRgba?`, null means black), `Label` (text over the line, null for none),
  `HighlightMarkerIds` (markers to highlight while this edge is selected), `Properties`.
- `GraphVisData`: `Nodes`, `Edges`; validates unique ids and that edges reference existing nodes.

How level data maps onto it (done by the adapter, Phase 3):

| Level data | Model |
|---|---|
| `LilNode.RelSize` | `RelativeSize` (radius derived by the component) |
| `LilNode.Secret` | `IsDashed` |
| `LilNode.Style.Group` | `Color` via `GraphVisColors` key-to-colour; null group gives null (light gray) |
| `LilNode.Pois` | `Markers` (label = POI name, falling back to POI type) |
| `LilEdge.Direction` | `Bidirectional`: arrows at both ends; `OneWay`: arrow at `To` only |
| `LilEdge.Secret` | `IsDashed` and dark gray `Color` |
| `LilEdge.Conditions` | `Label` = condition count (none when zero); `HighlightMarkerIds` = `RequiresPoi` ids |
| everything else (ids, `MaxEdges`, `Chokepoint`, POI types, reach data…) | `Properties` |

### Drawing rules (component)

- Node: filled circle, thick outline in a darker shade of the fill; dashed outline when `IsDashed`.
- Edge: thick line from circle border to circle border, arrowheads per flags, dashed when
  `IsDashed`, `Label` centred on the line.
- Markers: small dots inside the node with the label above each dot. More than 3 markers
  (`GraphVisStyle.MaxMarkersShown`) collapse into one dot labelled with the count.
- Selection highlight for nodes and edges. With an edge selected, its `HighlightMarkerIds` dots
  and labels are highlighted; if such a marker is hidden inside a collapsed dot, that dot is highlighted.
- Draw order: edges, nodes, markers, texts, selection on top.

### Group colours

Requirement: same group name always gives the same colour, and colours are clearly distinct.
Planned: a fixed palette of well-separated colours (varying hue first, then lightness), indexed by
a stable hash of the name (own FNV-1a, not `string.GetHashCode`, which differs per process). When
two groups of one graph hash to the same slot, the later one in sorted-name order takes the next
free slot. A name therefore keeps its colour except in the rare collision case. Light gray is
reserved for "no group" and is not in the palette.

### Layout

Force simulation, run in small fixed steps from `Update`, sleeping when movement falls below a
threshold (so an idle panel costs almost nothing) and waking on drag or new data.

- Mass grows with node size. Every displacement between two nodes is split inversely to mass, so
  a large node yanks small neighbours and a small node barely moves a large one.
- Repulsion when two nodes are closer than their radii plus a gap, followed by a hard
  overlap-resolution pass so circles never overlap.
- Attraction along an edge only when the nodes are farther apart than a rest length.
- A node being dragged is pinned (treated as infinitely heavy).
- Initial placement is deterministic: breadth-first rings from the highest-degree node, ties broken
  by id; disconnected parts placed side by side.

### Input

- The interaction logic receives a plain per-frame record (pointer position in viewport pixels,
  button states, wheel delta, pan axis from keys, elapsed time) and is tested headlessly.
  `GumGraphViewport` fills that record from Gum/Stride.
- Pan: left-drag on empty space, or WASD. Zoom: mouse wheel, about the cursor. Left-click selects
  a node or edge; left-drag on a node moves it.
- **WASD conflict with the camera:** default decision is that the graph owns WASD while the
  pointer is over the viewport, implemented by extending the keyboard-capture predicate used by
  `GameInput` (camera stops) while the viewport reads raw keys as UI code. To be confirmed in
  spike S3.
- Viewport has `HasEvents = true` so right-drag over it does not rotate the camera
  (`GumFocus.IsPointerOverUi`).

## Phases

### Phase 0 – Research spikes

Throwaway code on a temporary overlay or behind a temporary toggle; removed at the end of the
phase. Findings are written into *Findings log*. Goal: remove every unknown before real code.

- [x] **S1 Canvas drawing.** `CanvasRenderable` inside a Gum container: does it draw at all in
      Stride; is the canvas origin the element or the screen; is it clipped to the element
      (else `ClipRect`); is it redrawn every frame; behaviour on window resize.
- [x] **S2 Skia primitives.** Thick anti-aliased circle with dashed outline, dashed line,
      arrowhead path, text (which typeface/size matches `EditorFonts`; measuring text for centring).
- [x] **S3 Input.** Pointer position relative to the element, left press/drag/release, wheel delta
      (Gum `Cursor` versus Stride `Input`), whether a `GraphicalUiElement` wrapper can have
      events or an `InteractiveGue`/container is needed on top, blocking camera right-look,
      WASD ownership while hovered.
- [x] **S4 Scroll bars.** Whether Gum Forms `ScrollBar` can be used standalone and driven by /
      drive the camera offset; otherwise a small custom one.
- [x] **S5 Load test.** 200 moving circles and lines with texts at stable frame time.
- [x] **S6 Fallback check (only if S1 fails).** Not needed, S1 passed. Same test with a pool of `CircleRuntime` /
      `PolygonRuntime` elements.
- [x] Record findings and decisions; list any further research needed.
- [ ] Remove spike code after the user has seen it. Everything is marked `SPIKE(graph-vis)`:
      folder `Ui/Gum/Components/GraphVisualizer/Spike/`, three places in `Ui/Main/UiMain.cs`
      (using, field + `Update` call, build block) and the capture predicate in
      `Main/LostInLimboEditor.cs` (restore `isKeyboardCaptured: GumFocus.IsKeyboardCaptured`).

**Verify:** user sees the spike on screen (shapes, dashes, text, pointer read-out) and agrees on
the decisions recorded.

### Phase 1 – Data model and pure helpers

No visuals. Test-first where practical.

- [ ] `GraphVisData.cs` with validation.
- [ ] `GraphVisStyle.cs`.
- [ ] `GraphVisColors.cs` (darken, key-to-colour palette with collision handling).
- [ ] `GraphVisCamera.cs` (transforms, zoom about point, zoom limits, clamp, fit-to-view).
- [ ] `GraphVisGeometry.cs` (border-to-border edge segment, arrowheads, marker placement, collapse rule).
- [ ] Tests for all of the above.

**Verify:** build and tests green; user reviews the model shape (it is the component's public API).

### Phase 2 – Component shell and static rendering

- [ ] `GraphVisRenderer.cs` drawing nodes, edges, arrows, dashes, labels and markers.
- [ ] `GumGraphViewport.cs` (canvas, clipping, empty-state label) and `GumGraphVisualizer.cs`
      with `SetGraph`; data panel is an empty placeholder column for now.
- [ ] Simple deterministic placement (circle) and automatic fit-to-view so anything is visible.
- [ ] Put the component into `GumLevelInfoPanel`: graph panel bottom centre, info card above it;
      fed with a hard-coded demo graph that exercises every visual feature, plus the empty state.

**Verify:** user opens the overlay and checks the look of every visual rule against the demo graph
and the "No graph data available" state.

### Phase 3 – LevelGen adapter and overlay integration

- [ ] `LevelGraphVisAdapter.cs` with tests (mapping table above, including POI name fallback and
      conditions pointing at markers).
- [ ] `GumLevelInfoPanel` converts the world graph on result change only and calls `SetGraph`;
      demo graph removed.
- [ ] Group colours wired through the adapter.

**Verify:** user generates several levels and sees real graphs (still with the simple placement).

### Phase 4 – Layout simulation

- [ ] `GraphVisLayout.cs`: deterministic initial placement, forces, mass rule, overlap resolution,
      sleeping; tests (no overlap after settling, determinism, heavy/light displacement ratio,
      connected nodes pulled within range, settles in bounded steps).
- [ ] Simulation stepped from `GumGraphVisualizer.Update`; fit-to-view after first settle.
- [ ] Tune constants on real graphs.

**Verify:** user judges readability of real graphs and the settling animation.

### Phase 5 – Navigation

- [ ] `GraphVisInteraction.cs` pan and zoom parts with tests.
- [ ] Mouse-drag pan, WASD pan, wheel zoom about cursor, zoom and pan limits.
- [ ] Scroll bars shown only when the graph exceeds the viewport, in sync with the camera both ways.
- [ ] Keyboard capture so the 3D camera does not move while the graph is being navigated.

**Verify:** user navigates a large graph; camera stays still; nothing leaks outside the viewport.

### Phase 6 – Selection, dragging and data panel

- [ ] `GraphVisHitTest.cs` with tests (nodes win over edges; edge tolerance scales with zoom).
- [ ] Selection and node dragging in `GraphVisInteraction` (click versus drag threshold; dragged
      node pinned; neighbours react through the simulation) with tests.
- [ ] Highlight rendering for node, edge, condition markers and collapsed-marker dot.
- [ ] `GumGraphDataPanel.cs`: shows all `Properties` of the selection, scrollable, cleared on
      deselect or new data.

**Verify:** user selects and drags nodes, selects relationships with conditions, checks panel content.

### Phase 7 – Polish and cleanup

- [ ] Review constants, naming, docs, regions; remove leftovers from spikes and demo data.
- [ ] Check cleanup when the graph is replaced or the overlay hidden (no leaked elements, Skia
      paints/paths disposed, focus/capture released).
- [ ] Behaviour on window resize.
- [ ] Update `AGENTS.md` (component and input conventions) and `TODO.md` if needed.
- [ ] Final build, tests, warnings check.

**Verify:** full pass by the user over the whole feature.

## Open decisions

Defaults are applied unless the user says otherwise; confirm at the phase noted.

| # | Question | Default | Confirm at |
|---|---|---|---|
| 1 | When does the graph own WASD instead of the camera? | While the pointer is over the viewport | Phase 0 (S3) |
| 2 | Group colours on hash collision | Next free palette slot in sorted-name order | Phase 1 |
| 3 | Which button pans? | Left-drag on empty space (right-drag stays unused) | Phase 5 |
| 4 | Size of the graph panel in the overlay | About 80 % of window width, 55 % of height | Phase 2 |
| 5 | Keep manual node positions when the same level is shown again? | No; layout restarts on every `SetGraph` | Phase 6 |
| 6 | Markers show POI name; what if the name is null? | POI type name | Phase 3 |

## Decisions log

| Date | Decision |
|---|---|
| 2026-11-12 | Draw with one canvas element in immediate mode; shape runtimes are the fallback. |
| 2026-11-12 | Canvas = own renderable deriving from the public `SkiaGum.Renderables.RenderableShapeBase`, overriding `DrawBound(SKRect, SKCanvas, float)`, wrapped in an `InteractiveGue` with `HasEvents = true`. |
| 2026-11-12 | All six open decisions accepted with their defaults (user). |
| 2026-11-12 | Input is polled from `GumService.Default.Cursor` each frame (not Gum events), which fits the per-frame input record of `GraphVisInteraction`. |
| 2026-11-12 | Scroll bars are standalone Gum Forms `ScrollBar` controls; no custom scroll bar needed. |
| 2026-11-12 | Component model is generic (markers, dashed, labels); level meaning is applied only by the adapter. |

## Findings log

Filled during Phase 0 and later.

| Date | Finding |
|---|---|
| 2026-11-12 | **Canvas API.** `CanvasRenderable` is `internal` (compile error CS0122). Subclassing `RenderableShapeBase` and overriding `DrawBound` works on Stride and is called once per frame (render count tracks update count). |
| 2026-11-12 | **Coordinate space.** The canvas arrives in window space: identity matrix, no translation, clip = whole window. The `SKRect` passed to `DrawBound` is the element rectangle in window pixels (equal to `AbsoluteLeft/Top/Width/Height`). The renderer must `Save`, `Translate(rect.Left, rect.Top)`, `ClipRect` and `Restore` itself; with that, drawing is clipped correctly. |
| 2026-11-12 | **Primitives.** Anti-aliased thick circles, dashed outlines and lines (`SKPathEffect.CreateDash`), arrowheads and text all render well. `SKPath.MoveTo/LineTo/Close` are obsolete in SkiaSharp 4.153; use `SKPathBuilder` + `Detach()`. Text API: `SKFont` + `canvas.DrawText(text, x, y, SKTextAlign, font, paint)`, width via `font.MeasureText`. |
| 2026-11-12 | **Font.** `SKTypeface.FromFile(EditorFonts.ResolvePath(EditorFonts.RegularFile))` at size 18 looks the same as a Gum `Label` (Gum default: Inter, size 18). `TextRuntime.FontSize` is not an `int`. |
| 2026-11-12 | **Pointer.** `Cursor.X/Y` equal `XRespectingGumZoomAndBounds()` and are window pixels; local position = cursor minus the element's `AbsoluteLeft/Top`. `Cursor.VisualOver == canvasElement` is a reliable "pointer over viewport" test. `PrimaryPush`/`PrimaryDown` polling is enough for dragging. Gum events `Push`, `Click`, `Dragging`, `RollOn`, `RollOff`, `RightClick`, `MouseWheelScroll` also fire on the `InteractiveGue`. |
| 2026-11-12 | **Wheel.** `Cursor.ScrollWheelChange` and Stride `Input.MouseWheelDelta` both report 1 per notch. |
| 2026-11-12 | **Keyboard and camera.** Raw `Input.IsKeyDown` works for WASD panning. OR-ing "pointer over viewport" into the `GameInput` capture predicate stops the camera (it moved again once the pointer left the panel). Right-drag over the canvas does not rotate the camera because the element has `HasEvents`. |
| 2026-11-12 | **Scroll bar.** A standalone horizontal `ScrollBar` (`Minimum`, `Maximum`, `ViewportSize`, `ValueChanged`) renders with the editor theme and drives an offset when its thumb is dragged. Thumb length is `ViewportSize / (Maximum - Minimum + ViewportSize)`, so `Maximum` = content size minus viewport size. Vertical orientation and setting `Value` from code were not tried. |
| 2026-11-12 | **Load.** 200 circles with outlines, 200 lines (a quarter dashed) and 200 texts, all moving: about 1.8 ms per render in a Debug build, creating paints and fonts every frame. No fallback needed. Frame time was about 18 ms with the spike; the baseline without it was not measured. |
| 2026-11-12 | **Design note for Phase 2.** Marker labels of neighbouring dots overlap ("Start" / "Red Key" ran together at 10 px); marker placement must space dots by label width or stagger labels. |
| 2026-11-12 | **Method note.** Input findings come from synthetic mouse/keyboard input sent to the running app and read off screenshots, not from a person using it. |

## Session log

| Date | Phase | Done | Next |
|---|---|---|---|
| 2026-11-12 | – | Research of Gum drawing options; this plan written. | Start Phase 0. |
| 2026-11-12 | 0 | Spike panel built (`Spike/GraphVisSpike.cs`), run and measured; findings and decisions recorded. Build and 146 tests green. | User verification; remove spike; Phase 1. |

## Further research needed

- Vertical ScrollBar and two-way sync (setting Value from code while panning) – check in Phase 5.
- Behaviour on window resize and on non-100 % display scaling – check in Phase 2 / Phase 7.
- Baseline frame time without the canvas, to confirm the canvas costs nothing noticeable – check in Phase 2.
