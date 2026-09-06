# Rebirth validation — 2026-09-06

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
