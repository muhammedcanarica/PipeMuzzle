# Ruilay branding verification

Branding-only change on `main`. Gameplay, all 36 levels, story flow, save keys, namespaces, assemblies and asset GUIDs are unchanged. Company Name remains `DefaultCompany`.

## Changed

- Global Player Settings and active WebGL RC1 profile: Product Name and application description are `Ruilay`.
- World Map main menu, legacy level-selection heading and Credits show `Ruilay`.
- Unity validation/debug menus and asset creation menus use `Ruilay`.
- The supplied logo is imported at `Assets/Art/Branding/Ruilay_Logo.png` with its generated Unity metadata. Its SHA-256 matches the supplied file. `LevelSelectUI.brandLogo` supplies the main-menu image; aspect ratio is preserved and raycasts are disabled.
- README retains Turkish-first bilingual technical/setup information and now describes the cozy story-driven water-flow puzzle across Sakura Garden, Bamboo Workshop and Moon Shrine. The original repository links still work.
- Documentation headings and current editor-menu labels are updated; historical backup filenames and commands retain exact original paths.

## Retained references

[Complete source reference audit](branding-reference-audit.csv) lists every remaining match with file, line, column, context and reason (480 entries across 170 files, including five path entries). Audit files themselves are explanatory records, excluded from this source inventory to avoid recursive self-reporting. Tracked text files were scanned case-insensitively for `pipe[ _-]?muzzle`; new branding assets contain no old-name text. Generated caches, Git internals, ignored player builds and external archives were excluded.

Retained categories: namespaces and qualified/reflected types; Unity serialized type identifiers; PlayerPrefs key prefixes and their tests/documentation; Android and UWP package identities; linked Unity Services project metadata; shader identity; deterministic artwork GUID seeds; live GitHub link targets; solution/script filenames; synthetic manifest test label; original recovery scenes and external archive paths. Recovery scenes are historical snapshots excluded from the build, so their old heading is intentionally preserved. Physical repository folder and generated solution/project paths are also retained while Unity is open. Remote URLs were not changed.

## Verification

- Unity runtime and Editor assemblies compiled; `dotnet build PipeMuzzle.slnx --no-restore -m:1 -p:UseSharedCompilation=false --verbosity minimal`: 0 warnings, 0 errors after restoring generated projects with `RestoreProjectStyle=PackageReference`.
- Unity EditMode: 615 passed, 0 failed, 0 skipped. Initial MCP job timed out during domain reload; the retry passed.
- `python -m unittest discover -s tools -p 'test_*.py'`: 9 passed.
- Level validation: all 36 assets, 0 errors.
- Global and active WebGL profile Product Name verified through Unity as `Ruilay`; company unchanged.
- Live main menu at 1493x840: title `Ruilay`, assigned logo, no intercepted raycasts, 0 missing scripts and 0 broken serialized object references. Screenshot: ignored `Temp/BrandingQA/ruilay-main-menu.png`.
- Live Console: 0 error/warning entries. Expected negative-test logs are not gameplay errors.
- Initial build-profile trailing whitespace on `m_WebGLClientBrowserPath` remains preserved; it is the sole `git diff --check` finding.

## Manual release actions and limits

- Supply a square icon at `Assets/Art/Branding/Ruilay_Icon.png`, then assign it in Player Settings / Icons and the WebGL template favicon if needed. No suitable Ruilay icon existed; existing default template icons remain unchanged. No image was generated or cropped.
- Rebuild the player with the updated profile. Existing ignored RC1 binaries retain the old branding; no new player build was made. Future WebGL title uses Product Name; for Windows choose an output such as `Builds/Windows/Ruilay.exe` (the executable output path is chosen at build time).
- Manually rename the GitHub repository and itch.io project title to Ruilay when ready. GitHub rename and remote changes were deliberately not performed.
- Product Name changes the native PlayerPrefs storage location on Windows/Linux and the Windows/macOS Editor. macOS standalone storage also depends on bundle identifier. Old data was not deleted, but an existing player may see fresh progress until a separately designed platform-specific migration is performed. Keys remain unchanged; no migration behavior was added. See [Unity PlayerPrefs documentation](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.html).
- No commit or push performed for this branding task.

## Affected source files

- `AGENTS.md`
- `Assets/BuildProfiles/WebGL RC1.asset`
- `Assets/Editor/LevelHintBaker.cs`
- `Assets/Editor/LevelValidationUtility.cs`
- `Assets/Editor/PipeMuzzleStoryDebugMenu.cs`
- `Assets/Editor/PipeMuzzleTutorialDebugMenu.cs`
- `Assets/Editor/PipeSpriteAlignmentValidator.cs`
- `Assets/Scenes/Gameplay.unity`
- `Assets/Scripts/Data/ComicStoryDefinition.cs`
- `Assets/Scripts/Data/LevelDefinition.cs`
- `Assets/Scripts/Data/LevelPathLayoutDefinition.cs`
- `Assets/Scripts/Data/WorldDefinition.cs`
- `Assets/Scripts/Data/WorldGameplayTheme.cs`
- `Assets/Scripts/Data/WorldLevelSelectTheme.cs`
- `Assets/Scripts/UI/JourneyCompleteUI.cs`
- `Assets/Scripts/UI/LevelSelectUI.cs`
- `Assets/Scripts/UI/WorldMapUI.cs`
- `ProjectSettings/ProjectSettings.asset`
- `README.md`
- `docs/level-design-pass.md`
- `docs/recovery-2026-10-03.md`
- `docs/superpowers/plans/2026-09-17-world-map-phase-1.md`
- `docs/superpowers/plans/2026-09-24-world-aware-progression.md`
- `docs/superpowers/specs/2026-09-17-world-map-phase-1-design.md`
- `docs/superpowers/specs/2026-09-24-world-aware-progression-design.md`
- `Assets/Art/Branding.meta` and logo asset / `.meta`
- `docs/branding-reference-audit.csv`
- `docs/ruilay-branding.md`
