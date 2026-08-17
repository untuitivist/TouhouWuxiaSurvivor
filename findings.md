# Findings and Decisions

## Documentation Consolidation Audit - 2026-08-17

- `docs/` starts with six tracked files: plugin-first product architecture, ECS architecture, combat
  balance, enemy balance, canonical diagnostics, and historical beta diagnostics.
- The document count is small enough for a complete code-backed audit. Deletion should be based on
  obsolete ownership, not size alone.
- The newly approved architecture distinguishes internal modules from player-facing content packs,
  uses ECS for high-frequency bulk state, and keeps low-frequency orchestration in OOP Nodes.
- Content lifecycle is now typed: Base and TH06 are `development`; TH01-TH05 and TH07-TH20 are
  `inventory`; no package is currently `complete`.
- Existing root planning files contain extensive historical records but are outside the requested
  `docs/` cleanup and are required by the active planning workflow.
- `docs/ecs_architecture.md` has no inbound reference and still describes the completed ECS migration
  as future work. Its valid hybrid-boundary content is already superseded by `plugin_first_design.md`.
- `docs/beta_debug_diagnostics.md` also has no inbound reference and calls itself a legacy compatibility
  filename. Both it and the tracked canonical `docs/diagnostics.md` still described alpha-0.0.4; only
  the canonical file should retain the current procedure.
- `combat_balance.md` and `enemy_balance.md` are linked by the main README and should remain separate:
  the first owns player/build budgets, while the second owns enemy species and spawn-pressure policy.
- Diagnostics tools already expose the canonical `build_diagnostics.cmd`; the legacy
  `build_beta_debug.cmd` remains outside the requested `docs/` cleanup.
- `enemy_balance.md` is materially stale: it documents time-indexed batches and a dynamic alive cap,
  while current pacing uses eight explicit rates from 2.40 to 9.30 per second and a 30-second
  adaptive state. The user has explicitly rejected an enemy survival soft cap.
- `combat_balance.md` still says projectile count is granted automatically at 2/5-minute marks;
  current design requires normal-shot and centered-barrage counts to come from upgrades instead.
- Current code contains `AdaptiveRunPacingState`, `EnemyPressureCurve`, and an eight-phase
  `RunPacingTimeline`; documentation must distinguish the implemented runtime from older time-only
  simulation assumptions.
- The implemented finite gate is exactly a 30-second sliding difference of cumulative ordinary
  spawns and defeats. It advances only when `S > 0` and `K >= ceil(0.90*S)`; failed observations do
  not clear the history. Each newly entered phase must itself accumulate 30 seconds before advancing.
- Pressure rates are `2.40/3.15/4.05/5.10/6.15/7.20/8.25/9.30` per second, with tier mixes progressing
  from `100/0/0/0` to `40/30/22/8`. The spawner uses fractional spawn credit and has no alive cap.
- Stage changes feed only spawn supply, tier scheduling, and unlock selection. `EnemySpawner` passes
  the authored `EnemyDefinition` directly into ECS; it does not apply time-based health or damage.
- `RunPacingTimeline.Evaluate` remains a nominal/testing projection, while formal runtime progression
  is owned by `AdaptiveRunPacingState`; retained docs must state this distinction to avoid reviving
  fixed-time phase switching.
- Enemy definitions confirm that authored attributes are species data. Base enemies use explicit
  values, optional-pack enemies share three ecological baselines (38/92/216 HP and 60/48/38 speed),
  and `EnemyDifficultyScaler.Scale` returns the original definition unchanged.
- Runtime strength labels are `Common`, `Veteran`, `Elite`, and `Champion`. Optional-pack outer,
  core, and deep species currently map to Common/Veteran/Champion, while selected base species also
  occupy Elite.
- The current finite build is six parallel routes, capped at rank 3 or 4, with no time-granted
  projectile count. `追魂诀` adds predictive ordinary shots, `天罗弹阵` adds player-centered barrage
  shots, and both share the same single-projectile damage budget.
- Every finite route has an endless continuation after its own cap. The current specialization gate
  is run level 4 plus base rank 2, and the two specializations are intentionally not mutually exclusive.
- The main README is also stale and must be corrected with the docs: it still claims time-granted
  1/3/5-shot growth, five-rank finite paths, global enemy stat growth, and a 46-card catalog. Current
  intent and manifests use upgrade-only projectile growth, 3-4 finite ranks, fixed species stats,
  six base cards plus seven TH06 Boss cards, and 51 cards total.
- `plugin_first_design.md` already contains the current hybrid ECS/OOP boundary and content lifecycle.
  It needs a documentation-authority map, not another architecture section.
- `build_diagnostics.cmd` copies `docs/diagnostics.md` into the package. The file existed but was stale;
  rewriting it is required so newly built diagnostic packages carry current instructions.
- `CHANGELOG.md` correctly preserves historical claims as version history. The docs cleanup should not
  rewrite old release entries as if those versions had shipped with current rules.
- The canonical spell-card balance tool confirms the current manifest inventory is exactly base plus
  20 optional packs and 51 cards, with the shared budget range still passing (`0.168..0.699`).
- `docs/diagnostics.md` is now rewritten as the sole current guide consumed by
  `tools/diagnostics/build_diagnostics.cmd`.
- Literal stale-text checks now find no `alpha-0.0.4`, five-rank build, or 46-card claims in current
  docs/README/notes. The remaining numeric `46` is the authored insect movement speed.
- Stable spell targeting is implemented with `SpellCardTargetReference` carrying an `EcsEntity`; the
  combat backend resolves current position and damage through that stable handle.
- Initial diagnostics-source search found entity/projectile/collision/chunk metrics but not pacing
  window or build-count fields. The new guide must not promise those fields unless the snapshot audit
  proves they are emitted.
- Full diagnostics snapshot audit confirms pacing gear, spawn rate, K/S, and current ordinary/barrage
  counts are not serialized. The guide must limit its field list to actual entity/projectile/collision,
  chunk, level, content, character, pause/modal, visual fallback, frame, memory, and renderer metrics.
- `EnemyPressureCurve` has a pure post-final `+0.12` continuation, but formal `RunPacingCoordinator`
  stops advancing in endless and emits a terminal snapshot pinned at final difficulty. Current runtime
  therefore holds 9.30 spawns/s after choosing endless. This is a documented implementation gap, not
  an implemented endless-pressure feature.


## Five-Minute Core Loop Audit - 2026-08-16

- The user replaced fixed time-only phase jumps with combat-capability progression: once the
  current pressure becomes mowable, the run should advance to a harder phase.
- A pure instantaneous difficulty response would punish every power gain and could trap weak
  builds. The accepted design is sustained dominance plus a minimum showcase duration and maximum
  timeout; the phase HUD exposes this progress instead of applying hidden rubber-banding.
- The user clarified that Boss starvation is primarily a five-minute DPS/throughput failure, not
  merely a nearest-target policy defect. By four minutes thirty seconds, every valid base route
  must already mow ordinary enemies faster than the runtime supplies them.
- The first deterministic timeline exposed the gap: Utility still carried 37.7 ordinary enemies
  at five minutes while the other three routes had cleared. After separating finite ordinary-enemy
  durability from the unchanged Boss health curve, projected five-minute ordinary counts are
  Baseline/Assault/Rapid `0` and Utility `15.3`; Utility defeats `0.83/s` against `0.35/s` supply,
  so even its remaining crowd keeps shrinking.
- The user explicitly rejected Boss-only projectiles, pass-through collision, and reserved fire
  shares. Boss access must emerge from sufficient ordinary-enemy clearing under the unchanged
  nearest-target rule.
- Spell-card visual diversity must come from atlas-region data. Reusing one yin-yang-orb identity
  across effects wastes the installed original bullet sheets and makes forty-six cards look alike.
- A rotating formation that does not acquire or intersect enemies is decorative output and cannot
  count toward barrage progression; trajectory tests must measure eventual target intersection.
- The dynamic final gate now requires at most one quarter of the current ordinary-enemy limit or a
  five-minute timeout. An already empty field counts as dominance, preventing strong builds from
  stalling merely because there is nothing left to kill.
- Codex-launched Godot inherited a write restriction on `%APPDATA%`; default log rotation failed and
  Godot 4.7.1 then crashed natively at the same address shown by the user. Redirecting `--log-file`
  to project `.godot/codex-playtest.log` made the identical balance test pass with exit code zero.

## Combat Balance Audit - 2026-08-12

- Character player and Boss values are currently derived from SHA-256 bytes of `CharacterId`; this is stable but not designable, explainable, or tied to Touhou identity.
- Official ordinary-enemy values currently use work-number modulo variation. Source work therefore changes stats without a gameplay reason, contradicting horizontal DLC.
- The six finite core upgrades are almost entirely flat stat growth. Their specializations also map back to the same six stats, so most choices do not alter targeting, projectile lifecycle, hit behavior, or defensive decisions.
- Player barrage projectile count advances automatically by elapsed time (1/3/5/7 shots) independent of build. This makes time itself grant a large multiplicative DPS increase and can swamp upgrade balance.
- Ordinary-enemy health, damage, spawn density, rewards, level requirements, and player projectile growth were authored separately. They need one simulation before individual constants can be considered balanced.
- Spell cards correctly use runtime-resolved character/build attributes and no longer use spirit power, but their 42 factor sets still require archetype-budget comparison by trigger reliability, area coverage, target count, defense, and interval.


## Horizontal build, world, and supplied-log result - 2026-08-12

- D3D12 is the reproduced slow path: the supplied session runs around 13.9 FPS even in the main menu with no combat entities, while GPU render time remains below 1 ms and one CPU core stays saturated. This excludes the FPS cap, ECS population, collision count, and GPU saturation as the primary cause.
- Both OpenGL sessions run correctly on the same RTX 3050 Laptop at 1280x720. Unpaused gameplay reaches roughly 298-645 FPS at the recorded low-to-mid entity load, so OpenGL Compatibility is now the default and D3D12 remains a diagnostic comparison path.
- The 343-487 ms first-world spike aligns with synchronously generating 25 chunks. Prime now builds only the nearest 3x3 and queues the remaining 16 under the existing frame budget.
- The second OpenGL session found a separate deterministic failure: a spell-card choice queried rank by upgrade kind, threw an exception, and left the tree paused. Stable upgrade-ID rank queries now cover all cards.
- Horizontal affinity is derived only from choices made in the current run. Content source, selected character, biome, and enemy identity cannot change offer weight; every offer reserves an exploration route once affinity exists.
- The world now owns one deterministic semantic region plan. Generation, biome art, enemy lookup, structures, map color, and diagnostics consume that identity instead of recomputing unrelated views.
- Loaded world data and discovered map data are separate. Initial streaming no longer reveals a square, and stable structure instances appear on the travel map only after the player approaches them.
- Structure placement is per definition with spacing, separation, chance, salt, footprint, rarity, orientation, and variant. Official structures bind to the matching work domain layer instead of sharing one global random grid.


## Beta debug performance diagnostics - 2026-08-12

- A concrete 3 FPS failure path exists: `GameSettingsService` previously accepted any persisted `MaxFps` from 0 through 360 and assigned it directly to `Engine.MaxFps`, while `VideoSettingsPanel` displayed the first list item for unknown values. A stale `MaxFps: 3` therefore truly caps gameplay at 3 FPS while the UI misleadingly appears to show 30 FPS.
- Resolution had the same representational mismatch: an arbitrary persisted size could be applied while the UI displayed 640x360. Frame-rate and resolution options must come from one catalog, and migrations must retain before/after values for diagnostics.
- Godot 4.7.1 C# exposes adapter, vendor, API, renderer, display driver and viewport timing directly. The potentially multi-second `OS.GetVideoAdapterDriverInfo` call is intentionally omitted in-process; the same session's verbose engine log supplies driver detail without risking startup or shutdown stalls.
- The environment header and explicit driver-policy record are flushed immediately. JSON, metric, or disk failures disable sampling and viewport render-time measurement without aborting gameplay or leaving diagnostic overhead active.
- Useful one-second monitors include FPS, process/physics frame time, draw calls, rendered objects/primitives, video/texture/buffer memory, object/node/resource counts, 2D physics load, and canvas pipeline compilations.
- `EcsCombatWorld` already exposes alive enemies/Bosses, total/enemy projectiles, pickups, spirits, elapsed time, and visual fallbacks. `ChunkStreamer` exposes active/pending chunks. Diagnostics need only a read-only aggregate snapshot from `WorldDemo`, not entity traversal.
- Godot viewport timing can report render CPU and GPU milliseconds independently of FPS caps. Combined with managed heap/GC, process memory/CPU, draw calls, and `player projectiles × enemy pool count`, this separates renderer, allocation, and the real O(P×E) collision traversal pressure without per-collision instrumentation.
- The project explicitly forces the Windows D3D12 rendering driver with the Mobile renderer. A tester falling back to a software adapter or an unsuitable D3D12 path is a credible explanation for roughly 3 FPS, so the log must record the actual adapter, vendor, driver, rendering method, and display driver rather than only the configured value.
- Video settings are user-persistent and can select up to 7680x4320, borderless desktop size, VSync, and a 0-360 FPS cap. Diagnostics must record the applied window size/mode, screen size, VSync state, and frame cap because another machine does not share the developer's settings file.
- The local Godot 4.7.1 executable confirms `--export-debug`, `--log-file`, `--print-fps`, renderer overrides, and debug-template-only options are available. The diagnostic artifact can reuse the resource exclusions and embedded PCK while remaining a separate debug export.
- `project.godot` has no explicit file-logging policy. A project-owned structured session log is still needed so double-click users produce the same evidence without knowing command-line flags.
- A useful A/B test should run the same debug EXE once with its configured D3D12 path and once with `--rendering-method gl_compatibility --rendering-driver opengl3`; the guide must keep the logs separate.
- A companion launcher must pass its unique session directory into the runtime diagnostics host. Engine logs beside the package and structured JSONL under AppData are otherwise easy for a remote tester to separate accidentally.
- The optimized exported diagnostic executable completed a 600-frame headless smoke run and produced four one-second samples with no engine warning or error. Final ZIP SHA-256 is `ead9f558dfabc66e633e73e8c0d276a9b6e7dd01b8d88e7c86270de31eceef46`; final main EXE SHA-256 is `e051da32a11ba51f173b43d55107a64cd2bd04e48b1ddc6f8ef963f373d394f0`.
- Initial exported smoke attempts hit Godot's shared Mono cache because the Codex sandbox group had read-only ACLs on the real user's old `%LOCALAPPDATA%` extraction. Re-running with a fresh task-owned cache succeeded; alpha, normal beta, and both diagnostic directories were preserved and no cache file was deleted.

## Release-candidate findings - 2026-08-12

- Entity-count caps alone do not make an endless curve: ordinary health and contact damage now consume the same shared snapshot through ten-second cached definition tiers, while speed and entity counts retain readability/performance caps.
- A character-aware HUD test must disable persistent meta progression before comparing base health; otherwise the user's real save bonus legitimately changes the expected maximum and contaminates the test.
- Character Boss visuals use the Character mapping category rather than ordinary Enemy mappings. Portraits are aspect-fitted, ActorStrips animate in four frames, and missing sources retain an explicit Chinese-name fallback.
- Player and enemy bullets share one 2,000-projectile ECS pool with separate 1,600/400 budgets, preventing either side from starving the other during late-game danmaku.
- The supplied TH01-TH05 and TH20 source package cannot provide same-work art for every entry. Runtime mappings must therefore retain reviewed proxy provenance or explicit unavailable declarations instead of pretending same-work coverage.
- The previously removed branding icon is byte-identical to the user's specified start-game source and the committed icon blob, so restoring it preserves rather than substitutes the requested release identity.
- Two command probes failed because cmd.exe interpreted regex pipe/caret quoting; subsequent Windows searches use separate `-e` patterns and direct file reads.

## Five-domain runtime completion - 2026-08-11

- TH01-TH20 regions, structures, and ordinary enemies already run through the infinite-world and ECS selection pipeline; 60 region/structure/enemy groups are covered.
- Characters outside the hard-coded Reimu shell are currently compendium strings only. There is no stable character ID, runtime character catalog, character selection, or Boss catalog.
- The target identity rule is one shared character definition with playable and Boss profiles. A run context stores the selected character ID; Boss candidate filtering removes that ID before random selection and never re-adds it as fallback.
- Character Bosses must use a separate encounter director, Character visual mappings, persistent offscreen behavior, and a compact Boss health projection. They must not enter ordinary ecology weights.
- Only two Reimu TH06 spell cards currently exist. The all-work target is at least two automatic spell cards per official work, declared in content manifests and filtered by enabled content.
- Spell cards remain passive build unlocks and automatic combat decisions. The current enum/switch and fixed Reimu loadout must be replaced with stable IDs and reusable trigger/effect strategies.
- TH01-TH05 and TH20 have no same-work source assets in the supplied pack. Any spell bullet atlas for those works must be an explicit reviewed proxy; unavailable character images continue to use Chinese text fallback rather than false attribution.
- Endless combat adds three separate responsibilities: a monotonic difficulty budget, AI decisions that consume that budget, and pooled projectile emitters that translate it into bounded patterns.
- Early-to-late danmaku must increase pattern density and cadence for both sides without unbounded node creation. ECS pools retain explicit live-entity caps and recycle policies while strength continues through health, damage, speed, pattern complexity, and reward scaling.
- Upgrade progression must remain available after finite named techniques reach their caps; repeatable post-cap cultivation or an endless reinforcement tier is required so every later level still resolves to a meaningful build choice.

## DLC source and crop audit - 2026-08-11

- Supplied original material contains complete integer-game trees for TH06-TH18 and only a TH19 trial tree; TH01-TH05 and TH20 have no same-work source directory.
- Missing-source works must use an explicit cross-work proxy declaration or remain text fallback; a proxy cannot be described as same-work original art.
- TH07 stage atlases share the same generic first row, so three separate files at `[0,0,32,32]` still produced three identical enemies. Ghost rows at y=448/480 are distinct and semantically closer.
- TH09's source at x=256 contains 64-pixel frames; the previous 32-pixel declaration cut alternating halves. Frame dimensions must be audited, not inferred from a general enemy atlas convention.
- TH11 `enemy2.png` contains elemental orbs, not ravens. The stage-6 Utsuho strip and stage-5 Orin strip are more honest visual proxies for the registered hell-raven and kasha-cat enemies.
- TH12's first enemy row is 64x64. Reading it as 32x32 alternated complete fragments and empty halves despite passing simple nonblank tests.
- TH13+ portraits are layered. TH13 uses transparent expression layers; TH14-TH15 require clearing only edge-connected near-white pixels before overlay so white costume and eye pixels survive.

## Final all-DLC verification - 2026-08-11

- The generated catalog records 294 source files. Runtime provenance contains 60 explicit cross-work proxies and 42 explicit unavailable entries rather than silently presenting substitutes as same-work art.
- All 21 source groups pass texture existence, dimensions, nonblank alpha, actor-frame variation, and contact-sheet layout checks; the inspected outputs retain nearest-neighbor filtering and complete character silhouettes.
- The formal WorldDemo no longer loads the legacy EnemyActor, PickupActor, SpiritDropActor, or PlayerProjectile scenes. All 69 registered enemies resolve through the ECS renderer; only a synthetic unknown identity exercises the text fallback.
- ECS spirit collection previously wrote progression directly and then emitted an event whose subscriber wrote it again. SpiritSystem now emits collection only, leaving SpiritDropSpawner as the single progression writer.
- Test-world teardown must allow the audio mixing thread to release active OGG/WAV playback before clearing streams and freeing the world; immediate QueueFree produces nondeterministic ObjectDB and resource leaks.
- That historical validation build is now named `alpha-0.0.0`. Inspection found the ignored local export at `release/TouhouWuxiaSurvivor_alpha-0.0.0.exe` (196,463,928 bytes, SHA-256 `8b687f65ff34d4106b2a7ff92600644cd9482bc7c25aa9d47206955525425478`). Its embedded paths still contain the excluded legacy `item_sheet.png`, proving it predates the final source state and cannot verify a later build.
- Legacy reference files remain physically present only because deletion requires explicit user confirmation; export policy excludes every legacy namespace. No release file will be removed or overwritten without separate user direction.

## All-DLC Completion - 2026-08-11

- ECS refactor is committed at `91ed4a1`; all DLC work begins from a clean rollback point.
- The prior internal-original pipeline fully covered only Gensokyo base and TH06, so existing TH01-TH20 content-pack JSON files do not prove actual visual/runtime completion.
- Every DLC will be accepted only after data coverage, normalized original asset mappings, formal runtime wiring, and real Godot visual inspection all pass.
- No supplied source-pack file may be modified or deleted; derived assets stay under `assets/internal_original`.
- ECS runtime currently draws all enemies, pickups, and spirit drops as text in `EcsCombatWorld._Draw`; this regressed the already completed compendium enemy visuals.
- `InternalVisualCatalog` is already the intended shared boundary for compendium and formal gameplay, so ECS rendering must query it instead of duplicating asset paths.
- Existing normalized enemy strips are 192x48 with four 48x48 frames; formal ECS rendering can use the same frame contract as the compendium.
- Pickup definitions have no internal visual mapping yet; all three currently read a borrowed reference-game sheet and therefore require a new shared original-item mapping.
- Visual inspection confirms `assets/internal_original/base/actors/wild_fairy.png` is a valid four-frame normalized strip.
- The supplied TH16 `bullet/item.png` is a 256x64 Touhou item atlas containing distinct P, life, star, S, and F icons; it is the selected source for temporary pickup and spirit visuals.
- The existing TH06 `bullet_atlas.png` is already mapped and is the selected source for player bullets, so no formal combat path needs the borrowed item sheet.
- The rebuilt `base/item_atlas.png` exactly preserves the 256x64 TH16 source with transparency; selected P, green star, F, and cyan season-item regions are visibly complete and distinct.
- Formal ECS coverage must assert renderer counters after a real physics/draw frame; tests that instantiate `EnemyActor.tscn` cover only the compatibility path and cannot prove the survivor runtime uses original assets.
- Formal gameplay and tests now have zero reference-game audio references: TH17.5 provides Reimu BGM/player events and TH18 provides item/enemy events; the only semantically imperfect mapping is `landing.wav` as an explicitly declared temporary footstep beat.
- The supplied pack has complete mainline visual sources only for TH06-TH18, TH19 is a trial build, and TH01-TH05 plus TH20 are absent. Missing works require explicit cross-work proxy metadata or new sources; they cannot be falsely marked same-work originals.
- Existing TH01-TH20 manifests represent data placeholders rather than complete DLC: the formal mapping remains base/TH06 only, and modern layered portraits require per-work composition rules before bulk generation.
- User explicitly rejected all remaining reference-game art, including `assets/combat/projectiles/item_sheet.png` and `assets/combat/pickups/pickup_sheet.png`; those files must not be referenced by formal runtime code.
- Pickup mechanics may retain survivor-style temporary movement/fire-pattern effects, but their names and visuals must become Touhou original-related items sourced from the provided original pack.


## Base Enemy Visual Audit - 2026-07-31

- Godot provides `Image.GetUsedRect()` for automatic nontransparent Alpha bounding boxes, but it cannot select semantic frames from a sprite sheet.
- Dedicated generic enemy atlases exist at TH10 `ANM/ANM/enemy/enemy.png` and TH08 `ANM/ANM/stgenm/enemy.png`.
- TH10 `stgenm/*` is composed mainly of named stage characters and effects; using it for generic Gensokyo enemies is semantically wrong even when a crop is nonblank.
- The previous record update failed because it assumed a nonexistent heading level; no code or asset file changed.
- TH08 and TH10 generic enemy atlases are regular 512x512 sheets with multiple colored small/large fairies, directional animation frames, and shared bullet/effect regions.
- These atlases are appropriate for fairy-class enemies but do not visibly provide nine distinct ecologies; mapping every base enemy to a color variant would still be wrong.
- Full-pack inventory shows later official games split generic enemies across `enemy2.png`, `enemy5.png`, `enemy_g.png`, and `enemy_ll*.png` rather than stage-character sheets.
- TH19 `animal_spirits.png` contains clean four-frame rows for multiple animal spirits, including a wolf-like green row appropriate for the generic `妖兽` role.
- TH19 `enemy2.png` contains projectile/spirit-glow effects only and must not be mapped as a creature.
- TH19 `enemy5.png` contains regular four-frame bat rows plus larger winged enemy frames; these are candidates for `夜行妖怪` and `大妖怪` respectively.
- TH19 `enemy_g.png` contains larger multi-color generic fairies and is suitable for stronger generic fairy/spirit roles.
- TH19 `enemy_ll.png` is a repeated named character animation rather than a generic family and is rejected for base-content mapping.
- TH11 `enemy2.png` is another projectile/spirit-glow sheet; TH12 `enemy3.png` is a single aura/effect. Neither is a creature source.
- TH12 `enemy4.png` is a UFO object strip, while `enemy5.png` matches the later bat/winged generic sheet.
- Numbered extra-sheet guessing is no longer productive; continue with explicit generic atlases and older works known to contain kedama/spirits.
- TH09 provides an explicit `ANM/ANM/stgenm/enemy.png`; TH07 has only stage-specific enemy sheets and therefore requires stricter visual validation before any use.
- TH09's generic sheet contains regular/large fairies plus a named large-fairy character; it does not visibly contain kedama.
- TH07 `stg1enm.png` mixes stage characters and effects, so it is rejected as a base generic source.
- External sprite index confirms kedama appear in Mountain of Faith and exposes a `Th10Kedama.png` four-frame-width strip; the fuzzy colored rows in TH10 `enemy.png` are therefore enemy frames, not bullets.
- The same indexes identify TH11 evil-spirit sprites and TH12 ghost-fairy sprites as explicit generic enemy families available in the supplied local works.

## Requirements
- Second progression milestone: durable out-of-run rewards, unlocks, bounded permanent growth, and a main-menu cultivation surface.
- Use Godot C# and preserve the current survivor-style automatic combat.
- First progression milestone: spirit experience, level-up choices, run build state, compact UI, and run-summary integration.
- Keep systems highly decoupled, one class per file, organized by responsibility.
- Keep each file at or below 250 lines and document every class and function in Chinese.
- Do not introduce permanent stat growth until the in-run curve can be measured.

## Research Findings
- The completed in-run milestone is the stable baseline; meta progression must compose with its modifiers, pause ownership, HUD, and death summary rather than replacing them.
- `GameSettingsService` establishes the project's JSON and UTF-8-without-BOM convention, but saves directly to the final file. The progression profile should reuse serialization conventions while writing a temporary sibling and atomically replacing the final file.
- Settings initialization is static and idempotent. Progression needs the same simple call surface plus an explicit test reset/reload seam so integration tests can isolate `user://` data without mutating the player's real profile.
- `MainMenu` already follows a single-active-panel pattern for content, compendium, and settings. A cultivation panel can use the same `Present`/`BackRequested` contract and one additional 32-pixel button; the existing 228-pixel menu panel is just large enough for five commands without increasing first-viewport density.
- The main menu's right-side role block can remain the immediate character signal. Meta currency should appear inside the cultivation panel rather than permanently adding another HUD strip to the menu.
- `PlayerHealth` initializes from an exported `MaxHealth` during child `_Ready`, before `WorldDemo._Ready`. Permanent health must therefore use an explicit post-ready API that raises both maximum and current health and emits `HealthChanged`; mutating only `MaxHealth` would leave HUD and current health inconsistent.
- Player movement already reads `RunModifierState`, so permanent movement and spirit-attraction bonuses should become base multipliers inside that projection. Run-upgrade refresh must multiply from the base rather than overwrite it.
- Existing balance tests pin the six run-upgrade first-rank multipliers to their no-profile baselines. Base permanent multipliers can be added with default `1.0` values so all current contracts remain valid, then extended with explicit composition assertions.
- No existing UI-asset test inspects main-menu command count or layout, so a dedicated cultivation-panel scene test is required instead of weakening unrelated asset coverage.
- `RunSummary` currently contains only immutable performance data. Reward calculation should happen from raw final seconds/kills/level before constructing the summary; the settlement result can then add `RewardEarned` and current balance without making the summary mutable.

## Meta Progression Contract
- Currency is neutral幻想乡通用`钱`; a run earns `floor(seconds/45) + floor(kills/12) + max(level-1, 0)`, clamped to 0-80. Immediate intentional deaths therefore earn nothing, while survival, combat, and leveling all contribute.
- Profile fields are version, current/lifetime money, completed settled runs, cultivation ranks, and a bounded list of 32 settled run IDs.
- Four Reimu-specific preparations have actual consumers: `博丽护身结界` (+1 max/current health, 3 ranks), `空中飘浮` (+2% move speed, 5 ranks, unlock at 30 lifetime money), `阴阳玉共鸣` (+8% spirit attraction, 5 ranks), and `封魔针调律` (+1 damage, 2 ranks, unlock at 100 lifetime money).
- Costs rise by current rank: health `16 + 12r`, movement `12 + 8r`, attraction `10 + 6r`, damage `40 + 45r`. Spending never reduces lifetime jade, so unlock progress cannot regress.
- Reset overwrites the profile with defaults after a two-step in-game confirmation; it does not delete files and does not touch settings.
- Canon review: Reimu's profiles consistently identify flight/floating and spiritual power, while her established combat tools include amulets, Yin-Yang Orbs, and needles. The Hakurei Shrine donation box exists but receives few visitors/donations.
- User-facing meta framing is therefore `博丽神社整备`, not generic cultivation. Currency is `赛钱`; the four nodes are `博丽护身结界`, `空中飘浮`, `阴阳玉共鸣`, and `封魔针调律`. Internal persisted fields will use donation terminology as well.
- Follow-up currency review found no single canon fantasy-token name. Official-print discussion depicts Gensokyo as substantially barter-based while still using money-like exchange media; fan rules that name `円/钱` are interpretations rather than a unique canon currency.
- Per the user's requirement for a Gensokyo-wide medium, the UI will use the neutral Chinese `钱`, and persisted fields will use `Money/LifetimeMoney`. It is not shrine donation income and does not invent a new `幻想币` or `灵玉` denomination.
- The Reimu-specific preparation panel passes a real-scene test with no scrolling: canonical labels, money purchase, lifetime unlock gate, and two-step reset all behave correctly.
- World scenes now expose `PersistMetaProgression`; production defaults to disk persistence, while death/integration tests use a volatile store and cannot alter the player's real profile.
- Main-menu entry/return and atomic JSON round-trip now pass in integration tests. The suite contains 17 scenes after adding meta balance and shrine-preparation coverage.
- New requirement: a rebindable `toggle_stats` action defaults to `E` with an empty second slot, matching the existing two-slot input catalog.
- The character stats panel should own pause like the map, but switch directly from an open map and ignore input while pause/level-up/death owns the modal state. It will receive an immutable snapshot provider rather than querying arbitrary scene paths.
- `PauseMenuOverlay` already exposes `IsOpen` and blocks map input while active. The stats overlay can ignore E when pause is open, block pause while stats is open, and use public map `Close/Open` methods for direct E/M switching.
- `LevelUpOverlay` centralizes modal blocking for map and pause; extending its configure contract with the stats overlay is the correct place to prevent E during mandatory upgrade choices.
- The E stats overlay is 179 lines and its snapshot/factory are 60/67 lines. `WorldDemo` reached exactly 250 nonblank, non-documentation lines, technically satisfying the limit but leaving no maintenance margin; HUD snapshot collection should be extracted into a dedicated coordinator before release.
- `AutoShooter` consumes `RunModifierState.DamageBonus` additively and movement/attraction consume multipliers, so all four permanent effects can be projected through one `ConfigureBase` method. `Refresh` must calculate `base + run damage` and `base multiplier * run multiplier` to avoid order dependence.
- The full meta-domain contract passes with an in-memory store: reward math, migration repair, unlock thresholds, rising costs, idempotent settlements, save-failure rollback, and runtime bonus projection behave as specified.
- `WorldDemo` is the only `RunSummary` construction site. A small `MetaRunSession` can own the profile manager and unique run ID, exposing startup bonuses and one settlement method; this keeps save mechanics out of the composition root and limits its added effective lines.
- The project already has enemy drops, temporary pickups, automatic targeting, pause ownership, a compact HUD, and death summaries.
- Enemy damage and death originate in `EnemyActor`; `EnemySpawner` already republishes combat events.
- `PickupSpawner` owns current random temporary-drop creation, so spirit XP should use a separate deterministic drop path rather than enter `PickupCatalog` weights.
- `AutoShooter` exposes damage and interval-like runtime properties, while player movement and health are separate components.
- World UI is split into HUD, pause, map, death, and compendium components, so level-up needs its own pause-owning overlay.
- `EnemyActor.Defeated` and `EnemySpawner.EnemyDefeated` already carry both global position and `EnemyDefinition`; spirit value and placement can be decided outside the actor without changing its death lifecycle.
- Enemy recycling does not emit defeat, so off-screen cleanup cannot accidentally award experience.
- `PickupActor` is intentionally coupled to `PlayerBuffController` and a 14-second temporary lifetime; reusing it for spirit XP would violate both semantics.
- `PickupSpawner.TrySpawnForEnemy` can remain unchanged for random buffs while a new spirit spawner subscribes to the same enemy-defeated event and awards XP deterministically.
- Player movement reads a multiplier from `PlayerBuffController`; permanent run modifiers should compose with this temporary multiplier instead of being written directly into pickup timers.
- `PlayerBuffController` is a clean temporary-effect boundary and should not own level-based progression state.
- `AutoShooter` currently reads temporary fire-rate state directly and exposes base damage, range, speed, and interval; a separate run-modifier component can be injected and multiplied with temporary buffs.
- `WorldDemo` is already near the file-size ceiling and acts as composition root. Progression wiring should be delegated to a dedicated coordinator node rather than expanding `WorldDemo` with leveling logic.
- Combat entities are rebased by category containers, so spirit pickups placed under `CombatEntities` will automatically remain aligned in the infinite world.
- The compact HUD is embedded in `WorldDemo.tscn` and already has a bottom status band; level and XP can be added there without introducing a second persistent HUD card.
- The world scene currently has separate `Enemies`, `Projectiles`, and `Pickups` containers. A sibling `SpiritDrops` container and progression coordinator node fit the existing composition pattern.
- HUD formatting already consumes an immutable `WorldHudSnapshot`; level and experience fields can be added without letting HUD nodes query progression services directly.
- The bottom status text is already dense, so XP should use a thin dedicated progress bar while only the level number joins the status string.
- Map and pause overlays each remember and restore the previous `SceneTree.Paused` value. The level-up overlay must follow the same ownership model and temporarily set both existing overlays' `InputBlocked` flags.
- Level-up input must run with `ProcessMode.Always`; gameplay state and spirit pickups remain paused while a choice is open.
- `PlayerHealth` has no safe maximum-health mutation or healing API. The first upgrade pool should avoid health upgrades rather than bypassing health invariants.
- The first six upgrades can map cleanly to current systems: damage, fire rate, move speed, target range, projectile speed, and spirit attraction range.
- `RunSummary` is immutable and the death overlay binds explicit fields. Final level and build summary should be added to the snapshot rather than queried from live progression after death.
- Existing integration tests instantiate the real `WorldDemo` and wait process/physics frames, so progression tests can validate an actual enemy defeat, spirit collection, pause choice, modifier application, and HUD update in one scene.
- `PlayerProjectile` consumes itself on the first successful hit, so the first progression slice will not advertise piercing or multihit upgrades.
- `WorldDemo` still calls the old `AutoShooter.Configure` overload, so the first integration build reported only the missing `RunModifierState` argument; the new progression classes themselves passed compile-time parsing.
- `WorldDemo.tscn` has only enemy, projectile, and temporary-pickup combat containers. Spirit XP needs its own sibling container and spawner, while the level overlay needs a higher layer than the map and pause menu.
- `RunProgressionCoordinator` already owns the complete state/build/modifier trio and existing overlay input exclusion, so `WorldDemo` only needs dependency wiring and snapshot reads. Its no-choice fallback is recursive and should be converted to a bounded loop before final verification.
- `WorldDebugHud` owns direct node caches while `WorldHudSnapshot` remains a pure immutable value. Adding a level label and XP bar therefore requires only three snapshot fields and three HUD node caches; combat code does not need any UI references.
- HUD smoke coverage already asserts a single-line status and a maximum 44-pixel band. The progression display must preserve those constraints, so level/XP will be compact controls within the same `HBoxContainer`, not a second row or panel.
- `RunProgressionState` exposes exactly the HUD inputs needed (`Level`, `Experience`, `ExperienceToNext`) and also retains `TotalExperience` for the later death summary.
- `RunSummary` and `DeathScreenOverlay` bind explicit immutable fields, so progression results should be added as `FinalLevel`, `TotalExperience`, and one preformatted build-description string. This keeps the death layer independent of upgrade definitions and live coordinator state.
- `RunSummaryTextFormatter` belongs to the gameplay/session layer rather than the death UI folder; future summary formatting changes should preserve that existing ownership.
- The death summary uses a two-column grid inside a fixed 560x340 logical panel. Three short progression rows fit without scrolling if the build summary wraps in the value column; the quick popup can add only the final level to remain uncluttered.
- `RunBuildState.Describe()` already produces a stable catalog-ordered Chinese summary and explicitly handles an empty build, so no UI-side construction logic is needed. Existing death-flow coverage can assert the new labels without changing navigation behavior.
- `SpiritDropSpawner.Spawn` is deliberately public and deterministic, and `LevelUpOverlay.SelectChoice` is public with a stable choice count. A real-scene smoke test can therefore drive collection and selection without synthesizing combat or exposing private internals.
- A spirit spawned exactly at the player is collected on its first process tick before any movement calculation. This makes the integration test deterministic: award 8 XP, observe level-up pause, choose index 0, then assert build/modifier/HUD restoration.
- Every first-rank upgrade changes exactly one public modifier away from its baseline, and both existing modal overlays expose `InputBlocked`. The smoke test can verify actual modifier application plus pause/input restoration without adding test-only production APIs.
- The progression balance, real-scene progression, compact HUD, and death-flow tests all pass. The remaining integration scenes still cover combat, pause, map, audio, settings, texture, content, and compendium behavior and should be rerun before export.
- All 15 integration scenes pass after progression integration. Existing map, pause, settings, audio, texture, compendium, content-pack, enemy-balance, and combat-loop contracts show no behavioral regression.
- Every audited C# file is below 250 physical lines except `WorldDemo.cs` at 264 physical lines. Because the agreed limit excludes documentation comments, its effective code count needs a separate check before deciding whether composition-root extraction is necessary.
- `WorldDemo.cs` has 218 nonblank, non-documentation lines, satisfying the user's explicit comment-excluded 250-line limit. The UTF-8 BOM scan found no BOM in source, tests, or planning text.
- The existing Windows preset already embeds the PCK, excludes tests/tools/release, uses the project icon, and targets `release/TouhouWuxiaSurvivor_beta0.2.exe`. Existing `TouhouWuxiaSurvivor_beta0.1.exe` is a separate file and will remain untouched.
- Export produced a single 189,610,952-byte `TouhouWuxiaSurvivor_beta0.2.exe` with no adjacent `.pck`, confirming the embedded-PCK layout. The beta0.1 executable remains untouched.

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| Treat spirit experience as a separate pickup purpose | Temporary combat buffs and persistent run XP have different lifetime and collection semantics |
| Put upgrade effects behind run-state modifiers | Player, weapon, pickup, and UI consumers should read one stable runtime contract |
| Spirit value is `ceil(sqrt(enemy health))`, clamped to 1-8 | Rewards durable enemies without making deep-tier drops explode linearly |
| XP requirement is `6 + level*2 + floor((level-1)/5)*5` | Starts at 8 XP and adds a visible step every five levels while staying testable |
| Upgrades have five ranks | Matches the agreed build size and keeps descriptions and balance legible |
| Wide level choices are a mandatory modal | The world pauses and existing map/pause input is blocked until one choice resolves |
| Spirit drops persist but merge at a 240-node cap | Avoids losing earned XP while bounding infinite-world node growth |
| First pool has six modifier upgrades | Damage, fire rate, movement, target range, projectile speed, and attraction all map to current runtime parameters |

## Issues Encountered
| Issue | Resolution |
|-------|------------|
| None in the pure progression contract phase | Build completed with zero warnings and the balance test passed |
| `WorldDemo.cs(87)` omitted the new `runModifiers` parameter | Resolve by wiring the progression coordinator and modifier state through the composition root |

## Resources
- `src/gameplay`
- `src/actors`
- `src/combat`
- `src/ui`
- `tests/integration`

## Visual Findings
- Current UI uses a 640x360 logical viewport and must remain dense enough for 2x presentation at 1280x720.
# Attribute Panel and HUD Boundary

- `toggle_stats` is a normal rebindable input action whose default primary key is E; it is not hard-coded in the overlay.
- The stats overlay is mutually exclusive with the map, pause menu, level-up selection, and death flow. E closes itself, Escape closes it, and M transfers to the map.
- `WorldHudCoordinator` now owns runtime HUD snapshot collection. After extraction, `WorldDemo` contains 218 non-comment, non-blank lines; the coordinator is 89 physical lines.
- Targeted Godot tests for the stats overlay, HUD, run progression, and death flow all exit successfully.

# Spell Card Audit

- The runtime upgrade catalog currently contains six wuxia-styled stat upgrades: 封魔针法、博丽呼吸法、天狗步、追魂诀、御风诀、聚灵诀。
- The repository has no `SpellCard` data model, no original-work/source field on upgrades, and no separate spell-card activation or evolution layer.
- Only 封魔针 has a direct Reimu equipment reference. The other names are mostly original wuxia flavor and must not be presented as canon spell cards.
- A correct future model should keep basic weapons/abilities, in-run cultivation modifiers, and named spell cards as separate content types. Spell cards need their exact name, owner, source work, unlock/evolution requirements, and gameplay effect.
- Final source lookup confirms `toggle_stats` is registered in `InputActionCatalog` with E as its default primary key, while the overlay consumes the action name; rebinding therefore remains supported.
- The project directory is not a Git repository, so completion evidence is based on direct source inspection, compilation, and scene tests rather than Git diff metadata.
# Spell Card Implementation Session

- The user approved integrating spell cards into the wuxia setting as real gameplay, while preserving their identity as named ultimates rather than ordinary stat ranks.
- `planning-with-files` governs this multi-module change; implementation must update the persistent plan after phases and record every error.
- No release/export work is authorized in this milestone.
- Canon lookup confirms Reimu's `灵符「梦想封印」`/Spirit Sign "Fantasy Seal" as a luminous homing-orb spell and `梦符「封魔阵」`/Dream Sign "Evil-Sealing Circle" as a short-range barrier technique.
- `灵符「梦想封印」` uses bounded homing orbs, while `梦符「封魔阵」` uses close-range damage plus the existing player invincibility state; both are implemented automatic build outcomes.
- Active spell-card buttons are explicitly rejected by the user. Spell cards must enter through level-up build choices and trigger automatically from resource plus combat context.
- `RunProgressionCoordinator` is intentionally limited to experience, build ranks, modifiers, and level-choice modal flow; spell casting should not be folded into it.
- `WorldDemo` already composes map, combat, progression, stats, audio, death, and meta systems. Spell logic must live behind a dedicated coordinator, leaving the root with configuration/event wiring only.
- Existing death handling centrally blocks map, pause, stats, and progression. The new spell-card coordinator needs a matching death block so casts cannot occur after settlement.
- `EnemySpawner.EnemyDefeated` exposes death position and `EnemyDefinition`, making it a clean spiritual-power gain source without coupling the resource state to individual enemies.
- The enemy container is already resolved in `WorldDemo`; a spell effect service can receive that container and call each `EnemyActor` through its public damage API so normal death, drops, kill counts, and audio events remain intact.
- `EnemyActor.ReceiveDamage(int)` is the correct effect boundary: it preserves damage feedback and emits the same defeated event used by kill, drop, spirit, and audio systems.
- HUD presentation already flows through `WorldHudSnapshot` and `WorldHudCoordinator`; spell power, selected spell, and readiness should extend the snapshot instead of creating a direct UI-to-runtime dependency.
- `WorldHudSnapshot` already has 17 constructor parameters. Spell presentation should be grouped into a dedicated immutable `SpellCardHudSnapshot` and added as one composed value to prevent parameter sprawl.
- `WorldDebugHud` owns fixed health/level/experience widgets plus one compact status string. Spell power should receive its own stable-width bar/value in the same bottom strip rather than expanding the status text.
- The F3 formatter can expose exact spell power, cost, and cooldown for diagnostics while normal play only shows the readable spell name, key, and readiness.
- The bottom HUD was 590 px wide at a 1280 px reference window. It now uses a compact `奥义 自动/未悟` caption, power bar, and numeric value; full names remain in the E panel.
- `CharacterStatsSnapshotFactory` is the established composition boundary for the E panel. It should receive one spell runtime snapshot rather than making the stats UI query gameplay nodes directly.
- `CharacterStatsSnapshot` already carries 15 constructor values. Spell information should be one composed snapshot value, reused by HUD and stats projections where practical.
- `CharacterStatsOverlay` writes to a fixed non-scroll layout. A single full-width spell summary row is safer than adding another multi-column attribute group.
- `SpiritDropSpawner.SpiritCollected(int)` is a better power-gain source than raw kills: it rewards positioning and collection, respects current attraction/merge mechanics, and prevents an immediate spell-kill feedback loop.
- The E panel's existing two-column `Sources` section can accept one additional `奥义` row without adding a scroll container or widening the panel.
- Spirit values are bounded from 1 to 8. A 0-100 power pool with `collected value * 4` gain gives predictable pacing while rewarding stronger drops.
- `GameSettingsService` would expose any added spell action globally, so production code deliberately adds none; a negative test protects the movement-and-build-only contract.
- Enemy health spans 2 for early fairies to 30+ for late threats. Fantasy Seal will use up to 8 distinct homing orbs at 8 damage each: reliable mob clearing without deleting late heavy enemies.
- Spell visuals can use a text `灵` orb scene, matching the project's current Chinese-text entity art policy while still providing visible movement and impact timing.
- Existing projectiles use Area2D collision, but Fantasy Seal orbs have explicit distinct targets. Direct target tracking plus `ReceiveDamage` on arrival avoids tunneling while preserving the enemy's normal damage/death contract.
- Integration-test convention instantiates `WorldDemo.tscn`, disables persistent meta progression, drives actions with `InputEventAction`, and exits the scene tree explicitly. The spell-card test should follow the same pattern.
- `PlayerHealth` already owns one invincibility timer. Evil-Sealing Circle should extend it through a public bounded grant method rather than introduce a parallel barrier state.
- Implement two automatic cards in the build pool. Fantasy Seal should trigger against a worthwhile distant group; Evil-Sealing Circle should trigger for close pressure or defensive danger.
- Fantasy Seal: 100 power, 8 distinct homing orbs, 8 damage, long target radius. Evil-Sealing Circle: 70 power, 6 radial damage, short radius, 1.25 s defensive invincibility.
- `RunBuildState.CanUpgrade` currently checks only max rank. One optional `RunUpgradeRequirement` gives spell cards prerequisite cultivation without creating a parallel selection UI.
- Spell cards will be one-rank `RunUpgradeCategory.SpellCard` choices. Their choice text should identify `符卡奥义` rather than presenting ordinary martial rank wording.
- `RunUpgradeCatalog` is the sole level-up offer pool, so two stable upgrade kinds integrate spell acquisition with existing pending choices, build summaries, and death summaries.
- Fantasy Seal requires NeedleDamage rank 2; Evil-Sealing Circle requires SpiritAttraction rank 2. The automatic coordinator treats only acquired spell-card ranks as unlocked.
- `WorldDemo.tscn` already rebases every child category under `CombatEntities`; a new `SpellEffects` container will automatically participate in infinite-world origin shifts.
- `RunProgressionBalanceTest` hard-codes six definitions and five ranks for all. It must distinguish six five-rank cultivation entries from two one-rank spell cards and verify their prerequisites explicitly.
- `SpellCardBalanceTest` passes: two cards, unique effects, bounded shared power, prerequisite ranks, one-rank caps, and absence of active spell input are all enforced.
- `SpellCardSmokeTest` passes in the real world scene: Fantasy Seal homing damage, Evil-Sealing Circle area damage/invincibility, automatic power spending, compact HUD, and no-scroll E-panel presentation are connected.
- All new spell-card source/test files are at most 155 physical lines; `WorldDemo` remains at 230 non-comment, non-blank lines.
- Active spell action identifiers occur only in the negative contract test that enforces their absence; production source defines no cast or cycle binding.
- UTF-8 BOM scan returned no matches across source, tests, and planning files.
- The project renders at a 640x360 logical viewport and scales to 1280x720. Expanding the HUD to 720 logical pixels would overflow; spell power must be folded into the existing 590 px status text instead of adding separate widgets.
- Release verification shows only the pre-existing base, beta0.1, and beta0.2 executables; no timestamp changed during this milestone.
- The oversized HUD addition consisted of two spell styles and three fixed controls. They are removed; the original 590 px container now carries a shorter `击/敌/强化/奥义` status string.
- The final compact HUD retains full `击破/敌人/强化` labels, removes only excess spacing, and appends `奥义自动0/100`; `WorldHudSmokeTest` passes again.
- Documentation update initially failed because the patch assumed an older paragraph order; the retry used a stable end-of-section anchor after inspecting the file tail.
# Compendium Spell Card Session

- The user requires the newly implemented spell-card mechanics and numbers to be documented in the in-game compendium.
- Entries must reuse `SpellCardCatalog` as their source of truth and preserve the existing fixed no-scroll/no-splitter detail layout.
- This milestone remains source-only; no release/export is authorized.
- All compendium types, catalog generation, preview, panel, and fact rendering live under `src/ui/compendium`; there is no gameplay compendium directory.
- `CompendiumCategory` currently has Biome, Structure, Enemy, and Character; SpellCard will be the fifth stable category.
- `CompendiumEntry` carries an optional `EnemyDefinition` for preview behavior. Add an optional `SpellCardDefinition` rather than infer spell animation from localized names.
- `CompendiumCatalog` already owns all world/enemy/character mapping and is long enough that spell fact construction belongs in a separate `SpellCardCompendiumEntryFactory`.
- `CompendiumPreview` dispatches by category and uses protected Control draw calls, so it should keep only a compact spell drawing branch rather than delegate drawing to a non-Control helper.
- Spell previews will use animated Chinese text: orbiting/closing `灵` orbs for Fantasy Seal and an expanding/fading `封魔阵` glyph for Evil-Sealing Circle.
- `CompendiumPanel` maps the tab index directly to the category enum. SpellCard must be appended as the fifth enum value and fifth tab to preserve all existing indices.
- `CompendiumFactView` already supports full-width facts for unbounded values and paired rows for short values. Auto trigger, wuxia role, and effect description should be wide; numeric fields should remain paired.
- Add `UnlockKind` to `SpellCardDefinition`; both runtime unlock checks and compendium prerequisite mapping can then share one stable source instead of parallel effect-kind switches.
- `CompendiumPanel.tscn` uses a fixed HBox plus non-interactive separator, no ScrollContainer/RichText detail box, and a 120x84 preview at 640x360.
- `CompendiumSmokeTest` already enforces no draggable splitter, no mouse-wheel detail box, fixed preview size, unwrapped keys, and detail minimum height. Extend these contracts to a fifth spell-card tab.
- Under the TH06 source filter, the SpellCard tab should contain exactly Fantasy Seal and Evil-Sealing Circle and expose at most eight compact fact rows.
- `CompendiumSmokeTest` passes with five tabs, two TH06 spell entries, eight fixed fact rows, live spell preview mode, and all prior no-scroll/no-splitter density assertions.
- Spell runtime/balance, UI assets, and official content coverage tests also pass after the shared `UnlockKind` refactor.
- `CompendiumPreview.cs` is 258 physical lines but 196 non-comment, non-blank lines, satisfying the explicit comment-excluded 250-line rule. The new entry factory is 83 physical lines and catalog is 230.
- Final BOM scan has no matches, and release directory timestamps remain unchanged with only the three pre-existing executables.
# Collapsible Content Selection Session

- The user requires a `显示旧作` toggle in the run-content selector.
- Every official work must be independently collapsible and default to a name-only row; clicking expands its incremental content details.
- Selection state and expansion state are separate concerns, and this milestone remains source-only with no export.
- `ContentPackSelectionPanel` is the only UI owner of run-content selection; `ContentPackSelectionService` and the 20 manifest definitions can remain unchanged.
- `ContentPackSmokeTest` already exercises all 20 checkboxes, detail text, apply behavior, and content isolation, so its UI assertions must be updated rather than bypassed.
- The current panel creates each entry as a VBox containing a CheckButton and an always-visible wrapped additions Label; this is the direct source of the oversized list.
- Keep the base pack always visible and non-collapsible. Treat TH01-TH05 as old works for the visibility toggle, while TH06-TH20 remain visible by default.
- A row component must own one checkbox plus one independent expanded flag; filtering should toggle row visibility and never rebuild or mutate checked state.
- Touhou's `旧作` boundary is TH01-TH05, the five PC-98 works; TH06 begins the Windows era. Use a stable `number <= 5` predicate and document the source: https://thwiki.cc/%E5%AE%98%E6%96%B9%E6%B8%B8%E6%88%8F
- Turning off `显示旧作` hides TH01-TH05 rows only; it must not uncheck an already-enabled old-work pack.
- Current tests depend on child indices inside anonymous VBox rows; replace those assertions with a public `ContentPackSelectionRow` contract.
- The collapsed header should be a standalone checkbox plus a flat name button. Checkbox clicks affect run selection; name/button clicks affect expansion only.
- Collapsed official titles must contain only `THxx + display name`; status and categorized additions move into the hidden detail label.
- The show-old-works switch defaults off but can retain its UI value while the app remains open; opening the panel collapses all rows without changing selections.
- `MainMenu` only calls `Present()` and listens to start/back events, so the row refactor is contained inside the selection panel.
- Expanded details should put status and each addition category on separate lines; only one chosen row grows, while the surrounding ScrollContainer handles navigation.
- Adding a compact filter row requires reducing the scroll minimum from 220 to about 190 logical pixels to remain inside the 640x360 centered panel.
- `ContentPackSmokeTest` passes with 20 registered packs, 15 default-visible Windows works, TH01-TH05 filtering, independent expansion, categorized details, and preserved hidden selection.
- Line audit passes: row component 140 physical lines, panel 149, and the expanded integration test 226 physical / 187 non-comment non-blank lines.
- Final UTF-8 BOM scan has no matches; release directory remains the same three pre-existing executables with unchanged timestamps.
# 2026-07-31 暂停返回按失败结算

- 用户要求：暂停菜单中的“返回主菜单”不能直接离开本局，必须按失败完成本局结算。
- 现有计划记录表明项目已经具备幂等结算和死亡总结基础；实现应复用现有权威路径，避免新建平行结算流程。
- 本阶段不导出发布版。
- `PauseMenuOverlay.ReturnToMainMenu()` 当前直接调用 `ChangeSceneToFile`，这是绕过结算的明确入口。
- `WorldDemo` 已订阅 `DeathScreenOverlay.MainMenuRequested` 并负责死亡总结之后的真正场景切换，暂停返回应汇入同一场景根流程。
- `WorldDemo.OnPlayerDied()` 当前独占最终坐标、群系、战绩、奖励结算和死亡页展示；主动放弃若另写一份必然产生漂移，应重构为带失败原因的共享终局入口。
- 暂停菜单在确认时场景树已经暂停，因此失败总结必须使用 `ProcessMode.Always` 的 UI 路径，并在展示前关闭/隐藏暂停层而不恢复世界运行。
- `DeathScreenOverlay` 本身是 `ProcessMode.Always`，能在暂停状态响应“查看总结/重新开始/返回主菜单”；它可作为通用失败覆盖层继续复用。
- 当前 `DeathScreenOverlay.Present` 只接受不含失败原因的 `RunSummary`，且类语义和场景文案可能绑定死亡；需要检查并数据驱动化失败标题，避免主动放弃被错误描述为阵亡。
- 场景快速弹窗标题固定为“符力耗尽”，消息固定为“这次幻想乡之行已经结束”；主动放弃需要覆盖标题，不能伪装成生命归零。
- `RunSummary` 是稳定的不可变终局快照，适合新增 `RunEndReason`；奖励计算仍由 `MetaRunSession.Settle` 统一负责，失败原因只影响语义和展示。
- `MetaRunSession.Settle` 已通过唯一运行 ID 保证重复调用不重复加钱，因此共享终局方法仍应增加本地 `_runFinalized` 守卫，以同时阻止重复 UI 和重复快照。
- 现有 `DeathFlowSmokeTest` 覆盖生命归零和总结导航；`PauseMenuSmokeTest` 应新增“确认返回后仍在游戏场景、打开主动结束失败弹窗、暂停层隐藏”的真实场景断言。
- `RunSummary` 只有 `WorldDemo` 一个生产构造点，新增失败原因的迁移范围可控；死亡与暂停测试各自验证一个终局原因即可覆盖语义分支。
- 两次外层工具终止后仍残留 PID 54296 与 26972 的 Godot 控制台进程；工具只结束了 `cmd` 包装层，必须先核对完整命令行再定向清理本轮测试子进程。
- `PauseMenuSmokeTest.tscn` 是有效的单节点 C# 测试场景，资源路径和脚本绑定没有异常。
- `PauseMenuSmokeTest._Ready` 原先没有异常捕获；断言失败会结束异步回调却不会调用 `SceneTree.Quit`，从而表现为无输出的永久运行。测试夹具需要统一捕获并以退出码 1 结束。
- `InputActionCatalog` 明确规定覆盖层与调试开关第二槽默认未绑定；旧暂停测试已按地图 M、属性 E、调试 F3 的真实契约修正，不改动生产键位。
- 改造后的 `PauseMenuSmokeTest` 已通过：确认放弃不会直接切场景，会显示 `Abandoned` 失败弹窗并可进入标准总结页。
- 语义审计发现升级、属性和符卡协调器仍暴露 `BlockForDeath`/`CancelForDeath`；调用面仅限共享终局入口及内部转发，可安全统一为 `RunEnd` 语义，避免主动放弃流程误用死亡命名。
- `SpellCardCoordinator.IsDeathBlocked` 没有外部消费者，可与内部字段一并改成 `IsRunEndBlocked`，无需保留兼容别名。
- 最终审计中 `WorldDemo` 为 288 物理行、266 行非空非行注释代码，超过 250 行限制；暂停层和测试分别为 202、144 行有效代码。
- 失败终局方法同时负责快照、坐标/群系解析、输入封锁、奖励结算和 UI 展示，具备明确内聚边界，应提取为独立 `RunFailureCoordinator` 而非压缩格式。
- 提取后 `WorldDemo` 为 246 物理行、226 行有效代码；`RunFailureCoordinator` 为 111 物理行、102 行有效代码，均低于限制且职责边界清晰。
- 所有核心改动文件经 `file` 检查均为 UTF-8 text，未报告 BOM；暂停层已无 `ChangeSceneToFile`，主菜单切换只保留在 `WorldDemo` 的失败页导航入口。
- 重复终局回归通过：主动放弃后的生命归零不会替换第一次的 `RunSummary`；最终有效行数为 WorldDemo 226、RunFailureCoordinator 102、PauseMenuOverlay 202、PauseMenuSmokeTest 151。
# 2026-07-31 本局总结紧凑化

- 用户截图为 2048x1111；总结面板约占画面宽度 84%，高度超过可视区域，底部按钮被裁切。
- 统计标签和值字号、行距和左右留白均过大，不符合此前“游戏 HUD/面板紧凑、不遮挡视线”的约束。
- “本局武学”是明显的不定长值，目前两行大字号文本继续撑高统计网格，应作为独立受控区域处理。
- 本阶段以 640x360 逻辑视口为权威尺寸，不导出发布版。
- `SummaryPanel` 当前固定为 560x340，在逻辑视口中占宽 87.5%、高 94.4%；左右 padding 30、上下 18，按钮最小高度 40。
- 普通统计使用一个 2 列、10 行 GridContainer，垂直间距为 8；不定长的启用内容和武学与普通数值共用网格，任一换行都会推高后续所有内容。
- 目标结构：普通统计拆为左右两个 2 列网格，武学移到全宽独立两行区域；面板收至约 500x300，保留完整按钮与明显的背景留白。
- `project.godot` 明确逻辑视口为 640x360，未发现项目级默认字号覆写；总结场景应显式声明紧凑字体尺寸，不能依赖桌面窗口缩放补救。
- `UiAssetSmokeTest` 只验证像素 UI 图片资产，不覆盖控件尺寸；总结布局边界应加入 `DeathFlowSmokeTest` 或新的专用布局测试。
- 项目内未找到共享 Theme `.tres` 或现成截图保存逻辑；死亡界面应使用场景局部 Theme，视觉验证使用独立测试场景，避免污染其他 UI。
- 首次 `DeathSummaryVisualTest` 已通过面板尺寸、视口留白、武学两行和按钮边界断言；随后因 headless dummy renderer 无视口图像而在截图步骤失败，布局本身未失败。
- 普通渲染截图显示 460x260 面板尺寸、四周留白和按钮位置符合目标，但 LocationValue、ContentValue、BuildValue 三个自动换行 Label 为空白；普通单行值均正常。
- 空白可能来自动态文本设置后一帧内自动换行布局尚未完成，也可能来自启用 clip/max-lines 后控件最小高度为零；先用额外帧验证再修改生产布局。
- 额外等待一帧后空白不变；尺寸诊断为 LocationValue=(147,1)、ContentValue=(147,1)、BuildValue=(366,1)，三者可见行数均为 0，确认是自动换行 Label 的最小高度坍缩。
- 设置稳定高度后普通不定长信息已正常绘制；第二次截图中武学仍只有一行并在“梦想…”处裁切，36px 高度不足以容纳当前字体的两行。
- 统计区下方存在由垂直扩展产生的空白，可将武学行增至 44px 而不扩大 460x260 面板。
- 最终截图确认长武学稳定显示两行，右侧位置/内容完整，按钮与文本没有重叠或越过边框；640x360 与 1280x720 最近邻画面均清晰。
- 总结面板面积由 190400 逻辑平方像素降至 119600，约减少 37.2%，同时保留至少 90px 水平和 50px 垂直视口留白。
- 行数审计：DeathScreenOverlay 117 行、DeathFlowSmokeTest 105 行、DeathSummaryVisualTest 133 行；均低于 250 行限制。
- 修改文件均为 UTF-8 无 BOM；仅含 ASCII 的视觉测试场景保持 ASCII 文本。
- `release` 目录仍是 2026-07-29/30 的三个既有 EXE，本轮未导出；最终检查无残留 Godot console 测试进程。
# 2026-07-31 导出 beta0.2

- 用户明确授权重新导出并覆盖 `release/TouhouWuxiaSurvivor_beta0.2.exe`；其他发布文件不得修改。
- `Windows Release` 预设的目标路径正确，`binary_format/embed_pck=true`，会排除 tests/tools/release 和已替换的旧图集资源。
- 导出图标为 `res://assets/branding/icon.png`；预设的文件版本和产品版本仍是 `0.1.0.0`，应随 beta0.2 修正为 `0.2.0.0`。
- 覆盖前 beta0.2：189,610,952 字节，修改时间 2026-07-30 15:06，SHA-256 `b7cc089dd5a79067f844a8c285f1469ffabea0c1908db747eb361356196fb7a9`。
- 导出进行中目标已写为 2026-07-31 11:31、189,733,976 字节；Godot 子进程仍处于运行状态并占用约 323MB，等待正式退出后再验收。
- PID 17192 创建于 2026-07-30 22:25，命令行无导出参数，是用户原有 Godot 实例，不属于本轮导出且不得终止。
- 外层超时后仍有 PID 65712 的 Godot console 包装进程；只继续核对该 PID 及其子进程。
- PID 65712 无子进程且确认属于本轮导出包装层，已定向结束；未触碰用户原有 Godot PID 17192。
- 新 beta0.2 元数据：Version=0.2.0.0，FileSize=189,733,976，LastModified=2026-07-31 11:31:50 +0800。
- `release` 仍仅有三个 EXE，没有外置 `.pck`；正式版和 beta0.1 的大小、时间戳保持不变。
- 新 beta0.2 从内嵌 PCK 单 EXE 无头启动 120 帧后退出码为 0，Godot 4.7.1 Mono 初始化正常。
- 新 SHA-256：`21bb09dc8ad0fc47ecbb98e5ad72fea6e4551b5a65b8a4b50fd967fbdb1402e0`；没有残留 beta0.2 进程。
# 2026-07-31 内部原作占位素材

- 用户明确授权把本地原作解包素材用于不公开的内部版本，并要求在图鉴声明这些资源后续会替换。
- 实施边界：首批只接入 TH06 图鉴预览；原始素材包保持只读，不做全量复制。
- 所有选中资源进入 `assets/internal_original/`，配套来源/用途/替换状态清单；运行时保留现有文字动态图标回退。
- 现有 `Windows Release` 预设应排除内部原作目录，防止后续误导出；本阶段不导出。
- TH06 提取包包含：CM_DAT 的自机/弹幕/UI 图集，ST_DAT 的分关背景、敌人/Boss 图集、立绘与特效，以及菜单/结算等大量无关资源。
- 首批最小候选为 `CM_DAT/etama3.png`（弹幕）、一个 `ST_DAT/stg*enm.png`（敌人）和一个 `ST_DAT/stg*bg.png`（场景）；不复制 ED、标题、音频、脚本或全量图集。
- `CompendiumPreview` 当前只加载代表地表并以中文文字实时绘制地区、结构、敌人、角色和符卡动画，没有可选图片字段。
- `CompendiumCatalog` 已承担全部条目生成且接近行数上限；内部资源映射应放在独立目录/渲染器中，按 SourceId/Category 查询，缺失时返回现有文字预览。
- `CompendiumPanel.tscn` 顶部只有标题、来源筛选和数量；内部素材声明可放在标题栏下方的单行低对比警示，不占用详情布局。
- `etama3.png`、`player00.png`、`stg1enm.png`、`stg1bg.png` 均为 256x256；弹幕图集内容完整，适合预览动画。
- 原作图集使用 RGB 颜色图加独立 `*_a.png` alpha 蒙版；内部拷贝必须合并为 RGBA，否则会把底色一起绘制。
- `CM_DAT/etama3_a.png` 是白色前景、黑色背景的灰度遮罩，可直接以亮度作为 Alpha；对应 `etama3.png` 是弹幕图集。
- `ST_DAT/stg1enm.png` 是 256x256 的一面道中妖精/敌人图集，适合作为敌人图鉴动态预览的首批内部占位图。
- 首次追加本轮发现时因上下文措辞不一致导致补丁未命中；读取文件末尾后改用稳定锚点重试，不涉及项目源码改动。
- `stg1enm_a.png` 与敌人图集一一对应，包含 32x32 左右的多帧妖精动作遮罩；可按行列裁切并随时间切帧。
- `stg1bg.png` 不是完整场景截图，而是一面森林关卡的背景贴图片集；必须配合 Alpha 并由预览渲染器拼接/平铺，不能直接整张当插画展示。
- `CompendiumPanel` 只负责筛选、列表和详情绑定；声明文本可以完全由场景节点承载，无需给控制器增加状态或业务分支。
- 图鉴来源使用稳定 `ContentPackDefinition.Id` 进行筛选；内部素材映射也应以该 ID 和分类为键，避免依赖中文作品名。
- 图鉴场景的主面板占 640x360 逻辑视口的 95% 左右，现有纵向间距仅 5；声明必须是约 9px 字号的单行状态文本，不能新增说明卡片或多行段落。
- 详情预览节点已经强制 `texture_filter = 1`（Nearest），内部图集裁切可沿用该节点，无需修改全局纹理策略。
- `CompendiumSmokeTest` 已固定来源选项索引 7 为 TH06，并验证敌人/角色/符卡切换、动画时间推进和紧凑布局；内部声明与素材回退应扩展该测试，而非新建重复的全场景测试。
- Windows 版 `rg.exe` 位于 `C:/Users/untuitivist/AppData/Local/OpenAI/Codex/bin/3e42d49ad3e35a50/rg.exe`，后续仓库搜索从 `cmd.exe` 直接调用，避免 WSL 权限问题。
- TH06 的稳定来源 ID 是 `th06_eosd`；内部预览映射必须使用该值。
- `CompendiumPreview` 当前 258 物理行、196 有效行，继续堆入图集裁切会降低维护余量；应提取一个独立的内部素材预览渲染类，由现有预览在分类分支前尝试调用，失败则继续文字动画。
- `Windows Release` 当前导出所有资源并只排除测试、工具、release 和若干旧图集；必须把 `assets/internal_original/*` 加入同一排除列表。
- 环境已有 Pillow 11.3.0，可用于一次性、可复核地把颜色图与灰度遮罩合并为 RGBA；无需安装或提交额外图像工具。
- `stg1bg_a.png` 的非空区域与森林/天空贴图一致，黑色为透明、白色/灰色为不同不透明度；亮度转 Alpha 的合并规则同样适用。
- 继续遵守既有“角色以中文名作图标”的决定：首批内部图只覆盖 TH06 地区/结构、敌人和符卡，角色分页继续使用现有文字动画。
- `etama3.png` 的前几行包含 16x16 彩色弹丸帧，后半包含更大的光球/符形；符卡预览可从前部按颜色列切换并沿轨迹绘制，保持动态图标而非静态整图。
- 项目 `assets` 当前按 audio/branding/characters/combat/ui/world 分类；新增 `assets/internal_original/th06/` 能独立标识许可边界，同时不污染正式类别目录。
- 已从颜色图与对应遮罩生成 3 张 256x256 RGBA 内部资源；敌人图集帧对齐正常，保留了蓝/绿/粉妖精与后排行走角色动作。
- 背景合成图在通用图片查看器中仍显示原始黄绿色 RGB 底色；需读取 Alpha 通道像素确认其实际透明度后再接入，不能仅凭查看器合成结果判断。
- Alpha 通道核验通过：3 张输出的范围均覆盖 0 至约 255；背景 `(200,200)` 和敌人 `(200,200)` 的 Alpha 都为 0，查看器所见底色只是透明像素保留的 RGB 数据。
- 弹幕合成图的彩色 16x16 帧与光球区域保持完整，透明背景有效，可直接用于符卡轨迹动画。
- 新增 C# 渲染器首次编译为 0 警告/0 错误，Godot 也成功导入 3 张内部 PNG。
- Godot 不允许测试直接调用 `_Draw()`；动态图状态测试必须让场景树产生真实绘制通知，等待一帧后再读取公开状态。
- 修改 C# 测试后必须重新 `dotnet build`；未编译直接启动 Godot 会继续运行旧程序集并重复旧堆栈。重新编译后图鉴测试通过且日志无 ERROR。
- 项目已有 `DeathSummaryVisualTest` 的 `Viewport.GetTexture().GetImage().SavePng` 截图模式，可复用其普通渲染器方法为图鉴生成 640x360 验收图。
- 现有视觉测试在 headless 中只做布局断言，在普通渲染器中保存 `user://` PNG 并额外生成二倍最近邻图；图鉴视觉测试应沿用同一机制且不把截图写入仓库。
- 图鉴需要至少捕获 TH06 敌人和符卡两个状态，才能同时验证动作帧裁切、弹幕裁切、声明文本和固定详情布局。
- 普通 OpenGL 截图确认：内部声明完整单行显示，来源筛选、五分页、列表、预览和属性区均未重叠或越界；敌人透明动作帧与中文名叠加清晰。
- 符卡页已显示六枚彩色动态图集弹幕且详情仍完整，但当前截图中的弹丸轮廓偏竖条，需要核对 16x16 源格的实际 Alpha 包围盒，排除裁切行/列偏移后再验收。
- 放大前 64 像素确认：`y=16` 是纵向激光条，圆形弹丸从 `y=32` 开始；当前效果确实是源行选择错误，不是透明通道或最近邻缩放问题。
- 将符卡源行修正为 `y=32` 后，普通渲染截图显示六枚完整圆形彩弹，轨迹、中文名和详情文字互不遮挡。
- 集成测试目录已有内容包、官方内容覆盖、纹理策略和 UI 资产测试；本轮应串行复跑这些场景，并新增内部资源边界测试锁定 RGBA、清单和导出排除项。
- 六个原始颜色图/遮罩的最终 SHA-256 与处理前记录完全一致，确认用户提供的解包目录未被修改。
- 行数审计通过：`CompendiumPreview` 225 有效行，内部渲染器 102，图鉴测试 154，视觉测试 77，边界测试 75；均不超过注释外 250 行。
- `src`、`tests`、内部清单、计划和导出预设的 UTF-8 BOM 扫描结果为空。
- 内容包、官方内容覆盖、纹理策略、UI 资产、图鉴逻辑和内部资源边界测试均串行通过，日志无 ERROR。
- `release` 目录仍只有原有正式版、beta0.1 和 beta0.2；beta0.2 时间保持 2026-07-31 11:31，本轮没有导出或改写任何发布文件。
- 最终检查没有残留 `Godot_v4.7.1-stable_mono_win64_console.exe` 测试进程。

# 2026-07-31 本体与红魔乡完整内部替换

- 用户把范围从“TH06 首批子集”扩大为“幻想乡本体与红魔乡全部替换”。
- 新验收口径是逐条目覆盖本体和 TH06 的地区、结构、敌人、角色与符卡，不能继续只按分类复用同一图集。
- 内部使用、公开导出排除、角色中文名识别、资源缺失文字回退和不主动导出等上一阶段边界继续有效。
- 需要先厘清本体条目与原作素材之间的来源归属；幻想乡本体并不是一部单独正作，不能伪称所有映射都来自 TH06。
- `CompendiumCatalog` 的本体条目来自本体清单、无内容包敌人目录和本体角色清单；TH06 条目来自三组官方世界内容、TH06 敌人目录、角色清单和独立符卡工厂。
- 当前预览键只有 `SourceId + Category + PreviewVariant`，无法区分同分类内全部具体条目；完整替换需要引入以稳定条目名为键的数据定义，而不是继续扩大分类分支。
- 本体没有符卡条目；五分类完整覆盖应按实际目录处理，本体覆盖地区/结构/敌人/角色，TH06 额外覆盖符卡。
- `content/packs` 只包含 TH01 至 TH20 的正作清单，本体清单不在该目录；需从 `ContentPackCatalog.Base` 的加载实现定位。
- 本体清单路径是 `content/base/pack.json`。
- TH06 清单包含 3 地区、3 结构、3 敌人、7 角色；再加符卡工厂的 2 张符卡，共 18 个必须逐项映射的红魔乡条目。
- 本体图鉴实际包含 5 地区、6 结构、9 敌人、1 角色，共 21 个条目；本体清单中的道具和系统目前不属于图鉴分页，不在本次“图鉴全部替换”范围。
- 本次完整覆盖总量为 39 条：本体 21 + TH06 18。
- 本体九类敌人的战斗原型已稳定区分 Fairy/Kedama/Insect/Beast/ForestSpirit/MountainSpirit/VillageOutlaw/WanderingYoukai/GreatYoukai，可据此映射不同原作帧，而不是仅按中文名绘制同一妖精。
- 用户提供的素材包覆盖 TH06 至 TH19 体验版及多部小数点作，可为“幻想乡本体”选择跨作品的合适场景；清单必须记录实际来源，不能统一标成红魔乡。
- TH06 自身有 stage1-stage7 的背景/敌人图集、角色立绘 `face00/01/03/05/06/08/09/10/12` 和两个自机图集，足以覆盖红魔乡全部 18 条而无需借用其他作品。
- TH06 立绘编号与角色可稳定对应：00 灵梦、01 魔理沙、03 露米娅、05 琪露诺、06 美铃、08 帕秋莉、09 咲夜、10 蕾米莉亚、12 芙兰朵露。
- TH10 提供完整的 stg1-stg7 多层背景贴图，适合本体原野、妖怪之山、梯田和神社等跨作品借用场景；TH08 提供 stg1-stg7 夜景、森林和人里相关背景。
- TH08/TH10 的素材路径不再采用 TH06 的独立 `*_a.png` 命名模式，需逐图检查模式与 Alpha，不能盲目执行颜色图+遮罩合并。
- TH10 `stg1bg`/`stg1bg2` 只是草地与红叶的可平铺材质，不是可直接展示的完整场景；可作为本体原野底层，但需要叠加结构或活动层。
- TH10 `stg4bg` 是瀑布/水流材质，适合妖怪之山环境层；`stg6bg` 是完整的月夜水面场景，可用于夜行/结界氛围，但不适合白昼人里或神社。
- 连续查看候选图时超过了技能要求的两次查看后立即落盘节奏；现已补记，后续严格每两张图更新发现。
- TH08/TH10 背景联系表表明大多数文件是可组合材质或透明层，只有少数是完整场景；完整场景包括 TH08 `stg6bg3/5`、`stg7bg2` 和 TH10 `stg6bg`。
- 本体的神社、人里等结构若只用整数作的飞行背景贴图，辨识度仍不足；应检查格斗作的小数点作品是否含完整场景背景，再决定最终素材来源。
- 区域预览可以使用材质层并保留日常中文活动；结构预览则需要明显建筑/场景轮廓，二者不能继续共用同一背景裁切规则。
- TH10.5 与 TH12.3 的解包均有独立 `background`、`battle`、`scene` 和 `stand` 目录，适合寻找本体结构全景与角色/敌人视觉。
- 两部格斗作的 `background/bgXX` 下每个场景由约 30 张编号 PNG 分层/分时段组成，直接列出得到数百文件且无法判定组合关系；不能把任意单层误当完整背景。
- 下一步只抽取每个 `bgXX` 的代表帧制作联系表，若代表帧仍是透明局部层，则退回整数作场景贴图加项目结构轮廓的方案。
- TH10.5/TH12.3 每个 `bgXX` 的首层联系表仍以树枝、屋檐、云层等透明局部为主，缺少可直接使用的完整合成场景；本次不引入格斗作场景重建器。
- 本体最终采用整数作的原作背景材质/完整场景作为底层，并叠加现有中文日常或结构轮廓；每个条目仍通过独立定义选择不同资源、裁切、速度和叠加类型。
- 该方案能做到“所有条目都有原作视觉”，同时不虚构格斗作的分层合成参数；后续若要复原格斗场景，应单独解析其背景定义文件。
- TH08 有 19 张、TH10 有 14 张敌人图集候选，且后期图集包含 Boss/角色帧；本体九敌人可以从多个原型中选择不同图集与帧区。
- 后续不按文件名臆测帧网格，先制作带透明背景的联系表查看实际尺寸和内容。
- 敌人联系表确认 TH08/TH10 图集本身已经带透明背景，并覆盖人形妖怪、兔、鸟、幽灵、使魔、机械与大型妖怪等多类轮廓。
- 本体九敌人可采用九个不同原作图集/行：野妖精、毛玉、妖虫、妖兽、森林精怪、山精、流窜妖怪、夜行妖怪、大妖怪分别使用小型人形、球/使魔、鸟/蝶、兔/兽、大型灵体、山神人形、持械人形、夜间人形和大型 Boss 轮廓。
- 为避免运行时塞入大量异构网格参数，资源流水线应把选中动作帧预裁成统一的 4 帧横向动画条；图鉴映射只保存资源路径、帧宽和动画速度。
- 选中的 TH08/TH10 敌人图集统一为 256x256 RGBA，可直接读取 Alpha 并预裁，不需要额外遮罩合并。
- 统一输出目标暂定为每帧 48x48、四帧横排的 RGBA 动画条；较小 32x32 源帧居中，较大 Boss 轮廓等比缩入 48x48，避免预览代码承担异构图集知识。
- TH06 八个角色立绘颜色/Alpha 合并正常，每张 256x256 图包含左右两个表情；输出角色预览可裁左半 128x256并等比缩入统一画布。
- TH06 场景素材用途已明确：`stg2bg` 水面用于雾之湖/湖岛，`stg4bg` 书架用于巴瓦鲁图书馆/大图书馆，`stg5bg` 红馆纹理与徽饰用于红魔馆领地/红魔馆，`stg6bg` 红月可作为馆内深层变化层。
- `stg1/3/5/7bg` 是透明局部贴图片集而非完整场景，最终输出应先在深色底上重组为 128x80 场景图，不能直接展示整张图集。
- 完整资源规范确定为：场景 128x80 RGBA、敌人四帧横排且每帧 48x48、角色 80x80 RGBA、符卡保留 256x256 弹幕图集。
- 39 条映射进入内部 `preview_mappings.json`；本体条目明确标注“跨原作视觉代用”，TH06 条目仅引用红魔乡源文件。
- 首次 C# 构建在逐像素 Alpha 合并阶段超时，尚未生成任何新的 `base` 或规范化 TH06 子目录文件；旧的三张 TH06 验证资源保持原状。
- 超时后残留一个约 5 MB 的 Godot console 进程 PID 56432，必须先核对命令行属于本轮构建器再定向结束，不能影响用户其他 Godot 实例。
- PID 56432 的完整命令行确认属于本轮 `InternalPreviewAssetBuilder.tscn`，已连同子进程定向结束。
- Alpha 合并改用 RGBA8/L8 字节缓冲：每张图只跨 Godot 绑定读取/重建一次，内部字节循环不再调用 `GetPixel/SetPixel`。
- 第二次构建仍未产生新的 base/scenes/actors/portraits 输出，确认执行卡在后续透明边界扫描之前或期间，没有留下半套规范化资源。
- 第二次超时残留 Godot console PID 53264；需重复完整命令行核对后定向清理。
- PID 53264 也确认属于本轮构建器并已连同子进程定向结束。
- `CropOpaque` 已改为单次读取 RGBA8 数据并直接扫描每四字节 Alpha；构建器内不再存在逐像素 Godot API 调用。
- 第三次运行约 30 秒后 `assets/internal_original/base` 仍不存在，说明阻塞发生在第一个场景保存之前，不能再归因于后续透明边裁切。
- 手动终止外层会话后仍残留约 5 MB console PID 41152；需核对并清理，再用阶段级打印定位。
- PID 41152 及其子进程确认属于第三次构建器运行并已定向结束；下一次执行会打印启动、清单和各构建类别完成点。
- 带诊断的运行已启动实际 Godot 子进程 PID 46796（约 128 MB），console 包装层为 PID 52780；用户原有编辑器 PID 17192 保持独立且不得触碰。
- 控制台在子进程结束前没有转发阶段打印，且 base 输出仍未出现；下一步需要把阶段标记写到 `user://` 诊断文件或缩小为单场景构建，不能依赖 stdout 判断。
- 19 秒短时诊断运行没有创建 `user://internal-asset-builder-progress.txt`，说明阻塞发生在场景脚本 `_Ready` 之前，与构建算法无关。
- 新增 C# 工具场景尚未经过 Godot 编辑器文件扫描；需要检查 `tools` 是否有忽略规则，并执行一次编辑器扫描/退出让 Godot 注册资源。
- 仓库内没有 `.gdignore`，`tools/internal_assets` 四个新文件均存在且可见；工具目录不是被忽略导致的阻塞。
- 短时运行残留的包装层 PID 60832/子进程 18168 已确认并定向结束，用户编辑器仍为 PID 17192。
## 2026-07-31 - Internal asset builder launch diagnosis

- `godot.log` contains no `ERROR`, `Failed`, `Cannot`, or `InternalPreviewAssetBuilder` matches after the silent builder launch.
- The missing `user://internal-asset-builder-progress.txt` therefore points to the requested tool scene not reaching `_Ready`, rather than a reported C# image-processing failure.
- Next diagnostic must distinguish scene argument handling from script instantiation with a minimal, observable scene invocation.
- `InternalPreviewAssetBuilder.tscn` is structurally valid and attaches `res://tools/internal_assets/InternalPreviewAssetBuilder.cs` to its root `Node`.
- The project default remains `res://src/ui/menu/MainMenu.tscn`; a silently ignored positional scene would therefore explain the indefinite headless run.
- Godot 4.7 documents `--scene <path>` for selecting a scene; a bare `.tscn` argument is not the supported selector in this invocation.
- With `--scene res://tools/internal_assets/InternalPreviewAssetBuilder.tscn`, Godot immediately reports that the associated C# class cannot be found. Scene selection is now proven; remaining work is C# assembly registration/build.
- `TouhouWuxiaSurvivor.csproj` contains `<Compile Remove="tools/**/*.cs" />`, so both internal builder classes were intentionally omitted even though `dotnet build` passed.
- The narrow fix is to explicitly include only the internal asset builder source files after the broad tool exclusion, preserving the rest of the runtime/tool boundary.
- Once included, the builder exposes one compile error: `Environment.NewLine` is ambiguous between `Godot.Environment` and `System.Environment`. It must be fully qualified.
- After qualification, the project builds cleanly and the builder processes all 48 declared source files in under 3 seconds.
- Three scene overlays call `Image.BlendRect` with mismatched formats. Godot logs an engine error but does not throw, so the current builder incorrectly continues and prints success; all blend inputs must be normalized to RGBA8 and validation must reject malformed outputs.
- `LoadImage` returns unmasked source images in their original format but `MergeAlpha` returns RGBA8. Centralizing `image.Convert(Image.Format.Rgba8)` inside `LoadImage` gives every build path one pixel-format contract.
- After central RGBA8 normalization, `dotnet build` passes with zero warnings/errors and the internal builder completes all stages from 48 source files without any Godot error.
- Generated coverage is complete for the intended visual families: base has 11 scene images, 9 actor strips, and 1 Reimu portrait; TH06 has 6 scene images, 3 actor strips, 7 portraits, and 1 shared bullet atlas.
- The two TH06 spell-card entries should reuse the bullet atlas with distinct animation/crop parameters; asset count intentionally differs from compendium-entry count.
- Existing `InternalOriginalPreviewRenderer` is category-wide and TH06-only: all scenes share one stage-1 atlas, enemies share one atlas, spell cards share one atlas, and characters explicitly fall back to text.
- Runtime replacement requires per-entry definitions keyed by source/category/name and render kinds for normalized scene, 4x48 actor strip, 80x80 portrait, and bullet atlas.
- `CompendiumEntry` already exposes immutable `SourceId`, `Category`, and `Name`; this triple is sufficient as the preview mapping key without coupling content definitions to UI assets.
- Public/internal separation remains graceful: when the mapping file or texture is excluded, the renderer should return `false` and `CompendiumPreview` will draw its generated text fallback.
- Exact manifest locations are `content/base/pack.json` and `content/packs/th06_eosd/pack.json`; TH06 world names match `OfficialWorldContentCatalog`.
- Exact counts from content manifests: base = 5 biomes + 6 structures + 9 enemies + 1 character = 21; TH06 world/actors = 3 + 3 + 3 + 7 = 16. Together with 2 TH06 spell cards, runtime mapping target is 39.
- Exact spell-card keys are `灵符「梦想封印」` and `梦符「封魔阵」`, both sourced from TH06 by the compendium factory.
- `CompendiumPreview` currently overlays original captions only for Enemy and SpellCard; Character must gain the same internal-active branch while retaining `DrawCharacterScene` fallback.
- The manifest-driven renderer now compiles with 39 exact mappings, lazy optional textures, and distinct Scene/ActorStrip/Portrait/BulletAtlas animation paths.
- Existing boundary test validates only three legacy 256x256 atlases and assumes a transparent corner; new outputs need type-aware contracts: scenes 128x80, actor strips 192x48, portraits 80x80, bullet atlas 256x256, all RGBA8.
- Existing smoke test explicitly rejects internal character art; it must now require TH06 character activation and exact 39-entry coverage.
- `MANIFEST.md` is stale and still describes only three TH06 atlases. It must state that base visuals are cross-work substitutes and do not establish canon/source attribution, while TH06 outputs use TH06 sources.
- Visual coverage currently captures only TH06 Enemy and SpellCard; representative screenshots should cover all five compendium categories and all four normalized render kinds.
- Updated tests compile cleanly, and Godot imports exactly 38 unique mapped textures without errors, matching the intended shared bullet-atlas design.
- `InternalOriginalAssetBoundaryTest` passes: 39 exact entries, 38 unique normalized outputs, 48 source hashes, governance text, and public export exclusion.
- `CompendiumSmokeTest` passes with base biome, TH06 enemy, TH06 character, and TH06 spell-card internal previews active.
- OpenGL visual test passes and produces all five 640x360 captures.
- Base biome and TH06 structure images are nonblank and framed correctly, but the TH06 structure capture shows a clipped legacy daily-activity text label at the preview's right edge. Internal scenes need bounded ambient decoration rather than the full generated text-entity overlay.
- `DrawDailyScene` intentionally moves labels from `area.Position.X - 24` to `area.End.X + 8` and overlays a generated structure roof, explaining both clipping and redundant composition over complete internal scene art.
- Base enemy capture is clear: the normalized actor strip animates and its Chinese caption remains legible without layout overlap.
- TH06 character capture shows a nonblank portrait with a readable Chinese caption and no face obstruction or frame overflow.
- TH06 spell capture shows distinct circular bullets, readable Chinese caption, and a complete no-scroll balance panel without overlap.
- After the bounded scene-life fix, the project rebuilds with zero warnings/errors and the five-category OpenGL visual test passes again.
- Reinspection confirms the clipped structure label is gone; both scene types now show bounded pixel life and no redundant generated roof.
- User visual review identified a remaining correctness bug: Scarlet sisters are only half-visible. The builder's universal `source.Width / 2` portrait crop is invalid for at least Remilia and Flandre and must become per-asset crop data.
- Direct inspection of generated 80x80 portraits confirms hard vertical cuts: Remilia loses her right body half; Flandre loses right body/wing content exactly at the half-width boundary.
- Original `face10a.png` and `face12a.png` are each a single full 256x256 portrait, not two side-by-side expressions. Their content legitimately spans the full width.
- The optimal correction may be to remove half-width cropping from all eight portraits if representative Reimu/Sakuya sources share this layout, then rely on alpha-bound cropping.
- Representative inspection disproves a global change: `face00a.png` (Reimu) and `face09a.png` (Sakuya) contain two side-by-side expressions, so their left-half crop is correct.
- Portrait build definitions need an optional explicit crop rectangle; default can remain left half for dual-expression files, while single-canvas portraits declare `[0,0,256,256]`.
- `face03a.png` (Rumia) and `face05a.png` (Cirno) are confirmed dual-expression layouts; current default left-half crop is correct for both.
- `face06a.png` (Meiling) and `face08a.png` (Patchouli) are also dual-expression layouts.
- Final crop audit: Reimu/Rumia/Cirno/Meiling/Patchouli/Sakuya use default left half; Remilia/Flandre require full 256x256 crops.
- Builder now supports an optional per-portrait `crop`; only Remilia/Flandre declare `[0,0,256,256]`, preserving default dual-expression behavior for the other six.
- Regeneration completes cleanly from all 48 source files after the crop fix; project build remains at zero warnings/errors.
- Direct inspection of regenerated outputs confirms both sisters are complete: Remilia retains both wings/body; Flandre retains body, staff, and crystal wing content.
- Visual test now selects both sisters by exact Chinese name rather than list index; build passes and Godot reimports only the two changed portrait textures.
- OpenGL UI captures confirm both sisters are now fully visible in the actual compendium preview.
- The same captures expose a pre-existing long-name layout defect: both Scarlet sister detail titles wrap the final `特` onto a separate line. The identity name label needs single-line dynamic sizing.
- The actual detail title control is `Panel/Padding/Layout/Browser/Details/Layout/Identity/Heading/EntryTitle`, bound to `_entryTitle` in `CompendiumPanel`; the fix can stay isolated from list and fact typography.
- `EntryTitle` is fixed at 15px with word-smart autowrap (`autowrap_mode = 2`) inside the compact identity column. Its available design width is narrower than the full Scarlet names.
- Correct behavior is single-line measured fitting from 15px down to 10px using the active theme font, leaving short names unchanged.
- Measured single-line sizing compiles cleanly and both exact-name visual assertions pass under the OpenGL renderer.
- Final manual screenshot inspection confirms both sister portraits are complete and both full Chinese names remain visible on one line without ellipsis or clipping.
- Final post-fix runs of `InternalOriginalAssetBoundaryTest` and `CompendiumSmokeTest` both pass.
- The repository has no existing architecture test for the 250-effective-line/comment rule; only an unrelated save-file BOM assertion exists. Direct changed-file audits are required.
- Direct effective-line audit results: all touched C# files are <=250 except `InternalPreviewAssetBuilder.cs` at 260; `CompendiumPreview.cs` is 238 and the new renderer is 114.
- The builder should be split by responsibility, extracting reusable image transformations into one static class instead of shaving syntax lines.
- Six stateless methods form a coherent extraction boundary: alpha merge, sprite placement, opaque crop, contain fit, cover fit, and transparent RGBA8 canvas creation. Path resolution/save remain builder concerns.
- Refactor compiles with zero warnings/errors. Effective lines are now 179 for the builder and 87 for `InternalImageTransformer`, both below 250.
- Post-refactor regeneration still completes from 48 sources without errors, and the full asset boundary test passes, demonstrating normalized-output equivalence.
- Encoding audit reports every touched text file as ASCII or UTF-8, with no `with BOM` marker.
- First declaration scan covered explicit test/renderer files and found documented classes/functions; Windows `rg` rejected unexpanded `*.cs` path globs, so remaining directories need a `-g "*.cs"` scan.
- Corrected declaration scan lists all touched/new classes and methods with their nearby documented declarations; no undocumented new class/function was found.
- `export_presets.cfg` continues to exclude both `tools/*` and `assets/internal_original/*` from Windows Release.
- Final compendium smoke test passes after all refactors.
- Process audit shows the known user editor PID 17192 plus unexpected Godot PIDs 72664 (console wrapper) and 42000 (engine). Their command lines/parentage must be checked before targeted cleanup.
- WMI command lines proved PIDs 72664/42000 were the early bare-scene builder invocation from this task. Targeted parent-tree termination succeeded; only user editor PID 17192 remains.
# 2026-07-31 - Base enemy visual correction

- Godot provides `Image.GetUsedRect()` for automatic nontransparent Alpha bounding boxes.
- Auto-trim does not choose semantic sprite-sheet frames; current base enemy failures originate from inappropriate source atlases and incorrect declared grids.
- Scarlet portrait layout handling remains a separate manifest-region concern and should not be replaced by whole-image auto-trim alone.
# Horizontal Build and World Audit - 2026-08-12

- `RunUpgradeCatalog.CreateOffer` currently filters by pack/prerequisite/rank, uniformly shuffles, and takes three. It has no affinity, exclusion, specialization, offer history, or pivot guarantee.
- `LevelUpOverlay.Present` queries rank by `RunUpgradeKind`; generic spell-card definitions require stable IDs, so the UI must query `definition.Id`.
- Official regions are independent circles selected inside 192-Tile cells. Neighbor cells are not evaluated, so circles can be clipped by straight cell boundaries and the three regions of one work have no spatial relationship.
- All structures share one 96-Tile grid and 62% chance. Official structures reuse three ground stamps; runtime markers reduce all structures to sixteen silhouettes and three sampled colors.
- Chunk generation stores only `TileId`; the biome renderer recomputes biome selection for every tile. The map also stores only `TileId`, so DLC biomes that share surfaces become visually indistinguishable.
- Chunk loading currently counts as exploration. The initial 5x5 active window therefore reveals a 160x160-Tile square before the player travels through it.
- Target architecture: shared horizontal affinity graph; deterministic macro-region plan; semantic chunks; per-definition structure sets and stable instances; player-radius discovery; cached semantic map tiles.
- Supplied diagnostics contain one D3D12 and two OpenGL sessions. Detailed comparison is being performed independently while implementation proceeds.

# Balance and alpha-0.0.2 Freeze - 2026-08-13

- The base game must fill the same 4 offensive + 2 support spell slots without any DLC; otherwise DLC becomes vertical progression rather than horizontal choice.
- Base now owns six permanent spell cards: four offensive and two support. Each of the twenty optional works contributes exactly two same-budget alternatives, for 46 unique cards total.
- Formal version identity is stage-first: `alpha-0.0.2`; Windows numeric metadata is `0.0.2.0`.
- The canonical diagnostics and spell-balance entry names are stage-independent. Older stage-specific filenames remain compatibility aliases because deleting them requires separate user authorization.
- Root files `-e` and `stdout` remain pending deletion approval and are explicitly excluded from both export presets.
- Godot's Windows export uses the `ExportRelease` configuration, so the project file must exclude tests under both `Release` and `ExportRelease`; the final published assembly is 707,072 bytes and contains no test, internal builder, or legacy sample-resource identifiers.
- The accepted formal artifact is `TouhouWuxiaSurvivor_alpha-0.0.2.exe`, 196,404,288 bytes, SHA-256 `d8339b4a54c3f3cfd0cc13ff6200e8a183d00336f5aa07353d14f1ff818c2acc`, Windows version `0.0.2.0`, with an embedded PCK and no sidecar package.
- A direct 180-frame headless execution of the exported EXE returned exit code 0 without log errors, leaks, or orphan warnings.

---
