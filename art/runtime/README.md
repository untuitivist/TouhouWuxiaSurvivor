# Runtime Aseprite Artwork

## Scope and provenance

Only two art sources are used: user-supplied original Touhou artwork and artwork created in the installed Aseprite. Original actors, bullets, abilities, grass, trees and torii remain unchanged. The existing title source lives in art/title.

These 21 layered files replace runtime-generated UI, legacy procedural stone, decorative scenery and touch artwork. The game icon combines newly drawn Aseprite framing with original TH10 orb/ofuda/star crops from assets/internal_original/base/combat. Their source coordinates and source hashes remain in that directory's source_manifest.json; its legacy branding output is no longer the active icon. Original-pack provenance does not grant public redistribution permission.

Each image is created inside Aseprite by tools/aseprite/draw_runtime.lua using Aseprite Image/Layer pixels. This is script-assisted pixel art, not a claim of mouse-drawn artwork, and no external PNG renderer is used. The script is initial construction history: do not rerun it over manually revised sources. Edit the .aseprite files in Aseprite and use tools/aseprite/export_runtime.cmd to export PNGs into assets/aseprite. The title has a separate exporter.

## Rendering contract

- UI frames: 64x64, 14px nine-slice margins, details confined to corner/border regions, flat stretch center. Existing control layout and interaction states remain intact.
- Stone: 24x24 seamless staggered slabs, displayed at 48x48 through the existing terrain batch. No new per-tile nodes.
- Shrine marker: 32x48 with an offering, mapped to the existing seal location; no collision or interaction changes.
- Petal and shadow: cached static textures; only position/opacity varies at runtime.
- Touch discs and grip: static textures, unchanged touch radii, ownership, cancellation and movement response.
- Icon: 256x256 layered composition, used by both native and Web presets.
- Text, bars, progress arcs, minimap symbols, collision points, direction/attack warnings and F3 remain data-driven UI/diagnostics, not substitute illustrated art.

Windows and Web share assets/aseprite. art/* is excluded from builds; the source remains in Git. Deprecated assets/branding and assets/world stay on disk but are excluded from maintained exports. The old external UI generator is historical tooling, not the current production workflow.

## Verification

2026-09-08: all 21 sources round-trip pixel-identically; native build and 38 core tests plus UI/settings smoke pass. A staged-only snapshot also builds and passes 38 core tests and font coverage without the separate uncommitted localization changes. The working-tree Web build at artifacts/web-builds/20260908-221921-713 passes title and growth navigation at three viewport/touch configurations each, with isolation and SharedArrayBuffer disabled. Working-tree screenshots and Web checks include the parallel localization work; they are not a clean formal release or a physical-device test.

Run tools/aseprite/verify_runtime.cjs with Node and the bundled pngjs dependency to re-export each source through Aseprite and compare runtime RGBA pixel-for-pixel. The report is artifacts/aseprite-runtime-verification.json. Run tools/rebirth/verify.cmd for core and native UI regressions. Visual captures live under artifacts/aseprite-runtime-*.png. Automated passes do not establish visual quality or physical-phone acceptance.
