using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Official;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 采用稀疏结构网格在无限世界中放置群系专属地标，并允许结构跨越区块边界连续生成。
/// </summary>
public sealed class StructureGenerator
{
    private readonly StructureLocator _locator;

    /// <summary>
    /// 创建使用共享结构选址器的结构生成器。
    /// </summary>
    public StructureGenerator(StructureLocator locator) => _locator = locator;

    /// <summary>
    /// 查找所有可能影响目标区块的结构网格单元，并把相交部分压印到区块地表。
    /// 出生点神社固定生成，其余结构由网格坐标确定性抽样。
    /// </summary>
    public void Apply(GeneratedChunk chunk)
    {
        long minX = chunk.Coordinate.X * WorldMetrics.ChunkTiles;
        long minY = chunk.Coordinate.Y * WorldMetrics.ChunkTiles;
        long maxX = minX + WorldMetrics.ChunkTiles - 1;
        long maxY = minY + WorldMetrics.ChunkTiles - 1;
        foreach (StructurePlacement placement in _locator.FindInBounds(
            minX - StructureLocator.Radius,
            minY - StructureLocator.Radius,
            maxX + StructureLocator.Radius,
            maxY + StructureLocator.Radius))
        {
            Stamp(chunk, placement);
        }
    }

    /// <summary>
    /// 根据结构类型分派对应图案，并使用记录中的绝对锚点压印。
    /// </summary>
    private static void Stamp(GeneratedChunk chunk, StructurePlacement placement)
    {
        switch (placement.Id)
        {
            case StructureId.HakureiShrine:
            case StructureId.ShrineCourt:
                StampShrineCourt(chunk, placement.X, placement.Y);
                break;
            case StructureId.HumanVillage:
                StampHumanVillage(chunk, placement.X, placement.Y);
                break;
            case StructureId.MagicCircle:
                StampMagicCircle(chunk, placement.X, placement.Y);
                break;
            case StructureId.LakeIsland:
                StampLakeIsland(chunk, placement.X, placement.Y);
                break;
            case StructureId.ScarletDevilMansion:
                StampScarletDevilMansion(chunk, placement.X, placement.Y);
                break;
            case StructureId.BambooTrail:
                StampBambooTrail(chunk, placement.X, placement.Y);
                break;
            case StructureId.MountainTerrace:
                StampMountainTerrace(chunk, placement.X, placement.Y);
                break;
            case StructureId.Crossroads:
                StampCrossroads(chunk, placement.X, placement.Y);
                break;
            default:
                OfficialStructureStamp.Stamp(chunk, placement);
                break;
        }
    }

    /// <summary>
    /// 压印带十字主街、石质屋基和外围草地的紧凑聚落，使地图能够辨认人间之里轮廓。
    /// </summary>
    private static void StampHumanVillage(GeneratedChunk chunk, long centerX, long centerY)
    {
        ForSquare(centerX, centerY, 12, (x, y, dx, dy) =>
        {
            bool road = Math.Abs(dx) <= 1 || Math.Abs(dy) <= 1;
            bool house = Math.Abs(dx) is >= 4 and <= 9 && Math.Abs(dy) is >= 4 and <= 9;
            bool houseWall = house && (Math.Abs(dx) is 4 or 9 || Math.Abs(dy) is 4 or 9);
            TileId tile = road
                ? TileId.DirtPebbles
                : houseWall
                    ? TileId.StoneCracks
                    : house
                        ? TileId.StoneBase
                        : TileId.GrassDots;
            chunk.TrySetAbsolute(x, y, tile);
        });
    }

    /// <summary>
    /// 压印带结界边缘、十字参道与花瓣庭院的方形神社场地。
    /// </summary>
    private static void StampShrineCourt(GeneratedChunk chunk, long centerX, long centerY)
    {
        ForSquare(centerX, centerY, 10, (x, y, dx, dy) =>
        {
            int edge = Math.Max(Math.Abs(dx), Math.Abs(dy));
            TileId tile = edge == 10
                ? TileId.BoundarySoilSparkles
                : Math.Abs(dx) <= 1 || Math.Abs(dy) <= 1
                    ? TileId.ShrinePathPebbles
                    : TileId.ShrineGrassPetals;
            chunk.TrySetAbsolute(x, y, tile);
        });
    }

    /// <summary>
    /// 压印由闪光魔法土环和苔藓核心构成的圆形遗迹。
    /// </summary>
    private static void StampMagicCircle(GeneratedChunk chunk, long centerX, long centerY)
    {
        ForSquare(centerX, centerY, 8, (x, y, dx, dy) =>
        {
            int distance = dx * dx + dy * dy;
            if (distance is >= 36 and <= 58)
            {
                chunk.TrySetAbsolute(x, y, TileId.MagicSoilSparkles);
            }
            else if (distance < 36)
            {
                chunk.TrySetAbsolute(x, y, TileId.MossDots);
            }
        });
    }

    /// <summary>
    /// 压印由浅水环绕卵石核心构成的雾之湖小岛。
    /// </summary>
    private static void StampLakeIsland(GeneratedChunk chunk, long centerX, long centerY)
    {
        ForSquare(centerX, centerY, 9, (x, y, dx, dy) =>
        {
            int distance = dx * dx + dy * dy;
            if (distance <= 25)
            {
                chunk.TrySetAbsolute(x, y, TileId.LakeShorePebbles);
            }
            else if (distance <= 72)
            {
                chunk.TrySetAbsolute(x, y, TileId.LakeWaterRipples);
            }
        });
    }

    /// <summary>
    /// 压印带外墙、主馆、中央门厅和南侧入口的红魔馆占地图形。
    /// </summary>
    private static void StampScarletDevilMansion(GeneratedChunk chunk, long centerX, long centerY)
    {
        ForSquare(centerX, centerY, 12, (x, y, dx, dy) =>
        {
            int edge = Math.Max(Math.Abs(dx), Math.Abs(dy));
            bool insideHall = Math.Abs(dx) <= 9 && Math.Abs(dy) <= 6;
            bool hallWall = insideHall && (Math.Abs(dx) == 9 || Math.Abs(dy) == 6);
            bool entrance = Math.Abs(dx) <= 1 && dy is >= 6 and <= 12;
            if (edge == 12)
            {
                chunk.TrySetAbsolute(x, y, TileId.BoundarySoilSparkles);
            }
            else if (hallWall)
            {
                chunk.TrySetAbsolute(x, y, TileId.MountainRockCracks);
            }
            else if (insideHall || entrance)
            {
                chunk.TrySetAbsolute(x, y, TileId.ShrinePathPebbles);
            }
        });
    }

    /// <summary>
    /// 压印斜穿结构范围的竹林小径，并散布规律落叶作为视觉提示。
    /// </summary>
    private static void StampBambooTrail(GeneratedChunk chunk, long centerX, long centerY)
    {
        ForSquare(centerX, centerY, 11, (x, y, dx, dy) =>
        {
            if (Math.Abs(dy - dx / 2) <= 1)
            {
                chunk.TrySetAbsolute(x, y, TileId.BambooPathStripes);
            }
            else if ((Math.Abs(dx) + Math.Abs(dy)) % 7 == 0)
            {
                chunk.TrySetAbsolute(x, y, TileId.BambooFloorLeaves);
            }
        });
    }

    /// <summary>
    /// 压印多条水平石阶与侧壁，形成妖怪之山的梯田轮廓。
    /// </summary>
    private static void StampMountainTerrace(GeneratedChunk chunk, long centerX, long centerY)
    {
        ForSquare(centerX, centerY, 10, (x, y, dx, dy) =>
        {
            if (Math.Abs(dy) is 2 or 6 || Math.Abs(dx) == 9)
            {
                chunk.TrySetAbsolute(x, y, TileId.MountainRockCracks);
            }
            else if (Math.Abs(dy) < 9)
            {
                chunk.TrySetAbsolute(x, y, TileId.MountainGrassFlowers);
            }
        });
    }

    /// <summary>
    /// 在普通原野压印由卵石土路组成的十字路口。
    /// </summary>
    private static void StampCrossroads(GeneratedChunk chunk, long centerX, long centerY)
    {
        ForSquare(centerX, centerY, 9, (x, y, dx, dy) =>
        {
            if (Math.Abs(dx) <= 1 || Math.Abs(dy) <= 1)
            {
                chunk.TrySetAbsolute(x, y, TileId.DirtPebbles);
            }
        });
    }

    /// <summary>
    /// 遍历以绝对坐标为中心的闭区间正方形，并同时提供世界坐标和局部偏移。
    /// </summary>
    private static void ForSquare(
        long centerX,
        long centerY,
        int radius,
        Action<long, long, int, int> visitor)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                visitor(centerX + dx, centerY + dy, dx, dy);
            }
        }
    }
}
