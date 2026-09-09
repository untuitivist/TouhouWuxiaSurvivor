# Automated Reference Replay — Not Accepted Redraws

This directory is an archived reference-reproduction experiment, not approved artwork or a finished animation pack. All assets remain excluded from the running game.

## Contents And Method

- Each character has a complete reference board, a transparent portrait, and twelve independent movement-frame files: 28 editable Aseprite sources in total.
- The script replays source pixel positions and colors with the native Aseprite pencil. It does not resize or quantize the reference. This is automated copying, not manual painting, independent redrawing, or proof of drawing skill.
- A hidden, locked layer retains the original guide. Visible layers are divided by heuristic image regions; their names do not guarantee clean anatomical or occlusion-based separation.
- The movement files each contain one frame. They are not validated walk cycles. Effects and gameplay panels embedded in the complete board remain reference images, not implemented skills or real screenshots of this game.

## Review Status

- Whole-board pixel equality verifies reproduction only.
- Transparent variants use a color rule and approximate polygons. Comparing output with that same mask cannot establish that the mask is correct.
- Visual inspection of both portraits found scenery fragments and boundary concerns. Transparent edges, clothing preservation, layer separation and real animation remain unaccepted.
- `manifest.json` records the actual method, selected-pixel checks and `runtime_eligible=false`. Technical checks must not be relabelled as art approval.

## Files And Editing

- Editable sources: `reimu/*.aseprite` and `marisa/*.aseprite`.
- Local previews and process log: `artifacts/native-v01/` from the project root.
- Generator and reproduction parameters: `tools/aseprite/draw_native_trace.lua`, `native_trace_engine.lua`, and `native_trace_layouts.lua`. The generator refuses to overwrite completed editions or existing source files.
- Use Save As with a new filename for manual work. Do not rerun the generator over hand-edited sources. Do not start a new million-stroke replay merely to claim redraw progress.
- The corrected production method and acceptance checklist are in `docs/sprite_drawing_study.md`. Accepted Aseprite redrawing remains unfinished.
