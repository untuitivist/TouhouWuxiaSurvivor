using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Map;

/// <summary>
/// 保存一个已生成区块的地图语义与逐格探索掩码；生成并不等同于玩家已经看见。
/// </summary>
internal sealed class ExploredChunkSnapshot
{
    private readonly BiomeId[] _biomes = new BiomeId[WorldMetrics.ChunkTiles * WorldMetrics.ChunkTiles];
    private readonly bool[] _revealed = new bool[WorldMetrics.ChunkTiles * WorldMetrics.ChunkTiles];
    private readonly TileId[] _tiles = new TileId[WorldMetrics.ChunkTiles * WorldMetrics.ChunkTiles];

    /// <summary>
    /// 从正式生成结果复制地表和群系，避免区块卸载后地图重新运行生成算法。
    /// </summary>
    public ExploredChunkSnapshot(GeneratedChunk chunk)
    {
        for (int y = 0; y < WorldMetrics.ChunkTiles; y++)
        {
            for (int x = 0; x < WorldMetrics.ChunkTiles; x++)
            {
                int index = Index(x, y);
                _tiles[index] = chunk.Get(x, y);
                _biomes[index] = chunk.GetBiome(x, y);
            }
        }
    }

    public int RevealedCount { get; private set; }

    /// <summary>
    /// 揭示一个区块内部格；首次揭示返回 true，重复经过不会重复计数。
    /// </summary>
    public bool Reveal(int localX, int localY)
    {
        int index = Index(localX, localY);
        if (_revealed[index])
        {
            return false;
        }

        _revealed[index] = true;
        RevealedCount++;
        return true;
    }

    /// <summary>
    /// 仅在玩家已经揭示该格时返回稳定地图语义。
    /// </summary>
    public bool TryGet(int localX, int localY, out ExploredMapCell cell)
    {
        int index = Index(localX, localY);
        if (!_revealed[index])
        {
            cell = default;
            return false;
        }

        cell = new ExploredMapCell(_tiles[index], _biomes[index]);
        return true;
    }

    /// <summary>
    /// 将二维区块内部坐标转换为紧凑数组下标。
    /// </summary>
    private static int Index(int x, int y) => y * WorldMetrics.ChunkTiles + x;
}
