# Rebirth validation — 2026-09-06

## Unreleased settings restoration

- No release bump/export/push. project.godot remains alpha-0.0.8; the existing EXE retains SHA-256 `881FB896745E0F61C80BDA9F648185CA018EEA1BC04A51D52065E58681304C2B`. The entire released changelog tail from alpha-0.0.8 through older releases matches HEAD before this work; new entries live under Unreleased.
- `tools/rebirth/verify.cmd`: Debug build 0 warnings/errors; 26/26 core regressions; six unchanged balance journeys; expanded UI smoke passes settings and existing navigation/choice/history/profile checks. Logs: artifacts/settings-verify.log. Deliberately corrupt JSON still produces the expected warning without overwriting its original file.
- Added settings checks: preview applies without persisting, Escape/15-second timeout rollback, keep confirmation, draft discard, per-page reset/cancel, current 12 actions/24 slots, conflicting/cancelled/cleared bindings, mandatory last key, Tab reservation, F11 capture suppression, rebound inspection/dash/Enter choice routing, restored defaults, old rebirth profile defaults, video/binding persistence and invalid-value repair.
- `tools/rebirth/verify_settings.cmd`: real OpenGL window changes verified on the local Windows/NVIDIA system, from exact 640x360 window to borderless maximized work area to fullscreen and back; preview timeout also returns to the exact old window. Mode/flag checks are exact. Maximized native geometry allows a symmetric 16px frame tolerance, because this system reports usable height 1410 and actual maximized client height 1408. Logs: artifacts/settings-display.log and artifacts/settings-render.log.
- Eight real-render captures cover audio/video/controls/display confirmation at 1280x720 and 640x360. Visual checks include control layout at 1280 and video/confirmation at 640. All fit without global scrolling; text at 640 is small. The confirmation capture retains the requested capture resolution rather than silently restoring default 1280.
- Diagnostics use separate fresh profiles from startup and keep playback disabled; they do not alter the player's profile or claim human audio listening. Old pre-rewrite user://settings.json is deliberately not auto-imported or deleted. This is source-runtime validation, not a new EXE or clean-machine/DPI certification.

## Exported delivery: alpha-0.0.8

- Character-faithful combat iteration, implementation commit `a40b2af`: three exclusive tracks per hero, shared tuning, homing/retargeting ofuda, stationary sealing field, star spread/focus, stardust and sustained Master Spark with separate signatures.
- Clean Debug build: 0 warnings/errors. Core regression: 26/26. Added checks cover owner-filtered offers and application, shared description/count/damage values, tracking and reacquisition, focus without extra damage, finite beam corridor, harmless warmup, pulse intervals, selective bullet clear, stationary fields and ability-state freezing during pause/choices.
- UI smoke retains Esc/P/E navigation, queued-offer preservation, settings, profile compatibility and complete embedded history; it additionally checks Marisa's own build and offer contents.
- Six ordinary navigation-pilot journeys (three seeds per hero) won without injected healing: Reimu 261.9–265.5 seconds; Marisa 268.2–276.6 seconds. The separate healing-assisted soak remains labeled as such; neither proves human difficulty balance or improved fun.
- Source render suite: 19 main/ability views plus eight 960x540 views. Reviewed dream orbs, sealing field, Marisa's beam and her 960x540 build panel. Ability fixture captures deliberately set ranks and stationary enemies to expose behavior; they are not ordinary player-run screenshots.
- File: `release/TouhouWuxiaSurvivor_alpha-0.0.8.exe`; 192,484,304 bytes; Windows file/product version `0.0.8.0`.
- SHA-256: `881FB896745E0F61C80BDA9F648185CA018EEA1BC04A51D52065E58681304C2B`.
- Embedded PCK and self-contained Microsoft.NETCore.App 8.0.6. Only this EXE was copied outside the repository; the child PATH excluded SDK/Godot and DOTNET_ROOT pointed at a nonexistent location. The isolated directory still contained only that executable after testing.
- All 13 standalone checks passed: smoke, title, boss, build, settings, changelog, Reimu field/spell, Marisa stars/warmup/beam/build/choices. New ability views also rendered from the exported executable at 960x540; the exported beam view was visually reviewed.
- Logs/report/screenshots: `artifacts/alpha-0.0.8-export-validation/`; source logs: `artifacts/alpha-0.0.8-verify.log` and `artifacts/alpha-0.0.8-captures.log`.
- Historical alpha-0.0.0 through alpha-0.0.7 changelog sections are unchanged. All eight earlier EXEs remain; alpha-0.0.7 SHA-256 still matches its previous record.
- Display name changes to 夜境异闻 while the application/config/name storage namespace stays unchanged. Existing preferences and scores remain in the same user:// location; no in-progress build was persisted by previous versions.

This is same-machine isolated-directory verification, not clean-VM certification, a listening test, public-release licensing approval, browser support or exact reproduction of original Touhou spell behavior. No public deployment or remote Git push was performed.

## Exported delivery: alpha-0.0.7

- Value-driven usability iteration: context-preserving E build inspection, Esc/P navigation, rank-specific upgrade descriptions, volume controls and per-version embedded history. Combat formulas, maps and content counts are unchanged.
- Source checks: clean Debug build (0 warnings/errors), 20/20 core regressions, real viewport input dispatch, unchanged upgrade offers across inspection, volume/profile persistence, backward-compatible version-1 profiles and corrupt-file preservation.
- Rendering: 12 primary screens plus five 960x540 checks (choices, build, maximum-rank build, settings, changelog). Build/settings/history screens visually reviewed, including exported 960x540 build output.
- File: `release/TouhouWuxiaSurvivor_alpha-0.0.7.exe`; 192,466,816 bytes; Windows file/product version `0.0.7.0`.
- SHA-256: `23FD603D815CD0FBC60034BC2365D8B3B30CD176FDEB9908E7B3FF6C1413516B`.
- Embedded PCK and self-contained Microsoft.NETCore.App 8.0.6. The executable alone was copied outside the project, with SDK/Godot removed from child PATH and DOTNET_ROOT pointed at a nonexistent directory.
- That copy passed six checks: UI/profile smoke, title, Boss, build, settings and changelog. The three new screens also rendered at 960x540; no external CHANGELOG.md was present, and current/historical entries loaded from the embedded resource.
- Logs/screenshots/report: `artifacts/alpha-0.0.7-export-validation/`; source logs: `artifacts/alpha-0.0.7-verify.log` and `artifacts/alpha-0.0.7-captures.log`.
- Historical alpha-0.0.0 through alpha-0.0.6 changelog sections are unchanged. All seven earlier release EXEs remain; alpha-0.0.6 SHA-256 still matches its prior delivery record below.
- Audio smoke checks validate control values, master bus mute/gain and stored preferences with playback disabled. They are not a listening test. An early test that restarted Ogg playback inside one headless frame produced an engine exit-resource warning; stream cleanup and delay alone did not resolve that test condition. The harness no longer starts playback as a slider-test side effect and rejects engine ERROR lines.

This verifies same-machine portable execution, not a clean Windows VM, browser compatibility, public asset licensing, or improved fun. No Web migration or full legacy-system restoration is included.

## Exported delivery: alpha-0.0.6

The user subsequently requested the actual self-contained Windows EXE and reaffirmed the historical
stage-major.release.optimization naming rule. The earlier source-only delivery notes below are historical.

- File: `release/TouhouWuxiaSurvivor_alpha-0.0.6.exe`.
- Size: 192,443,224 bytes. Windows file/product version: `0.0.6.0`.
- SHA-256: `C89225B7C86132FBE7175DC82361CEBA39E632BEE2FA42356177697716954072`.
- PCK and .NET publish outputs are embedded; publish metadata identifies included Microsoft.NETCore.App 8.0.6.
- `tools/rebirth/verify_release.ps1` copies only the EXE into a unique temporary directory outside the repository,
  removes SDK/Godot locations from the child PATH and points DOTNET_ROOT to a nonexistent directory.
- That copy passes UI/profile smoke, a real OpenGL title capture and a real Boss-scene capture; the isolated
  directory still contains only the executable afterward. Actual exported screenshots were visually reviewed.
- Logs, screenshots and machine-readable report: `artifacts/alpha-0.0.6-export-validation/`.
- All six previous EXEs remain in `release/`. Export is for local internal validation, not public distribution.

This is a same-machine isolated-directory test, not a clean Windows virtual-machine certification.

## Scope

The old project is preserved at `d229b36`. The new compile graph contains only `game/**/*.cs`.
No old source, assets, build outputs or archive files were deleted. Nothing was pushed. Export was initially
deferred and is now explicitly authorized and verified as described above.

## Commands

```bat
tools\rebirth\verify.cmd
tools\rebirth\capture.cmd
run_game.cmd
```

The verification scripts use the locally verified Godot Mono 4.7.1 console executable and .NET SDK 8.0.302.
The launcher accepts a `GODOT_EXE` environment override. Generated logs and screenshots are in ignored `artifacts/`.

## Core regression coverage

19 standalone tests compile the same simulation sources as the actual game:

- Character differences, focus speed, diagonal normalization and arena boundaries.
- Pause freeze, queued upgrade freeze, one-time choices and upgrade caps.
- Dash cooldown and invulnerability; one graze per bullet; qi burst and experience attraction.
- Swept bullet collision, one-time deaths, one hit per piercing target and distinct lightning chains.
- Persistent seal progress and one-time rewards.
- Boss victory, player defeat, seeded repeatability and clean restart state.
- Complete timeline with entity bounds. This separate soak test explicitly injects healing and is not a balance test.

Debug build: zero compiler warnings and errors. Core runner: 19/19 passing.

## Engine and interface

The real Godot headless smoke test traverses title, character selection, run start, dash, pause, settings,
multiple queued upgrades, victory, replay and help. Labels and buttons are checked against the viewport
and their parent cards. Profile checks cover round-trip storage, preferences, victory count, UTF-8 without
BOM and preserving malformed files. One warning from the intentionally malformed test profile is expected;
the user's normal profile is not modified by diagnostics.

Real OpenGL captures cover title, heroes, help, settings, combat, upgrades, pause, Boss and results.
The reduced-window capture uses 960 x 540. Screenshot generation alone does not establish visual acceptance;
the final review record is in `progress.md`.

Final visual review inspected the overview of all nine screens and full-size key captures, including the
960 x 540 upgrade screen. Upgrade descriptions stay inside their cards. The result-screen capture uses a
constructed victory to review layout; it is not presented as evidence of a 75-second normal completion.

## Unassisted navigation-bot runs

These runs use normal health, damage and progression. The pilot navigates toward seals, repels away from
nearby enemies/bullets and favors weapon upgrades. It has exact simulation-state access, so its skill is not
representative of a new human player.

| Character | Seed | Outcome | Seconds | Kills |
| --- | --- | --- | ---: | ---: |
| Reimu | 42 | Win | 267.2 | 1307 |
| Reimu | 260906 | Win | 267.5 | 1303 |
| Reimu | 781 | Win | 279.8 | 1407 |
| Marisa | 42 | Win | 263.7 | 1270 |
| Marisa | 260906 | Win | 264.9 | 1276 |
| Marisa | 781 | Win | 265.0 | 1290 |

## Corrections found by verification

- Upgrade text overflow: restore intended Label bounds after wrap/theme setup, then check parent containment.
- Arena-edge framing: clamp the camera independently of the player's boundary clamp.
- Projectile mutation: clear hostile projectiles after collision iteration rather than inside nested death callbacks.
- Rank-zero sword attack: require an unlocked sword rank just like every other weapon.
- Boss pacing: raise durability so the final encounter has time to show all three phases.

## Remaining human acceptance

- Is the first minute understandable without reading this document?
- Does dodging feel responsive, and are near misses worth taking?
- Are the four build directions distinct enough to motivate replay?
- Does the 4–5 minute journey remain engaging across multiple human runs?

This is a playable internal prototype, not a claim of commercial polish, full legacy-feature migration,
cross-platform certification or permission to redistribute the existing internally sourced Touhou assets.
