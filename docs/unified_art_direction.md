# Unified Aseprite Visual Direction

## Fidelity rejection and Marisa reference — 2026-09-09

The user rejected the simplified tracing study: resemblance is not sufficient. Preserve the supplied reference proportions, silhouette, facial placement, costume construction, folds, palette and detail instead of replacing them with approximate polygons. The Reimu front-01/front-02 studies are rejected, not approved tracing examples. Their valid layer structure and reference-independent exports do not establish visual fidelity.

Marisa now has an explicit full reference board at `art/reference/marisa-gameplay-style-approved.png`. Deliver the same complete design scope as Reimu: full-body illustration, directional gameplay sprites, casting presentation, isolated effects, palette and genuinely editable Aseprite sources. The earlier generic witch walking sheet is superseded as her design basis. Image captions about herb fields do not authorize gameplay changes.


## Mandatory tracing/redrawing — superseding clarification, 2026-09-09

The user now explicitly requires **Aseprite tracing and redrawing**. Generated images are reference only, including the sidebar ChatGPT outputs and the corrected gameplay board. Cropping, keying, resizing, palette reduction, separating imported pixels into layers, or saving an image as an Aseprite file does not satisfy this requirement. The earlier permission to split the board is not permission to ship its pixels.

- Create the visible artwork on blank Aseprite layers. Redraw silhouette, form, material, folds and pixel clusters rather than merely resampling the reference. Preserve the approved pose, character identity and visual language instead of redesigning it into a generic style.
- Reference layers must be clearly labelled, locked and omitted from exports. Body parts and materials need meaningful editable layers; do not present cutout layers or empty layers as a completed redraw.
- Aseprite-native script drawing is identified as such, not described as manual mouse strokes. Pixel provenance and visual quality are separate acceptance criteria. Reference-independent output alone does not prove a pleasing or faithful result.
- `art/trace/reimu_front_02.aseprite` is an unapproved single-front-frame process study. No full animation, new gameplay camera or complete game redraw has shipped. `tools/aseprite/export_trace.lua` forces reference layers off without overwriting the source.
- The corrected board is now the overall game/camera reference, not just a character reference. Keep the existing playable artwork until actual redraws are ready; do not temporarily replace it with keyed AI sheets. The camera integration prototype is archived in `artifacts/reference-camera-wip-20260909/`.


## Latest gameplay reference — 2026-09-09

The user explicitly approved the viewpoint and visual style in `art/reference/reimu-gameplay-style-approved.png` and requested corresponding in-game changes. This supersedes earlier character/gameplay styling where inconsistent; it is not permission to copy every mechanic, caption, pixel dimension, or layout in the reference. Preserve the previously approved startup scene until separately changed.

- View: top-down 2D with an oblique view of standing characters and scenery, not a side-scroller or a mandatory 3D/isometric conversion. Keep feet, silhouettes and front/side/back directions readable.
- Scale: draw small gameplay sprites independently from the full-body illustration. The reference caption of 32–48 pixels is a design clue, not a locked export specification. The existing 48-pixel strips describe the current implementation, not a requirement to retain one-direction artwork.
- Palette and environment: ink-blue shadows, warm ivory, vermilion, subdued stone paving, moss, restrained red foliage and warm shrine lamps. Environment detail must remain quieter than hostile bullets and actor silhouettes.
- Ability correction: orbiting objects are yin-yang orbs, NOT ofuda. Ofuda are fired attacks. Unlocking the orb grants its existing orbit behavior; clearing bullets and charged launches remain separate growth branches. The ground boundary retains its existing placement and control behavior. Do not copy the reference caption or persistent player-centered ofuda formation as a new mechanic.
- Effects: distinguish friendly orbs, fired ofuda, ground seals and enemy bullets by shape as well as color. Avoid opaque rings hiding the player, hitbox or incoming bullets.
- First proposed in-game sample: Reimu front/side/back movement, yin-yang orbit, fired ofuda, the boundary, and one shrine-ground patch viewed together. Review this at actual gameplay scale before extending to other characters, enemies, scenery or HUD.
- Production: the user permits image editing followed by Aseprite correction and another image-editing pass. Preserve immutable references and versioned outputs; keep real editable Aseprite sources and separate AI reference layers. Do not label copied/cut layers as a full redraw.

This update records an approved visual target and a proposed implementation slice. No runtime art or game behavior has been replaced in this step. The existing implementation already uses yin-yang orbs for orbiting; no gameplay bug is claimed.

## User reference correction

The user rejected the visual direction in 6917463. Technical checks were not aesthetic approval. The supplied startup screenshot (art/reference/approved-startup.png) is the visual reference: ink-blue layered mountains, muted foliage and blossoms, warm lamps, brown/gold paper panels and a pale-green primary button. Reuse the existing project-drawn Aseprite title landscape exactly. Redraw Reimu and Marisa with slimmer proportions rather than importing original sprites. This correction is a startup/UI/two-character sample; other artwork remains pending user review. No gameplay, version or deployment changes.

### Correction verification

- Native capture and 14 checks: artifacts/redraw-native/20260908-200655-393.
- Threadless Web: artifacts/web-builds/20260909-040756-633; title desktop/small/touch, language persistence and Reimu/Marisa batch checks passed.
- 54 Aseprite source/export comparisons and exact approved-background pixel equality passed; core 40/40 and localization 860/860 passed.
- This verifies rendering and behavior, not user aesthetic approval or mobile hardware performance. Existing audio import emitted UTF-8 metadata warnings; no audio files were changed.

## Superseding rule

The user requires all active game imagery to share one redrawn style. Original Touhou images are visual references only, not runtime images, crops, recolored exports or collage elements. Historical references and previous releases remain untouched. Audio is outside this drawing task and stays unchanged. Fan-character identity is retained; redrawing does not establish permission to commercially distribute Touhou-derived content.

## Art language

- Moonlit shrine pixel art: deep teal and ink-blue shadows, warm paper light, vermilion identity accents, restrained gold.
- Hard pixel contours and stepped highlights. Texture detail stays quieter than characters and bullets; no continuous blurred bloom or high-frequency ground noise.
- Reimu keeps her red bow, shrine-maiden costume, ofuda, yin-yang orbs and seals. Marisa keeps her witch hat, broom, golden stars and luminous magic cannon.
- Dark lacquer UI and warm paper surfaces share corner inlays, focus marks and compass motifs. UI state distinctions and readable text are preserved.
- Character/enemy sheets retain 192x48 dimensions and four 48x48 poses. The beam retains its 256x128 cap/body layout. No new per-entity scene nodes, collision geometry or gameplay changes.

## Active inventory

All 54 textures are generated inside Aseprite from blank canvases and saved with editable layers. There are seven player/enemy strips, ten combat textures, seven effects, four scenery images and 26 UI/environment/icon textures.

- Editable sources: art/redraw/, indexed by art/redraw/manifest.json.
- Runtime PNGs: assets/aseprite/redraw/.
- Central C# paths: game/presentation/VisualAssets.cs. The journal reads the same resource family as gameplay.
- Drawing entry: tools/aseprite/draw_redraw.cmd. Its Lua modules use the local Aseprite image API only; they do not open or read reference pixels.
- Source/export verification: tools/aseprite/verify_redraw.cmd. It checks Aseprite format, all 54 round trips, dimensions, nonempty animation frames, reference-image inequality and runtime path/export boundaries. Pixel inequality alone is not a proof of creative originality; the no-image-input drawing pipeline is also checked.
- Both build scripts run that verification and stage only the redrawn image family, fonts and unchanged audio. Historical images are excluded from export presets as well.

## Scope and acceptance

This is a development change on alpha-0.1.6, not a new release. Existing EXE files and the public deployment are unchanged until the user requests publication. Fonts, progress gauges, aim warnings and dynamic diagnostic geometry are functional rendering, not replacements for illustration assets.

Artwork round trips and automated UI checks do not establish artistic preference. Native and Web captures are retained for review; physical-phone performance is not inferred from desktop touch emulation.

## Validation evidence

- All 54 Aseprite source/export round trips passed; report: artifacts/aseprite-redraw-verification/report.json.
- Core 40/40 and localization 860/860 passed. Native snapshot artifacts/redraw-native/20260908-194232-838 passed 14 checks, including ten real screenshots and both heroes under batch/color comparisons.
- Final Web build artifacts/web-builds/20260909-034138-085 passed both hero batches, five skill views, four journal layouts, bilingual persistence and three title layouts. The prior integrated draft also passed five scenery layouts before the final ground-noise reduction.
- Existing full-load desktop reference gates passed in both languages: Chinese mean 58.93 FPS (minimum sample 54), English 59.95 (59). No entity limits, damage or simulation timestep changed. Raw reports are artifacts/redraw-final-combat-zh.json and artifacts/redraw-final-combat-en.json.
- These are development snapshots with preserved source manifests, not newly published artifacts. The live alpha-0.1.6 release remains unchanged.
