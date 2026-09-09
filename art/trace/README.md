# Rejected Tracing Studies

The later `reimu_front_03a.aseprite` and `reimu_front_03b.aseprite` are new single-pose studies with their own review status, not replacements for approved runtime art. See `reimu_front_03.md` for the current sample, method, editable-layer layout and limitations.

The newer `native-v01/` directory is a separately labelled automatic reference-replay experiment, also not accepted redrawing or runtime art. See its README and `docs/sprite_drawing_study.md`; no script or layer-count check establishes visual approval.

These files are retained for audit and possible manual inspection, not accepted game artwork.

- `reimu_front_01.aseprite`: first coordinate-based reconstruction, not a faithful trace.
- `reimu_front_02.aseprite`: eye/detail iteration, still rejected for simplifying the supplied reference. Eight populated drawing layers and one hidden, locked reference layer; layer correctness is not visual acceptance.
- No study is installed in the running game. Both the current production art and historical sources remain intact.

The intended standard is fidelity to the user-supplied boards in `art/reference/gameplay_reference_manifest.json`, including Marisa’s full design-board scope. Do not ship these studies or copy their simplified silhouette into new characters.

The current generator is an Aseprite-native script using explicit drawing coordinates, not manual mouse painting. It requires `allow_rejected_study=true` for audit reproduction and refuses to overwrite existing source files.

To inspect/export a manually edited copy, run Aseprite with `--batch --script-param source=<copy.aseprite> --script-param output=artifacts/<review-directory> --script tools/aseprite/export_trace.lua`. The exporter suppresses the labelled reference layer and does not save over the source. Its report is a process audit, never an art-quality approval.
