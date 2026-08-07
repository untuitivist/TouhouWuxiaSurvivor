namespace TouhouWuxiaSurvivor.World.Coordinates;

/// <summary>
/// 定义像素、Tile、Chunk 与本地原点重定位之间的统一尺度，供生成、渲染和地图共享。
/// </summary>
public static class WorldMetrics
{
    public const int TilePixels = 16;
    public const int ChunkTiles = 32;
    public const int ChunkPixels = TilePixels * ChunkTiles;
    public const int RebaseDistanceChunks = 8;
}
