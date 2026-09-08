# Unified Aseprite Visual Direction

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
