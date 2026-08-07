namespace TouhouWuxiaSurvivor.World.Tiles;

/// <summary>
/// 维护运行时 TileId 到 Godot 资源路径的一对一映射，保证生成逻辑不依赖文件布局细节。
/// </summary>
public static class TileCatalog
{
    private const string Root = "res://assets/world/tiles/";

    /// <summary>
    /// 返回指定 TileId 的 res:// PNG 路径；未登记枚举值会抛出异常以尽早暴露资源缺失。
    /// </summary>
    public static string GetResourcePath(TileId tile) => Root + tile switch
    {
        TileId.GrassBase => "common/grass_base.png",
        TileId.GrassDots => "common/grass_dots_01.png",
        TileId.DirtBase => "common/dirt_base.png",
        TileId.DirtPebbles => "common/dirt_pebbles_01.png",
        TileId.StoneBase => "common/stone_base.png",
        TileId.StoneCracks => "common/stone_cracks_01.png",
        TileId.WaterShallowBase => "common/water_shallow_base.png",
        TileId.WaterShallowRipples => "common/water_shallow_ripples_01.png",
        TileId.ShrineGrassBase => "hakurei_shrine/shrine_grass_base.png",
        TileId.ShrineGrassPetals => "hakurei_shrine/shrine_grass_petals_01.png",
        TileId.ShrinePathBase => "hakurei_shrine/shrine_path_base.png",
        TileId.ShrinePathPebbles => "hakurei_shrine/shrine_path_pebbles_01.png",
        TileId.BoundarySoilBase => "hakurei_shrine/boundary_soil_base.png",
        TileId.BoundarySoilSparkles => "hakurei_shrine/boundary_soil_sparkles_01.png",
        TileId.ForestFloorBase => "magic_forest/forest_floor_base.png",
        TileId.ForestFloorLeaves => "magic_forest/forest_floor_leaves_01.png",
        TileId.MossBase => "magic_forest/moss_base.png",
        TileId.MossDots => "magic_forest/moss_dots_01.png",
        TileId.MagicSoilBase => "magic_forest/magic_soil_base.png",
        TileId.MagicSoilSparkles => "magic_forest/magic_soil_sparkles_01.png",
        TileId.LakeWaterBase => "misty_lake/lake_water_base.png",
        TileId.LakeWaterRipples => "misty_lake/lake_water_ripples_01.png",
        TileId.LakeShoreBase => "misty_lake/lake_shore_base.png",
        TileId.LakeShorePebbles => "misty_lake/lake_shore_pebbles_01.png",
        TileId.WetGrassBase => "misty_lake/wet_grass_base.png",
        TileId.WetGrassDroplets => "misty_lake/wet_grass_droplets_01.png",
        TileId.BambooFloorBase => "bamboo_forest/bamboo_floor_base.png",
        TileId.BambooFloorLeaves => "bamboo_forest/bamboo_floor_leaves_01.png",
        TileId.BambooMossBase => "bamboo_forest/bamboo_moss_base.png",
        TileId.BambooMossDots => "bamboo_forest/bamboo_moss_dots_01.png",
        TileId.BambooPathBase => "bamboo_forest/bamboo_path_base.png",
        TileId.BambooPathStripes => "bamboo_forest/bamboo_path_stripes_01.png",
        TileId.MountainGrassBase => "youkai_mountain/mountain_grass_base.png",
        TileId.MountainGrassFlowers => "youkai_mountain/mountain_grass_flowers_01.png",
        TileId.MountainRockBase => "youkai_mountain/mountain_rock_base.png",
        TileId.MountainRockCracks => "youkai_mountain/mountain_rock_cracks_01.png",
        TileId.StreamStoneBase => "youkai_mountain/stream_stone_base.png",
        TileId.StreamStoneWet => "youkai_mountain/stream_stone_wet_01.png",
        _ => throw new ArgumentOutOfRangeException(nameof(tile), tile, null)
    };
}
