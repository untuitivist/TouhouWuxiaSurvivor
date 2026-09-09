# Restore Published alpha-0.1.6

Date: 2026-09-09. User request: “算了，回退版本到 0.1.6。”

## Scope

- Restore target: `e902d466deaf5757ad34cc69dd1686e7a481a641` (published release receipt commit).
- Pre-rollback HEAD: `072a2eb369522dad3528042e008a663baa849777` (15 local commits after the release).
- Restore the actual runtime, resources, view, icon, build and validation tools; the version string already read `alpha-0.1.6`.
- Preserve the single C# project and original published Windows/Web behavior. No new feature, balance change, release export, push, server deployment or branch change.

## Preservation

- Local archive: `artifacts/rollback-alpha-0.1.6-from-072a2eb/snapshot/`, preserving original relative paths.
- `manifest.json` records source/target commits, file sizes and SHA-256 hashes for 1181 verified files: 42 pre-restore copies, 871 moved tracked additions and 268 moved ignored import sidecars (63550118 bytes). No file deletion.
- The archive is ignored by Git and excluded by `artifacts/.gdignore`. The 15 original commits remain in Git ancestry, retaining all tracked drafts even on another clone. Local generated import sidecars are archive-only.
- Published changelog entries remain unchanged; withdrawn unreleased notes are retained in `docs/withdrawn_visual_changelog_20260909.md`. Project notes/plans explicitly mark earlier visual work as historical.
- Existing `release/TouhouWuxiaSurvivor_alpha-0.1.6.exe` and older deliveries are not overwritten.

## Validation

- `dotnet build TouhouWuxiaSurvivor.csproj --configuration Debug`: passed, zero warnings/errors.
- Restored `tests/rebirth` suite: 40/40 passed; no long performance benchmark or additional balance run.
- Godot headless import refresh and native UI/settings/journal/minimap/F3/profile smoke: passed. The malformed-profile warning is an intentional preservation fixture.
- Font coverage: passed for all 1247 required characters with existing Anaconda fontTools; the restored font bytes are unchanged.
- Rendered and visually reviewed title, Reimu field and Marisa beam scenes with the original release view and artwork. Captures: `artifacts/render-performance/rollback-alpha-0.1.6-20260909-223412/`.
- Build/core/import/UI logs: `artifacts/rollback-alpha-0.1.6-from-072a2eb/validation/`.
- Active runtime/resource/build/test inputs match e902d46 exactly. Windows and Web retain the published shared project; no new Web export or browser test was run for this local rollback.
- Existing release EXE SHA-256 matches its original receipt: `003b15729c019daaa3b8eecfe063e3119d29a1114f083bd662e8684f48a093ca`.
- No new physical-device FPS claim, no long full-load rerun and no fresh online deployment verification.
