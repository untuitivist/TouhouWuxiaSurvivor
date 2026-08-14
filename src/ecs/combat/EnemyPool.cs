using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Core;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 以尾部交换维护紧凑敌人数组，避免每个敌人都进入 Godot 场景树。
/// </summary>
public sealed class EnemyPool
{
    private readonly List<EnemyComponent> _items = new();
    private readonly Dictionary<int, int> _indicesByEntity = new();
    private int _nextEntityValue = 100000;

    /// <summary>获取当前活跃敌人数量。</summary>
    public int Count => _items.Count;

    /// <summary>获取仍存活的角色 Boss 数量，普通敌人与死亡反馈不会被计入。</summary>
    public int AliveBossCount
    {
        get
        {
            int count = 0;
            for (int index = 0; index < _items.Count; index++)
            {
                if (_items[index].Alive && _items[index].Definition.IsBoss)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>添加一个由刷怪器选定定义的敌人。</summary>
    public EcsEntity Add(Vector2 position, EnemyDefinition definition)
    {
        var entity = new EcsEntity(_nextEntityValue++);
        _indicesByEntity.Add(entity.Value, _items.Count);
        _items.Add(new EnemyComponent(entity, position, definition));
        return entity;
    }

    /// <summary>按连续索引读取敌人数据。</summary>
    public EnemyComponent Get(int index) => _items[index];

    /// <summary>写回系统计算后的敌人数据。</summary>
    public void Set(int index, EnemyComponent component)
    {
        EnemyComponent previous = _items[index];
        if (!previous.Entity.Equals(component.Entity))
        {
            _indicesByEntity.Remove(previous.Entity.Value);
            _indicesByEntity[component.Entity.Value] = index;
        }

        _items[index] = component;
    }

    /// <summary>用尾部数据覆盖需要删除的敌人索引，并同步维护实体句柄到新索引的映射。</summary>
    public void RemoveSwap(int index)
    {
        int lastIndex = _items.Count - 1;
        EnemyComponent removed = _items[index];
        _indicesByEntity.Remove(removed.Entity.Value);
        if (index == lastIndex)
        {
            return;
        }

        EnemyComponent moved = _items[lastIndex];
        _items[index] = moved;
        _indicesByEntity[moved.Entity.Value] = index;
    }

    /// <summary>删除连续数组中的尾部项；直接删尾时一并清除仍指向尾索引的句柄映射。</summary>
    public void TrimLast()
    {
        int lastIndex = _items.Count - 1;
        int entityValue = _items[lastIndex].Entity.Value;
        if (_indicesByEntity.TryGetValue(entityValue, out int mappedIndex) &&
            mappedIndex == lastIndex)
        {
            _indicesByEntity.Remove(entityValue);
        }

        _items.RemoveAt(lastIndex);
    }

    /// <summary>按稳定实体句柄查找仍存活的敌人和当前池索引，尾部交换不会使句柄失效。</summary>
    public bool TryGetAlive(
        EcsEntity entity,
        out int index,
        out EnemyComponent component)
    {
        if (entity.IsValid && _indicesByEntity.TryGetValue(entity.Value, out index))
        {
            component = _items[index];
            return component.Alive && component.Entity.Equals(entity);
        }

        index = -1;
        component = default;
        return false;
    }

    /// <summary>对全部敌人执行只读访问，供批量绘制和调试统计使用。</summary>
    public void ForEach(Action<EnemyComponent> visitor)
    {
        for (int index = 0; index < _items.Count; index++)
        {
            visitor(_items[index]);
        }
    }

    /// <summary>无分配统计指定范围内的存活敌人，供自动触发与调试查询复用紧凑数据池。</summary>
    public int CountAliveInRange(Vector2 origin, float range)
    {
        if (!float.IsFinite(range) || range <= 0.0f) return 0;
        int count = 0;
        float squared = range * range;
        for (int index = 0; index < _items.Count; index++)
        {
            EnemyComponent enemy = _items[index];
            if (enemy.Alive && origin.DistanceSquaredTo(enemy.Position) <= squared) count++;
        }
        return count;
    }
}
