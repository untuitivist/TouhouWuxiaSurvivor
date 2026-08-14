using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Map;

/// <summary>
/// 保存旅行地图中一个已经亲自探索的地表格，地形与群系语义均来自同一次世界生成。
/// </summary>
public readonly record struct ExploredMapCell(TileId Tile, BiomeId Biome);
