using Godot;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.World.Coordinates;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 把存活敌人的中心写入固定尺寸均匀网格，使玩家弹只检查附近候选而非完整敌人池。
/// </summary>
public sealed class EnemySpatialHash
{
    public const int CellSize = 64;
    private readonly Dictionary<(long X, long Y), List<int>> _cells = [];
    private readonly List<(long X, long Y)> _emptyCells = [];
    private float _maximumRadius;

    public int AliveCount { get; private set; }

    /// <summary>
    /// 每个物理帧从紧凑敌人池重建索引；死亡反馈实体不进入网格，但仍保留在原池供渲染。
    /// </summary>
    public void Build(EnemyPool enemies)
    {
        foreach (List<int> indices in _cells.Values)
        {
            indices.Clear();
        }

        AliveCount = 0;
        _maximumRadius = 1.0f;
        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyComponent enemy = enemies.Get(index);
            if (!enemy.Alive)
            {
                continue;
            }

            (long x, long y) = GetCell(enemy.Position);
            if (!_cells.TryGetValue((x, y), out List<int>? indices))
            {
                indices = [];
                _cells.Add((x, y), indices);
            }

            indices.Add(index);
            AliveCount++;
            _maximumRadius = Math.Max(_maximumRadius, enemy.Definition.CollisionRadius);
        }

        RemoveEmptyCells();
    }

    /// <summary>
    /// 查询与投射物重叠且原池索引最小的敌人，保持与旧版从零遍历时完全相同的首个命中语义。
    /// </summary>
    public bool TryFindFirstOverlap(
        ProjectileComponent projectile,
        EnemyPool enemies,
        out int enemyIndex,
        ref int candidateChecks)
    {
        enemyIndex = int.MaxValue;
        float reach = projectile.Radius + _maximumRadius;
        long minimumX = FloorCell(projectile.Position.X - reach);
        long maximumX = FloorCell(projectile.Position.X + reach);
        long minimumY = FloorCell(projectile.Position.Y - reach);
        long maximumY = FloorCell(projectile.Position.Y + reach);
        for (long y = minimumY; y <= maximumY; y++)
        {
            for (long x = minimumX; x <= maximumX; x++)
            {
                if (!_cells.TryGetValue((x, y), out List<int>? indices))
                {
                    continue;
                }

                FindOverlapInCell(projectile, enemies, indices, ref enemyIndex,
                    ref candidateChecks);
            }
        }

        return enemyIndex != int.MaxValue;
    }

    /// <summary>
    /// 检查单个网格桶内候选；即使较大索引先被访问，最终仍返回旧算法会命中的最小索引。
    /// </summary>
    private static void FindOverlapInCell(
        ProjectileComponent projectile,
        EnemyPool enemies,
        IReadOnlyList<int> indices,
        ref int firstIndex,
        ref int candidateChecks)
    {
        foreach (int index in indices)
        {
            candidateChecks++;
            EnemyComponent enemy = enemies.Get(index);
            float radius = projectile.Radius + enemy.Definition.CollisionRadius;
            if (index < firstIndex && enemy.Alive &&
                (ulong)enemy.Entity.Value != projectile.LastHitIdentity &&
                projectile.Position.DistanceSquaredTo(enemy.Position) <= radius * radius)
            {
                firstIndex = index;
            }
        }
    }

    /// <summary>
    /// 删除上帧存在而本帧没有实体的空桶，防止玩家无限旅行后字典只增不减。
    /// </summary>
    private void RemoveEmptyCells()
    {
        _emptyCells.Clear();
        foreach (var pair in _cells)
        {
            if (pair.Value.Count == 0)
            {
                _emptyCells.Add(pair.Key);
            }
        }

        foreach ((long x, long y) in _emptyCells)
        {
            _cells.Remove((x, y));
        }
    }

    /// <summary>把浮点世界位置映射到支持负坐标的整数网格。</summary>
    private static (long X, long Y) GetCell(Vector2 position) =>
        (FloorCell(position.X), FloorCell(position.Y));

    /// <summary>使用向下取整而非截断，保证零点两侧网格尺寸一致。</summary>
    private static long FloorCell(float coordinate) =>
        (long)Math.Floor(coordinate / CellSize);
}
