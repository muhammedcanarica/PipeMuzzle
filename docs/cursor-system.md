# Ruilay cursor

The desktop cursor uses native `Cursor.SetCursor` with `CursorMode.Auto`.
`ScreenManager` loads `Resources/UI/CursorManager.prefab` once; the manager
survives scene loads. No scene YAML or existing artwork has been changed.

## Configuration

Select `Assets/Resources/UI/CursorManager.prefab` in the Inspector. Its four
texture/hotspot pairs are Default, Hover, Rotate and Pressed. The PNG sources live
in `Assets/Art/UI/Cursor/`. All four import as readable, uncompressed RGBA32 cursor
textures at **32x32**, with alpha, bilinear filtering and no mipmaps. The original
imagegen PNGs retain their generated 1261x1247 resolution; Unity normalizes them
to square power-of-two textures during import. There are no runtime resize calls.

All hotspots are **(3, 3)** in imported pixels from the upper-left corner. This
matches the pointed upper-left petal tip, rather than the texture center. Keep
this tip aligned when replacing an image; update the corresponding hotspot if
its position changes. Oversized hotspot values are clamped to texture bounds.

## Behavior

| State | Trigger |
| --- | --- |
| Default | Empty area, disabled UI or a pipe that cannot rotate |
| Hover | Active, interactable Button, including level/world nodes |
| Rotate | Current normal pipe accepted by controller interaction guards |
| Pressed | Left mouse press, briefly overriding hover for 0.09 unscaled seconds |

Source/target, permanent locks, hint locks, empty tiles, pending tile rotation,
pause, completion and stale board membership reject Rotate. Existing clicks,
audio subscriptions, rotation animation and navigation remain unchanged.

`UiSfxFeedback` forwards pointer events through its existing shared Button binding.
`TileView` forwards its existing EventSystem pointer events. There is no additional
raycast or per-frame resource loading. Cached target eligibility is rechecked so
stationary-pointer hints and disabled buttons immediately return to Default.

Exit/disable/destruction releases only the matching target. Screen/scene/focus
changes clear hover and press state. After a reset, the manager can reuse the
input module's cached mouse event/raycast without casting another ray. Its current
device is checked because the Input System reuses mouse/pen event records. Touch,
pen and keyboard/controller selection do not create mouse hover or press state.

A missing Hover/Rotate/Pressed texture falls back to Default. A missing Default
restores the system cursor, even if another variant is assigned. Texture/hotspot
presentation changes are cached; focus recovery may deliberately reapply them.

## Verification

Focused EditMode tests cover ownership, mouse/pen/touch filtering, press expiry,
screen/focus cleanup, missing textures, import configuration and pipe guards.
The existing UI/audio click contract is covered as well.

- Before implementation, 20 cursor tests failed because CursorManager did not
  exist; after implementation all passed.
- The prefab/import test failed before prefab creation and then passed.
- Mouse-to-pen ownership and missing-Default regressions failed before their fixes.
- Final focused cursor tests: **23 passed, 0 failed**.
- Final entire EditMode suite: **691 passed, 0 failed, 0 skipped**.
- Python tests: **9 passed, 0 failed**.
- Unity level validation: **36 levels, 0 errors**.
- Editor runtime: button Hover on World Map, Comic, Level Select, Settings and
  Gameplay; empty areas Default, except Comic's full-screen AdvanceButton.
- WebGL production build succeeded in 290.9 seconds under
  `Builds/CursorVerification/WebGL`. Build report: two Unity Cloud authentication
  errors (HTTP 401), zero warnings; these did not prevent player compilation.
- In-app browser: World Map → Comic → Level Select → Gameplay and Settings/Back
  navigation worked. DOM canvas cursor data used a 32x32 CUR with hotspot (3,3).
  Actual UI Hover and normal-pipe Rotate used different images from Default;
  Start, End, hint-locked pipe and empty space returned to Default. Left click
  rotated a pipe and completed level 1 normally. Right clicks were used to inspect
  hover without changing puzzle state. No browser errors; one existing Unity
  persistent-data synchronization deprecation warning.
- Permanent locks, stationary hint changes, short Pressed duration and focus
  reset are covered by EditMode tests; they were not all independently exercised
  in the browser. An earlier Editor pipe-position probe was inconclusive and is
  not counted as a passed runtime check.
- Build output and verification caches are ignored; Git index remains empty.

The build's TMP preprocessor cleared dynamic glyph/atlas caches in five existing
font assets. A hash audit detected this; no font source, scene, audio or level was
intentionally edited. `UnityConnectSettings.asset` was restored byte-for-byte to
the task baseline. Font cache restoration is pending explicit permission after
automatic approval review rejected writing to these protected assets. The older
font backup preserves their visual settings but does not match the task baseline
byte-for-byte. Do not treat the protected-font hash audit as passed.

Manual checks: open World Map, Comic, Level Select, Settings and Gameplay; hover
enabled/disabled buttons, normal/locked/hint-locked/source/target pipes, click and
leave targets, restart, pause, switch panels, and refocus the app/browser. Cursor
state should recover without changing input behavior. Native cursor size can vary
with browser/platform and OS display scale; 32 imported pixels are the logical size.

## Artwork provenance

Generated with the built-in imagegen tool on 2026-10-07; no image API dependency
was added. Prompt set:

- Default: standalone transparent sakura petal/pointer, pointed upper-left click
  tip, cream/pastel pink watercolor, thin warm rose outline, subtle shadow,
  readable at 32 pixels, no text, pixel art, glow or backdrop.
- Hover: preserve Default silhouette, tip and canvas; add restrained pink accents
  and one tiny four-point sparkle in the upper-right transparent margin.
- Rotate: preserve Default petal and tip; add a small secondary muted rose circular
  arrow in the upper-right transparent margin.
- Pressed: preserve styling and pointed tip; shrink the petal body about 8 percent
  toward that tip, with a slightly richer pink edge and no additional symbol.
