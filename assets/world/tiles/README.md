# World Tile Assets

All runtime ground tiles in this directory follow these rules:

- Tile size: `16x16` logical pixels.
- File format: opaque RGBA PNG.
- Naming: lowercase ASCII `snake_case`.
- Category folders represent shared terrain or a Gensokyo biome.
- Files ending in `_base` are the default tile for that terrain.
- Files ending in `_01`, `_02`, and so on are visual variants.
- `tile_catalog.png` is a preview only and must not be imported as a terrain tile.
- `tile_manifest.json` is the generated source of tile IDs and relative paths.

Current categories:

- `common`
- `hakurei_shrine`
- `magic_forest`
- `misty_lake`
- `bamboo_forest`
- `youkai_mountain`

Regenerate from the project root:

```text
dotnet run --project tools/tile_generator/tile_generator.csproj -- assets/world/tiles
```

Transition, edge, corner, and animated tiles are intentionally excluded from this first palette.
