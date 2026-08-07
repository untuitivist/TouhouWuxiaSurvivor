using Godot;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Map;
using TouhouWuxiaSurvivor.World.Rendering;

namespace TouhouWuxiaSurvivor.World.Streaming;

/// <summary>
/// 围绕玩家维护有限活动区块窗口，分帧生成新区域，并把生成结果登记到探索地图。
/// </summary>
public sealed class ChunkStreamer
{
    private readonly HashSet<ChunkCoordinate> _active = [];
    private readonly Queue<ChunkCoordinate> _pending = [];
    private readonly WorldGenerator _generator;
    private readonly IChunkRenderer _renderer;
    private readonly int _loadRadius;
    private readonly int _chunksPerFrame;
    private ChunkCoordinate _lastCenter;
    private bool _hasCenter;

    /// <summary>
    /// 创建区块流送器；加载半径和每帧预算都会被限制为至少一。
    /// </summary>
    public ChunkStreamer(
        WorldGenerator generator,
        IChunkRenderer renderer,
        int loadRadius,
        int chunksPerFrame)
    {
        _generator = generator;
        _renderer = renderer;
        _loadRadius = Math.Max(1, loadRadius);
        _chunksPerFrame = Math.Max(1, chunksPerFrame);
    }

    public ChunkCoordinate OriginChunk { get; private set; }

    public int ActiveCount => _active.Count;

    public int PendingCount => _pending.Count;

    public ExploredMapStore ExploredMap { get; } = new(8192);

    /// <summary>
    /// 根据玩家位置刷新需要的区块，并按每帧预算处理生成队列。
    /// </summary>
    public void Update(Vector2 localPlayerPosition)
    {
        EnsureWindow(localPlayerPosition);
        GeneratePending(_chunksPerFrame);
    }

    /// <summary>
    /// 同步生成初始可视窗口，确保第一帧进入场景时周围不存在地图空洞。
    /// </summary>
    public void Prime(Vector2 localPlayerPosition)
    {
        EnsureWindow(localPlayerPosition);
        GeneratePending(int.MaxValue);
    }

    /// <summary>
    /// 将本地原点平移指定区块数，清空本地渲染缓存，但保留绝对探索地图历史。
    /// </summary>
    public void Rebase(ChunkCoordinate localShift)
    {
        OriginChunk += localShift;
        _renderer.Clear();
        _active.Clear();
        _pending.Clear();
        _hasCenter = false;
    }

    /// <summary>
    /// 把本地玩家位置转换为绝对中心区块，仅在跨越区块边界时刷新窗口。
    /// </summary>
    private void EnsureWindow(Vector2 localPlayerPosition)
    {
        ChunkCoordinate localCenter = GridMath.LocalPositionToChunk(localPlayerPosition);
        ChunkCoordinate absoluteCenter = OriginChunk + localCenter;
        if (!_hasCenter || absoluteCenter != _lastCenter)
        {
            RefreshWindow(absoluteCenter);
        }
    }

    /// <summary>
    /// 按预算消费待生成队列；过期请求会被丢弃，完成区块同时用于场景渲染与地图存档。
    /// </summary>
    private void GeneratePending(int budget)
    {
        int generated = 0;
        while (_pending.Count > 0 && generated < budget)
        {
            ChunkCoordinate coordinate = _pending.Dequeue();
            if (_active.Contains(coordinate) || !IsWanted(coordinate, _lastCenter))
            {
                continue;
            }

            GeneratedChunk chunk = _generator.Generate(coordinate);
            _renderer.Draw(chunk, OriginChunk);
            ExploredMap.Remember(chunk);
            _active.Add(coordinate);
            generated++;
        }
    }

    /// <summary>
    /// 卸载窗口外区块，收集窗口内缺失区块，并按到玩家的距离由近到远排队。
    /// </summary>
    private void RefreshWindow(ChunkCoordinate center)
    {
        _lastCenter = center;
        _hasCenter = true;

        foreach (ChunkCoordinate coordinate in _active.ToArray())
        {
            if (IsWanted(coordinate, center))
            {
                continue;
            }

            _renderer.Erase(coordinate, OriginChunk);
            _active.Remove(coordinate);
        }

        var needed = new List<ChunkCoordinate>();
        for (int y = -_loadRadius; y <= _loadRadius; y++)
        {
            for (int x = -_loadRadius; x <= _loadRadius; x++)
            {
                var coordinate = new ChunkCoordinate(center.X + x, center.Y + y);
                if (!_active.Contains(coordinate))
                {
                    needed.Add(coordinate);
                }
            }
        }

        needed.Sort((left, right) =>
            DistanceSquared(left, center).CompareTo(DistanceSquared(right, center)));
        _pending.Clear();
        foreach (ChunkCoordinate coordinate in needed)
        {
            _pending.Enqueue(coordinate);
        }
    }

    /// <summary>
    /// 判断区块是否位于以 center 为中心的方形加载窗口内。
    /// </summary>
    private bool IsWanted(ChunkCoordinate coordinate, ChunkCoordinate center) =>
        Math.Abs(coordinate.X - center.X) <= _loadRadius &&
        Math.Abs(coordinate.Y - center.Y) <= _loadRadius;

    /// <summary>
    /// 返回两个区块坐标的平方距离，排序时避免不必要的平方根计算。
    /// </summary>
    private static long DistanceSquared(ChunkCoordinate coordinate, ChunkCoordinate center)
    {
        long dx = coordinate.X - center.X;
        long dy = coordinate.Y - center.Y;
        return dx * dx + dy * dy;
    }
}
