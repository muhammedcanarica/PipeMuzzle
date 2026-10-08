# Ruilay custom cursor design

Date: 2026-10-07
Status: In-chat design and written specification approved by the user.

## Purpose and boundaries

Replace the desktop pointer inside Ruilay with a small, readable sakura cursor
that matches its cream, pastel pink, hand-painted presentation. Cover World Map,
Comic/Story, Level Select, Gameplay and Settings, including dynamically created
buttons. Preserve existing click behavior, puzzle rules and screen navigation.

Work on `main` only. Do not create branches or worktrees, commit or push. Preserve
unrelated working-tree edits. Change only cursor assets, cursor code, narrowly
related interaction hooks, focused tests and cursor documentation. Do not alter
existing art, fonts, audio, levels, gameplay timing or serialized scene content.
No new package dependency is required.

## Approach

Use native `Cursor.SetCursor` with `CursorMode.Auto`. This avoids a per-frame
pointer-following UI overlay and keeps cursor size independent of Canvas scaling.
An overlay alternative would add tracking and canvas integration; a central
per-frame raycast alternative would duplicate the existing EventSystem work.

Add one `PipeMuzzle.UI.CursorManager`, configured through a small Resources prefab
with texture and hotspot references. Initialize it from the existing
`ScreenManager` lifecycle without editing the scene YAML. Keep one manager across
scene loads, avoid duplicates and restore the system cursor when its lifetime
ends. Do not lock the pointer or hide it when no mouse is present.

## States and artwork

- Default: cream/pink sakura petal with an unmistakable pointed click tip.
- Hover: the same petal with a restrained pink accent and tiny sparkle.
- Rotate: the same petal with a small secondary circular arrow.
- Pressed: a briefly smaller petal within the same texture canvas; preserve the
  tip and hotspot rather than scaling a native cursor every frame.

Create transparent PNG sources under `Assets/Art/UI/Cursor/`, preferably 64x64.
Import cursor textures at 32x32, with alpha, readable pixels, no mipmaps and no
compression. Confirm the required cursor import settings against this Unity
version before implementation. Use a warm fine outline and a subtle shadow;
avoid pixel art, neon or excessive glow.

Each texture has its own Inspector hotspot in imported texture pixels, measured
from the upper-left corner. Align all variants to the same visible pointed tip.
Determine the final numeric coordinates from the actual artwork. Missing Hover,
Rotate or Pressed references fall back to Default; missing Default restores the
system pointer without repeated error messages. Cache references and call
`SetCursor` only when the effective texture/hotspot changes or focus recovery
requires reapplying it.

## UI and gameplay integration

Reuse `UiSfxFeedback.BindHierarchy`, which already binds real Buttons during
screen changes. Add pointer enter/exit/down/up notifications to that existing
shared component instead of adding a separate cursor script to every button.
Only active, interactable buttons with an enabled component may advertise Hover.
Preserve their existing onClick/audio listeners and LevelSelectNodeFeedback.

Add pointer enter/exit/down/up notifications to `TileView`, which already receives
clicks through the existing Physics2DRaycaster and InputSystemUIInputModule.
Do not introduce a second raycast path or consume pointer events.

Rotate requires a current board tile that the active GameController can accept:
normal role, nonempty shape, no permanent/hint lock, no pending tile rotation,
and no paused/completing/completed state. Expose a narrow eligibility query based
on the existing click guards; preserve the order and effects of click handling.
Check the cached hovered tile/button eligibility when it can change under a
stationary pointer, including after hints, rotation and completion. This check
must not scan the board, load resources or allocate every frame.

UI Hover takes precedence over pipe Rotate. A noninteractive UI surface must not
permit a stale pipe target behind it to retain Rotate. Exit, disable, destruction,
screen changes, scene changes and lost focus release stale hover/press ownership.
Use unscaled time for a short left-mouse Pressed indication, then resolve the
current target again. Touch and keyboard/controller navigation must not create
mouse-hover or pressed cursor state. Treat non-mouse pointer IDs according to the
active Input System module rather than assuming every pointer event is a mouse.

## Expected files

- New: CursorManager script and metadata; Resources cursor prefab and metadata;
  four PNGs and required folder/texture metadata; focused EditMode cursor tests
  and metadata; cursor verification documentation.
- Narrow edits: UiSfxFeedback, TileView, ScreenManager and GameController.
- Preserve Assets/Scenes/Gameplay.unity and all unrelated existing edits.

## Verification and acceptance

1. Compile scripts and run focused EditMode tests: state precedence, fallback,
   mouse filtering, disabled targets, cleanup, and pipe eligibility including
   locked/source/target/empty/hint-locked/paused/completing cases.
2. Run relevant existing interaction tests, level validation and Python tests.
3. Inspect actual PNG transparency, size, shared tip/hotspot and importer settings.
4. In Play Mode, verify World Map, Comic, Level Select, Settings and Gameplay;
   button/pipe exits, stationary-pointer hints, pause/completion, screen changes,
   brief presses, unchanged clicks and absence of new console warnings/errors.
5. Build WebGL into ignored Builds/ and, if available, verify the actual cursor,
   alpha and hotspot in a browser. Review focus loss/re-entry and browser fallback.
   Do not claim browser validation from API compatibility or compilation alone.
6. Review the final diff for scope and report executed checks, unavailable checks
   and material limitations. Never commit or push this task.

Native pointer presentation can vary with browser/platform and OS display scale.
The 32-pixel import fixes the logical cursor size; verify visual size on available
desktop and browser environments. If Unity or browser checks are unavailable,
report that explicitly rather than treating the feature as visually verified.
