# Web Combat Performance Investigation

## Released alpha-0.1.6 evidence

The final clean source is c5bf1684b88d3b825556919fa7ae9eba6d255059. Both the standalone Windows executable and public Web deployment use this revision. Earlier candidate failures and measurements below are retained as investigation history, not final release results.

The final build is artifacts/web-builds/20260909-024731-641. Both combat-gate-zh.json and combat-gate-en.json passed the unchanged budgets. The user confirmed that the existing full-load scenario is sufficient; this release does not expand the enemy limit to 1000.

| Language | 40/200 FPS mean | 180/600 FPS mean | 320/1600 FPS mean | Full-load minimum sampled FPS | Full-load simulation p95 ms | Full-load batch mean ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Chinese | 60 | 60 | 58.09 | 49 | 9.9 | 3.45 |
| English | 60 | 60 | 58.41 | 52 | 9.2 | 3.59 |

The test starts with an additional 400 pickups. These are desktop Edge touch-emulation measurements without shared memory, not physical-device tests or a claim of a locked 60 FPS. Final simulation speeds at full load were 59.97 and 59.80 ticks/s, recorded separately from rendered FPS.

Additional equivalent optimizations filter swept candidates within grid enumeration, delay hit-history checks until distance qualifies a target, skip untouched compaction prefixes, reject distant bullet-clear candidates, and reuse heading math for homing and sprite orientation. Candidate order, collision budgets and gameplay timesteps remain unchanged. Geometry, storage, query-order and actual rotated-sprite comparisons cover these changes.

The release passed 40 core tests, 860 localization checks, 26 standalone Windows checks and the complete Web compatibility suite. Public deployment alpha-0.1.6-c5bf168-20260908T190557Z passed normal desktop, touch and touch without isolation headers. Unlike alpha-0.1.5, the online build now includes the language selector; both language directions persisted through public-page reloads. See docs/deployment.md for the release receipt and rollback location.

## alpha-0.1.6 follow-up

The release candidate retains the existing ECS/OOP division. It adds a conservative swept AABB rejection before narrow-phase collision, replaces in-arena dictionary buckets with a directly indexed grid (with overflow support and identical query order), reuses fixed-style sprite attributes, and caches render mappings. In the integrated English unisolated stress run, 320/1600 averaged 58.7 rendered FPS, 6.85 ms simulation and 3.30 ms batch preparation. This is desktop evidence, not a mobile guarantee. Earlier measurements below remain the investigation history.

Release builds now require both languages at all three load levels with --enforce: mean rendered FPS at least 55, sampled FPS at least 45, simulation at least 55 ticks/s, p95 logic at most 16.7 ms and mean batch preparation at most 5 ms. These gates are bound to the exact Web build; deployment also requires interactive language persistence and shared language smoke checks. The full release receipt records the final clean-build measurements.

## Scope and architecture

The reported failure is Web gameplay slowing as entity counts rise, not initial downloads.
Reuse the boundary in `docs/plugin_first_design.md`: dense combat data and batch systems for enemies, projectiles, pickups and combat state; OOP for UI, persistence, resource ownership and low-frequency encounter orchestration. A Boss director may be OOP while its combat state remains in the simulation. Rendering is a batch adapter, not a Godot Node for every projectile.

The current implementation uses ComponentStore, EnemyGrid, batch systems and MultiMesh. It is not a claim that every old module/snapshot boundary has already been restored. No architecture-wide rewrite is part of this fix.

## Reproduction

`tools/platform/benchmark_combat.cjs` accepts `--build=<pointer.json>`, `--output=<report.json>`, `--loads=40,180,320`, `--throttle=1` and `--isolation=false|true`. It uses the existing deployment fixture, not a different game runtime.

The `combat-performance` diagnostic runs normal fixed physics steps, collision, growth abilities, event handling and rendering. Seeded scenarios maintain 40/200, 180/600 and 320/1600 enemy/projectile targets, initially add 400 pickups, and enable compatible Reimu branches. Enemies have synthetic high health; replenished bullets deal zero damage so the test cannot silently terminate. Ordinary collisions and expiry still remove bullets; post-step counts may be below refill targets. This is a synthetic stress fixture, not a balance test or a recording of normal play.

The original `performance` fixture is still useful for rendering checks, but its Tick stays zero and it cannot establish gameplay performance. The native benchmark now also includes growth stress and rejects premature simulation termination.

## Measurements

Local measurements use desktop Edge, 844x390 touch emulation, DPR 1, no CPU throttle and no cross-origin isolation. After loading and warmup, the tool samples for 15 seconds. These are not physical-phone FPS measurements. Slow baseline runs produce fewer samples and advance fewer simulation ticks; the table is not a frame-aligned replay comparison. Raw samples are retained.

| Refilled enemies / projectiles | Before logic mean (ms/tick) | After logic mean (ms/tick) | After rendered FPS mean | After simulation ticks/s |
| --- | ---: | ---: | ---: | ---: |
| 40 / 200 | 11.95 | 1.75 | 60.0 | 60.1 |
| 180 / 600 | 56.57 | 5.39 | 60.0 | 60.1 |
| 320 / 1600 | 137.33 | 10.91 | 27.9 | 59.8 |

A separate profiled baseline at 320/1600 attributed about 136.5 ms/tick to projectiles and 11.6 to enemy updates, versus 0.94 to grid rebuilding. Changing only shared direction/segment-distance math reduced that run to about 33.6 ms total. Extending scalar math to hot movement, distance and clamping reduced the final mean to about 10.9 ms. This supports optimizing the hot math path; it does not support blaming grid architecture or rewriting every object into ECS.

MultiMesh bounds and instance-size construction now avoid repeated temporary vector operations. Full-load batch construction decreased from about 8.5 to 6.6 ms in separate runs, but full-load rendered FPS remained about 28. The simulation catching up at 60 ticks/s must not be presented as 60 rendered FPS. Full-load rendering/physics catch-up still needs work, followed by physical-device testing.

No entity limits, damage, branch budgets, artwork, controls or timestep were reduced. Windows and Web use the same implementation. Scalar geometry is checked against the previous vector formulation with 10,000 deterministic samples, including degenerate and swept segments; existing growth, piercing, homing and determinism tests remain in place.

## Local evidence

Evidence is under `artifacts/performance-worktree/artifacts/` (ignored local diagnostic output):

- `combat-before.json`: initial dynamic baseline. Build `web-builds/20260909-002944-289`.
- `combat-profile.json` and `combat-profile.log`: per-system baseline.
- `combat-math.json`: direction/segment-only experiment.
- `combat-after.json`: remaining hot simulation math optimized.
- `combat-final.json`: final simulation and batch changes. Build `web-builds/20260909-004545-536`.
- `combat-isolated.json`: isolated-browser follow-up using the final build.
- `combat-final-tests.log`, `combat-native.json`, `combat-windows-build.log`, `combat-native-smoke.log`, `combat-batch-check.log`: regression/build evidence.

The comparison workspace excludes unrelated, uncommitted localization changes. Source manifests in each build retain hashes. This task does not export a formal EXE, change the version, push, or deploy.

Validation completed: 39 core tests, native Debug build and UI/settings/journal smoke, isolated/unisolated dynamic Web scenarios, and both heroes under the Web batch-color stress check. Importing the entire historical asset inventory in the diagnostic worktree crashed the editor (0xC0000374); the native runtime smoke then passed in a separate snapshot containing the same runtime asset subset used by the Web build. The inventory-import failure is retained in `combat-native-import.log`, not treated as fixed.

## Why language is absent online

The public entry was checked and still selects `alpha-0.1.5-48f8775-20260908T154824Z`. The release receipt explicitly excludes uncommitted localization. The newer documentation receipt does not contain a new game payload, so deploying that receipt alone would not add a language selector. Language and these performance changes require a newly built, tested game release and server activation; Git commit/push alone is not deployment.
