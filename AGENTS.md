# Ruilay repository instructions

- Work only on `main` unless the user explicitly requests another branch. Do not create feature, Codex or worktree branches.
- Keep the recovered gameplay, world/UI presentation, 36-level difficulty pass, pipe artwork, flow animation, delayed completion, audio/haptics and story checkpoints at 3/6/9/12 as the current source of truth. Inspect the architecture before editing; preserve unrelated changes and avoid broad refactors.
- Use the workflow: edit on `main`, run relevant checks, commit, then push to `origin/main` when requested. Never force push, hard reset, rebase, or blindly merge historical branches over current files.
- Before consolidation or branch cleanup, verify a backup and inspect unique commits. Preserve useful content with minimal changes before deleting obsolete references.
- Commit source, Unity assets and required `.meta` files. Exclude local caches, generated project files and player builds (`Library`, `Temp`, `Logs`, `Build`, `Builds`, `obj`, `.vs`, `UserSettings`, local versioned build directories and archives).
- Prefer small, readable changes. Explain any new dependency before adding it. Keep Unity state/input separation and Inspector configuration clear; preserve UI language unless translation is requested.
- Run relevant compilation, Unity EditMode tests, level validation and Python tests. Report actual output and distinguish known failures or unavailable checks from new regressions.
- At completion, report changes, affected files, how to verify, and material risks or limitations. Do not update Codex memories unless explicitly asked.
