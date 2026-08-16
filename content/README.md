# Content Packages

`base` contains systems and world content that exist without selecting any official game.
Each `packs/thXX_slug` directory is an isolated optional content package with these boundaries:

- `actors`: characters, enemies, projectiles, and actor-specific logic.
- `world`: biomes, structures, encounters, and generation rules.
- `assets`: package-owned textures, audio, data, and scenes.
- `pack.json`: identity, development state, and categorized incremental content.

All integer-numbered packages from TH01 through TH20 are complete across five runtime categories:
biomes, structures, ordinary enemies, characters, and spell cards. Every package is independently
selectable in the new-game list and owns three generated biomes, three labeled structures, three
regional enemies, its declared character identities, and structured representative spell-card data.

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
