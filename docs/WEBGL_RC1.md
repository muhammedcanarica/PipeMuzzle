# WebGL RC1

Unity: **6000.3.9f1**. Release version: **0.9.0-rc1**.

## Rebuild and serve

1. Open Build Profiles and select `Assets/BuildProfiles/WebGL RC1.asset`.
2. Build to `Builds/WebGL/RC1`. Development Build, Script Debugging,
   Autoconnect Profiler and Deep Profiling are disabled.
3. From the repository root run:

   ```powershell
   python tools/serve_webgl.py Builds/WebGL/RC1 --port 8765
   ```

4. Open `http://127.0.0.1:8765` in a WebGL 2 capable browser.

The profile has a Unity Player Settings override snapshot; its changed values
are the Web release version, resolution, template and compression. Global
Android/iOS/desktop Player Settings are preserved. Code optimization is
`RuntimeSpeed`, with IL2CPP and debug symbols stripped. The RC1
template derives from Unity's standard Default template, fits a 16:9 canvas up
to 1280×720 into the available window, and retains Unity's fullscreen button.

## Hosting

Gzip compression is enabled and Decompression Fallback is disabled. Serve `.gz`
files with `Content-Encoding: gzip`; serve compressed WASM with
`Content-Type: application/wasm` and JavaScript with
`Content-Type: application/javascript`. The local server supplies these headers.

[Unity deployment documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-deploying.html)
explains these requirements and the effect of JavaScript decompression fallback
on WASM streaming. [itch.io HTML5 documentation](https://itch.io/docs/creators/html5)
documents Gzip detection, Brotli headers, and upload limits. Both compression
formats are supported; Gzip was selected for this RC and tested locally with
the required headers. No itch.io upload has been performed.

Build output and local QA captures remain under ignored `Builds/` and `Temp/`.
For itch.io, zip the contents of the build directory with `index.html` at the
archive root. Do not include QA captures or local tools. Preserve the hosting
URL between versions when testing browser save continuity; a different origin
or persistent-data path has a separate browser save.

## Validation record — 6 October 2026

The production build succeeded in 39.9 seconds on the cached retry, with **0
errors and 0 warnings**. The initial cold build was interrupted after 1,205
seconds with `TaskCanceledException`, after its WASM link step had completed.
The log does not identify the cancellation source. The same configuration
succeeded on retry; no gameplay fix or reduction in production optimization
was needed.

| Output | Gzip/on-disk bytes | Uncompressed bytes |
| --- | ---: | ---: |
| Complete build directory | 181,190,666 (172.80 MiB) | — |
| `RC1.data.gz` | 168,028,473 (160.24 MiB) | 183,643,871 |
| `RC1.wasm.gz` | 13,027,334 (12.42 MiB) | 49,484,683 |
| `RC1.framework.js.gz` | 88,149 (0.084 MiB) | 442,037 |
| `RC1.loader.js` | 26,982 | — |

The data payload dominates download size. Large Resources illustrations include
the Sakura/Bamboo/Moon destination and final artwork (roughly 2.2–2.8 MiB each
as source PNGs). No artwork quality or texture compression was changed. This
build fits itch.io's documented 200 MB per-file and 500 MB extracted-upload
limits, but download size warrants a separate optimization pass before a wider
release.

Browser testing used the Codex in-app Chromium browser over local HTTP, with
correct Gzip/WASM headers. Completed checks:

- Fresh save: World Map → Sakura intro → Level Select → Sakura 1 tutorial.
- Comic Next, Back to Map, and Skip to Level Select.
- Pipe click/rotation → visible flow → delayed completion → Next Level.
- Unassisted Level 1 BEST 1 and Level 2 BEST 3; assisted Level 1 completion
  preserved BEST 1.
- ESC Pause, Resume, Pause → Settings → Back, Sound OFF/ON, and gameplay
  return to Level Select.
- Refresh preserved Sound OFF, levels 1–3 unlock, and tutorial-seen state.
  A subsequent five-move Level 1 completion still showed BEST 1, confirming
  that a worse result did not replace the persisted best.
- Sakura 3 solved in four moves → checkpoint comic → Skip → Level Select,
  with Level 4 unlocked.
- World Map at 1366×768 and Level Select at 1920×1080 rendered without observed
  clipping. The canvas was 1280×720. The main mouse smoke flow also worked in
  the narrow default browser panel, approximately 610×518 with a 610×343 canvas.

No uncaught exception, missing asset, null-reference spam, audio error,
PlayerPrefs exception or unsupported-API error was observed. One benign Unity
warning was observed: manual `JS_FileSystem_Sync()` is deprecated in favor of
automatic persistent-data synchronization. Existing PlayerPrefs behavior was
preserved. Intro comics are intentionally replayable; they are separate from
persisted level checkpoints.

Rotation, flow, map particles and comic transitions showed no obvious freeze
during the smoke flow. This is a qualitative observation, not an FPS benchmark
or a cross-browser performance certification. Audio controls and the lack of
audio errors were checked, but this environment did not provide audible output
for confirming the four individual sound effects.

The browser session was interrupted, so the following checks remain **untested
in RC1**:

- A 1280×720 browser window and fullscreen enter/exit with gameplay input.
- Audible pipe rotation, flow, target-arrival and completion sounds after the
  first interaction and after Sound OFF/ON.
- Refresh persistence of newly unlocked Bamboo/Moon worlds and viewed level
  checkpoints.
- Bamboo 3 and Moon 3 checkpoint navigation.
- Moon 12 → final comic → Journey Complete → Credits → World Map.
- Reset Progress in the browser and additional browsers/real iframe hosting.

The available progress debug menu is Editor-only; no debug helper or new
gameplay feature was added to the production player. These validation gaps
remain release gates before uploading to itch.io; no confirmed gameplay blocker
was found in the completed flow.

Before and after the build: **615/615 EditMode tests passed**, **36/36 levels
validated with zero errors**, and `dotnet build PipeMuzzle.slnx --no-restore -m:1`
reported **0 errors / 0 warnings**. The final Python suite passed **9/9**, including
the HTTP header/404 regression check; the previous eight tests remain passing.
An initial HTTP-test fixture failed because Windows denied access to a directory
created with `tempfile.TemporaryDirectory`; the fixture now creates an ordinary
unique directory under ignored `Temp/`, and the rerun passed. `git diff --check`
passed. Build/test-generated settings changes were excluded and restored.

Changed source files: the RC1 Build Profile and required `.meta` files, the
Default-derived `Assets/WebGLTemplates/RC1` template and required `.meta` files,
`tools/serve_webgl.py`, `tools/test_serve_webgl.py`, and this report. No gameplay
script, global Player Settings, level asset, scene or generated build file was
changed for the release preparation.

Local evidence: `Temp/rc1-webgl-smoke.png`, `Temp/rc1-size-report.json`. These
files are ignored and are not part of the upload or source commit.
