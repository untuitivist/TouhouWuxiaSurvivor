# Content Packages

`base` contains systems and world content that exist without selecting any official game.
Each `packs/thXX_slug` directory is an isolated optional content package with these boundaries:

- `actors`: characters, enemies, projectiles, and actor-specific logic.
- `world`: biomes, structures, encounters, and generation rules.
- `assets`: package-owned textures, audio, data, and scenes.
- `pack.json`: identity, development state, and categorized incremental content.

All integer-numbered packages from TH01 through TH20 are complete for the first runtime-content
stage and selectable in the new-game list. Every package owns three generated biomes, three labeled
structures, three regional enemies, and a representative-character catalog. Character catalog entries
do not imply that those characters already have playable or boss implementations.
Decimal-numbered official spin-offs are intentionally outside this main-series directory.

The completed `base` manifest contains only setting-wide Gensokyo content. Locations and ecology
introduced as official-game increments remain package-owned; in particular Bamboo Forest,
Bamboo Trail, and Bamboo Spirit belong to `th08_in` and never generate in a base-only run.
