# Ruilay Cursor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Add the approved sakura cursor across Ruilay screens without changing input behavior.

**Architecture:** One persistent native CursorManager receives the existing UI and tile pointer events. Cached target eligibility selects Default, Hover or Rotate; a short unscaled Pressed state overrides those temporarily. A Resources prefab holds Inspector texture/hotspot configuration without scene YAML changes.

**Tech Stack:** Unity 6000.3.9f1, C#, uGUI/EventSystem, existing Input System, transparent PNGs, Unity EditMode tests.

**Spec:** `docs/superpowers/specs/2026-10-07-ruilay-cursor-design.md`

## Global Constraints

- Work on `main` only. Do not create branches or worktrees, commit or push.
- Preserve unrelated working-tree edits and existing gameplay, scene flow, artwork, audio and UI language.
- No new package dependency is required.
- Native cursor uses `CursorMode.Auto`, 32x32 imported textures and per-texture upper-left pixel hotspots.
- No extra raycast, per-frame resource loading or board scanning; SetCursor only when presentation changes or focus recovery requires it.
- Use existing button binding and tile EventSystem handlers; do not consume or replace existing click events.
- Generate WebGL output only under ignored `Builds/`; distinguish unavailable runtime checks from passing checks.

## Review Focus

- Hint locking under a stationary pointer immediately removes Rotate: Task 2 eligibility tests.
- Disabled UI targets and destroyed tiles release cursor ownership: Task 1 lifecycle tests.
- An old exit event cannot clear a newer hovered target: Task 1 ownership tests.
- Touch, pen and keyboard navigation do not generate mouse cursor hover/press: Tasks 1 and 2 pointer-filter tests.
- Focus loss/re-entry and screen changes cannot preserve stale Pressed/Rotate: Tasks 1 and 3 reset tests and browser check.

## File Structure

- Create `Assets/Scripts/UI/CursorManager.cs`: native cursor configuration, lifecycle, ownership and cached-target state resolution.
- Create `Assets/Resources/UI/CursorManager.prefab`: Inspector texture/hotspot references.
- Create four PNGs in `Assets/Art/UI/Cursor/`: default, hover, rotate, pressed.
- Modify `Assets/Scripts/UI/UiSfxFeedback.cs`: reuse its Button binding for cursor pointer notifications.
- Modify `Assets/Scripts/View/TileView.cs`: add cursor pointer notifications without replacing clicks.
- Modify `Assets/Scripts/Gameplay/GameController.cs`: expose current-tile interaction eligibility derived from existing guards.
- Modify `Assets/Scripts/UI/ScreenManager.cs`: bootstrap cursor and release ownership on screen changes.
- Create `Assets/Tests/EditMode/Editor/CursorManagerTests.cs` and `docs/cursor-system.md`.
- Include required Unity folder, script, prefab and PNG metadata. Do not edit `Assets/Scenes/Gameplay.unity`.

### Task 1: Native cursor state and artwork

**Interfaces:** CursorManager exposes `EnsureInstance()`, `ResetState()`, `EnterButton(Button, PointerEventData)`, `EnterTile(TileView, PointerEventData)`, `ExitTarget(UnityEngine.Object, PointerEventData)` and read-only `CurrentState`. Its four-state enum is `RuilayCursorState`. Mouse filtering uses ExtendedPointerEventData's device/pointer type for Input System and the legacy mouse pointer IDs for legacy events.

- [ ] Read imagegen, Unity MCP and test-driven-development skill instructions before their respective operations. Record current status and hashes of protected files, with a separate baseline for the four integration files.
- [ ] Confirm cursor import/readability requirements and Input System pointer-type API from installed Unity/package sources or official documentation. Confirm an active Unity instance and available test tools without changing the scene.
- [ ] Write focused tests in CursorManagerTests for missing variants falling back to Default, missing Default restoring system cursor, target identity on exit, reset/lifecycle cleanup, mouse filtering and Pressed expiry after 0.09 unscaled seconds. Run them and record the actual failing result; do not claim a red run if compilation/tooling prevents execution.
- [ ] Generate a coherent transparent sakura cursor family using the image generation workflow. Verify each image visually; normalize source canvases to 64x64 if necessary through the approved image workflow. Keep the pointed click tip aligned between variants; Pressed shrinks the body around that tip.
- [ ] Implement CursorManager with cached textures, target references and native presentation. Expose only four texture/hotspot pairs in the Inspector. Validate hotspots against imported dimensions, cache the applied texture/hotspot and clear static ownership during play-mode initialization with domain reload disabled.
- [ ] Use the existing Input System mouse device for left-press detection, including blank areas, and the legacy input fallback only when enabled. Resolve cached target eligibility in LateUpdate; no raycast or allocation is needed. Scene/focus lifecycle clears targets; focus recovery can reapply the cursor.
- [ ] Create the Resources prefab and configure PNG import settings: Cursor texture type, readable pixels, 32 maximum size, transparency, no mipmaps, no compression. Measure and set hotspot coordinates against the imported images.
- [ ] Run state/import tests and inspect all actual images and prefab references. Record passing results or explicit unavailable-check limitations. No commit.

### Task 2: Preserve UI and pipe interaction semantics

**Consumes:** Task 1 CursorManager interfaces.

**Produces:** `GameController.CanInteractWithTile(TileView tileView) : bool` and cursor event integration in existing UI/tile components.

- [ ] Add eligibility tests for normal pipes, locked/hint-locked/source/target/empty pipes, pending tile rotation, stale board membership, inactive controller, paused/completing/completed states. Include a stationary-pointer hint-lock transition. Add integration tests that existing Button and TileView click listeners still fire exactly once.
- [ ] Run the focused tests before changing interaction code and record failures.
- [ ] Implement CanInteractWithTile from the existing HandleTileClicked guards and tile rules. Keep accepted/rejected clicks and existing side effects equivalent; do not expand rotation eligibility or alter completion timing.
- [ ] Extend UiSfxFeedback with pointer enter/exit/down/up interfaces and disable cleanup. Keep its existing audio onClick subscription untouched. Forward only mouse events; query Button.IsInteractable and active/enabled state, including CanvasGroup effects.
- [ ] Extend TileView with pointer enter/exit/down/up interfaces and disable cleanup. Leave its IPointerClickHandler contract and animations intact. Resolve GameController once per relevant lifecycle/event, not through repeated per-frame scene searches.
- [ ] Verify UI priority, noninteractive overlay occlusion, target exit order, non-mouse input and existing LevelSelectNodeFeedback coexistence. Run focused tests plus relevant GameControllerWorldTests, GameplayPauseTests, HintAndBestMovesTests and SettingsTests. No commit.

### Task 3: Bootstrap, cross-screen verification and delivery

**Consumes:** Tasks 1 and 2 cursor lifecycle and eligibility interfaces.

- [ ] Add bootstrap/screen-change tests proving exactly one manager, Resources prefab references, reset on screen changes, and preservation of existing button-binding/navigation calls.
- [ ] Initialize CursorManager from ScreenManager and reset targets during SetScreen. Preserve BindButtonSounds and delayed BindCreatedButtons so dynamically created buttons receive the same integration. Do not modify serialized scene content.
- [ ] Run focused EditMode and existing related tests; run the existing level validation entry point in non-mutating mode and Python tests with the bundled runtime: `python -m unittest discover -s tools -p 'test_*.py' -v`.
- [ ] In Play Mode verify Default, button Hover on every named screen, normal-pipe Rotate, all disallowed-pipe cases, blank-area/target Pressed, hover exit, stationary hints, restart, pause/settings/completion, focus changes and no new console errors/warnings.
- [ ] Build WebGL using the existing project build entry point to ignored Builds/. Test the actual browser cursor, transparency, imported size, hotspot, button/pipe clicks and focus re-entry when tooling is available. Inspect console/build output; never claim a browser test from a successful build alone.
- [ ] Write docs/cursor-system.md with Inspector setup, measured hotspot values, state behavior, executed tests and browser limitations. Review task-only diffs and protected-file hashes against baseline; verify build output is absent from status and required metadata is present.
- [ ] Report added/changed files, UI/gameplay integration, hotspot values, WebGL results and remaining limitations. Leave changes uncommitted and do not push.

## Execution Choice

Recommended: native execution in this chat, because the three tasks share the same small set of interaction interfaces and protected working-tree changes. Await the user's plan review and execution-method choice before implementation. No subagents are dispatched merely by writing this plan.
