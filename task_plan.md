# Task Plan: Complete All Official DLC Content

## Current Goal - 2026-08-11

Complete the TH01-TH20 DLC visual and runtime pipeline after the formal ECS combat replacement. Every work must use an independent declarative source manifest, shared runtime/compendium mapping, auditable original or explicitly declared proxy source, and inspected normalized output.

## Current Phase

Complete - all-DLC original-asset pipeline and formal ECS regression verified.

## DLC Acceptance Criteria

Each work must pass all four checks:

- Content: biome, structure, enemy, character, spell-card/bullet additions are declared with correct ownership.
- Assets: original source files are normalized and mapped without broken crops, blank frames, or blurred filtering.
- Runtime: enabled DLC affects the formal game world, combat, compendium, and content-selection description.
- Visual: automated real-UI capture exists and is manually inspected for nonblank, complete, correctly framed output.

## Active Phases

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

- Initial commit command used a quoted message that the host shell split into pathspecs; retried with the ASCII message `refactor_combat_runtime_to_ecs` and committed successfully.
- First asset-builder run crashed because the sandbox denied Godot's `user://logs`; approved user-directory access fixed it.
- The first formal pickup visual test ran before Godot imported the generated PNG; a complete headless editor scan generated the required import metadata.
- Original TH17.5 OGG comments used invalid legacy metadata; FFmpeg stream-copy metadata stripping now removes the warning without audio re-encoding.
- Final review found that ignored `release/` contains only a local `0.0.0-alpha` executable created at 22:06; previous beta executables are absent and cannot be recovered from Git, so no release file may be removed or overwritten without user direction.

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
