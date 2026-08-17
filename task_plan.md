# Task Plan: Documentation Consolidation and Architecture Alignment

## Current Goal - 2026-08-17

Audit every document under `docs/` against the current code, product intent, versioning, content-pack
lifecycle, ECS/OOP boundary, balance rules, and release workflow. Update missing or stale material,
remove duplication through consolidation, and identify exact deletion candidates before requesting
the required deletion approval.

## Current Phase

Non-destructive consolidation complete; awaiting approval for the exact obsolete-file deletion list.

## Acceptance Criteria

- Every retained document has one explicit purpose and an owner topic; no two documents claim to be
  competing sources of truth.
- Version names, five-minute pacing, dynamic spawn pressure, fixed enemy species stats, build rules,
  content-pack lifecycle, and Base/TH06 scope match current code and `.NOTE.md`.
- ECS/OOP guidance identifies the hybrid boundary instead of requiring one paradigm everywhere.
- README and `content/README.md` link the authoritative documents and do not call migration inventory
  complete content.
- Links and referenced local paths resolve; UTF-8 without BOM, SourcePolicy, build, and relevant
  documentation tests pass.
- Any deletion is preceded by a concrete list, rationale, replacement location, and user approval.
- All accepted changes are committed without staging unrelated workspace edits.

## Active Phases

- [x] Inventory `docs/`, inbound links, duplication, stale versions, and code claims.
- [x] Decide the retained document map and list any deletion candidates for approval.
- [x] Rewrite architecture, gameplay balance, enemy balance, and diagnostics documentation.
- [x] Align README, content README, changelog references, and project intent notes.
- [x] Validate links, terms, code contracts, encoding, build, and documentation-facing tests.
- [x] Commit the documentation consolidation while preserving unrelated changes.

## Decisions

| Decision | Rationale |
|----------|-----------|
| Audit before deleting | The user permits cleanup, while repository policy requires exact approval before file deletion. |
| Prefer one authority per topic | Duplicate summaries become stale independently and caused the current completion-status conflict. |
| Verify claims against code | Documentation must describe the current executable and clearly label target architecture separately. |
| Delete two redirect-only files after approval | `ecs_architecture.md` is fully owned by the plugin guide; `beta_debug_diagnostics.md` is fully owned by the canonical diagnostics guide. |

---

# Historical Plan: Adaptive Mowing Pacing and Boss Access

## Current Goal - 2026-08-16

Make the base-only five-minute combat curve acknowledge actual clearing power. Ordinary enemies
must become mowable before the final encounter so nearest-target fire naturally reaches the Boss,
without adding Boss-only targeting or projectile rules. Spell-card projectiles must use the mapped
original bullet atlases, and every automatic barrage formation must converge on a useful predicted
target. Finite phases advance from sustained visible dominance with bounded timing guardrails.

## Current Phase

Phase 4 - frozen verification and repository cleanup.

## Acceptance Criteria

- Boss combat keeps nearest-target collision semantics; no Boss-only projectile, pass-through, or
  reserved fire share may replace the required late-run mowing power.
- At the five-minute checkpoint every legal base build has no more than sixteen ordinary enemies,
  defeats at least the formal spawn supply, and therefore cannot accumulate a permanent meat wall.
- Phase advancement consumes a sustained dominance signal (kill throughput, spawn supply, and
  crowd trend), grants a minimum mowing window, and retains a maximum timeout so strong builds are
  acknowledged without weak builds stalling forever.
- Spell cards select distinct atlas regions through data-driven visual variants; no gameplay
  system hard-codes one spell-card texture identity as the universal projectile.
- Spiral/rotating upgrades either acquire targets or create an intercepting formation; decorative
  projectiles that routinely expire without threatening an enemy are rejected by tests.
- Build, source policy, focused combat/pacing tests, visual acceptance, and full regression pass;
  every repository change is committed once the frozen suite is green.

## Active Phases

- [x] Audit targeting, projectile visuals, enemy supply, finite durability, and test seams.
- [x] Preserve nearest-target combat and implement useful target-converging formation trajectories.
- [x] Implement visible data-driven spell-card atlas regions for all installed spell cards.
- [x] Add sustained dynamic phases and a low-supply pre-Boss mowing window.
- [x] Separate finite ordinary durability from the unchanged Boss health curve.
- [x] Add focused tests, update intent notes, and inspect all Windows visual scenes.
- [x] Run frozen regression, source/encoding audits, and commit the accepted gameplay scope.
- [x] Remove the two rejected untracked Boss-exception files after explicit approval; no Boss-only
  targeting or projectile type remains in the workspace or runtime dependency graph.

## Decisions

| Decision | Rationale |
|----------|-----------|
| Boss access comes from mowing power | Nearest-target combat stays intact; lower add supply and higher effective output naturally remove the meat wall. |
| Bullet identity is data, not effect-class branching | Forty-six spell cards must reuse the atlas without forty-six rendering implementations. |
| Five-minute balance is the primary contract | Long endless convergence cannot excuse a threefold power gap at the normal ending. |
| Dynamic pacing uses guarded dominance gates | Pure rubber-banding erases growth; minimum/maximum phase bounds preserve power fantasy and run duration. |

---

# Historical Plan: Combat Balance and Skill Design

## Current Goal - 2026-08-12

Turn the existing playable systems into one measurable horizontal balance model. Replace opaque hash/modulo stat variation with curated combat roles, make core martial arts change combat behavior instead of only adding flat numbers, calibrate the endless enemy/experience/player curves against explicit time targets, and keep all content packs power-neutral.

## Current Phase

Phase 8 - final regression, formal export, and exported-build smoke verification.

## Acceptance Criteria

- Character, ordinary-enemy, Boss, martial-art, spell-card, experience, and endless curves share documented units and target windows.
- Character differences come from named roles with equal total budgets, never from ID hashes or work numbers.
- Core build choices produce visible mechanical differences in projectile behavior, survival, targeting, or economy; repeated ranks remain useful without becoming mandatory.
- Every DLC remains horizontal: enabling a pack adds alternatives but does not raise expected offer quality or raw power.
- The opening, transition, and late-game barrage phases have measurable damage, density, survival, and level-rate targets.
- Automated balance tests reject outliers, non-monotonic curves, invalid combinations, and content whose budget exceeds its peers.
- The E build view and compendium explain resolved effects using the same definitions consumed by combat.

## Active Phases

- [x] Audit current formulas, dead modifiers, duplicate scaling, and hard-coded content variance.
- [x] Define shared balance constants, role budgets, skill tags, and target time windows.
- [x] Replace opaque character/enemy variation with curated horizontal profiles.
- [x] Implement mechanically distinct core skills and their ECS/runtime effects.
- [x] Rebalance all 46 spell cards against shared effect and trigger budgets without erasing identity.
- [x] Calibrate level, reward, spawn, enemy, Boss, and barrage curves together.
- [x] Add deterministic simulations, invariants, UI text, and targeted regression coverage.
- [x] Pass the complete frozen regression suite and content tools.
- [x] Export and smoke-test the embedded-PCK `alpha-0.0.2` Windows executable.

## Decisions

| Decision | Rationale |
|----------|-----------|
| Balance by role budget, not source work | DLC is a horizontal choice set and must not become a power ladder. |
| One runtime definition owns each number | Combat, UI, compendium, and tests must not carry parallel constants. |
| Prefer mechanism over flat percentage | A build should change how the player positions and fights, not only inflate DPS. |
| Endless growth is bounded by performance, not progression | Entity counts may cap, while health, damage, reward, and player scaling remain monotonic. |

---

# Historical Plan: Horizontal Builds and World Geography

## Current Goal - 2026-08-12

Replace the equal-random upgrade pool and flat random-circle world with two shared, data-driven systems. Base content and every DLC remain horizontally equivalent: content packs add choices, never a higher power tier or mandatory progression gate. Player choices create build affinity naturally; regions and structures create tactical conditions without directly forcing upgrade odds.

## Current Phase

Phase 7 - implementation complete; running full regression and visual acceptance before test handoff.

## Acceptance Criteria

- Every base/DLC upgrade uses the same affinity, prerequisite, exclusion, rank, and specialization rules.
- Upgrade offers remain three choices, contain no duplicate IDs, naturally favor the affinities already chosen, and preserve a meaningful off-route option.
- Content packs cannot increase rarity or strength budgets merely because they are DLC.
- Regions do not directly boost drop or offer odds; enemy and terrain design produce contextual strengths naturally.
- The infinite world uses deterministic macro regions with coherent same-work region relationships and soft boundaries instead of cell-clipped circles.
- Structures use per-definition spacing, separation, footprint, variation, and discovery rules rather than one global 96-Tile lottery.
- Generated chunks retain biome/region semantics so rendering, spawning, diagnostics, and maps do not recompute terrain identity.
- The travel map records true player discovery, biome identity, structure discovery state, and semantic zoom without displaying chunk-grid seams by default.
- Build, source policy, gameplay/world/map integration tests, deterministic generation tests, and visual acceptance all pass.

## Active Phases

- [x] Audit the current build offer, world generation, structure, travel map, and diagnostic-log pipelines.
- [x] Implement horizontal affinity builds and deterministic three-choice offers.
- [x] Implement macro-region planning, coherent official-work region chains, and transition fields.
- [x] Implement data-driven structure placement, footprints, templates, and runtime discovery.
- [x] Upgrade semantic chunk storage and the travel map discovery/rendering pipeline.
- [x] Add regression, distribution, determinism, interaction, and visual tests.
- [x] Analyze the supplied D3D12/OpenGL sessions and correlate findings with the optimized build.
- [x] Run the complete verification suite and prepare the test handoff without publishing a release.

## Decisions

| Decision | Rationale |
|----------|-----------|
| DLC is horizontal content only | The complete base game must remain complete; DLC adds alternatives, not vertical power. |
| Affinity comes only from chosen build items | Characters and regions should create natural synergy through mechanics, not hidden offer manipulation. |
| Keep three choices but assign generation roles | Two choices may reinforce established affinity while at least one preserves exploration and pivoting. |
| Region identity is generated before chunks | Terrain, structures, enemies, and the map must share one coherent deterministic geography. |
| Structures are stable world instances | Discovery, encounter, map state, and cross-chunk rendering require stable IDs and footprints. |

---

# Historical Plan: Beta Debug Performance Diagnostics

## Current Goal - 2026-08-12

Produce a separate Windows diagnostic build variant for the historical version now named `alpha-0.0.1` to diagnose the reported roughly 3 FPS on another machine. Preserve existing formal artifacts. The diagnostic build must collect actionable hardware, renderer, frame pacing, ECS load, world-streaming, and graphics workload evidence with low sampling overhead, then provide a Chinese collection guide.

## Current Phase

Complete - the isolated Windows artifact, ZIP, structured log, launchers, guide, tests, and exported smoke run all passed.

## Debug Acceptance Criteria

- The normal runtime version remains `alpha-0.0.1`; diagnostics is an artifact flavor, not another semantic stage.
- A timestamped diagnostic log records OS, CPU concurrency, display/GPU/driver, rendering method, resolution, window mode, VSync, frame cap, and enabled content.
- Periodic samples record FPS/frame time plus ECS enemies, both projectile factions, pickups, spirits, active/pending chunks, and relevant Godot performance monitors.
- Sampling is bounded and buffered so the logger cannot become the cause of a 3 FPS result.
- The debug EXE launches independently with embedded PCK, retains console/debug logging, and does not overwrite alpha or beta.
- A Chinese guide states the exact log path, reproduction steps, and files the tester should return.

## Active Phases

- [x] Audit existing logging, performance APIs, and debug export behavior.
- [x] Implement and test low-overhead session diagnostics in the formal WorldDemo runtime.
- [x] Add a reproducible debug export command and artifact naming without changing semantic version.
- [x] Write the Chinese reproduction/log-collection guide.
- [x] Run build, diagnostics, gameplay, source-policy, and export-policy verification.
- [x] Export and smoke-test the diagnostic artifact now named `TouhouWuxiaSurvivor_alpha-0.0.1_windows-x86_64-debug.exe` without deleting existing artifacts.

---

# Historical Plan: Complete All Official DLC Content

## Goal - 2026-08-11

Complete the five runtime content domains for base and TH01-TH20: biome, structure, ordinary enemy, character, and spell card. Every registered character is both playable and Boss-capable; the selected player character is excluded from the current run's Boss candidates by stable character ID. Spell cards remain build-only automatic techniques with no active input.

## Final Phase

Complete - implementation, visual acceptance, full-suite verification, and the embedded-PCK beta export all passed.

## DLC Acceptance Criteria

Each work must pass all checks:

- Content: biome, structure, enemy, character, and at least two spell cards are declared with stable ownership.
- Assets: original source files are normalized and mapped without broken crops, blank frames, or blurred filtering.
- Runtime: enabled DLC affects the formal game world, combat, character selection, Boss pool, build pool, compendium, and content-selection description.
- Identity: every registered character can be selected as player and can be a Boss in other runs; the current player ID can never be selected as Boss.
- Interaction: spell cards unlock through the run build and trigger automatically; no active spell-card action exists.
- Visual: automated real-UI capture exists and is manually inspected for nonblank, complete, correctly framed output.

## Active Phases

- [x] Lock the TH01-TH20 character and two-spell-card-per-work content matrix.
- [x] Refactor content manifests, character definitions, and spell-card definitions into stable-ID catalogs.
- [x] Add compact character selection and inject the selected definition into the real player runtime.
- [x] Add a separate character Boss encounter pipeline and exclude the selected player ID before candidate selection.
- [x] Make upgrades, automatic spell execution, compendium, and bullet visuals content-pack aware.
- [x] Add all-work spell-card manifests, mappings, and honest proxy provenance for missing source packs.
- [x] Add ordinary-enemy and character-Boss AI profiles with movement, attack, and phase decisions.
- [x] Replace finite spawn/experience/upgrade assumptions with monotonic endless difficulty and progression functions.
- [x] Make player and enemy projectile budgets evolve from sparse opening shots to bounded late-game danmaku.
- [x] Run build, gameplay, selection, Boss, compendium, provenance, visual, encoding, and line-limit verification.
- [x] After all acceptance checks pass, update version/changelog to the version now named `alpha-0.0.1` and export one embedded-PCK Windows executable without deleting `alpha-0.0.0`.

## Completed Foundation

- [x] Commit ECS refactor baseline (`91ed4a1`).
- [x] Audit the formal ECS visual regression and every borrowed combat-sheet reference.
- [x] Add original pickup/bullet/audio mappings and wire ECS plus compatibility scenes to them.
- [x] Replace the obsolete enemy visual smoke test with formal ECS coverage and inspect captures.
- [x] Audit TH01-TH20 manifests, original sources, runtime mappings, and gaps.
- [x] Implement per-work build/mapping discovery that remains functional inside an exported PCK.
- [x] Add full-image, layered portrait, white-edge cleanup, and centered-scene normalization.
- [x] Generate and boundary-test TH07-TH12 mappings and repair invalid frame selections.
- [x] Generate and verify TH13-TH19 assets from available original sources.
- [x] Declare honest proxy boundaries for missing TH01-TH05 and TH20 sources.
- [x] Implement a unified all-DLC content and internal-asset manifest.
- [x] Generate/import every required original asset and validate dimensions/crops.
- [x] Wire every DLC into world, combat, compendium, and selection UI.
- [x] Capture and inspect every DLC in Godot; repair all invalid mappings.
- [x] Run build, coverage, gameplay, visual, encoding, and line-limit audits.

## Current Errors

- The first resumed status check targeted the outer workspace container instead of the nested Git project; all subsequent work is fixed to the inner `touhou-wuxia-survivor` directory.
- A quoted `findstr` memory probe was split by `cmd.exe`; it produced no project evidence and will not be repeated.
- A combined `findstr` planning-file read was rejected by quoting; direct independent file reads succeeded.
- The first three-file planning update used an incorrect findings heading and was atomically rejected; no file was partially changed.
- One `rg` alternation containing pipe characters was parsed by the outer shell; subsequent searches use separate `-e` arguments.
- The first Godot test command over-escaped a path without spaces and passed a literal leading backslash to `cmd.exe`; the direct executable path succeeded.
- The first post-refactor build failed only because two legacy spell tests still referenced removed per-card enum members; both tests now use stable catalog IDs and the next build passed.
- A later parallel build caught `EcsCombatRenderer` while the AI worker was mid-edit and missing an out assignment; ownership was preserved and the worker was notified instead of applying a conflicting patch.

- Initial commit command used a quoted message that the host shell split into pathspecs; retried with the ASCII message `refactor_combat_runtime_to_ecs` and committed successfully.
- First asset-builder run crashed because the sandbox denied Godot's `user://logs`; approved user-directory access fixed it.
- The first formal pickup visual test ran before Godot imported the generated PNG; a complete headless editor scan generated the required import metadata.
- Original TH17.5 OGG comments used invalid legacy metadata; FFmpeg stream-copy metadata stripping now removes the warning without audio re-encoding.
- Final review found one local artifact now named `alpha-0.0.0` created at 22:06; release files remain outside Git, so none may be removed or overwritten without user direction.

---

# Historical Plan: Complete Base and TH06 Internal Replacement

## Goal
Replace every current compendium preview for Gensokyo base content and TH06 with distinct, appropriate internal original-game visual mappings, while preserving Chinese identity labels, runtime fallbacks, strict non-public isolation, and the public export exclusion boundary.

## Current Phase
Phase 5

## Phases

### Phase 1: Complete Coverage Audit
- [x] Enumerate every base and TH06 compendium entry by category, name, and preview metadata
- [x] Inventory usable original image atlases and identify per-entry frame/background/portrait mappings
- [x] Define what "complete replacement" means for base content that has no single source work
- **Status:** complete

### Phase 2: Complete Internal Asset Set
- [x] Generate all required RGBA atlases under ASCII-only base and TH06 folders
- [x] Extend the manifest with exact source hashes, entry ownership, and replacement status
- [x] Keep supplied source packs read-only and public export exclusion intact
- **Status:** complete

### Phase 3: Entry-Specific Preview Integration
- [x] Replace category-wide mappings with data-driven per-entry preview definitions
- [x] Give every base and TH06 entry an appropriate animated visual while retaining Chinese labels
- [x] Preserve text/generated fallback when internal assets are excluded or missing
- **Status:** complete

### Phase 4: Complete Coverage and Visual Verification
- [x] Add coverage tests proving no base or TH06 entry remains unmapped internally
- [x] Capture representative pages for all five categories and inspect layout/animation
- [x] Build and run relevant tests serially with zero warnings and log errors
- [x] Verify source hashes, line limits, UTF-8 without BOM, public export exclusion, and no export invocation
- **Status:** complete

### Phase 5: Correct Base Enemy Visuals
- [ ] Replace named-character/Boss substitute atlases with appropriate generic enemy sources
- [ ] Declare correct source frame regions for all nine base enemies
- [ ] Replace manual Alpha-bound scanning with Godot `Image.GetUsedRect()`
- [ ] Regenerate, inspect all nine enemy outputs, and capture every base enemy in the compendium
- **Status:** in_progress

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Replace the earlier TH06 subset with complete base + TH06 coverage | The user explicitly expanded the scope to every current entry in both sources |
| Keep all extracted files below `assets/internal_original/` | Makes non-public material auditable and removable as one bounded tree |
| Preserve generated text previews as runtime fallback | Public builds and missing internal files must remain functional |
| Exclude internal originals from `Windows Release` | Prevents accidental inclusion in the existing public-style release preset |
| Do not export this milestone | The user authorized internal use, not a new binary export |
| Keep Chinese names over visual previews | The project established text as the identity/icon layer even when original motion assets are present |
| Mark base visuals as cross-work substitutes | Gensokyo base is not one official game and borrowed visuals must not be mislabeled as canon ownership |
| Normalize generated previews before runtime | 128x80 scenes, 4x48 actor strips, and 80x80 portraits keep heterogeneous original atlases out of drawing code |
| Store all 39 mappings in excluded JSON | Replacement ownership stays data-driven and disappears cleanly from public exports |
| Use Godot auto-trim only after a declared source frame is selected | Alpha bounds cannot infer sprite-sheet semantics or distinguish characters/expressions |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| WSL `rg` 命令末尾的 `2>/dev/null` 被外层 Windows shell 解释为本机路径 | 1 | 移除重定向，改由 `cmd.exe /d /c wsl.exe ...` 直接执行只读搜索 |
| `cmd.exe` 把 `rg` 正则中的管道符当作命令管道 | 1 | 不再传递管道正则，改用两个独立 `-e` 搜索模式 |
| WSL 将 `rg` 解析为 WindowsApps 内的 Windows 可执行文件并报无执行权限 | 1 | 停止从 WSL 调用该二进制，改由 `cmd.exe` 直接调用 Windows `rg.exe` |
| 系统未安装 `magick.exe` | 1 | 不引入外部依赖，优先使用工作区现有图像运行时或 Godot `Image` API 合并 Alpha |
| 图鉴测试直接调用 `_Draw()`，Godot 报告只能在绘制通知中调用 Canvas 绘图 API | 1 | 删除手动 `_Draw()`，等待真实 `ProcessFrame` 后读取渲染器状态，并要求最终日志无 ERROR |
| 截图设施搜索再次把正则管道符交给 `cmd.exe`，导致查询被拆成命令 | 1 | 后续 Windows `rg` 搜索统一使用多个 `-e` 参数，不再在命令文本中使用管道正则 |
| 边界测试中的 `FileAccess` 同时匹配 Godot 与 .NET 类型 | 1 | 两处文件读取显式限定为 `Godot.FileAccess` |
| `cmd if exist (...)` 检查 `progress.md` 被包装层报为命令行过长 | 1 | 不再使用带括号的复合命令，改为 `cmd /c dir /b progress.md` 后单独读取 |
| 在 `src/content` 与 `content` 中搜索预设的本体清单标识无匹配并返回退出码 1 | 1 | 改为直接读取 `ContentPackCatalog` 的本体加载路径，不再猜测标识符 |
| 工具编排的 JavaScript 环境没有 `btoa`，资产生成命令未能构造 Base64 参数 | 1 | 使用 `TextEncoder` 和纯 JavaScript Base64 编码函数后再调用同一 Python 逻辑 |
| 精简 JavaScript 环境同样没有 `TextEncoder`，第二次仍未启动 Python | 2 | 用 `encodeURIComponent` 手动解析 UTF-8 字节；若再失败则放弃 Base64 单命令并拆分处理 |
| C# 构建器逐像素经 Godot 绑定合并 Alpha，124 秒外层超时仍未完成 | 1 | 停止该实现，检查残留进程并改用图像字节缓冲批量合并，避免百万次绑定调用 |
| Alpha 已批量化后构建器仍超时，剩余 `CropOpaque` 仍逐像素跨绑定扫描 | 2 | 改用单次 RGBA8 字节缓冲扫描每四字节 Alpha；完成前不再重跑旧实现 |
| 并行检查中目录不存在使组合工具调用整体返回 1，未带回 tasklist 结果 | 1 | 输出不存在已确认；进程列表改为独立命令读取，不再和可能返回 1 的目录查询组合 |
| 缓冲版构建器运行约 30 秒仍无第一个 base 输出，手动终止外层会话 | 3 | 不再推测性能；增加阶段级诊断，先定位阻塞发生在脚本启动、参数、JSON、加载、混合还是保存 |
| console 包装层缓存阶段输出，64 秒超时仍无法看到新增 `GD.Print` | 1 | 将阶段标记写入 `user://` UTF-8 无 BOM 文件，短时运行后直接读取定位 |
| 19 秒短时运行没有创建启动阶段标记，证明工具节点 `_Ready` 尚未执行 | 1 | 检查 `tools` 导入边界与场景资源加载；先让 Godot 编辑器扫描新 C# 场景，再单独运行 |
| `.gdignore` 搜索无匹配返回 1，使与目录读取组合的工具结果丢失 | 1 | 已确认仓库没有 `.gdignore` 匹配；目录检查改为独立命令，避免和可返回 1 的搜索组合 |

## Notes
- Do not delete, rename, or modify any file in the supplied source pack.
- Do not export a binary unless the user explicitly requests an internal build.
- Keep each class in its own file and each file within the comment-excluded 250-line limit.
- Every class and function requires detailed Chinese documentation.

## Base Enemy Correction - 2026-07-31

- User reports all Gensokyo base enemy visuals are incorrect.
- Current mappings use TH08/TH10 stage-specific and named-character atlases as broad substitutes; several declared frame grids are not valid for those sheets.
- Corrective work must start from generic enemy atlas inventory and real frame layouts, then use `Image.GetUsedRect()` only for transparent-edge trimming.
- Generic atlas candidates found in TH08/TH10. Inspect their pixels/dimensions and derive real frame rectangles before editing mappings.
- TH08/TH10 generic sheets confirmed for fairies only. Search the full source pack for dedicated kedama, spirit, beast, insect, and miscellaneous generic enemy sheets.
- Later generic families and TH19 animal spirits found. Visually classify `enemy2`, `enemy5`, `enemy_g`, and `enemy_ll*` before final nine-entry mapping.
- `enemy2` rejected; `enemy5` reserved for night/large generic enemies. Continue with `enemy_g`, `enemy_ll`, `enemy_ll2`, and older extra enemy sheets.
- `enemy_g` accepted for strong generic fairies; `enemy_ll` rejected. Inspect TH11/TH12 extra sheets for spirits, kedama, and non-humanoid mobs.
- TH11 `enemy2` and TH12 `enemy3` rejected as effects. Check TH12 `enemy4/5` once, then pivot if they are also non-creatures.
- Pivot complete. Search TH07/TH09 and explicit generic sheets for kedama/spirits; keep TH19 animal spirits and TH12/19 bats as confirmed sources.
- Inspect TH09 `enemy.png` first; inspect TH07 stage sheets only if a remaining ecology lacks a verified generic source.
- TH09 accepted only for fairy variants; TH07 stage 1 rejected. Verify kedama's official-game appearances before inspecting a specific local stage sheet.
- Kedama source verified in TH10 generic atlas; inspect local TH11/TH12 generic sheets for evil-spirit and ghost-fairy frame coordinates.
- Planning record error: a findings update assumed a nonexistent `## 2026-07-31` heading and was atomically rejected; future updates use stable file headings.
### Diagnostic note - 2026-07-31

- The silent builder run produced neither console output nor its first `user://` progress marker.
- A targeted search of `godot.log` found no script-load or builder error.
- Do not repeat the same long builder invocation until a minimal scene-launch probe proves the command-line scene argument is honored.
- Retry only with an explicit `res://tools/internal_assets/InternalPreviewAssetBuilder.tscn` positional resource and a bounded quit timeout.
- Correction: use the documented `--scene res://tools/internal_assets/InternalPreviewAssetBuilder.tscn` option. The invocation now reaches the tool scene but fails because `InternalPreviewAssetBuilder` is absent from the loaded C# assembly.
- Add explicit compile includes for only `tools/internal_assets/InternalPreviewAssetBuilder.cs` and `InternalSourceHashWriter.cs`, then rebuild before regenerating assets.
- Qualify `System.Environment.NewLine`, then repeat the build and bounded builder run.
- Normalize every scene base/overlay image to RGBA8 before `BlendRect`, add output validation, and rerun until the console is error-free.
- Diagnostic error: the combined `cmd more +75` / manifest search returned exit code 1 without output. Do not repeat it; use targeted `rg -n -C` queries instead.
- Diagnostic error: an `rg` alternation pattern containing `|` was parsed by `cmd.exe` as a pipeline. Use repeated `-e` options for Windows `cmd` searches.
- Convert every loaded source image to RGBA8 in `LoadImage`, then regenerate and inspect all output dimensions/counts.
- Clean regeneration complete. Inspect output inventory/dimensions, then implement the 39-entry manifest-driven runtime renderer.
- Inventory coverage confirmed. Implement `preview_mappings.json`, typed manifest loader, and category-specific animated renderers for all 39 base/TH06 entries.
- Replace the renderer's hard-coded TH06 category logic with exact-entry definitions, including character portraits and base-content visual substitutes.
- Inspection error: searched nonexistent `assets/content`; content manifests are under project-root `content`. Use catalog factories plus those JSON files for exact names.
- Keep the mapping dependency inside the UI/internal-preview layer; do not add asset paths to `CompendiumEntry`.
- Add the typed manifest/catalog classes, all 39 mappings, a lazy nearest-neighbor texture cache, and character internal-caption routing.
- Runtime implementation complete and compiling. Extend boundary/smoke/visual tests, update licensing notice, then visually inspect representative captures.
- Make boundary tests manifest-driven by normalized kind, assert exact base/TH06 coverage, and update the TH06 character smoke expectation.
- Rewrite the internal-use manifest for full base/TH06 scope and expand visual capture to Biome, Structure, Enemy, Character, and SpellCard.
- Implementation/import phase complete. Run boundary, smoke, and visual tests; inspect screenshots and enforce file/encoding/process constraints.
- Boundary and smoke tests passed. Run OpenGL visual capture, inspect all five screenshots, then audit line limits/comments/BOM/export/process state.
- Visual capture passed; fix the clipped generated activity label on internal Biome/Structure scenes, then regenerate and re-inspect.
- For internal scenes, skip `DrawDailyScene` and draw small bounded pixel inhabitants/lights inside `DrawScene`; retain the old text scene only for public fallback.
- Scene overlay fix implemented. Rebuild and recapture; if clean, complete source/style/encoding/process audits.
- Recapture passed. Visually confirm the two scene screenshots, then run final audits and all relevant tests once more.
- New visual blocker: inspect all TH06 portrait source layouts, replace the universal half-width crop with per-entry manifest crops, regenerate, and recapture every character.
- Confirmed blocker. Determine opaque bounds/expressions from original `face10a` and `face12a`, then encode explicit crop rectangles.
- Evidence favors full-canvas alpha-bound crops. Check representative base/TH06 portraits before choosing global removal over sister-only exceptions.
- Mixed layouts confirmed. Finish auditing Rumia/Cirno/Meiling/Patchouli, then add optional portrait `crop` to the build manifest and builder.
- Rumia/Cirno audited as dual-expression. Check Meiling/Patchouli to close the eight-portrait layout audit.
- Audit complete. Add optional manifest crop support and full-canvas rectangles only for Remilia/Flandre, then regenerate and inspect both outputs.
- Crop fix regenerated cleanly. Inspect sister outputs, import them, and capture both sisters in the real compendium UI.
- Asset outputs are complete. Extend the visual test to select Remilia and Flandre by name, import, capture, and inspect both UI states.
- Dedicated sister capture paths are ready and imported. Run OpenGL capture and inspect both actual UI screenshots.
- Sister portraits verified complete. Fix the one-character title wrap for long names, then recapture both before final audits.
- Inspection note: guessed `EntryName/NameLabel` searches returned no matches. Locate the actual title node from the detail subtree and label bindings before editing.
- Apply dynamic one-line sizing only to `Identity/Heading/EntryTitle`, then add a screenshot/test assertion for both long names.
- Inspection error: `cmd.exe` mangled a quoted `rg` pattern containing backslashes. Use plain `rg -e EntryTitle` and `rg -e _entryTitle` searches.
- Disable detail-title autowrap, fit it with measured theme-font width in `ShowEntry`, reset size in `ClearDetails`, and assert one line in sister captures.
- Single-line title logic and assertions pass. Visually inspect final sister captures, then run boundary/smoke/style/encoding/process audits.
- Sister visual inspection complete. Run final automated and repository hygiene audits; no export is authorized.
- Final runtime tests passed. Audit changed-file line/comment/encoding rules, export boundary, source hashes, git diff, and spawned-process cleanup.
- Audit note: current Godot subdirectory is not inside a Git repository, so `git status` is unavailable. Also abandon the piped style search that returned no useful output; inspect direct test/file lists instead.
- Run a direct effective-line counter on every touched C# file, then scan touched text files for UTF-8 BOM and inspect class/function XML comments.
- Refactor image-transform methods from the 260-line builder into `InternalImageTransformer.cs`, whitelist that file, rebuild/regenerate, and rerun the effective-line audit.
- Extract all six stateless transform methods, import them with a static using, and keep I/O/manifest logic in the builder.
- Refactor patch attempt was atomically rejected because the file starts with `System.Text.Json` before `Godot`. No partial edit occurred; reapply with exact boundaries (`MergeAlpha` through `CreateTransparent`).
- Refactor complete: builder 179 lines, transformer 87. Regenerate assets and rerun boundary test to prove behavioral equivalence.
- Behavioral equivalence confirmed. Finish BOM/comment/export/process checks and one final smoke/visual pass if static audits are clean.
- BOM audit passed. Repeat declaration scan with `rg -g "*.cs"`, then verify export exclusion and spawned-process cleanup.
- Comment and export-boundary audits passed. Verify process cleanup, then run final smoke and OpenGL visual tests.
- Final smoke passed. Inspect PIDs 72664/42000; terminate only if their command lines belong to this task, preserve user editor PID 17192.
- Task-owned stale processes cleaned. Run final seven-state OpenGL capture and confirm it exits, leaving only PID 17192.
