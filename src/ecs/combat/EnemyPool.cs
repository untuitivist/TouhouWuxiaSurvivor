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
        _items.Add(new EnemyComponent(entity, position, definition));
        return entity;
    }

    /// <summary>按连续索引读取敌人数据。</summary>
    public EnemyComponent Get(int index) => _items[index];

    /// <summary>写回系统计算后的敌人数据。</summary>
    public void Set(int index, EnemyComponent component) => _items[index] = component;

    /// <summary>用尾部数据覆盖需要删除的敌人索引。</summary>
    public void RemoveSwap(int index) => _items[index] = _items[^1];

    /// <summary>删除连续数组中的尾部项。</summary>
    public void TrimLast() => _items.RemoveAt(_items.Count - 1);

    /// <summary>对全部敌人执行只读访问，供批量绘制和调试统计使用。</summary>
    public void ForEach(Action<EnemyComponent> visitor)
    {
        for (int index = 0; index < _items.Count; index++)
        {
            visitor(_items[index]);
        }
    }
}
