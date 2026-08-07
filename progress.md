# Progress Log

## Session: 2026-07-31 - Complete Base and TH06 Internal Replacement

### Phase 1: Complete Coverage Audit
- **Status:** complete
- Actions taken:
  - Expanded the prior bounded TH06 proof into complete base + TH06 compendium coverage.
  - Preserved internal-only use, public export exclusion, Chinese identity labels, and runtime fallback requirements.
  - Began auditing every entry and available original atlas before changing mappings.
  - Confirmed complete coverage requires per-entry data rather than source/category-wide rendering branches.
  - Counted 18 TH06 entries across regions, structures, enemies, characters, and spell cards.
  - Counted 21 base entries; complete coverage therefore means 39 distinct catalog records.
  - Confirmed TH06 contains stage atlases and portrait pairs sufficient for all 18 TH06 records.
  - Selected normalized scene, actor-strip, portrait, and spell preview formats.
  - Defined base visuals as explicitly labeled cross-work substitutes rather than false canon mappings.

### Phase 2: Complete Internal Asset Set
- **Status:** in_progress
- Actions taken:
  - Started producing the normalized base and TH06 internal preview tree and its 39-entry mapping manifest.
  - Added a reusable C# Godot asset builder and declarative build manifest; first execution exposed an unacceptable per-pixel interop cost in Alpha merging.
  - Buffer-based Alpha merging compiled, but the second run exposed the same interop issue in transparent-bound cropping; that scan is now the remaining optimization target.
- Files created/modified:
  - `task_plan.md`
  - `findings.md`
  - `progress.md`

## Session: 2026-07-30 - Meta Progression

### Phase 1: Discovery and Economy Contract
- **Status:** complete
- Actions taken:
  - Resumed from the completed in-run progression milestone.
  - Replaced the completed plan with a dedicated meta-progression and unlock plan.
  - Defined the spirit-jade economy, four cultivation nodes, caps, unlock gates, and reset behavior.
- Files created/modified:
  - `task_plan.md`
  - `findings.md`
  - `progress.md`

### Phase 2: Domain and Persistence
- **Status:** complete
- Actions taken:
  - Added versioned profile repair, atomic JSON storage, profile manager, reward formula, purchase results, and runtime bonus projection.
  - Added an in-memory store and passed `MetaProgressionBalanceTest`.
- Files created/modified:
  - `src/gameplay/meta/definitions/*`
  - `src/gameplay/meta/persistence/*`
  - `src/gameplay/meta/runtime/*`
  - `tests/support/MemoryProgressionProfileStore.cs`
  - `tests/integration/MetaProgressionBalanceTest.*`

### Phase 3: UI and Runtime Integration
- **Status:** complete
- Actions taken:
- Added the no-scroll `博丽神社整备` panel and main-menu entry.
- Reframed all generic cultivation names around Reimu's flight, barrier, Yin-Yang Orb, and persuasion needle tools.
- Replaced the invented currency with neutral Gensokyo-wide `钱` across persistence, UI, summary, and tests.
- Applied profile health/damage/movement/attraction bonuses at run start and added idempotent death settlement.
- Added volatile profile storage so integration tests do not touch real user progress.
- Files created/modified:
- `src/ui/meta/*`
- `src/ui/menu/MainMenu.*`
- `src/demo/WorldDemo.cs`
- `src/actors/player/PlayerHealth.cs`
- `src/gameplay/session/*`
- `src/ui/death/*`
- `tests/integration/CultivationPanelSmokeTest.*`

### Phase 4: Verification
- **Status:** in_progress
- Actions taken:
  - Passed shrine-preparation UI, main-menu switching, meta balance, JSON round-trip, death flow, and run-modifier composition tests.
- Files created/modified:

## Session: 2026-07-30

### Phase 1: Requirements and Discovery
- **Status:** complete
- **Started:** 2026-07-30
- Actions taken:
  - Confirmed the staged progression direction with the user.
  - Scoped this implementation to a complete in-run progression vertical slice.
  - Created persistent planning, findings, and progress files.
  - Mapped enemy defeat, temporary pickup, player modifiers, HUD, pause, map, and death-summary boundaries.
- Files created/modified:
  - `task_plan.md`
  - `findings.md`
  - `progress.md`

### Phase 2: Architecture and Balance Contract
- **Status:** complete
- Actions taken:
  - Selected dedicated spirit drops, run modifiers, progression coordinator, and level-up overlay boundaries.
  - Selected six initial upgrades supported by current combat systems.
  - Implemented upgrade definitions, build state, modifier projection, XP state, level curve, and spirit value calculation.
  - Added and passed `RunProgressionBalanceTest`.
- Files created/modified:
  - `src/gameplay/progression/definitions/*`
  - `src/gameplay/progression/runtime/*`
  - `tests/integration/RunProgressionBalanceTest.cs`
- Files created/modified:

### Phase 3: Gameplay Implementation
- **Status:** complete
- Actions taken:
- Implemented spirit-drop actor/spawner, level-up overlay, progression coordinator, and runtime modifier consumers.
- Ran the first integration build; it identified the expected stale composition-root call to `AutoShooter.Configure`.
- Wired progression through the real infinite-world scene, compact HUD, and immutable death summary.
- Added and passed a real-scene smoke test for collection, pause ownership, upgrade application, modifier projection, and input restoration.
- Files created/modified:
- `src/actors/spirit/*`
- `src/gameplay/spawning/SpiritDropSpawner.cs`
- `src/ui/progression/*`
- `src/gameplay/progression/runtime/RunProgressionCoordinator.cs`
- `src/actors/player/PlayerController.cs`
- `src/combat/weapons/AutoShooter.cs`

### Phase 4: Testing and Verification
- **Status:** complete
- Actions taken:
- Passed progression balance, progression flow, HUD, and death-flow tests.
- Passed all remaining integration tests: map, audio, UI assets, texture policy, settings, pause, official content, enemy balance, content pack, compendium, and combat loop.
- Confirmed the comment-excluded 250-line rule and UTF-8 without BOM for the audited implementation files.
- Files created/modified:

### Phase 5: Delivery
- **Status:** complete
- Actions taken:
- Exported a single embedded-PCK Windows executable as `release/TouhouWuxiaSurvivor_beta0.2.exe`.
- Launched the exported executable and confirmed its running process (PID 67720).
- Preserved `release/TouhouWuxiaSurvivor_beta0.1.exe` without modification or deletion.
- Files created/modified:

## Test Results
| Test | Input | Expected | Actual | Status |
|------|-------|----------|--------|--------|
| Build | `dotnet build --nologo` | No diagnostics | 0 warnings, 0 errors | pass |
| Integration suite | 15 Godot test scenes | Every scene exits 0 | 15/15 passed | pass |

### 2026-07-30 属性面板回归验证

- `dotnet build --nologo`：通过，0 个警告、0 个错误。
- HUD 数据采集已从 `WorldDemo` 拆入 `WorldHudCoordinator`，等待 Godot 场景级回归验证。
- 一次用于检索历史测试命令的 `cmd.exe /c rg` 调用因正则中的管道字符被命令解释器处理而失败；未修改任何项目文件，后续改用无管道的明确查询。
- 首次批量运行四个 Godot 场景时，命令中的 `@` 被宿主解释成 here-string 标记，Godot 未启动；改为不含该字符的独立 `cmd.exe` 调用。
- 属性面板、HUD、局内升级、死亡结算四个场景测试均通过。退出阶段仍报告测试节点相关的 ObjectDB/resource 未释放警告，未影响断言与退出码，后续作为测试夹具清理问题单独处理。
- 首次行数审计命令因宿主不接受 `&&` 分隔符而未执行；改为独立并行命令，避免跨命令解释器拼接。
- `WorldDemo.cs` 注释外有效行数为 218；`WorldHudCoordinator.cs` 物理行数为 89，均低于 250 行上限。
- UTF-8 BOM 扫描无匹配；受检源码、测试和规划文档均未发现 BOM。
- 首次完整套件编排把工具结果对象当作普通字符串解析，因此在已通过的 `WorldMapSmokeTest` 后提前停止；测试本身退出码为 0，后续改用明确批次完成余下场景。
- 同项目的第二批 Godot 测试并行运行时争用共享状态并在 120 秒超时；确认残留的是本批启动的控制台测试进程 PID 68288，已连同子进程终止。后续所有场景严格串行运行。
- 后续串行批次中 `RunProgressionSmokeTest` 与 `RunProgressionBalanceTest` 通过，随后 `PauseMenuSmokeTest` 无输出等待；按用户要求停止扩展验证，终止该批控制台 PID 63648 及子进程。
- 用户明确要求不要每次迭代都发布；本轮不导出、不启动发行版，发布改为仅在用户明确要求时执行。
- 符卡审计的首轮 `rg` 查询因 `cmd.exe` 把正则管道解释为命令分隔符而失败；改用多 `-e` 后，中文参数查询仍无可信输出。后续通过 ASCII 类型名定位并直接读取定义文件。
- 最终状态审计发现当前工程目录不属于 Git 仓库，因此无法提供 `git status`/diff 汇总；改为按实际文件路径、构建和场景测试结果核对。
| Meta progression balance | In-memory profile and catalog | All economy contracts hold | Passed | pass |

## Error Log
| Timestamp | Error | Attempt | Resolution |
|-----------|-------|---------|------------|
| 2026-07-30 | Level-up opening did not block the new character stats overlay | First E-panel integration regression run | Replace duplicated map/pause assignments with the unified three-overlay blocker |
| 2026-07-30 | `WorldDemo.cs(87)` lacks `runModifiers` | First build after runtime modifier API change | Pending full composition-root wiring |
| 2026-07-30 | Assumed HUD files were under `src/ui/hud` | Parallel source read | Locate the actual paths before editing |
| 2026-07-30 | `cmd` interpreted the regex pipe/parentheses | First path-search command | Use two literal `rg --files -g` patterns instead |
| 2026-07-30 | `find /v /c` quoting was rejected by `cmd` | First line-count attempt | Use `rg -n "^" file` with `findstr /R` only during final constraint verification |
| 2026-07-30 | Assumed `RunSummaryTextFormatter` was under `src/ui/death` | Parallel death UI read | Located it under `src/gameplay/session` before editing |
| 2026-07-30 | Nullable warning at `WorldDemo.cs(242)` | First build after death-summary integration | Replace unnecessary null-conditional call after the explicit guard so flow analysis keeps `_progression` non-null |
| 2026-07-30 | Progression smoke test asserted before the spawned spirit's first `_Process` | First real-scene progression run | Wait up to four process frames, then retain the exact level/XP assertion |
| 2026-07-30 | `git status` failed in the project and parent directory | Changed-file discovery for final audit | Workspace is not a Git repository; audit the explicitly tracked implementation file list instead |
| 2026-07-30 | `cmd` interpreted the alternation pipe in the non-comment line-count regex | First effective line-count attempt | Use WSL `awk` as one command so no shell pipe is involved |

## 5-Question Reboot Check
| Question | Answer |
|----------|--------|
| Where am I? | Phase 1 discovery |
| Where am I going? | Architecture, implementation, verification, and release |
| What's the goal? | A playable in-run progression vertical slice |
| What have I learned? | Existing systems are present but boundaries require inspection |
| What have I done? | Scoped the phase and initialized persistent planning files |
# 2026-07-30 符卡武侠化系统

- 已切换计划目标：将符卡实现为具备原作元数据、灵力资源和真实战斗效果的武侠化奥义。
- 本阶段不导出、不启动发布版。
- HUD 定义的首次路径读取失败，已记入计划；改用文件清单定位真实源码。
- HUD 场景并非独立文件，而是内联在 `WorldDemo.tscn`；后续按节点名读取场景片段。
- Phase 1 完成：已确定原作符卡、双符卡数值、灵息充能、输入和 UI/战斗职责边界。
- 用户明确取消主动技能操作；实现方向改为升级构筑解锁、灵力充能、战况自动施放，撤回所有新增符卡按键。
- Phase 2/3 完成：两张符卡已进入三选一构筑，并按灵力和敌群态势自动施放；HUD 与 E 面板完成投影。
- `SpellCardBalanceTest` 与 `SpellCardSmokeTest` 均退出 0；真实场景退出仍有既有 ObjectDB/resource 测试夹具警告。
- 六项相关回归通过：RunProgressionBalance、RunProgressionSmoke、WorldHud、CharacterStatsOverlay、SettingsPanel、CombatLoop。
- 新文件最大 155 物理行；`WorldDemo` 注释外 230 行；生产源码无主动符卡动作。
- UTF-8 BOM 扫描无匹配；未执行任何发布导出。
- 发现逻辑视口仅 640 宽，取消会达到 720 宽的独立符卡条方案；改为复用原 590 宽状态文字。发行目录仍仅有旧的三个 EXE。
- 紧凑 HUD 初版将“击破/敌人”缩写后触发 `WorldHudSmokeTest` 可读性契约失败；决定恢复完整标签，仅压缩间距和奥义后缀。
- 恢复完整标签后重新构建 0 警告、0 错误，`WorldHudSmokeTest` 通过。
- 最终 `SpellCardSmokeTest` 与 `CharacterStatsOverlaySmokeTest` 通过；BOM 扫描无匹配，Phase 4 验证完成。
# 2026-07-30 图鉴符卡分类

- 已切换计划：新增符卡图鉴分类、运行定义映射、动画文字预览与固定详情排版。
- 本阶段不导出发布版。
- Phase 1 完成：旧作边界为 TH01-TH05，确定独立复选/展开交互和 640x360 紧凑布局。
- Phase 2 完成：新行组件、显示旧作开关、默认折叠、独立展开和选择保留已接入；ContentPackSmokeTest 通过。
- Compendium、UiAsset、OfficialContentCoverage 回归通过；行组件/面板/测试均低于 250 行有效代码上限。
- BOM 扫描无匹配；发行目录未变化，本轮未导出。
- 首次文件清单包含不存在的 `src/gameplay/compendium`，已改用真实的 `src/ui/compendium` 边界。
- Phase 1 完成：确定第五分类、TH06 来源映射、八行以内字段与两类动态文字预览。
- Phase 2 完成：符卡第五分类、运行定义映射、固定详情与动态文字预览已接入；`CompendiumSmokeTest` 通过。
- SpellCardBalance、SpellCardSmoke、UiAsset、OfficialContentCoverage 回归通过。
- 行数审计：CompendiumPreview 注释外 196 行；新工厂 83 物理行；CompendiumCatalog 230 物理行。
- Phase 3 完成：构建 0 警告/0 错误，五项相关场景测试通过，BOM/无滚动/无拖拽分界线/发行隔离均验证完成。
# 2026-07-30 本局内容选择折叠列表

- 已切换计划：显示旧作开关、每作独立折叠、默认仅作品名、点击展开增量详情。
- 本阶段不导出发布版。
- Phase 1/2 完成：TH01-TH05 默认隐藏，TH06-TH20 默认展示；复选状态与展开、筛选状态彼此独立。
- 每个正作默认只展示复选框和作品名，点击标题独立展开状态及分类增量详情，再次点击收起。
- `dotnet build --nologo` 为 0 警告、0 错误；ContentPack、Compendium、UiAsset、OfficialContentCoverage 四项场景测试通过。
- 行数组件 140 行、选择面板 149 行、测试 187 行有效代码；BOM 扫描无匹配，`release` 未变化且未执行导出。
- 最终状态补丁首次误指向外层同名任务记录且未命中；已定位两套规划文件并只更新 Godot 子项目记录。

# 2026-07-31 暂停返回按失败结算

- Phase 1 完成：确认暂停菜单直接切场景是绕过结算的入口，确定以 `RunEndReason` 和共享终局方法统一死亡、主动放弃。
- Phase 2 完成：暂停菜单改为发布放弃意图；`WorldDemo` 一次性锁定输入、结算并展示原因化失败弹窗；失败页继续负责真正导航。
- 首次编译发现测试静态辅助函数错误，修正后构建为 0 警告、0 错误。
- 修复暂停测试无异常退出导致的夹具停滞，并清理两个确认属于本轮测试的残留进程树。
- 修正暂停测试对 `toggle_stats` 的过期双键断言；生产键位未改变。
- `PauseMenuSmokeTest` 通过，主动放弃的失败总结主路径已验证。
- 提取 `RunFailureCoordinator` 后，`WorldDemo` 有效代码从 266 行降至 226 行；新协调器为 102 行。
- 终局 API 已统一为 `BlockForRunEnd` / `CancelForRunEnd`，不再把主动放弃误称为死亡。
- 最终构建 0 警告、0 错误；PauseMenu、DeathFlow、MetaProgressionBalance 三项串行测试通过。
- PauseMenu 回归额外验证主动放弃后的生命归零不会覆盖第一次总结或重复终局。
- DeathFlow 仍有既有 ObjectDB/resource 退出夹具警告，但场景断言通过且退出码为 0。
- UTF-8/BOM、有效行数、发行隔离和残留测试进程检查均通过；未导出发布版。

# 2026-07-31 本局总结紧凑化

- Phase 1 完成：确认旧总结窗为 560x340，十行单网格和无界武学文本导致面板接近铺满 640x360 视口。
- Phase 2 完成：失败弹窗收至 360x184，总结窗收至 460x260；普通统计拆成左右栏，武学独立为两行区域。
- 新增 `DeathSummaryVisualTest`，使用 148 击破、红魔乡内容和长构筑验证真实拥挤场景。
- 无头模式通过尺寸/留白/可见行/按钮边界断言；普通 OpenGL 渲染生成 640x360 和 1280x720 截图。
- 首轮截图暴露自动换行 Label 高度坍缩为 1px；设置稳定最小高度后普通信息和两行武学均完整显示。
- 最终构建 0 警告、0 错误；DeathSummaryVisual、DeathFlow、PauseMenu、UiAsset 四项测试通过。
- 640x360 与 1280x720 最终截图人工复核通过；面板面积减少约 37.2%，无裁切、重叠、滚动或可拖拽分界线。
- 行数、UTF-8/BOM、发行隔离和残留进程检查通过；本轮未导出。

# 2026-07-31 导出 beta0.2

- 用户明确授权覆盖 beta0.2；正式版和 beta0.1 保持不变。
- 导出元数据从 0.1.0.0 修正为文件/产品版本 0.2.0.0，内嵌 PCK 和项目图标保持启用。
- Release 构建 0 警告、0 错误；`Windows Release` 预设完成导出。
- 外层命令超时后识别并定向结束本轮 PID 65712 的无子进程 console 包装层，未触碰用户原有 Godot PID 17192。
- 新产物 189,733,976 字节，时间 2026-07-31 11:31:50，单 EXE 无外置 PCK。
- 新 EXE 无头启动 120 帧后退出码 0；SHA-256 为 `21bb09dc8ad0fc47ecbb98e5ad72fea6e4551b5a65b8a4b50fd967fbdb1402e0`，无残留进程。
## 2026-07-31 - Continue base + TH06 replacement

- Searched the current Godot log for builder/script errors; no matching error was emitted.
- Kept the existing source material and generated-output directories intact; no files were deleted.
- Next: stop the silent builder session, verify positional scene launching, then resume normalized asset generation.
- Verified the builder scene resource and the project main-scene setting before changing the launch command.
- Corrected the launch contract to use `--scene`; obtained a deterministic C# class-instantiation error in 4.6 seconds instead of another hanging run.
- Confirmed the application project builds with zero warnings/errors and that its tool-source exclusion is the cause of the missing builder class.
- Added the two-file compile whitelist; compilation now reaches the builder and identified one namespace ambiguity to fix.
- Fixed the namespace ambiguity; build passes with zero warnings/errors.
- Ran the builder successfully through all stages, but rejected the run as incomplete because three scene layer blends emitted format-mismatch errors.
- Logged and abandoned one no-output combined inspection command; no project files were changed by it.
- Logged the `cmd.exe` alternation escaping failure and switched subsequent searches to repeated `rg -e` arguments.
- Located the format inconsistency at the image-loader boundary; no manifest-specific workaround is required.
- Regenerated the complete internal preview asset set with a clean console after normalizing all inputs to RGBA8.
- Audited the generated file inventory against the build manifest and confirmed all planned output families exist.
- Audited the current runtime renderer and confirmed it must be replaced rather than extended with more category branches.
- Located the base source ID (`base`) and TH06 source ID (`th06_eosd`); corrected one read-only content-path mistake.
- Confirmed the current compendium data model already provides a stable composite key, so no content-model migration is needed.
- Located both source manifests and began exact-name coverage extraction for the 39-entry runtime mapping.
- Extracted and count-checked all base and TH06 manifest names; only the two spell-card names remain to copy from the spell catalog.
- Completed exact-name extraction for all 39 mapping entries and identified the character rendering branch change.
- Added typed mapping classes, replaced the category-wide renderer, added all 39 JSON entries, and routed internal character portraits through the Chinese-caption branch.
- Verified `dotnet build`: zero warnings/errors; verified mapping count: 39.
- Reviewed integration tests and identified the two obsolete three-atlas/text-character assertions that must be replaced.
- Reviewed internal governance documentation and visual test coverage; both still describe the previous limited TH06 prototype.
- Replaced obsolete boundary/smoke/visual assertions, rewrote the internal-use manifest, and imported all 38 unique textures successfully.
- Verified build again: zero warnings/errors.
- Passed the internal asset boundary test and compendium smoke test under Godot headless mode.
- Passed the OpenGL visual test and began manual inspection; identified one clipped text overlay in internal structure previews.
- Confirmed the clipping comes from the legacy generated overlay, not the imported texture or preview-frame clipping; enemy rendering is visually valid.
- Inspected character and spell-card captures; both normalized render paths are visually valid.
- Replaced the internal scene's unbounded text/roof overlays with bounded pixel inhabitants and light animation.
- Rebuilt and reran the full visual capture after the scene overlay fix; all five category state assertions pass.
- Visually confirmed the scene clipping fix, then received user correction that Scarlet sister portraits are half-cropped.
- Inspected both generated sister portraits and confirmed the universal half-width crop is the cause.
- Inspected the two original source images and established that both use full-canvas single portraits.
- Compared Reimu and Sakuya sources and confirmed TH06 portrait files use mixed layouts, requiring per-entry crop data.
- Audited Rumia and Cirno source layouts; both remain on the default dual-expression crop.
- Completed the eight-portrait source-layout audit and isolated full-canvas exceptions to the Scarlet sisters.
- Implemented per-entry portrait crops and regenerated the full internal asset set without errors.
- Visually inspected both regenerated 80x80 portraits and confirmed the half-character defect is removed at the asset level.
- Added dedicated UI captures for both sisters, rebuilt cleanly, and reimported the corrected textures.
- Passed extended visual capture and inspected both sister states; portrait defect is fixed, but long detail names need one-line sizing.
- Logged one no-match title-node search; no project state changed.
- Located the exact detail-title node and code binding for a narrowly scoped single-line sizing fix.
- Logged and abandoned one command-escaping failure while reading title settings; no files changed.
- Confirmed the long-name wrap is caused by fixed 15px type plus enabled autowrap in the compact identity column.
- Added measured 15px-to-10px one-line fitting and passed the seven-state visual test with explicit sister title assertions.
- Manually inspected the final sister screenshots and confirmed both the crop and title-wrap defects are resolved.
- Re-ran and passed the asset-boundary and compendium smoke tests after all portrait/title changes.
- Confirmed no Git metadata is available at the current project path; final audit will use direct filesystem/test evidence.
- Enumerated the integration suite and confirmed no reusable code-style test covers the requested effective-line limit.
- Ran a direct comment/blank-excluding line audit and identified one 10-line overage in the internal asset builder.
- Inspected the builder implementation and selected a responsibility-based image-transform extraction boundary.
- Logged one atomically rejected refactor patch and verified the builder remained unchanged before retrying with exact context.
- Completed the image-transform extraction and passed build plus effective-line audits.
- Regenerated all internal assets and passed the boundary test after the builder refactor.
- Passed the UTF-8-without-BOM audit; logged one Windows glob syntax issue in the comment declaration scan.
- Passed the corrected XML-comment declaration scan and direct public-export exclusion check.
- Passed final smoke test; paused cleanup to identify two non-editor Godot processes safely.
- Identified and terminated only the two task-owned stale Godot processes; preserved user editor PID 17192.
- Final completion: base + TH06 internal replacement covers 39 entries with 38 unique normalized assets; build, boundary, smoke, and seven-state OpenGL visual tests pass.
- Scarlet sister mixed-layout portrait crops and long-name single-line sizing are verified in dedicated screenshots.
- No binary export or release publication was performed; only the user's pre-existing Godot editor PID 17192 remains.
# 2026-07-31 - Base enemy visual correction

- Reopened the completed internal-preview plan after user reported all base enemy visuals are wrong.
- Decided to use Godot-native Alpha auto-trim after correct frame selection, not as a substitute for atlas-layout metadata.
# Base Enemy Visual Audit - 2026-07-31

- Located the dedicated TH08/TH10 generic enemy atlases.
- Confirmed the current stage-character substitutions are the wrong source family.
- Visually inspected both generic atlases and limited their intended use to fairy-class base enemies until other generic creature sources are found.
- Searched all works for generic enemy sheets and inspected TH19 animal spirits as a valid beast-family source.
- Classified TH19 `enemy2` as effects and `enemy5` as bat/large-winged generic enemies.
- Classified `enemy_g` as large generic fairies and rejected `enemy_ll` as named-character content.
- Rejected TH11 `enemy2` and TH12 `enemy3` as effects rather than enemies.
- Classified TH12 `enemy4` as UFO objects and confirmed `enemy5` as the reusable bat family; ended numbered-sheet guessing.
- Located TH09's explicit generic sheet; confirmed TH07 has no standalone generic enemy atlas.
- Inspected and limited TH09 to fairy roles; rejected TH07 stage 1 as another mixed named-character sheet.
- Verified kedama/Mountain-of-Faith provenance and identified TH11 evil spirits plus TH12 ghost fairies as generic ecology candidates.
- Logged one atomically rejected planning-record patch caused by an incorrect heading assumption.
