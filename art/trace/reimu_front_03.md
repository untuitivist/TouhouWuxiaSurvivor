# Reimu Front Pose Study 03

Status: single-pose drawing study for review; not approved runtime artwork.

## Open And Edit

- Current sample: `art/trace/reimu_front_03b.aseprite`.
- Earlier iteration retained: `art/trace/reimu_front_03a.aseprite`.
- Open either source directly in Aseprite. Use Save As with a new name for personal edits; the generator refuses to overwrite existing sources.
- 03b contains eleven nonempty editable drawing layers, a hidden/locked anatomy guide, and a hidden/locked original reference crop. The eye layer is separate from the front hair for local adjustments.
- Frame canvas: 112×60, matching the reference crop at (704,503) in `art/reference/reimu-gameplay-style-corrected.png`. The first front-facing movement pose is the target, not a resized large portrait.

## What Was Actually Done

- New visible pixels were constructed with explicitly authored paths, strokes and pixel clusters applied through Aseprite scripting. This is not human mouse painting and not an automatic replay of reference pixel positions/colors.
- A small set of material colors was sampled for reference; the palette is explicit in the script. The reference image is loaded only after the standalone drawing PNG has been exported, to add the hidden guide and comparison view.
- The old 48×48 rejected study and native-v01 automatic reproduction were not used as drawing bases.
- 03b refines the boxy lower face, eye positions, bow-edge segments, hair highlights and overlong neck ribbon found in 03a.
- The silhouette, hair locks and fabric rendering still differ from the reference. This is not a pixel-identical copy, completed character animation, full design sheet or proof of user acceptance.

## Review Files

- Local comparison: `artifacts/aseprite-tracing/reimu-front-03b/comparison-4x.png`; left is the supplied reference, right is the new scripted drawing. The reference panel is not new art or an actual game screenshot.
- Native transparent preview: `artifacts/aseprite-tracing/reimu-front-03b/reimu-front.png`.
- Enlarged preview: `artifacts/aseprite-tracing/reimu-front-03b/reimu-front-8x.png`; integer nearest-neighbor enlargement, not a different-resolution sprite.
- Per-run provenance and source audit: `report.json` and `source-audit.json` in the same local preview directory.

## Reproduction And Validation

- Drawing recipe: `tools/aseprite/reimu_front_clusters.lua`. The current recipe is 03b; the retained 03a source is its own editable historical snapshot.
- Driver: `tools/aseprite/draw_reimu_front_study.lua`; a new ASCII `edition` is required if a source already exists.
- Source check: `tools/aseprite/verify_front_study.lua`. It checks a reopened source, layer visibility/editability and exports an in-memory copy without guides; it never saves over the original source. RGBA equality of the exports is a separate read-only check.
- All drawing, source creation and image exports use Aseprite. No Pillow/canvas/SVG image drawing, new image-generation model or bulk reference replay was used.
- No runtime assets, C# code, gameplay values, view projection, version, release exports or deployment changed.
