using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Map;

/// <summary>
/// 按绝对区块坐标保存已生成地图语义，并以独立掩码记录玩家真正走过的范围。
/// 使用有界 FIFO 容量，使无限世界的旅行地图历史不会无限占用内存。
/// </summary>
public sealed class ExploredMapStore
{
    private readonly Dictionary<ChunkCoordinate, ExploredChunkSnapshot> _chunks = [];
    private readonly Queue<ChunkCoordinate> _insertionOrder = [];
    private readonly int _capacity;
    private long _lastRevealX;
    private long _lastRevealY;
    private int _lastRevealRadius;
    private bool _hasRevealOrigin;

    /// <summary>
    /// 创建探索地图存储；容量以完整区块为单位，至少保留一个区块。
    /// </summary>
    public ExploredMapStore(int capacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    public int Count => _chunks.Count;

    public long RevealedTileCount { get; private set; }

    public int Revision { get; private set; }

    /// <summary>
    /// 将已生成区块复制为稳定快照，但不把加载视为探索；重复坐标不会扰乱淘汰顺序。
    /// </summary>
    public void RememberGenerated(GeneratedChunk chunk)
    {
        if (_chunks.ContainsKey(chunk.Coordinate))
        {
            return;
        }

        _chunks.Add(chunk.Coordinate, new ExploredChunkSnapshot(chunk));
        _insertionOrder.Enqueue(chunk.Coordinate);
        if (_hasRevealOrigin)
        {
            RevealedTileCount += RevealChunkIntersection(
                chunk.Coordinate, _lastRevealX, _lastRevealY, _lastRevealRadius);
        }

        while (_chunks.Count > _capacity)
        {
            ChunkCoordinate expired = _insertionOrder.Dequeue();
            if (_chunks.Remove(expired, out ExploredChunkSnapshot? snapshot))
            {
                RevealedTileCount -= snapshot.RevealedCount;
            }
        }
    }

    /// <summary>
    /// 保留旧调用入口；语义已经改为只登记生成数据，不会自动揭示整个区块。
    /// </summary>
    public void Remember(GeneratedChunk chunk) => RememberGenerated(chunk);

    /// <summary>
    /// 揭示玩家周围的圆形视野，并记住当前位置，让稍后生成的相交区块自动补齐揭示。
    /// </summary>
    public int RevealAround(long worldTileX, long worldTileY, int radius)
    {
        _lastRevealX = worldTileX;
        _lastRevealY = worldTileY;
        _lastRevealRadius = Math.Max(0, radius);
        _hasRevealOrigin = true;
        int revealed = 0;
        long leftChunk = GridMath.FloorDiv(worldTileX - _lastRevealRadius, WorldMetrics.ChunkTiles);
        long rightChunk = GridMath.FloorDiv(worldTileX + _lastRevealRadius, WorldMetrics.ChunkTiles);
        long topChunk = GridMath.FloorDiv(worldTileY - _lastRevealRadius, WorldMetrics.ChunkTiles);
        long bottomChunk = GridMath.FloorDiv(worldTileY + _lastRevealRadius, WorldMetrics.ChunkTiles);
        for (long chunkY = topChunk; chunkY <= bottomChunk; chunkY++)
        {
            for (long chunkX = leftChunk; chunkX <= rightChunk; chunkX++)
            {
                revealed += RevealChunkIntersection(
                    new ChunkCoordinate(chunkX, chunkY), worldTileX, worldTileY, _lastRevealRadius);
            }
        }

        RevealedTileCount += revealed;
        if (revealed > 0)
        {
            Revision++;
        }

        return revealed;
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
        if (TryGetCell(worldTileX, worldTileY, out ExploredMapCell cell))
        {
            tile = cell.Tile;
            return true;
        }

        tile = default;
        return false;
    }

    /// <summary>
    /// 按绝对 Tile 坐标查询生成时保存的群系；不会为地图重新运行群系选择器。
    /// </summary>
    public bool TryGetBiome(long worldTileX, long worldTileY, out BiomeId biome)
    {
        if (TryGetCell(worldTileX, worldTileY, out ExploredMapCell cell))
        {
            biome = cell.Biome;
            return true;
        }

        biome = default;
        return false;
    }

    /// <summary>
    /// 查询已经由玩家揭示的完整地图格；正确处理负坐标和未生成区域。
    /// </summary>
    public bool TryGetCell(long worldTileX, long worldTileY, out ExploredMapCell cell)
    {
        var chunk = new ChunkCoordinate(
            GridMath.FloorDiv(worldTileX, WorldMetrics.ChunkTiles),
            GridMath.FloorDiv(worldTileY, WorldMetrics.ChunkTiles));
        if (!_chunks.TryGetValue(chunk, out ExploredChunkSnapshot? snapshot))
        {
            cell = default;
            return false;
        }

        int localX = (int)GridMath.PositiveMod(worldTileX, WorldMetrics.ChunkTiles);
        int localY = (int)GridMath.PositiveMod(worldTileY, WorldMetrics.ChunkTiles);
        return snapshot.TryGet(localX, localY, out cell);
    }

    /// <summary>
    /// 揭示一个已生成区块与圆形视野相交的格，并返回首次揭示数量。
    /// </summary>
    private int RevealChunkIntersection(
        ChunkCoordinate coordinate,
        long centerX,
        long centerY,
        int radius)
    {
        if (!_chunks.TryGetValue(coordinate, out ExploredChunkSnapshot? snapshot))
        {
            return 0;
        }

        long originX = coordinate.X * WorldMetrics.ChunkTiles;
        long originY = coordinate.Y * WorldMetrics.ChunkTiles;
        long radiusSquared = (long)radius * radius;
        int revealed = 0;
        for (int localY = 0; localY < WorldMetrics.ChunkTiles; localY++)
        {
            long dy = originY + localY - centerY;
            for (int localX = 0; localX < WorldMetrics.ChunkTiles; localX++)
            {
                long dx = originX + localX - centerX;
                if (dx * dx + dy * dy <= radiusSquared && snapshot.Reveal(localX, localY))
                {
                    revealed++;
                }
            }
        }

        return revealed;
    }
}
