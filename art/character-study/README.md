# Character Proportion and Costume Study

Status: design study, pending user review. Not runtime artwork or an approved animation sheet.

## Reference and constraints

- The user-provided idol Koishi image is stored at art/reference/koishi-idol-user.png for visual reference only. No reference pixels are imported by the character painter. Its identity-preserving theme transformation is the reference, not its chibi anatomy or idol accessories.
- The user explicitly allows non-chibi characters. These studies use approximately five-to-six-head proportions, a shared outline/shading treatment and the previously approved moonlit startup palette.
- Reimu retains the red/white shrine-maiden silhouette, bow, detached sleeves, gohei and ofuda. A restrained waist tie and trailing cloth add movement without turning her into a sword fighter.
- Marisa retains her pointed hat, blonde hair, black/white dress, apron, broom and star motif. Belt, cloth folds and an off-center pose provide the theme accents; no sword or lightning substitution.

## Files and viewing

- reimu.aseprite and marisa.aseprite: editable six-layer 128x192 drawings, one pose each, drawn from blank Aseprite canvases. Matching PNGs are transparent exports.
- comparison.aseprite and comparison.png: design board. Main figures are enlarged 2x with nearest-neighbor pixels. BEFORE / 48 shows the first frame of the project's existing redraw, not an original Touhou image. STUDY / 48 and STUDY / 144 show reductions to those canvas heights.
- The reduced images are scale studies, not engine screenshots. They reveal detail loss; small runtime sprites must be deliberately simplified and animated after the design direction is approved. Do not automatically replace the current 48x48 frame strips with these illustrations.
- Run tools/aseprite/draw_character_study.cmd from the repository to recreate this initial study using Aseprite. This overwrites the study files; preserve any later manual source edits before rerunning.

## Scope

No runtime textures, C# code, character abilities, balance, version number or deployed files change. No new playable character, full animation set, export or deployment is included. Technical source/export checks do not establish aesthetic approval.

## Verification

All three Aseprite files were reopened and exported, and their decoded PNG pixels matched the checked-in exports exactly. The existing 54 runtime assets also passed their source/export verification unchanged. The comparison was visually inspected, including the 48-pixel reduction. No native/Web rebuild or performance claim is needed for these unconnected design files.
