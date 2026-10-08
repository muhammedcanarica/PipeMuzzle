# Background music

Music extends the existing persistent `GameFeedback` object. Its four SFX
voices are unchanged; two additional looping 2D voices handle music. There
is no second audio manager, per-level source, or fade coroutine.

## Inspector setup

Select `Assets/Resources/Audio/MusicTracks.asset` in Unity's Project window.
Its four AudioClip fields are already assigned:

| Field / context | Clip in `Assets/Audio/Music` |
| --- | --- |
| Main / World Map | `turning_pages-level-select-screen-602555.mp3` |
| Sakura Garden | `pianocafe_kumi-japanese-calm-pianomoon-411035.mp3` |
| Bamboo Workshop | `solarflex-cozy-chill-lounge-music-491499.mp3` |
| Moon Shrine | `tomomi_kato-crescent-moon-173121.mp3` |

The user-provided files were copied unchanged from Downloads/Music. Import
uses Streaming, 2D audio and no normalization. GameFeedback loads the catalogue
from `Resources/Audio/MusicTracks`; its optional Inspector catalogue field can
override this default when placing the existing feedback component explicitly.
Missing clips issue one warning per context and leave valid playback intact.

## Selection and transitions

The existing entry screen is World Map; there is no separate Main Menu scene.
`ScreenManager.ShowWorldMap` requests Main. A future Main Menu can call the same
`GameFeedback.PlayMainMusic` API and transition to World Map without restarting.

Successful `LevelSelectUI.ConfigureForWorld` supplies the selected `WorldId`
to ScreenManager. Showing Level Select or Gameplay requests that world's clip.
Every level in the world uses the same track. Next, restart, completion and
returning from Gameplay to Level Select keep the current playback position.
Settings, pause, comics and journey completion retain the current music.
Scene names are not used to choose tracks.

Repeated requests for the desired AudioClip do nothing: no Play, seek or fade
restart. A different clip starts silent on the unused voice and crossfades
with a one-second SmoothStep envelope. Fade time is unscaled, so pause does
not stop music or transitions. The old voice stops only after reaching zero.

During a fade, further requests replace one pending target. The current mix
finishes smoothly before reusing the silent voice for the latest target. This
prevents audible cuts and stacked sources. Under rapid navigation the final
request can take up to about two seconds to settle; intermediate queued tracks
are skipped. At most two music sources play at once.

`GameFeedback` retains its existing singleton guard and DontDestroyOnLoad
ownership. Recreated scene UI requests the logical screen/world context from
the same owner. Returning to the same context preserves position; entering a
different context crossfades even if the Unity scene itself did not change.

## Independent settings

Music Volume defaults to 40%, clamped to 0–100%, and persists as
`PipeMuzzle.Feedback.MusicVolume`. The existing SFX preference and gain are
independent. Settings labels the existing toggle `SFX SOUND` to clarify that
it does not mute music. The music slider applies immediately to both voices.
Music volume zero silences them while playback continues, so unmuting does
not restart the track. SFX zero / OFF does not affect music.

## Verification

Run Unity EditMode tests, `Ruilay/Validate All Levels`,
`python -m unittest discover -s tools -p 'test_*.py'`, and
`dotnet build PipeMuzzle.slnx --no-restore -m:1 -v:minimal`.

Manual check: launch World Map, open each world's Level Select, play a level,
complete it and select Next, return to Level Select, then return to the map.
Verify music continuity within each world and smooth changes across worlds.
Change Music and SFX sliders independently, including zero and gameplay pause.
Check stored values after reopening Settings and scene reload.

EditMode tests cover persistent independent gains, same-track requests,
rapid pending requests, silent music continuation, missing-clip tolerance,
and all four streaming catalogue mappings. Play Mode verification uses actual
AudioSource sample positions and Unity mixer output. Physical speaker listening
and a fresh player/device build remain separate checks.

Verified on 2026-10-06: 668/668 full EditMode tests passed; 27/27 focused
music/settings tests passed after the final slider geometry adjustment.
All 40 Play Mode checks passed, including actual sample-position continuity,
the three worlds' solved Level 1 → Next flow, rapid changes, pause, independent
mute, missing-clip tolerance and scene reload. PlayerPrefs matched the pre-test
registry snapshot after restoration. The temporary QA helper was removed.
Python tests passed 9/9, the level solver validated all 36 levels, and the final
solution build had zero warnings/errors. Normal startup Console had zero
warnings/errors; the deliberate missing-clip check produced one expected warning.

## Changed files

- `Assets/Scripts/Feedback/GameFeedback.cs`, `FeedbackSettings.cs`
- `Assets/Scripts/Feedback/MusicTracks.cs`, `MusicPlayback.cs`
- `Assets/Scripts/UI/ScreenManager.cs`, `LevelSelectUI.cs`, `SettingsUI.cs`
- `Assets/Resources/Audio/MusicTracks.asset`
- `Assets/Audio/Music/*.mp3` and required `.meta` files
- `Assets/Tests/EditMode/Editor/MusicTests.cs`, `SettingsTests.cs`
- This document and the SFX document's scope clarification

Level assets, gameplay rules/timing, progression logic, artwork and font assets
were preserved. No dependency, commit or push was added.
