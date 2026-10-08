# Ruilay SFX polish

The existing persistent `GameFeedback` service remains the sole audio owner.
`FeedbackSettings` retains the sound/haptics toggles and adds a global SFX gain.
The SFX pass added no music, ambience, loops, external services or dependencies.
Independent background music was integrated later; see [background-music.md](background-music.md).

## Original local assets

`python tools/generate_sfx.py` deterministically rebuilds mono 44.1 kHz, 16-bit
WAVs using only Python math, seeded noise and soft attack/release envelopes.
Assets are cached once from `Assets/Resources/Audio/SFX` in the feedback service.
Import uses PCM, decompression on load, preloaded data and no normalization.
The old procedural clip utility remains available for compatibility and its
existing tests; normal runtime playback no longer synthesizes clips.

| WAV | Duration | Trigger |
| --- | --- | --- |
| PipeRotate | 95 ms | Accepted manual tile rotation; pitch 0.96–1.04 |
| UiClick | 65 ms | Real uGUI Button onClick, including keyboard submit |
| Hint | 320 ms | Hint successfully applied and budget consumed |
| WaterFlow | 800 ms | Existing solved-path visual begins; pitch adapts to its travel duration |
| TargetReached | 85 ms | Existing target arrival, once per flow owner |
| LevelComplete | 940 ms | Existing delayed completion callback |
| WorldUnlock | 1350 ms | Newly unlocked following world; queued after completion chime |

WaterFlow uses a quiet, filtered noise texture with 110 ms attack and 220 ms
release. The previous sustained 620/930 Hz electronic tones were removed;
its existing 800 ms duration and visual synchronization are preserved.

Manual rotation still uses the existing 140 ms animation and controller guards.
Locked pipes, hint locks, invalid/stale views and pending rotations are rejected
before requesting sound. Failed hints do not request the Hint cue. Clicking a
valid Hint button also produces the quiet UI click, independently of hint success.

Four nonlooping 2D SFX AudioSources live on the single feedback object: rotation,
UI, flow/target and hint/completion/unlock. Each voice replaces its previous clip;
there is no unbounded PlayOneShot stacking. UI/rotation have 40/60 ms cooldowns.
The target chime uses the flow voice; completion follows the unchanged visual
arrival hold. The unlock cue waits for completion audio plus 60 ms.

`WorldProgressService.MarkWorldCompleted` reports whether its existing save
operation actually unlocked the next world. It retains the existing keys,
world mapping and writes. Only the gameplay completion handler uses this return
value to request audio; map refresh and replay cannot request another unlock.

`UiSfxFeedback` attaches one listener per Button, without local AudioSources.
`ScreenManager` binds existing/inactive buttons at startup and screen changes,
then once on the following frame for buttons created by panel refresh. Sliders
do not receive this component. Settings supplies a transparent 44 px hit area.

## Volume

Settings displays `SFX VOLUME` and a 0–100% slider using the current typography.
The default is 75%, stored as a clamped float in
`PipeMuzzle.Feedback.SfxVolume`. NaN/infinity revert to the default.
The SFX sound toggle remains an independent gate. Changing SFX volume applies
immediately to its four voices. Zero or SFX sound OFF stops SFX playback and cancels a
pending unlock cue; raising volume does not replay skipped events.
The persistent service and PlayerPrefs preserve the setting across scenes.

## Verification

Run Unity EditMode tests, `Ruilay/Validate All Levels`,
`python -m unittest discover -s tools -p 'test_*.py'` and
`dotnet build PipeMuzzle.slnx --no-restore -m:1 -v:minimal`.

For manual listening, enter Gameplay, rotate a valid pipe, try Hint, solve the
path and wait for target/completion. Replay and reopen the map to check that
unlock audio only occurs on first progression. In Settings, drag the slider,
verify zero silence, reopen Settings and reload the scene to verify persistence.
Listen on target speakers/headphones to judge mix and timbre.

Automated Play Mode checks inspect the audio voices and sample Unity's actual
mixed output; this verifies emission and mute, not physical speaker audibility
or subjective sound quality. Device/player builds require a separate check.

## Files

- `Assets/Scripts/Feedback/GameFeedback.cs`, `FeedbackSettings.cs`
- `Assets/Scripts/Gameplay/GameController.cs`, `WorldProgressService.cs`
- `Assets/Scripts/UI/UiSfxFeedback.cs`, `ScreenManager.cs`, `SettingsUI.cs`
- `Assets/Resources/Audio/SFX/*.wav` and required importer `.meta` files
- `tools/generate_sfx.py`
- `Assets/Tests/EditMode/Editor/SfxPolishTests.cs`, `SettingsTests.cs`, `WorldProgressServiceTests.cs`

Existing world clip override fields are preserved. Gameplay/flow timings,
hint budgets, progression keys, level assets, artwork and typography assets
are unchanged by this pass. No commit or push was made.
