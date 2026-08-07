using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Generation;

/// <summary>
/// 保存一个固定 32×32 区块的完整地表 TileId，以及该区块在无限世界中的绝对坐标。
/// </summary>
public sealed class GeneratedChunk
{
    private readonly TileId[] _tiles = new TileId[WorldMetrics.ChunkTiles * WorldMetrics.ChunkTiles];

    /// <summary>
    /// 创建指定绝对坐标的空区块，随后由世界生成器填充所有 Tile。
    /// </summary>
    public GeneratedChunk(ChunkCoordinate coordinate) => Coordinate = coordinate;

    public ChunkCoordinate Coordinate { get; }

    /// <summary>
    /// 按区块内部坐标读取 TileId；调用者必须保证坐标处于有效范围。
    /// </summary>
    public TileId Get(int localX, int localY) =>
        _tiles[localY * WorldMetrics.ChunkTiles + localX];

    /// <summary>
    /// 按区块内部坐标写入 TileId；调用者必须保证坐标处于有效范围。
    /// </summary>
    public void Set(int localX, int localY, TileId tile) =>
        _tiles[localY * WorldMetrics.ChunkTiles + localX] = tile;

    /// <summary>
    /// 尝试用绝对 Tile 坐标写入本区块；坐标落在区块外时保持不变并返回 false。
    /// </summary>
    public bool TrySetAbsolute(long worldX, long worldY, TileId tile)
    {
        long originX = Coordinate.X * WorldMetrics.ChunkTiles;
        long originY = Coordinate.Y * WorldMetrics.ChunkTiles;
        long localX = worldX - originX;
        long localY = worldY - originY;
        if (localX < 0 || localY < 0 ||
            localX >= WorldMetrics.ChunkTiles || localY >= WorldMetrics.ChunkTiles)
        {
            return false;
        }

        Set((int)localX, (int)localY, tile);
        return true;
    }
}
