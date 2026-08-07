using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Structures;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Official;

/// <summary>
/// 为数据驱动的正作结构压印统一占位轮廓，并使用各作品自己的地表配色保持可辨识性。
/// </summary>
public static class OfficialStructureStamp
{
    /// <summary>
    /// 按地区序号绘制据点、殿堂或秘境轮廓；找不到目录定义时保持原地形不变。
    /// </summary>
    public static void Stamp(GeneratedChunk chunk, StructurePlacement placement)
    {
        if (!OfficialWorldContentCatalog.TryGet(
            placement.Id,
            out OfficialWorldContentDefinition definition))
        {
            return;
        }

        int radius = 9 + definition.RegionIndex;
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                bool feature = IsFeature(definition.RegionIndex, dx, dy, radius);
                bool path = definition.RegionIndex switch
                {
                    0 => Math.Abs(dx) <= 1 || Math.Abs(dy) <= 1,
                    1 => Math.Abs(dx) <= 2,
                    _ => Math.Abs(dx - dy) <= 1 || Math.Abs(dx + dy) <= 1,
                };
                TileId tile = feature
                    ? definition.DetailTile
                    : path
                        ? TileId.ShrinePathPebbles
                        : definition.BaseTile;
                chunk.TrySetAbsolute(placement.X + dx, placement.Y + dy, tile);
            }
        }
    }

    /// <summary>
    /// 为三个地区层级生成不同骨架：方形据点、双墙殿堂和圆环秘境。
    /// </summary>
    private static bool IsFeature(int regionIndex, int dx, int dy, int radius)
    {
        int edge = Math.Max(Math.Abs(dx), Math.Abs(dy));
        int distance = dx * dx + dy * dy;
        return regionIndex switch
        {
            0 => edge == radius || distance is >= 16 and <= 25,
            1 => edge == radius || Math.Abs(dx) == radius / 2,
            _ => distance >= (radius - 1) * (radius - 1) &&
                distance <= radius * radius || distance is >= 9 and <= 16,
        };
    }
}
