# Complete Aseprite Visual Pack

## Scope and status

2026-09-09. The active edition is shrine-v04. This replaces the previous runtime pack, not the gameplay simulation. All 64 entries have editable Aseprite sources. Earlier studies and editions remain untouched. This is authored drawing performed by Aseprite Lua scripts, not manual mouse painting or reference-pixel extraction. Technical validation does not establish visual approval or exact likeness.

## Editable originals

All paths are relative to the repository root.

| Purpose | Source |
| --- | --- |
| Reimu full-body portrait | art/shrine-v04/portraits/reimu.aseprite |
| Marisa full-body portrait | art/shrine-v04/portraits/marisa.aseprite |
| Reimu directional animation | art/shrine-v04/players/reimu.aseprite |
| Marisa directional animation | art/shrine-v04/players/marisa.aseprite |
| Complete design boards | art/shrine-v04/boards/reimu.aseprite and marisa.aseprite |
| Enemies and orbiting yin-yang orbs | art/shrine-v04/actors/ |
| Projectiles, seals and Master Spark | art/shrine-v04/combat/ and effects/ |
| Courtyard, buildings, trees and title | art/shrine-v04/scenery/ |
| UI skins and icon | art/shrine-v04/ui/ |
| Asset list, dimensions and layers | art/shrine-v04/manifest.json |

Editable anatomical/garment layers include face, hair, headwear, sleeves, coat, legs, boots and identity props. REFERENCE ONLY layers are hidden and locked. They must remain so during export. Portraits and small sprites intentionally have different detail budgets; full-body portraits do not become oversized combat sprites.

## Animation contract

- Player canvas: 64 x 64, foot anchor at y=60, transparent perimeter.
- Runtime sheet: 512 x 256, eight columns and four rows: front, right, back, left.
- Each direction has one idle frame, four distinct walk phases and three cast phases, with named Aseprite tags. Idle duration 250 ms; walk 110 ms; cast 110 / 70 / 110 ms.
- Runtime casting observes the existing primary cooldown reset and beam state through a presentation-only animation controller. It does not produce projectiles or alter damage. Pause uses simulation time so animation does not advance while combat is paused.
- Left and right are separately authored; gameplay does not mirror the entire character or move props between hands.
- Enemies use four-frame, 48 x 48 strips. Orbiting yin-yang orbs use four-frame, 32 x 32 strips and remain locked behind their existing upgrade.

## Rendering and projection

WorldProjection maps ground coordinates by y * 0.72; input is inverse-projected and normalized while preserving analog strength. Upright actor quads compensate for ground compression. Combat collision, targeting, growth, skill geometry and limits remain in existing simulation coordinates. No per-entity Godot nodes are added.

The 1024 x 1024 actor atlas has fourteen regions. A single reusable MultiMesh receives scenery, enemies, the hero and unlocked orbs, ordered by foot depth with a stable sequence tie-break. UV regions support both rows and columns. Ground effects remain projected on the ground. The atlas and transient list are shared presentation resources, not a replacement for ECS component storage.

Scene objects fade when they overlap the player from the front. This is visual cover only, not new collision or new gameplay. Hero selection and character compendium pages load the full portraits. Settings, localization, minimap and touch controls remain in screen space.

## Editing and exporting

Use Aseprite to edit the source file, preferably Save As a personal revision first. Keep canvas size, tags and frame count unless also updating the manifest and runtime contract. Do not paint the atlas directly: it is derived from primary source images. Keep reference layers hidden and locked.

The build_complete_art.lua generator refuses to overwrite an existing source edition and skips a completed edition. It is for generating a new edition, not for applying manual edits to existing sources. Run source validation with tools/aseprite/verify_redraw.cmd; it reopens sources in Aseprite without saving over them and compares all PNG pixels, tags, layer flags and atlas rectangles. A manual change requires re-exporting its PNG and refreshing derived atlas/board assets before a build can pass.

For manual Aseprite export: export player sprite sheets as an 8-column by 4-row grid without padding or trim; export enemy/orb strips in one row; export other primary sources as visible-layer PNGs. Preserve the file names recorded by the manifest. The atlas layout is recorded in assets/aseprite/shrine-v04/atlas/actors.json; its layers are named after their source entries. Replace the matching atlas cel at its existing position after a primary sprite edit. The design-board layers likewise retain readable source names. Never copy visible pixels from the hidden reference layer.

## Validation and release boundary

The authoritative validation records are recorded in docs/complete_visual_validation.md after verification. Native and Web use the same C# sources and active art pack; threadless Web stages the existing compatible runtime, not a second maintained game.

The version remains alpha-0.1.6. Building a local Web test site is not deployment. No production EXE, Git push, release tag or server update is part of this work. Physical-phone performance and user visual approval are not inferred from desktop browser emulation.
