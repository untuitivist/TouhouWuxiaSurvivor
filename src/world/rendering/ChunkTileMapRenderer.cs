using Godot;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 将绝对生成区块投影到当前本地原点附近的 Godot TileMapLayer，并负责卸载可视单元格。
/// </summary>
public sealed class ChunkTileMapRenderer : IChunkRenderer
{
    private readonly TileMapLayer _layer;

    /// <summary>
    /// 绑定目标 TileMapLayer，并安装由全部 TileId 构造的运行时 TileSet。
    /// </summary>
    public ChunkTileMapRenderer(TileMapLayer layer)
    {
        _layer = layer;
        _layer.TileSet = RuntimeTileSetFactory.Create();
    }

    /// <summary>
    /// 把区块的所有 Tile 绘制到相对于 originChunk 的本地单元格位置。
    /// </summary>
    public void Draw(GeneratedChunk chunk, ChunkCoordinate originChunk)
    {
        ChunkCoordinate localChunk = chunk.Coordinate - originChunk;
        int baseX = checked((int)localChunk.X) * WorldMetrics.ChunkTiles;
        int baseY = checked((int)localChunk.Y) * WorldMetrics.ChunkTiles;

        for (int y = 0; y < WorldMetrics.ChunkTiles; y++)
        {
            for (int x = 0; x < WorldMetrics.ChunkTiles; x++)
            {
                _layer.SetCell(
                    new Vector2I(baseX + x, baseY + y),
                    (int)chunk.Get(x, y),
                    Vector2I.Zero);
            }
        }
    }

    /// <summary>
    /// 擦除指定绝对区块在当前本地原点下占据的所有 TileMap 单元格。
    /// </summary>
    public void Erase(ChunkCoordinate absoluteChunk, ChunkCoordinate originChunk)
    {
        ChunkCoordinate localChunk = absoluteChunk - originChunk;
        int baseX = checked((int)localChunk.X) * WorldMetrics.ChunkTiles;
        int baseY = checked((int)localChunk.Y) * WorldMetrics.ChunkTiles;

        for (int y = 0; y < WorldMetrics.ChunkTiles; y++)
        {
            for (int x = 0; x < WorldMetrics.ChunkTiles; x++)
            {
                _layer.EraseCell(new Vector2I(baseX + x, baseY + y));
            }
        }
    }

    /// <summary>
    /// 清空整个渲染层，通常在本地原点重定位后调用以重新投影活动区块。
    /// </summary>
    public void Clear() => _layer.Clear();
}
