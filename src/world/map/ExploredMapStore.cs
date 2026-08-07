using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Map;

/// <summary>
/// 按绝对区块坐标保存玩家已经探索过的地表砖块快照。
/// 使用单字节 TileId 和有界 FIFO 容量，使无限世界的地图历史不会无限占用内存。
/// </summary>
public sealed class ExploredMapStore
{
    private readonly Dictionary<ChunkCoordinate, byte[]> _chunks = [];
    private readonly Queue<ChunkCoordinate> _insertionOrder = [];
    private readonly int _capacity;

    /// <summary>
    /// 创建探索地图存储；容量以完整区块为单位，至少保留一个区块。
    /// </summary>
    public ExploredMapStore(int capacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    public int Count => _chunks.Count;

    /// <summary>
    /// 将已生成区块复制为稳定快照。重复坐标不会重新排队，避免重定位后扰乱淘汰顺序。
    /// </summary>
    public void Remember(GeneratedChunk chunk)
    {
        if (_chunks.ContainsKey(chunk.Coordinate))
        {
            return;
        }

        var tiles = new byte[WorldMetrics.ChunkTiles * WorldMetrics.ChunkTiles];
        for (int y = 0; y < WorldMetrics.ChunkTiles; y++)
        {
            for (int x = 0; x < WorldMetrics.ChunkTiles; x++)
            {
                tiles[y * WorldMetrics.ChunkTiles + x] = (byte)chunk.Get(x, y);
            }
        }

        _chunks.Add(chunk.Coordinate, tiles);
        _insertionOrder.Enqueue(chunk.Coordinate);
        while (_chunks.Count > _capacity)
        {
            _chunks.Remove(_insertionOrder.Dequeue());
        }
    }

    /// <summary>
    /// 判断指定绝对区块是否仍保存在探索历史中。
    /// </summary>
    public bool ContainsChunk(ChunkCoordinate coordinate) => _chunks.ContainsKey(coordinate);

    /// <summary>
    /// 按绝对 Tile 坐标查询地表；正确处理负坐标，并在未探索区域返回 false。
    /// </summary>
    public bool TryGetTile(long worldTileX, long worldTileY, out TileId tile)
    {
        var chunk = new ChunkCoordinate(
            GridMath.FloorDiv(worldTileX, WorldMetrics.ChunkTiles),
            GridMath.FloorDiv(worldTileY, WorldMetrics.ChunkTiles));
        if (!_chunks.TryGetValue(chunk, out byte[]? tiles))
        {
            tile = default;
            return false;
        }

        int localX = (int)GridMath.PositiveMod(worldTileX, WorldMetrics.ChunkTiles);
        int localY = (int)GridMath.PositiveMod(worldTileY, WorldMetrics.ChunkTiles);
        tile = (TileId)tiles[localY * WorldMetrics.ChunkTiles + localX];
        return true;
    }
}
