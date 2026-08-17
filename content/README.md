# Content Packages

## Active Delivery Scope

The product follows a plugin-first contract. `base` is the mandatory bundled plugin and must provide
a complete run without optional content. `th06_eosd` is the first optional reference plugin used to
validate world, structure, enemy, character, build, boss, presentation, and isolation extension points.
All optional packages except `th06_eosd` remain installed catalog and asset inventory while this
contract is being proven; their existing manifest coverage is not evidence that their runtime
gameplay is complete.

The governing product and architecture baseline is documented in
[`docs/plugin_first_design.md`](../docs/plugin_first_design.md).

`base` contains systems and world content that exist without selecting any official game.
Each `packs/thXX_slug` directory is an isolated optional content package with these boundaries:

- `actors`: characters, enemies, projectiles, and actor-specific logic.
- `world`: biomes, structures, encounters, and generation rules.
- `assets`: package-owned textures, audio, data, and scenes.
- `pack.json`: identity, development state, and categorized incremental content.

All integer-numbered packages from TH01 through TH20 currently have catalog entries across five
runtime categories: biomes, structures, ordinary enemies, characters, and spell cards. Every package
is independently selectable in the new-game list and declares three generated biomes, three labeled
structures, three regional enemies, character identities, and representative spell-card data. These
declarations are migration inventory; each package still requires the reference-plugin acceptance
contract before it can be called complete gameplay.

The shared character catalog normalizes the package declarations into 132 stable identities. Every
identity uses one definition for both playable-character and character-boss roles; the selected player
identity is excluded from the current run's boss candidates by stable ID, but remains boss-capable in
other runs. The versioned spell-card manifests provide 51 automatic build choices with owner,
source, prerequisite, attribute factors, and internal preview mapping. Each learned card keeps an
independent timer; final combat values resolve from the selected character and current build. TH01 through TH05 are explicitly marked as
adaptations of pre-spell-card attack imagery, while later entries preserve their official-card boundary.
Decimal-numbered official spin-offs are intentionally outside this main-series directory.

The completed `base` manifest contains only setting-wide Gensokyo content. Locations and ecology
introduced as official-game increments remain package-owned; in particular Bamboo Forest,
Bamboo Trail, and Bamboo Spirit belong to `th08_in` and never generate in a base-only run.
