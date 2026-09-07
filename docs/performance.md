# Active Combat Runtime And Performance

## Scope And Architecture

The active project compiles `game/**/*.cs`; the old ECS under `src` is not part of the rebuilt executable. Before this change, active combat used lists of mutable objects. It was not accurate to describe the old implementation as a completed ECS/OOP hybrid just because old architecture documents existed.

The active runtime now uses a deliberately small, fixed-archetype component/system design rather than a general-purpose ECS framework:

- `CombatWorld` owns dense `ComponentStore` storage for enemies, projectiles and pickups. Projectile/pickup data are value components; enemies remain managed data components to preserve their stable identity across targeting, boss inspection and encounter code. This is not a claim that all state is unmanaged or split into SoA columns.
- `EnemySystem`, `ProjectileSystem` and `PickupSystem` update batches. `RunState` sequences systems, weapons, progression and events. High-frequency entities are not individual Godot Nodes. Removal compacts in stable order so seeded behavior does not change because of swap removal.
- The spatial grid supplies non-allocating value enumerators and identity lookup. Ordinary hit histories store four IDs inline; exceptional long piercing histories retain a fallback set, rather than silently dropping hit exclusions.
- OOP Nodes remain responsible for UI, platform input, settings, audio, rendering and low-frequency encounter orchestration. Presentation reads component data and combat events; it must not retain slot references across structural changes or mutate combat state to produce an animation.

## Rendering

### Dynamic Visibility Regression (2026-09-07, Unreleased)

- Static screenshots and draw-call counters did not catch mid-run missing entities. A deterministic Reimu journey at approximately 144 simulated seconds reproduced missing fairy sprites: 1461 pixels differed from independent immediate texture draws, although the CPU/native transforms, colors and texture references remained valid. The affected batch had moved into a narrow region near the screen edge.
- SpriteBatch now accumulates the actual rotated quad bounds during Add and supplies CustomAabb on Submit. This keeps the batch visibility bounds synchronized with the submitted instances without a broad arena-sized bound, disabling culling, adding one node per enemy, or changing simulation. It also avoids relying on the deferred automatic bound refresh for moving/reused batches. The identical failing comparison passes after this change.
- The same reference test found that the QuadMesh vertical UV direction was reversed relative to ordinary sprites; the shared shader corrects it. This is a separate confirmed defect, not evidence that every reported black texture had this cause.
- `tools/rebirth/verify_batches.cmd` runs two heroes against real desktop rendering. Each performs 72 sprite comparisons across capacity boundaries, animation/tint changes and zero/reuse/GC cycles plus 12 complete battle comparisons. `tools/platform/verify_batches.cjs` executes the same C# checks in the threadless browser build without isolation or SharedArrayBuffer. Tests do not replace production batches with per-entity nodes outside the explicit diagnostic mode.
- Before-fix evidence: `artifacts/batch-state.log` and `artifacts/batch-validation-before-fix/`. Native results: `artifacts/batch-render-latest.txt`. Browser results are stored under the selected build's `batch-verification` directory. Native and browser physical-device coverage must be stated separately; black-texture symptoms were reported but not independently reproduced before the fix.
- Final verification: native logs `artifacts/batch-render/20260907-113422/`; threadless Web build `20260907-113430-145`, report `batch-verification/2026-09-07T03-35-19-681Z/report.json`. Each platform passes 144 sprite comparisons and 24 battle comparisons across both heroes through victory, with zero differing pixels above tolerance. Web reports actual isolation false, SharedArrayBuffer undefined and zero failed requests/errors. Core 29/29, UI, five native render fixtures and Web DPR 1/3 checks pass; the Web stress fixture retains 817 draw calls. This is not physical-phone FPS acceptance or a deployed update.

### Batch Design

- Static terrain is uploaded once to two MultiMesh batches. Retained scenery is not re-issued from C# every frame. Dynamic seals, telegraphs, player, effects and HUD have separate drawing passes; HUD refreshes at 10 Hz and paused battle passes remain cached.
- Enemies, projectiles and pickups are grouped by source texture and uploaded in bulk through transform/color/frame buffers. Sprite dimensions are cached instead of repeatedly crossing the C#/engine boundary. Off-screen objects are culled for rendering only; simulation and collision are unchanged.
- Web uses viewport stretching with a fixed 1280x720 render target, avoiding high-DPI native resolution multiplying the world rendering workload. Windows keeps its existing display/settings workflow. There is no automatic enemy-count reduction or hidden change to collision radii, damage, attack timing or spawn limits.
- F3 includes batch instance count, batch-build CPU time and actual render-target size. Debug-only Web telemetry is still opt-in via validation arguments; the normal entry does not generate continuous diagnostics JSON.

## Original-Pack Art And Icon

`tools/rebirth/prepare_combat_assets.py` reads the user-provided adjacent original-game pack. TH10 bullet, ofuda, star, orb, item and grass crops are recorded in `assets/internal_original/base/combat/source_manifest.json`, including source hashes, crop rectangles and output hashes. Existing source-backed character/enemy strips remain in use. Existing stone and decorative scenery remain as fallback where no reviewed replacement has been selected; this is not a claim that every UI/background asset has been replaced.

`assets/branding/night_journal.png` combines the original yin-yang orb, ofuda and star with a pixel frame. Both desktop export presets and the project/Web icon use it. The old icon is preserved. These are user-provided original assets for an internal prototype; source attribution does not grant redistribution rights or make this an official Touhou game.

## Measured Local Comparison

Both simulation runs use .NET 8.0.6 and 1200 measured ticks against the same fixture. The baseline source is commit `943fbf2`, with only matching benchmark instrumentation added to its isolated copy.

| Simulation fixture | Before mean / p95 | After mean / p95 | Before / after allocation per tick |
| --- | --- | --- | --- |
| 180 enemies, 600 projectiles | 0.217 / 0.350 ms | 0.138 / 0.192 ms | 39,319 / 55 bytes |
| 320 enemies, 1600 projectiles | 0.690 / 1.272 ms | 0.364 / 0.522 ms | 122,508 / 158 bytes |

The separate real-render comparison uses OpenGL on an RTX 4070 Ti at 1280x720, with 320 enemies, 1600 projectiles and 400 pickups. A one-second warmup precedes a three-second wall-clock sample: before 6971 draw calls and 58.67 ms mean frame interval; after 806 draw calls and 5.03 ms. This identifies excessive drawing submission as a substantial measured cost, not proof of the exact bottleneck on the user's unknown mobile devices. The render fixture freezes simulation to isolate rendering; these numbers are not full-game or physical-mobile FPS.

Raw local reports: `artifacts/performance-baseline.json`, `artifacts/performance-after.json`, `artifacts/render-performance/before-20260907-020747/performance.json`, and `artifacts/render-performance/after-20260907-020754/performance.json`. The six complete seeded character journeys preserve their before/after time, kills, level, seals, health and graze outputs. No hidden gameplay reductions are used to obtain the measurements.

## Reproduction And Limits

- Desktop validation passes 29 core tests plus the UI/settings/profile/F3 and render-cache smoke checks. The five rendered battle fixtures and an additional F3 capture are visually checked.
- Final shared build `artifacts/web-builds/20260907-024930-329` passes all six loading-UX scenarios. Its full Web suite passes desktop, touch and both complete character journeys, but the touch-upgrade case and separate performance gate report the request event described below. No Web deployment or formal EXE was created by this task.
- Web acceptance is not fully green. Local Edge sometimes reports `index.pck net::ERR_ABORTED` after receiving the entire pack, while gameplay completes. The strict request-failure checks remain enabled; passing a retry is not treated as a fix. Earlier DPR 1/3 fixtures both measured a 1280x720 target and 817 draw calls, but the final acceptance gate must still be resolved before declaring Web validation complete.
- `tools/platform/probe_request.cjs --download-only --stream-response` reproduces the same network event in a standalone page without Godot, C#, the game's loader or any ECS code. In the final probe, both the received byte count and SHA-256 match the built pack (`948c51e951843e10d255d04495aac46db4822236699a2d98a2482b4966e14431`). Direct native `Response.arrayBuffer()` passed 16 comparison attempts. This narrows the symptom to the tested browser/stream-consumption path, but does not establish an upstream root cause or prove other browsers are affected. Attempts to retain responses/readers, copy chunks or replace the measuring stream did not reliably fix it and are not included in production code.

- Simulation: `dotnet run --project tests/rebirth/Rebirth.Tests.csproj -c Release -- --performance --output=artifacts/performance-after.json`.
- Real desktop render fixture and screenshots: `tools/rebirth/verify_render.cmd`. Run the identical fixture against an isolated pre-change source snapshot for comparison. The fixture uses 320 enemies, 1600 projectiles and 400 pickups; this is a stress case, not a typical minute of gameplay.
- Web: build with `build_web.cmd`, then run `tools/platform/verify_performance.cmd` for DPR 1/3 touch-emulated render-target/batch checks, followed by `tools/platform/verify_web.cmd` for full gameplay/input/save regression.
- User reports of phone 9 FPS and tablet 25 FPS are real feedback, but model/browser/scene details remain unknown. Desktop timings and browser emulation are not physical Android/iPhone/tablet FPS. The current task does not authorize a new release, EXE export or server deployment.
