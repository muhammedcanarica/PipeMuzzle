# Ruilay typography

The normal UI uses Sour Gummy. Grandstander is reserved for the game title,
large screen headings and celebration titles. Existing layout, input and
progression remain owned by the existing UI and gameplay components.

| Role | Bundled face | Uses |
| --- | --- | --- |
| Body | Sour Gummy Regular | Descriptions, normal supporting text |
| Label | Sour Gummy Medium | JOURNEY, COMPLETE / LOCKED / 12 LEVELS, counters, tutorial hint |
| Emphasis | Sour Gummy SemiBold | World names, level numbers, buttons including comic Next/Continue, important UI information |
| Heading | Grandstander SemiBold | Pause and Settings screen titles |
| Title | Grandstander Bold | Ruilay, Level / World Complete, Journey Complete, credits title |

`Assets/Scripts/UI/UiTypography.cs` caches five local Resources assets and
applies real font weights without synthetic TMP bold. It does not change
positions, dimensions, alignment, auto sizing, font sizes or spacing.
World names on Level Select use modest 0.4 character spacing. The small world
name inside the World Complete message explicitly uses Sour Gummy SemiBold.

## Assets and licenses

Only five upright static TTFs are included in `Assets/UI/Fonts/`. No variable,
italic or unused weight files are bundled. Sources:

- [Sour Gummy upstream](https://github.com/eifetx/Sour-Gummy-Fonts/tree/36ed784f767ea3c7a56d8141ab187e49d3e9bb8d/fonts/ttf)
  at commit `36ed784f767ea3c7a56d8141ab187e49d3e9bb8d`.
- [Grandstander upstream](https://github.com/Etcetera-Type-Co/Grandstander/tree/0bf9e31d529d7a67b23510b841cb3597db0cb130/fonts/ttf)
  at commit `0bf9e31d529d7a67b23510b841cb3597db0cb130`.

Both families ship with their SIL Open Font License 1.1 in their font folder.
TMP assets live in `Assets/Resources/Fonts/` so runtime-created UI and rich
text font tags work in player builds. They include local source TTF references,
prepopulated 1024x1024 SDF atlases, real materials and dynamic multi-atlas
support. English UI, Turkish letters and the game's punctuation are seeded.
The existing Liberation Sans asset remains available only as a technical
fallback. There is no web font request or runtime system-font dependency.

`Ruilay > Typography > Generate Font Assets` creates missing TMP assets and
sets Sour Gummy Regular as the TMP default. It preserves existing asset GUIDs
and tuned atlases. The TMP default font resource path is `Fonts/`.

## Coverage

The shipping build uses `Assets/Scenes/Gameplay.unity`: its 19 serialized TMP
components have matching new font/material references. World Map, three world
Level Select views, gameplay HUD, hint, pause, Settings/reset popup, comic UI,
completion, Journey Complete and credits also choose fonts when constructing
or styling runtime UI. Main-menu title/button styling uses the same helper.

Changed UI scripts: `WorldMapUI.cs`, `LevelSelectUI.cs`,
`LevelSelectJourneyView.cs`, `GameUI.cs`, `GameplayHintUI.cs`,
`GameplayPauseUI.cs`, `SettingsUI.cs`, `ComicViewerUI.cs`,
`JourneyCompleteUI.cs`, `FirstLevelTutorial.cs` and `SoftBlossomUITheme.cs`.
Added `UiTypography.cs`, `Editor/TypographyFontAssets.cs`,
`Tests/EditMode/Editor/TypographyTests.cs` and their Unity metadata.
Also changed the shipping scene's font references and the default font/path
in `Assets/TextMesh Pro/Resources/TMP Settings.asset`.

`TilePrefab.prefab`, `SampleScene.unity` and `URP2DSceneTemplate.unity` contain
no serialized TMP components. The three scenes in `Assets/_Recovery/` are
historical recovery snapshots and are intentionally unchanged. Baked text
inside story illustrations and existing branding artwork is also unchanged.

## Verification

Run Unity EditMode tests, including `TypographyTests`, and
`Ruilay > Validate All Levels`. Typography tests verify bundled font/glyph
coverage, matching serialized atlas materials, preserved layout settings and
real weights for inactive HUD buttons. Python checks:

```powershell
python -m unittest discover -s tools -p 'test_*.py'
```

Manual visual route: World Map -> world comic -> Level Select -> Gameplay ->
Hint / Pause -> Completion. Check Settings/reset popup, all three world
headings, Journey Complete and credits at wide, small landscape and portrait
sizes. Verify overflow, descenders, button labels, auto sizing, glyph warnings
and the Console. Temporary QA captures and measurements are under
`Temp/TypographyQA/` and are not source assets.

Known pre-existing scene issue: Canvas has component `810000004` serialized
outside its component list. Unity reports a scene-open repair message when
loading the scene. The same inconsistency was verified in the pre-change
working-tree backup; typography does not alter that unrelated component.

### Results (2026-10-06)

- Unity EditMode: **637 passed, 0 failed, 0 skipped**, including eight
  typography cases. The baseline suite passed 629 tests before these additions.
- Python: **9 passed**. Level validator: **36 levels** reported valid layouts
  and solver results.
- `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` compile with
  **0 errors and 0 warnings** after restoring their generated project assets.
  The aggregate `dotnet build PipeMuzzle.slnx --no-restore -m:1 -v:minimal`
  also succeeded with **0 errors and 0 warnings**. Early attempts required
  generated NuGet project assets to be restored and a stale temporary-helper
  entry to be removed from the ignored generated editor project file.
- Play Mode: World Map, three world Level Select views, comic UI, gameplay,
  hint, pause, Level Complete, Settings and reset confirmation inspected.
  Level Complete was reached through the normal hint/flow completion path.
  World Complete, Journey Complete and credits received presentation previews;
  the full 36-level journey was not replayed.
- Wide, **540x960 portrait** and **640x360 landscape** views checked. Across
  250 captured text measurements, **0 overflow flags**. No font/TMP messages
  were returned by the final Console query. Actual-device readability and
  a fresh WebGL/mobile player build were not tested.
- Progress keys and Game View selection restored after QA. The PlayerPrefs
  registry comparison found **0 differences** from the saved QA snapshot.
  The temporary QA helper was removed, and unrelated working-tree edits
  were preserved. No commit or push was made.
