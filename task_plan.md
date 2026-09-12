## Growth And Character Boss Release — 2026-09-13

1. [in_progress] Audit current core/UI/tests and release/deployment paths; verify the actual published baseline and retain controls, shared C# runtime, gravity invariants and delivery optimizations.
2. [pending] Implement canonical dual-role characters, self-excluding Boss encounters, independent combat state, readable character attacks and journal/UI integration.
3. [pending] Implement finite mainline stances, compatible upgrades, effective repeatable growth, longer staged pacing and optional post-victory continuation with safe records.
4. [pending] Add focused regressions, balance and long-run/performance checks, fix only task-related failures, and complete localization/visual/native/Web validation.
5. [pending] Update version/changelog/docs, commit the tested source, build immutable self-contained Windows EXE and matching compatible Web artifacts, and verify exact exports.
6. [pending] Push and deploy using the established rollback-safe pipeline, verify public direct loading/gameplay/version, record artifact hashes and deployment receipts, then commit final records.

Setup notes: the persistent Node session had reset, so tool paths were reinitialized; ripgrep exit 1 for no AGENTS.md matches is handled explicitly rather than treated as a command failure. No workspace edits were lost.

---

## Legacy Playable Character / Boss Return — 2026-09-12

1. [complete] Verify the user-confirmed pre-rebuild design against the legacy document, character catalog/profiles, Boss director/resolver and test assertions; preserve the design-only boundary.
2. [complete] Restore explicit dual-role identity, per-run self-exclusion, enabled-content filtering, empty-pool safety and encounter contracts to the design. Separate recovered rules from new branch, pacing and gravity proposals.
3. [complete] Validated five Markdown files only, UTF-8 without BOM, all design links and whitespace. Deliver via a local documentation-only commit; no runtime tests, gameplay changes, push or release.

---

## Single-Mainline Growth Design — 2026-09-12

Scope: design documents only. Do not implement, rebalance, edit game assets/changelog/version, build, push, or deploy. Preserve historical design records and distinguish confirmed intent from proposed tuning.

1. [complete] Inspect actual run timing, upgrade availability and repeatable growth, plus the existing character/design documents.
2. [complete] Write an internally consistent design covering longer pacing, one mainline, finite branch decisions, compatible support, sustained growth and concrete character examples.
3. [complete] Reviewed reachability, exclusions, scaling and proposal scope; six Markdown files pass UTF-8-without-BOM, link and docs-only checks. Deliver as a local documentation-only commit; no push, gameplay implementation, build, export or deployment.

---

## Direct First-Download Optimization — 2026-09-11

1. [complete] Measured direct range transfer (16.9 KB/s), fast origin-local transfer, and 26 erroneously packed static archives. Preserve alpha-0.1.8 EXE/gameplay and all histories.
2. [complete] Implemented guarded export compaction (32.54 -> 21.61 MB; 289 runtime/game payloads unchanged), then measured severe CUBIC retransmission. A rollback-guarded BBR A/B/A trial proved the transport bottleneck; installed the two dedicated persistent files affecting new server TCP connections. No DNS, routes, qdisc, paid service or engine/gameplay changes.
3. [complete] Deployed alpha-0.1.8-7aca71e-delivery-20260911T151234Z; cold direct startup passes at 20.549/25.290s. All three public direct entrances pass, together with local four-mode gameplay, failures/cache, languages and dual-hero load gates; Python 20/20, JavaScript 22/22, font coverage pass. Original EXE/assets/releases and rollback state are retained; device/route variability is explicit.

---

## Alpha-0.1.8 Release — 2026-09-11

User explicitly requested publication after the color/flow correction. Keep this existing candidate version, ship one committed C# source to Windows EXE and compatible Web, preserve historical artifacts and logs.

1. [complete] Resolved the release template accidentally disabling Mono interpreter optimizations. Correct-mass high-load Marisa now passes unchanged thresholds at 60 FPS; core 60/60 and same-investment DPS remain valid.
2. [complete] Archived the old unpublished candidate; Windows EXE and optimized compatible Web were built and verified from clean 7aca71e. All 90 C# files match, Windows has 31 standalone checks, and both heroes pass all three loads in both languages.
3. [complete] Pushed and deployed alpha-0.1.8-7aca71e-20260911T133928Z. Three public browser entrances pass through the existing local proxy; direct header/routes pass but direct download timed out and remains explicitly unresolved. Receipts/hashes/history and limitations are recorded; final documentation commit does not change the shipped source.

---

## Original Color Effects — 2026-09-11

1. [complete] Inspect supplied TH10 star atlas and TH08 beam texture; retain original pixels and separate cosmetic RNG.
2. [complete] Implement multi-color stars and spatially flowing rainbow beam with pause/reduced-motion behavior. Native rendering and core regressions pass.
3. [complete] Fonts, native UI/core/rendering and fresh compatible-Web color/flow checks verified; source committed as 13a3d91. Actual browser frames archived as an Aseprite GIF preview. Full Web long-run gate still times out, so release remains pending.

---

## Marisa Many-Body Correction — 2026-09-11

1. [complete] Replace group casts/guaranteed planets with frequent single-star radial emission and independently sampled mass distribution. Add explicit enemy masses and reciprocal star-star/star-enemy gravity.
2. [complete] Make distribution and lifetime training repeatable; retain nearest-enemy beam tracking, finite remedies, original-first art and shared C# runtime.
3. [complete] Many-body, mass/lifetime, Boss attraction, matched-investment DPS, core 60/60 and native/Web release acceptance completed. Automatic route outcomes and real-device/network limits are recorded without claiming all seed routes win.
4. [complete] Corrected runtime and final 7aca71e artifacts pass unchanged gates and are published. The previous failed candidate (minimum 41 < 45) remains archived and was not deployed.

---

## Superseded Marisa Gravity Candidate — 2026-09-11

Implement the accepted randomized-mass star growth, sustained gravity damage, directional growing beam and limited mushroom/herb sustain in the existing shared C# game. Preserve Reimu, established controls/art, all release history and the compatible threadless Web renderer. Publish only after new validation: self-contained Windows EXE plus matching Web deployment. No deletions or speculative WebGPU work.

1. [complete] Audited runtime/release gates. Remote origin and pinned server confirm alpha-0.1.7; next version is alpha-0.1.8.
2. [complete] Implemented bounded random stars/planets, sustained damage, steering/resonant beams and finite remedies; shared rendering/probes; user requested original-first assets, so the active mushroom is now the verified original TH17.5 sprite, with the self-drawn draft retained only in art/.
3. [in_progress] Corrected independent per-star mass/damage and nearest-enemy beam tracking; new core 56/56, localization 997/997, native build/import/UI smoke and both heroes' three-seed journeys pass. Exact exported Windows/Web verification still required.
4. [in_progress] Changelog/version/docs updated without changing published history. Commit corrected sources, then build immutable Windows/Web artifacts, verify and publish/deploy.
5. [pending] Verify public deployment and final artifacts, record receipts and honest validation boundaries, commit final records.

Errors: rg is not on the inherited PATH; using Git grep/argument-array inspection. Initial substring patch failed because apply_patch requires complete lines; corrected the wrapper to expand exact matches to full-line contexts before applying. No partial edit resulted from that failed patch. One read-only Git grep expression had an unescaped bracket; corrected to a simpler alternation. CMD multiline/compound commands did not execute reliably in this host, so long jobs use direct argument arrays with logs and hidden child windows. Two NuGet attempts exposed missing APPDATA/ProgramData/ProgramFiles variables; restored explicit local environment (not dependency changes). New tests needed qualified Check calls because the top-level test helper shadows static imports.

---

## Full Actual-Game WebGPU Validation — 2026-09-10

Keep one unchanged C# game and alpha-0.1.7. Validate isolated exports and existing Windows EXE; no deletion, push, deployment or release replacement.

1. [complete] Pin and hash engine sources; integrate Mono and WebGPU in a separate engine copy, build and export the actual game. Title and both populated hero fixtures start on hardware WebGPU without isolation or shared memory.
2. [complete] Isolated output selection, hardware evidence and failure injection implemented; all 47 existing/new gate groups attempted. Core/native pass; real WebGPU acceptance fails. Marisa invalid low/medium fixtures, real high-load CPU problems, missing sprites/ground, readback, persistence and lifecycle failures are retained rather than suppressed.
3. [complete] Six actual-game image pairs reviewed: all fail equivalence, so WebGPU FPS is not comparable. Full report, 179-file input audit and final tool/UTF-8 checks pass their stated scopes; results and tooling are included in the local verification commit. Physical mobile remains untested and all four actual fallback/loss gates failed; investigation is concluded with NO-GO, not production approval or release.

Retained build failures: three-way support-list conflict; Dawn download/cache contention; missing native C++ compiler. Resolved with checked Mono/WebGPU union, private dependency caches and native g++ installation (zero packages removed). Original engine source and production artifacts remain unchanged; the first Dawn attempt touched the original Emscripten dependency cache before cache isolation was added. A CMD directory command failed parsing during resume; read-only rg/Node argument arrays replace it.

---

## WebGPU Feasibility and Local Experiment — 2026-09-09 UTC

Continue the requested next-version WebGPU exploration without releasing alpha-0.1.8, changing the live site, splitting gameplay, deleting artifacts or replacing the current compatible renderer. Preserve one C# game project, current assets and controls.

1. [complete] Audited official support and pinned tools. The candidate WebGPU fork lacks Mono support on Web, so no safe drop-in game switch is available; check actual browser hardware inside the isolated experiment.
2. [complete] Implemented the isolated real-texture WebGPU/WebGL2 comparison, reusable 16-float instances, pixel checks and explicit device-loss/unavailable fallbacks; the published C# game remains unchanged.
3. [complete] All 12 tests and six browser scenarios pass; documented measurements and the no-drop-in-engine conclusion in docs/webgpu_experiment.md. UTF-8/no-BOM, input-hash and unchanged-runtime audits pass. Delivery is a local commit only, with no version bump, push, export or deployment.

---

## Dual-Platform alpha-0.1.7 Release — 2026-09-09 UTC

Publish the restored alpha-0.1.6 game plus the explicitly permitted temporary AI hero-selection portraits. Deliver a self-contained Windows EXE and activate the matching threadless Web build. WebGPU is next-version research, not a renderer change in this release. Preserve all existing files, history, source artwork and release artifacts.

1. [complete] Confirmed release gates, 32 preserved artifact/source hashes and pinned SSH access; version/log/font are ready, and source/UI/loader/deployment regressions pass. Commit these release inputs before export.
2. [complete] Both targets built from clean 82563e6; 28 standalone EXE checks and all existing Web gates plus four portrait layouts pass. All 81 C# files and two PNGs match both stages.
3. [complete] Source pushed and deployment alpha-0.1.7-82563e6-20260909T164729Z activated with backup. Public desktop/touch/unisolated tests and extra bilingual portrait/hero/persistence checks pass.
4. [complete] Preserved all 32 original release/archive/source hashes; saved versioned changelog, receipts and validation boundaries. Final documentation records these completed results without changing published source inputs.

Errors: the first CMD rg compound command returned no output; switched to argument-array tools and bounded UTF-8 reads. No files changed by that command.
Built-in Windows ssh exits 255 even for -V with no diagnostic; Git-bundled OpenSSH succeeds with the same pinned host/key. Adding its entire bin directory to PATH also selected incompatible MSYS tar, so the final local deployment entry uses per-command SSH/SCP aliases and native Windows tar.
The new changelog requires three missing glyphs (U+4E34/U+822C/U+FF0B); regenerated the existing font subset without downloads and verified all 1250 glyphs.
The first batch fixture received OS port 4045, rejected by the browser before game startup; preserved the failure and reran on newly allocated listeners without changing assertions.
An exploratory Windows-glob search and one incorrectly prefixed screenshot path failed only to read; corrected the inputs. The combined archive patch exceeded Windows argument length and this entry does not accept stdin; bounded per-file apply_patch chunks succeeded. All failure logs are retained.

---

## Temporary AI Portraits on Hero Selection — 2026-09-09

Only the hero-selection illustrations are authorized to use the two archived AI boards. Preserve alpha-0.1.6 gameplay, all in-run artwork, the general Aseprite + original asset policy, archived originals and release/deployment state.

1. [complete] Extracted both main illustrations in Aseprite with hidden locked references and editable alpha-masked foregrounds; no redraw, rescaling or recoloring.
2. [complete] Isolated hero-selection layout in its own partial file, preserved original text/actions and 76-pixel touch targets; added portrait paths to both export staging lists.
3. [complete] Native build, 40/40 core tests, UI/portrait/language checks, unchanged font coverage and Aseprite source fidelity pass; four unisolated Web desktop/touch cases pass and native/Web captures are reviewed.
4. [complete] Recorded the narrow policy exception, editable source instructions, hashes and unreleased log; archives are unchanged. Include all verified changes in the local commit, without publish or deployment.

Errors: CMD grep yielded no matches; switched to argument-array Git. One diagnostic grep used unescaped parentheses; use literal patterns or valid expressions.
Aseprite preview via synchronous Node timed out without producing images; no process remained. Version check passes; retry through a logged CMD entry. Tasklist quoting failed in CMD; native process query through PowerShell 7 succeeded.
All four Web portrait scenarios passed, but the new verifier used an incorrect server teardown method and did not exit. Use the existing server.closeAllConnections/close convention, stop only that owned verifier process, and rerun.

---


## Restore Published alpha-0.1.6 — 2026-09-09

Current request supersedes all visual-rebuild plans below. Restore the actual published tree, not only its version string. Preserve files and history; do not release, push, deploy, or change branches.

1. [complete] Identify clean HEAD 072a2eb and published target e902d46; classify 42 modified and 871 added tracked files.
2. [complete] Copied 42 changed-file snapshots and moved 871 tracked additions plus 268 ignored imports to artifacts/rollback-alpha-0.1.6-from-072a2eb; verified all 1181 SHA-256 hashes without deleting files.
3. [complete] Restored published runtime, assets, build and validation tools; retained explicitly historical notes and archived the withdrawn unreleased changelog.
4. [complete] Build (zero warnings/errors), 40/40 restored core tests, Godot import, native UI smoke, 1247-character font check and three rendered scenes pass; shared runtime/build inputs match the release and the existing EXE matches its receipt hash.
5. [complete] Record results in docs/rollback_alpha_0_1_6.md and include these changes in the local rollback commit; no export/push/deployment.

Errors: the first quoted Git invocation through cmd.exe failed before changing files; switched to execFile argument arrays.
Font verification initially found no fontTools in the bundled Python; check existing Python environments rather than installing or changing the restored font.
Resolved with existing D:/_soft/Anaconda/python.exe; no dependency download or restored-source change.
Final whitespace review found one extra EOF blank line in the withdrawn log; corrected it before committing.

---


## Complete Reference-Driven Visual Integration — Complete 2026-09-09

The user explicitly requests the complete job, not another isolated sample. Complete both characters, all active visual categories, the shared camera/view integration, and actual Windows/Web verification. Do not add gameplay, change balance, publish, or delete historical sources.

1. [complete] Inventory all 54 active assets, presentation consumers, archived projection/atlas code and build/verifier assumptions.
2. [complete] Build a new all-Aseprite art pack with layered Reimu/Marisa portraits, four-direction walk/cast timelines, enemies, independent effects, courtyard/scenery, UI and icon; no input-reference pixels in visible drawing layers.
3. [complete] Generate character design boards and source/provenance manifest; inspect full-category previews and animation phases. Preserve all earlier art and manual edits.
4. [complete] Complete shared projection, foot-depth ordering, frame selection and view-relative controls while preserving ECS simulation and MultiMesh batching.
5. [complete] Route all consumers/build staging/export checks to the new pack; integrate portraits and keep settings, localization, compendium, minimap and touch controls intact.
6. [complete] Verify editable sources/exports, core and native rendering, then build and check threadless Web on desktop/touch layouts. Distinguish measured technical results from visual approval and physical-device FPS.
7. [complete] Save current design, update history and local commits. No release version bump, production EXE export, push or deployment unless separately requested.

## Reimu Front Pose Study — Prepared For Review 2026-09-09

1. [complete] Inspected the approved first front-facing pose at native dimensions, including an enlarged face comparison; no rejected base, guide scaling or source-pixel replay.
2. [complete] Constructed 03a and refined 03b in Aseprite from authored clusters. The current source has eleven editable drawing layers and two hidden/locked guides; the scripted method is explicit.
3. [complete] Reviewed side-by-side previews and refined the face, eyes, bow, hair and ribbon. Reopened source, guide exclusion, nearest-neighbor and non-overwrite checks pass. Visual differences and unapproved status remain explicit.
4. [complete] Preserved both sources, comparison images and truthful review notes for local delivery. No animation, runtime integration, version bump, release, push or deployment.

Scope completion is a reviewable single pose, not accepted art. Do not expand it into other directions or a full character pack without resolving the remaining reference-fidelity issues.

## Reference-Driven Game Visual Rebuild — Active 2026-09-09

1. [complete] Preserve corrected Reimu and full Marisa boards; archive sidebar-generated reference candidates and inspect the shared rendering boundary.
2. [in_progress] Faithful Aseprite tracing/redrawing is mandatory. Extraction/color cleanup cannot be shipped. The simplified Reimu front-01/front-02 coordinate studies were rejected; they are not completed character art.
3. [pending] Integrate the new view only with acceptable redraws. The unfinished camera/atlas prototype is preserved in artifacts/reference-camera-wip-20260909; the runnable game remains unchanged.
4. [pending] Validate final art and actual native/threadless Web captures. Only process checks for the rejected study and unchanged core regressions have passed, not new-view or art-quality acceptance.
5. [in_progress] Record the clarified fidelity requirement and rejected studies; local commit only, no version bump, release EXE, push or deployment.

Fixed scope: Touhou + wuxia + survivor; identity-preserving Reimu ofuda shots / orbiting yin-yang orbs / ground boundary, existing growth, balance and controls retained. Latest user clarification overrides the earlier extraction interpretation: all final new art requires faithful Aseprite tracing/redrawing, not cropped/generated pixels. Both Reimu and Marisa require full design-board scope.

## Startup Reference Correction — Active

1. [complete] Identify the supplied screenshot and preserve the matching existing Aseprite background through a shared painter.
2. [complete] Adjust paper UI and redraw two slimmer character samples in Aseprite scripts.
3. [complete] Generated with Aseprite; 54 round trips and exact reference-background equality passed. Native 14 checks, core 40/40, localization 860/860 and Web title desktop/small/touch, language and both character batch checks passed. Native title and Web touch captures inspected.
4. [complete] Local sample prepared for commit and presentation; no release or whole-art approval claim. Other art remains pending user direction.

## Unified Aseprite Redraw — Rejected Visual Direction

1. [complete] Audit active visual resources and establish one original pixel-art palette and provenance rule.
2. [complete] Draw all active visual categories from blank Aseprite canvases, preserving layered editable sources and animation-strip dimensions.
3. [complete] Route both targets and compendium to the new artwork; exclude reference images from exports.
4. [complete] Verified 54 Aseprite round trips, native 14 checks, core 40/40, localization 860/860, Web visual/UI/batch checks and both full-load language gates. Local-only delivery without a version bump or deployment.


## Reimu Playable Growth Tree — 2026-09-08

- Complete: stable upgrade definitions, build state and offer policy; Reimu starts with straight ofuda and explicitly unlocks other abilities/signature.
- Complete: six compatible behaviors, shared choices/build/journal descriptions and existing original-art rendering.
- Complete: 38 core regressions, six existing seeded journeys, independent native full smoke and three non-isolated Web layout/interaction checks. Human fun, route balance and physical mobile testing remain outside this automated acceptance.
- Local commit includes only growth work; concurrent localization work is preserved separately. See docs/reimu_growth_sample.md.
- Scope: Reimu sample only; Marisa keeps current gameplay until a later tree migration. No new controls, version bump, release export, push or deployment. No deletions.

## Gameplay Direction Audit — 2026-09-08

- Complete: recorded user report that stutter is resolved and redirected priority to fun, decoupling and worthwhile legacy ideas.
- Complete: compared legacy contracts and progression implementation with current candidate selection, linear ranks and journal/runtime dependencies.
- Complete: documented a bounded build-diversity slice, incremental decoupling and acceptance criteria. This task changes planning documents only; gameplay proposals are not implemented or released.

## Release alpha-0.1.4 — 2026-09-08

- Complete: clean source dd1eaf1 audited, live alpha-0.1.3 checked, pinned SSH host and private key located without reading key contents. GitHub HTTP/1.1 retry succeeded after initial reset.
- Complete: version/log promotion and source validations; commit before both exports.
- Complete: standalone Windows 24 checks, matching threadless Web release gates plus journal/minimap/scenery checks, push and backup-preserving deployment, public verification and receipt.
- Preserve all historical files; no cleanup/deletion.

## Card Compendium — 2026-09-08

- Audit complete: legacy six categories, source filters, immutable facts and runtime projections. Legacy src is excluded from current compilation.
- Complete: current-content catalog, original-asset cards and separate details; retain filters/page/focus and pause safety.
- Complete: native and Web navigation/visual checks, glyph subset and documentation. Local commit follows final diff review; no release or deployment.

## Web Minimap Fix — 2026-09-08

- Completed shared touch-aware minimap layout without moving touch buttons or changing gameplay/version.
- Native core 29/29 and UI/settings smoke passed, including actual visibility changes and desktop restoration.
- Threadless Web build 20260908-174744-434 passed four Edge browser layouts: desktop, phone DPR1, phone DPR3 and small touch. Pause/build input and return passed; mobile screenshot visually inspected. These are browser emulations, not physical-device performance tests.
- Report: artifacts/web-builds/20260908-174744-434/minimap-verification/2026-09-08T09-49-53-434Z/report.json. No deployment, push or release export.

# Task Plan: Runtime Alignment with Canonical Design

## Original Scenery Replacement — 2026-09-08

1. [complete] Inspect supplied tree, torii and shrine artwork; preserve native before captures and select usable transparent source textures.
2. [complete] Replace procedural scenery/title drawing with cached original resources, preserve gameplay and add source integrity checks.
3. [complete] Review real native/Web screenshots and before/after comparisons; source integrity, 29 core cases, UI, native/Web colors, five threadless Web scenes and DPR checks pass. Record provenance/changelog for local-only delivery without publication.
## Release alpha-0.1.3 — 2026-09-08

1. [complete] Promote the art integration to alpha-0.1.3, preserve previous releases and validate the source before committing.
2. [complete] Build and verify self-contained Windows and threadless Web from clean 4d6fb6b, including hero art and all existing release gates.
3. [complete] Push source, deploy through server Git fast-forward with backups, verify public access with explicit slow-start budget, and record hashes, failure evidence and limitations. Apply the user-requested landing-only network hint separately without changing game artifacts; public text/layout checks pass.

## Original Hero Art Integration

1. [complete] Inventory the supplied original pack and inspect player/effect atlases. Capture unchanged native battle/ability baselines; identify the remaining procedural beam, charge-star and seal placeholders.
2. [complete] Extract original textures with hashes and exact crop validation, integrate character-owned textures with correct blend/alpha handling, and compare actual game captures without changing combat.
3. [complete] Validate native and threadless Web rendering and color/bounds regressions; retain unchanged gameplay, record remaining art debt and prepare before/after evidence for local-only delivery without publication.

## Release alpha-0.1.2 — 2026-09-07

1. [complete] Promote the shared color repair to alpha-0.1.2 and preserve history; require color regressions of actual artifacts. Core 29/29, UI, font coverage, 18 JavaScript tests, deployment tests and PowerShell syntax pass.
2. [complete] Clean cf5e5c5 builds both targets. Final EXE passes 22 checks; Web 20260907-180715-471 passes raw downloads, normal/injected pixel regressions, all four gameplay suites, six loading cases and DPR 1/3 checks. First failed editor import and pre-UID EXE candidate are preserved.
3. [complete] Source pushed and server fast-forwarded. Backed-up deployment alpha-0.1.2-cf5e5c5-20260907T102610Z passes public desktop, touch/save and genuinely unisolated touch checks. Active metadata matches source and Windows hash; receipts recorded in docs/deployment.md.

## Black Sprite Follow-Up — 2026-09-07

1. [complete] Inspect alpha-0.1.1 screenshot and pinned GLES3 source. Ordinary polygon/batch interleaving passes locally; default-color fault injection fails on the original mesh (4926 pixels). Preserve this distinction from natural device reproduction.
2. [complete] Supply explicit white vertex colors on one shared ArrayMesh, preserving animation UVs, instance tint, bounds and batching. Native and injected unisolated Web dual-hero comparisons pass.
3. [complete] Final-source core 29/29, UI, native dual-hero pixel checks, normal/injected Web dual-hero checks and DPR 1/3 checks pass. Native static captures visually reviewed; eighteen JavaScript tests pass. Document evidence and limitations for a local-only commit, without release or deployment.

## Release alpha-0.1.1 — 2026-09-07

1. [complete] Promote shared sprite fixes to alpha-0.1.1, preserve released history and require dynamic batch validation of exported artifacts. Source core/UI, fourteen JavaScript tests, seven deployment tests and script syntax checks pass.
2. [complete] Clean 71e3220 builds both targets. Standalone EXE passes 22 checks including both batch journeys; threadless Web passes dynamic pixel comparisons, four full suites, twenty raw transfers, six loading scenarios and DPR checks.
3. [complete] Pushed, server fast-forward pull and backed-up deployment complete. Public desktop, touch/save/reload and genuinely unisolated touch pass; active metadata confirms exact source and Windows hash. Receipts recorded in docs/deployment.md.

## Shared Sprite Visibility Repair — 2026-09-07

1. [complete] Reproduce missing entities with full-battle pixel comparisons; isolate the batch path from simulation/assets, preserving before-fix evidence.
2. [complete] Maintain tight rotated instance bounds on submission and correct vertical UV mapping; keep batching and unchanged combat. Native core/UI/static and dual-hero dynamic rendering checks pass.
3. [complete] Same C# fix passes native and genuinely unisolated Web checks: each platform has 144 sprite comparisons and 24 full-battle comparisons across Reimu/Marisa victories. Final Web build 20260907-113430-145 also passes DPR 1/3 render checks; core 29/29, UI and five native render fixtures pass. Daily commit only, no publication.

## Compatible Dual-Target Release alpha-0.1.0

1. [complete] Confirm publication authorization, existing local/remote alpha-0.0.9 and preserved historical artifacts.
2. [complete] Promote release notes, connect the threadless deployment gate and correct version-independent changelog regression. Windows source smoke/core and seven deployment safety tests pass.
3. [complete] Build both targets from clean 07b96e7: twenty standalone EXE checks, twenty browser journey checks across four environments, twenty raw downloads, six loading scenarios and DPR 1/3 render fixtures pass.
4. [complete] Push source, server fast-forward pull, checksum/backed-up activation and three public browser scenarios pass. Record alpha-0.1.0-07b96e7-20260907T022831Z, artifact hashes, preserved initial diagnostic failure and physical-device limitations in docs/deployment.md. Follow-up test/documentation commits do not change the release artifact source.

## Mobile Performance, Hybrid Runtime And Original Assets

1. [complete] Audited the actual compiled runtime versus legacy ECS; measured equal-size simulation/render fixtures and inventoried source assets.
2. [complete] Added dense component storage and batch systems plus retained/MultiMesh rendering, preserving one C# project, OOP presentation and seeded mechanics.
3. [complete] Integrated traceable original TH10 combat/terrain crops and a new source-derived icon; preserved old assets and export versions.
4. [partial] Desktop/core/visual and six Web loading scenarios pass; final strict Web gameplay/performance gates still report an intermittent complete-body ERR_ABORTED also reproduced without the game. Preserve failing assertions and diagnostic evidence; save daily work without publishing. Physical-device FPS remains unverified.


## Web Loading Progress

1. [complete] Inspect actual Godot progress semantics; distinguish decoded engine counters from compressed transfers.
2. [complete] Implement exact compressed-body totals/received bytes/speed/ETA, separate initialization, stall/retry UI and failed-request gating without changing gameplay.
3. [complete] Five counter tests, seven deployment tests, six real-export loading scenarios and five full game scenarios pass on the final local build. Desktop/portrait/landscape captures inspected; history/version/EXE preserved. Daily development only, no push or deployment.

## Initial Domain Deployment

1. [complete] Confirm authorized target, SSH identity, DNS, Caddy and existing routes without changing the website.
2. [complete] Verified immutable assets, isolated Caddy route, gzip sidecars and backed-up activation deployed; source synced via authorized GitHub clone and subsequent fast-forward pull.
3. [complete] Real HTTPS headers, redirect, MIME, compression/cache, desktop and touch/save-reload/portrait checks pass; existing routes preserved. Screenshots inspected.
4. [complete] Exact deployed revision and rollback paths recorded; commits pushed as newly authorized, server receives final documentation via pull. No version bump or new Windows EXE.

## Active Shared Windows And Web Project — 2026-09-06

1. [complete] Establish shared C# project/build settings with isolated Web build caches; preserve Windows SDK and historical artifacts.
2. [complete] Implement portable font/profile storage, platform-aware settings/audio and multi-touch controls using the same runtime.
3. [complete] Windows 26/26 core/UI/profile/full settings pass; Web desktop/touch/save-reload/upgrade and both asynchronous boss-to-victory journeys pass. Physical-device limits documented.
4. [complete] Intent/design/unreleased records and final source/history/encoding audits complete; save as a local daily commit without release version bump, Windows release export, push or deployment.

Confirmed scope: one maintained game project and one C# gameplay implementation. Generated build staging is disposable build input, never a second maintained game. No game-rule changes or new account/backend features.

## Active CSharp Web Feasibility Probe — 2026-09-06

1. [complete] Verify experimental upstream, pin binaries and isolate the SDK/editor/project from the Windows release toolchain.
2. [complete] Real C# exports and browser/control probes finished. 4.6.1 control renders in current Edge; baseline 4.7.1 does not. JSON/fonts/touch/Boss and physical-mobile limits are explicitly recorded; no engine repair or production migration.
3. [complete] Report/unreleased notes and reproducible scripts finished; repeated browser probe confirms no-go (exit 2), script syntax/UTF-8/history checks pass, Windows regression and original SDK/EXE preserved. Save as a local daily commit, without version bump, release export or push.

Scope: C# remains preferred. No engine migration, production deployment, deletion or replacement of historical builds. Mobile simulation is not physical Android/iPhone validation.

## Active Release alpha-0.0.9 — 2026-09-06

1. [complete] Promote settings/pixel UI/F3 changes into release notes and align runtime/PE/export metadata.
2. [complete] Source regressions passed; exported a new standalone EXE without overwriting history.
3. [complete] Twenty isolated EXE checks passed including settings/F3; inspected captures, recorded hash and prepared final local release commit without push.

## Active Pixel UI And Debug Overlay — 2026-09-06

1. [complete] Audit current UI and attempt reference access; document unreliable web/Steam access and do not claim an exact visual match.
2. [complete] Create an original warm pixel UI skin and consistent menu/control/HUD presentation without copying reference assets.
3. [complete] Restore rebindable F3 diagnostic overlay with honest performance/world/combat information and no cheats.
4. [complete] Regression/rendered checks and validation notes finished; save as a daily commit without version bump/export/push.

## Active Settings Restoration — 2026-09-06

1. [complete] Audit legacy audio/video/dual-slot bindings and current runtime integration.
2. [complete] Restore settings with validation, persistence, current-action bindings and safe video preview.
3. [complete] Test migration, input routing, rollback and rendering; append unreleased changelog and prepare the daily Git commit without export/push/version change.

Scope: restore functional settings, not absent map/debug gameplay. No deletion of historical files. Existing alpha-0.0.8 EXE remains untouched; this request is settings restoration, not an explicit new release export.

## Active Delivery alpha-0.0.8 — 2026-09-06

1. [complete] Replace shared sword/lightning identities with character-owned ability catalogs and shared, inspectable tuning.
2. [complete] Implement homing ofuda, yin-yang orbs, sealing field, star volleys, stardust and a telegraphed sustained Master Spark; differentiate signature spells and visuals.
3. [complete] Update character/build/HUD text, add ownership/geometry/pause regressions, run full journeys and inspect real render captures for both heroes.
4. [complete] Preserve old changelog and releases, export alpha-0.0.8 as a standalone EXE, isolate-test it and commit the delivery.

Canon sources are recorded in docs/character_identity.md. This is an explicitly labeled survivor adaptation, not a frame-accurate reproduction. No new mandatory attack buttons or unrelated world expansion.

## Active Character Identity Audit — 2026-09-06

1. [complete] Read THBWiki character entries and source-linked spell/shot documentation for the two currently playable characters.
2. [complete] Audit the current shared sword/lightning loadouts against those sources and define character-faithful replacements.
3. [complete] Record the corrected creative direction and source-backed redesign boundaries; preserve the released EXE and history, without pretending the implementation is already fixed.

## Active Iteration alpha-0.0.7 — 2026-09-06

1. [complete] Inspect input, UI, profile and diagnostic seams; retain useful habits rather than legacy architecture.
2. [complete] Implement consistent pause/back navigation, character/build inspection, volume controls and embedded historical changelog.
3. [complete] Extend regression/smoke coverage, verify actual rendering and preserve historical changelog text.
4. [complete] Export and isolate-test a new self-contained EXE; commit all source/document changes without replacing previous releases.

Scope excludes infinite maps, content inventory migration, permanent stat progression and Web migration. New combat mechanics remain experimental, not endorsed by this usability iteration.

## Active Assessment — 2026-09-06

1. [complete] Verify official Godot Web/C# constraints and this project's actual platform assumptions.
2. [complete] Compare pre-rewrite snapshot d229b36 (alpha-0.0.5 runtime) against 41a893a (alpha-0.0.6).
3. [complete] Separate genuinely new mechanics from retained, removed, simplified and unverified capabilities.
4. [complete] Deliver a source-backed comparison and bounded deployment recommendation; no gameplay or deployment changes.

User additionally reaffirmed append-only historical release notes. The six previous version sections were
verified identical to d229b36; alpha-0.0.6 now documents actual additions, simplifications and removed features.
The complete changelog is also attached as release/CHANGELOG.md without replacing the exported executable.

## Active Delivery Plan — 2026-09-06

1. [complete] Verify historical executable naming and release/version policy against Git and CHANGELOG.
2. [complete] Restore alpha-0.0.6 naming, update export metadata and preserve all previous binaries.
3. [complete] Export a self-contained Windows x86_64 executable with embedded resources and .NET outputs.
4. [complete] Copy only the executable to an isolated directory, test headless/UI and real rendering, then deliver its exact path.

The user now explicitly authorizes export. No deletion, remote push or public distribution is authorized.

Export note: packaging completed but the console wrapper remained attached to its shared C# compiler server.
A graceful `dotnet build-server shutdown --vbcscompiler` released it with exit code 0. Future export disables shared compilation.

## Active Plan: Clean Rewrite — 2026-09-06

Old sections below are historical. Current product authority: `docs/rebirth_design.md`.

1. [complete] Preserve the old project in Git; confirm only Touhou + wuxia + survivor is immutable.
2. [complete] Design a compact complete game; inspect local Godot, .NET and reusable art.
3. [complete] Implement independent deterministic combat, progression, encounters and tests in `game/`.
4. [complete] Implement presentation, menus, audio, persistence and the new entry scene.
5. [complete] Build, run deterministic tests, verify real Godot rendering and correct regressions.
6. [complete] Record results, commit every coherent change and deliver a playable local version.

No files are deleted. Legacy implementation stays out of the new compile graph.
Long validation runs use a CMD log window and wait for completion. Export was initially deferred; the active delivery request above now explicitly authorizes it.

### Current Session Errors

- New piercing regression exposed rank-zero swords still firing because only other weapons checked unlock rank. Add the same explicit unlock guard to sword casting; the failed run stopped before fresh UI tests.

- Camera clamp introduced an ambiguous Godot Vector2 overload with target-typed `new`; use explicit `new Vector2` arguments. This validation stopped at build, so prior core/UI logs were stale and not new evidence.

- CMD quoted commit messages split unexpectedly in the preceding turn; use ASCII hyphenated messages.
- Multiline apply_patch through its batch shim lost the final line; invoke the same bundled patch engine directly via PowerShell 7.
- PowerShell mixed object table formatting hid asset paths; emit paths as strings.
- A guessed asset mapping path did not exist; use the verified base asset paths directly instead.

## Current Goal - 2026-08-17

Optimize the playable game against the canonical documents in `docs/`. Close code-backed gaps in the
five-minute base loop, adaptive enemy pressure, build progression, plugin boundaries, and hybrid
ECS/OOP responsibilities without turning optional content packs into vertical progression.

## Current Phase

Complete - strict content identity, frozen run fingerprints, capability-driven spell ownership, and
endless-pressure continuation are implemented, documented, tested, and ready to commit.

## Acceptance Criteria

- Base-only play reaches a coherent Boss encounter in about five minutes through measured adaptive
  pressure, not hidden time-based enemy stat inflation.
- The 30-second sliding-window K/S gate, pressure rate, enemy mix, and endless continuation agree
  between runtime, F3 diagnostics, tests, and `docs/enemy_balance.md`.
- Player growth comes only from explicit upgrade choices; finite ranks, barrage presentation, spell
  slots, and resolved values agree between combat, the E build view, and `docs/combat_balance.md`.
- Optional content packs only add parallel regions, structures, enemies, characters, and spell-card
  choices; core rules remain owned by Base.
- High-frequency entities remain ECS data/systems while low-frequency orchestration remains OOP;
  communication crosses explicit commands, events, and snapshots.
- Focused and full regression, SourcePolicy, UTF-8/BOM checks, and `git diff --check` pass.
- Every accepted change is committed without staging unrelated workspace edits.

## Active Phases

- [x] Audit all canonical runtime contracts and identify measurable code gaps.
- [x] Select the smallest coherent implementation set that materially improves the base loop.
- [x] Implement the selected gameplay and architecture corrections with focused tests.
- [x] Validate the five-minute loop, adaptive pressure, upgrades, UI projections, and plugin isolation.
- [x] Update documentation and `.NOTE.md` only where implementation truth changes.
- [x] Run frozen regression and commit all accepted changes while preserving unrelated edits.

## Decisions

| Decision | Rationale |
|----------|-----------|
| Treat canonical docs as executable contracts | Optimization is complete only when production code, UI, and tests agree with the same rule. |
| Preserve horizontal content packs | A pack may add choices and presentation, never stronger global rules or required progression. |
| Use ECS and OOP by workload | Bulk combat stays data-oriented; scene flow and rare orchestration remain Nodes/services. |
| Prioritize observable loop failures | Fix pacing, upgrade choice quality, Boss access, and diagnostics before adding more content. |
| Establish identity before domain migration | Strict headers, capabilities, and fingerprints provide a stable boundary without pretending all catalogs are externally data-driven. |
| Project endless through the shared snapshot | One coordinator timestamp fixes spawner and F3 behavior together and avoids a parallel difficulty clock. |

---

# Historical Plan: Documentation Consolidation and Architecture Alignment

## Outcome - 2026-08-17

Canonical documents were consolidated and validated. The two redirect-only files were deleted after
explicit approval in commit `3a0c9fa`; the retained authorities are indexed by the README and `.NOTE.md`.

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
## Dual-Target Release alpha-0.0.10 (Historical Candidate; Superseded by alpha-0.1.0)

1. [in_progress] Prepare the next optimization version and release notes while preserving all earlier releases; explicitly exclude unimplemented shared-memory-free compatibility from release claims.
2. [pending] Validate the same committed gameplay revision for self-contained Windows export and Web, using the actual deployment resource path without hiding request failures.
3. [pending] Export and smoke-test the new EXE, push the release source, deploy with server Git fast-forward and preserved rollback backups, and verify the live website.
4. [pending] Record artifact checksums, deployed revision, limitations and rollback paths; commit/push the final delivery record.
## Sprite Drawing Tutorial Study — Complete 2026-09-09

1. [complete] Read original-author sprite/anatomy/animation and pixel-cluster tutorials plus official Aseprite animation documentation; distinguish text/static-image reading from watching complete demonstrations.
2. [complete] Record project-specific construction and review steps in docs/sprite_drawing_study.md without replacing the approved character designs or abilities with tutorial examples.
3. [complete] Archive the finished native-v01 automatic reference replay with explicit non-production provenance; both portraits have transparency concerns and all movement outputs remain separate single-frame files.
4. [complete] Update intent notes and unreleased change history; no runtime edits, version bump, release, push or deployment.

The original drawing request remains unfinished. Aseprite automation has reproduced reference pixels, not established drawing quality or completed animation. Heuristic region layers are not accepted anatomical layers. The next drawing gate is one faithful in-game pose, followed by reviewed directional and animated work; do not restart bulk replay as a substitute.
## Full Game WebGPU Validation — 2026-09-09 UTC

1. [complete] Backported the fixed WebGPU driver into a separate copy of the pinned Mono 4.6.1 engine; all 1,296 files merge, both backends and C# compile, and a threadless release template is produced. Existing game/engine source remains unchanged and WSL is closed after each run.
2. [in_progress] Export the actual alpha-0.1.7 game with the integrated template; run Windows/Web functional baselines and actual WebGPU startup checks in isolated test profiles without duplicating gameplay.
3. [pending] Exercise actual-game rendering, combat, settings/save/input/texture and fallback gates where runnable. Compare only equivalent real backends; mark unsupported engine paths and unavailable physical mobile explicitly, never as passed.
4. [pending] Preserve logs/artifacts, add reproducible validation evidence and outcome documentation, audit no-release boundaries, and commit locally.

---
## Marisa Growth Rebuild — 2026-09-10

Rebuild Marisa's upgrade route in the shared C# game, following the accepted basic-attack-first, unlockable branches and compatible combinations model. Preserve Reimu, inputs, assets/art policy, one Windows/Web project, alpha-0.1.7 and production artifacts. This is local development, not publication or renewed WebGPU work.

1. [complete] Inspected growth/abilities/UI/tests and existing character-source references; defined stars, stardust and beam unlock/refine/compatible behavior routes.
2. [complete] Implemented separate Marisa state/tuning/casting/projectile/beam systems, bilingual UI and journal, bounded mechanics tests and hero-correct continuous diagnostics. Core 49/49 and initial 16 equal-budget arenas pass; nine route-priority pilots include one loss, retained as evidence rather than hidden.
3. [complete] Current-source core 49/49, localization 933/933, Windows UI/language/five captures, four bilingual desktop/touch Web flows, persistence and all three continuous combat budgets pass. Documented first-pass balance and physical-mobile limitations; audited 184 staged inputs, unchanged release/history and UTF-8. Implementation and evidence are included in the local completion commit; no push, version bump, deployment or replacement of release EXEs.

---
