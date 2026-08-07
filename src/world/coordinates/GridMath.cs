using Godot;

namespace TouhouWuxiaSurvivor.World.Coordinates;

/// <summary>
/// 提供无限世界坐标换算所需的数学函数，特别保证负坐标与正坐标遵循同一网格规则。
/// </summary>
public static class GridMath
{
    /// <summary>
    /// 执行向负无穷取整的整数除法，修正 C# 整数除法向零截断造成的负区块错误。
    /// </summary>
    public static long FloorDiv(long value, long divisor)
    {
        long quotient = value / divisor;
        long remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }

    /// <summary>
    /// 返回非负余数，用于把任意绝对坐标稳定映射到区块内部坐标。
    /// </summary>
    public static long PositiveMod(long value, long divisor)
    {
        long remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }

    /// <summary>
    /// 将当前浮点本地像素位置换算为相对于重定位原点的区块坐标。
    /// </summary>
    public static ChunkCoordinate LocalPositionToChunk(Vector2 position)
    {
        long tileX = (long)Mathf.Floor(position.X / WorldMetrics.TilePixels);
        long tileY = (long)Mathf.Floor(position.Y / WorldMetrics.TilePixels);
        return new ChunkCoordinate(
            FloorDiv(tileX, WorldMetrics.ChunkTiles),
            FloorDiv(tileY, WorldMetrics.ChunkTiles));
    }

    /// <summary>
    /// 组合本地像素位置和当前原点区块，得到不会因重定位而改变的绝对 Tile 坐标。
    /// </summary>
    public static (long X, long Y) LocalPositionToAbsoluteTile(
        Vector2 position,
        ChunkCoordinate originChunk)
    {
        long localTileX = (long)Mathf.Floor(position.X / WorldMetrics.TilePixels);
        long localTileY = (long)Mathf.Floor(position.Y / WorldMetrics.TilePixels);
        return (
            originChunk.X * WorldMetrics.ChunkTiles + localTileX,
            originChunk.Y * WorldMetrics.ChunkTiles + localTileY);
    }
}
