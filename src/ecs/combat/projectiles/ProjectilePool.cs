using Godot;
using TouhouWuxiaSurvivor.Ecs.Core;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 以连续数组保存所有活跃投射物，移除时使用尾部交换避免节点式遍历和频繁分配。
/// </summary>
public sealed class ProjectilePool
{
    public const int MaximumActive = 2000;
    public const int MaximumEnemyActive = 400;
    private readonly List<ProjectileComponent> _items = new();
    private int _nextEntityValue = 1;

    /// <summary>获取当前连续数据区中的投射物数量。</summary>
    public int Count => _items.Count;

    /// <summary>统计指定阵营的活跃投射物，供 HUD、性能保护和测试读取。</summary>
    public int CountFaction(ProjectileFaction faction)
    {
        int count = 0;
        for (int index = 0; index < _items.Count; index++)
        {
            if (_items[index].Faction == faction)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 追加一颗投射物并返回其实体句柄；参数会被钳制到可运行的范围。
    /// </summary>
    public EcsEntity Add(Vector2 position, Vector2 direction, float speed, int damage)
    {
        TryAdd(position, direction, speed, damage, ProjectileFaction.Player,
            2.0f, 4.0f, 0, out EcsEntity entity);
        return entity;
    }

    /// <summary>
    /// 在全局硬上限内追加指定阵营投射物；池满时返回 false，让发射系统改用速度、伤害和后续波次增强。
    /// </summary>
    public bool TryAdd(
        Vector2 position,
        Vector2 direction,
        float speed,
        int damage,
        ProjectileFaction faction,
        float lifetime,
        float radius,
        int visualVariant,
        out EcsEntity entity)
    {
        if (_items.Count >= MaximumActive)
        {
            entity = default;
            return false;
        }

        Vector2 normalized = direction.IsZeroApprox() ? Vector2.Right : direction.Normalized();
        entity = new EcsEntity(_nextEntityValue++);
        _items.Add(new ProjectileComponent(
            entity,
            position,
            normalized * Math.Max(0.0f, speed),
            Math.Max(1, damage),
            Math.Max(0.05f, lifetime),
            Math.Max(1.0f, radius),
            faction,
            visualVariant));
        return true;
    }

    /// <summary>按连续索引读取组件快照，系统完成修改后必须写回。</summary>
    public ProjectileComponent Get(int index) => _items[index];

    /// <summary>把系统更新后的组件写回连续数据区。</summary>
    public void Set(int index, ProjectileComponent component) => _items[index] = component;

    /// <summary>
    /// 用最后一项填充被移除位置，保证下一帧遍历仍然是紧凑的 O(n) 数组。
    /// </summary>
    public void RemoveSwap(int index) => _items[index] = _items[^1];

    /// <summary>
    /// 移除连续数组中的最后一项；调用方应先用 RemoveSwap 完成覆盖。
    /// </summary>
    public void TrimLast() => _items.RemoveAt(_items.Count - 1);

    /// <summary>
    /// 对所有投射物执行只读回调，供渲染器或调试统计使用。
    /// </summary>
    public void ForEach(Action<ProjectileComponent> visitor)
    {
        for (int index = 0; index < _items.Count; index++)
        {
            visitor(_items[index]);
        }
    }
}
