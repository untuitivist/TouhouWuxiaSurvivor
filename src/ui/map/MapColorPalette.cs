using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.Ui.Map;

/// <summary>
/// 将游戏地表 TileId 转换为地图专用的高辨识度 RGB 颜色。
/// 地图配色独立于场景贴图，后续替换美术资源时不会破坏地图数据格式。
/// </summary>
public static class MapColorPalette
{
    private const uint Unknown = 0x111619;

    /// <summary>
    /// 将一个砖块或未知区域的颜色写入 RGBA8 像素缓冲区的指定偏移。
    /// </summary>
    public static void WritePixel(byte[] pixels, int offset, TileId? tile)
    {
        uint rgb = tile.HasValue ? GetRgb(tile.Value) : Unknown;
        pixels[offset] = (byte)(rgb >> 16);
        pixels[offset + 1] = (byte)(rgb >> 8);
        pixels[offset + 2] = (byte)rgb;
        pixels[offset + 3] = 255;
    }

    /// <summary>
    /// 返回砖块的 24 位 RGB 地图色；同类纹理变体保留轻微明暗差以显示地表细节。
    /// </summary>
    private static uint GetRgb(TileId tile) => tile switch
    {
        TileId.GrassBase => 0x4C8B4F,
        TileId.GrassDots => 0x69A65C,
        TileId.DirtBase => 0x927044,
        TileId.DirtPebbles => 0xAF8B55,
        TileId.StoneBase => 0x777C78,
        TileId.StoneCracks => 0x555D5B,
        TileId.WaterShallowBase => 0x4B91A5,
        TileId.WaterShallowRipples => 0x78B7C3,
        TileId.ShrineGrassBase => 0x568C4D,
        TileId.ShrineGrassPetals => 0xCE777F,
        TileId.ShrinePathBase => 0xD3B87B,
        TileId.ShrinePathPebbles => 0xEEE0AE,
        TileId.BoundarySoilBase => 0x704B58,
        TileId.BoundarySoilSparkles => 0xAE7895,
        TileId.ForestFloorBase => 0x243F35,
        TileId.ForestFloorLeaves => 0x4B6842,
        TileId.MossBase => 0x3D6945,
        TileId.MossDots => 0x6C8C51,
        TileId.MagicSoilBase => 0x493B59,
        TileId.MagicSoilSparkles => 0x8E69A0,
        TileId.LakeWaterBase => 0x347A9A,
        TileId.LakeWaterRipples => 0x65B4C5,
        TileId.LakeShoreBase => 0xB9A46A,
        TileId.LakeShorePebbles => 0xD6C48B,
        TileId.WetGrassBase => 0x397867,
        TileId.WetGrassDroplets => 0x5FA894,
        TileId.BambooFloorBase => 0x66743D,
        TileId.BambooFloorLeaves => 0x89934B,
        TileId.BambooMossBase => 0x3F6A3A,
        TileId.BambooMossDots => 0x6F994B,
        TileId.BambooPathBase => 0xB19B58,
        TileId.BambooPathStripes => 0xD4BE70,
        TileId.MountainGrassBase => 0x56734F,
        TileId.MountainGrassFlowers => 0x8C9D68,
        TileId.MountainRockBase => 0x737B7B,
        TileId.MountainRockCracks => 0x505A5D,
        TileId.StreamStoneBase => 0x617A80,
        TileId.StreamStoneWet => 0x779CA3,
        _ => Unknown
    };
}
