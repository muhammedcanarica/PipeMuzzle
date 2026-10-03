# Level design and difficulty progression

All 36 existing LevelDefinition assets were redesigned. Their GUIDs, world ordering, loaders and serialized format remain intact. Runtime UI, artwork, flow animation, completion delay and progression code were not changed by this pass. Existing saves retain their world/level identities; replaying a completed level loads its new layout.

## Difficulty curves

The previous layouts filled every cell. Bamboo minimum rotation costs varied from 23 down to 9 and its final required only 6; Moon also had downward steps and shortcut routes. This pass increases active pipes, route length and exact minimum clockwise clicks at every step within each world. Board expansion leaves breathing room, so density intentionally drops when a larger grid is introduced.

| World | Role | Minimum clockwise clicks, levels 1–12 | Active pipes | Route length | Distractors |
| --- | --- | --- | --- | --- | --- |
| Sakura Garden | Teach rotation, then route selection | 1, 3, 4 / 6, 7, 8 / 10, 11, 12 / 14, 15, 17 | 3 → 22 | 3 → 16 | 0 → 6 |
| Bamboo Workshop | Misleading branches and longer routes | 14, 16, 17 / 19, 20, 22 / 24, 25, 27 / 29, 31, 33 | 16 → 32 | 12 → 24 | 4 → 8 |
| Moon Shrine | Dense branch systems and crosses | 29, 31, 33 / 35, 36, 38 / 39, 41, 43 / 45, 47, 50 | 27 → 42 | 21 → 32 | 6 → 10 |

Slashes indicate the existing 1–3, 4–6, 7–9 and 10–12 story arcs. Bamboo starts slightly below Sakura's final; Moon starts slightly below Bamboo's final, with no tutorial reset. Sakura 1 has three pipes and one required rotation; Sakura 2 has six pipes, one distractor and three required rotations. Three-way pipes first appear at Sakura 6 and become more common in Bamboo. Moon adds crosses immediately and develops them further at levels 4 and 10.

## Route and distractor design

Every board has exactly one possible simple source-to-target route across valid tile orientations. This is route uniqueness, not uniqueness of the entire board configuration: Straight/Cross symmetry and optional off-route rotations remain valid. The player need not rotate or use every pipe.

Distractors occupy plausible branches that can be reached from the source under consistent rotations, but cannot become a second target-reaching route. Corners, dead ends and three-way junctions conceal the real route. All boards include Empty cells. Initial orientations are deliberately selected against a click budget and spread across the route, rather than shuffled until an arbitrary arrangement happens to work. Initial ports and solved route ports stay inside the grid; the existing rule allowing incorrect player rotations remains intact.

| Final | Board | Active / total cells | Solution pipes | Distractors | Three-way / Cross | Minimum clicks |
| --- | --- | --- | --- | --- | --- | --- |
| Sakura 12 | 5×5 | 22 / 25 | 16 | 6 | 3 / 0 | 17 |
| Bamboo 12 | 6×6 | 32 / 36 | 24 | 8 | 5 / 0 | 33 |
| Moon 12 | 7×7 | 42 / 49 | 32 | 10 | 6 / 3 | 50 |

Full before/after measurements are in [level-difficulty.csv](level-difficulty.csv). Minimum clicks are an objective metric; they do not prove perceived difficulty, enjoyment or satisfaction. Those require player testing.

## Development validation

`PipeMuzzle > Validate All Levels` runs an Editor-only exhaustive simple-path solver. It assigns one consistent orientation to each visited tile and minimizes clockwise clicks, accounting for shape symmetry and locked endpoints. An edge-only reachability search could incorrectly reuse one corner with incompatible orientations; this validator forbids tile reuse. Searches stop at 200,000 nodes and report incomplete results rather than claiming solvability.

Validation checks dimensions, every grid cell, duplicate/out-of-bounds coordinates, enum values, rotations, one locked nonempty source and target, outward spawn ports, initially solved boards and solvability. All 36 authored boards completed exhaustive search and have one valid route.

The content tests apply the independently computed solution through BoardState.TryRotateTile and verify it with the runtime ConnectionChecker. For all 36 boards, flow tests use the real BoardView and EnergyFlowView: the success line reaches the target, excludes distractors, exposes target arrival before the completion callback, completes exactly once, and clears on rebuild. Story checkpoint references at 3/6/9/12 and existing world asset order remain intact.

Offline authoring uses Python's standard library only:

```powershell
python -B tools/level_design.py --csv current-level-metrics.csv
python -B -m unittest discover -s tools -p test_level_design.py -v
```

The checked-in `tools/level_design_manifest.json` freezes the reviewed layouts. `--generate` selects deterministic offline candidates and replaces that manifest; review candidates before `--apply` writes existing assets. Application validates all 36 entries before writing, including rejection of solved spawns. No generator, solver, score UI, stars or Python dependency is introduced into gameplay.

## Verification and manual pass

- Relevant EditMode group: 197/197 passed, zero skipped.
- Entire EditMode suite: 459 tests completed; three preexisting ComicViewerPresentationTests.CounterTracksStoryLengthAndArtworkIsUnfiltered cases (Sakura/Bamboo/Moon) failed with AmbiguousMatchException. The same failures existed before this pass; no new failure was observed.
- Offline solver/manifest tests: 4/4 passed.
- Solution build: zero warnings and zero errors.
- All 36 initial boards were visually inspected together using their existing world pipe sprites. In Play Mode at 1280×720, Sakura 1 and all three finals fit the gameplay surface and HUD. Moon 12 was solved through the actual click handler in 50 moves: only the correct route filled, target arrival preceded completion, Moon_Final opened, and skipping it exposed World Complete. The preview clock and saved progression/checkpoint keys were restored.

For manual playtesting, prioritize Sakura 1/2/6/9/12, Bamboo 1/6/9/12, and Moon 1/6/9/10/11/12. Then complete each world's 3/6/9/12 checkpoints using an unviewed checkpoint save or the existing development reset. Confirm the correct comic, intermediate return to Level Select, final World Complete controls and unchanged next-world unlocks. Pay particular attention to final board readability, whether near-correct branches are convincing, and whether Moon 10–12 feel demanding without rotation spam or frustration.

## Recovery verification, 3 October 2026

An external Git reset and subsequent branch switches removed the uncommitted work. All 36 layouts and the existing presentation/runtime work were recovered from recorded edits, original assets and Git objects. The original asset identities remain intact. The 93 protected files match their recorded contents: 87 exact SHA-256 matches and six identical Git blobs with different line endings. Every metric for all 36 levels matches the previously validated pass.

The complete EditMode suite was rerun after recovery: 459/459 tests completed, with only the same three preexisting ComicViewerPresentationTests failures. The 197/197 targeted result and Play Mode screenshots above were recorded before the accidental reset; a second targeted/live run could not be completed because the Editor was later closed and reopening it reported no matching Unity license. The final solution build again passed with zero warnings/errors, Python tests passed 4/4, and git diff --check passed. All 26 saved progression/checkpoint keys still match the snapshot taken before the recovery test run.

Recovery details, file groups, backup location and remaining limitations are documented in [recovery-2026-10-03.md](recovery-2026-10-03.md).
