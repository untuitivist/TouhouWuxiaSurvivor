using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Spirit;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Ecs.Combat;

namespace TouhouWuxiaSurvivor.Gameplay.Spawning;

/// <summary>
/// 将每次敌人击破确定性转换为灵息实体，并在节点上限处合并价值以控制长期性能。
/// </summary>
public partial class SpiritDropSpawner : Node
{
    private Node2D? _container;
    private Node2D? _player;
    private RunModifierState? _modifiers;
    private RunProgressionState? _progression;
    private EcsCombatWorld? _ecsWorld;

    [Export]
    public PackedScene? SpiritScene { get; set; }

    [Export(PropertyHint.Range, "16,400,1")]
    public float BaseAttractionRange { get; set; } = 72.0f;

    [Export(PropertyHint.Range, "10,1000,1")]
    public int MaximumAlive { get; set; } = 240;

    public int AliveCount => _ecsWorld?.SpiritCount ?? _container?.GetChildCount() ?? 0;
    public int SpawnedCount { get; private set; }
    public event Action<int>? SpiritCollected;

    /// <summary>
    /// 注入灵息容器、玩家、局内倍率和经验状态，避免生成器查找场景全局节点。
    /// </summary>
    public void Configure(
        Node2D container,
        Node2D player,
        RunModifierState modifiers,
        RunProgressionState progression)
    {
        _container = container;
        _player = player;
        _modifiers = modifiers;
        _progression = progression;
    }

    /// <summary>绑定 ECS 世界并接收经验交付事件。</summary>
    public void ConfigureEcs(EcsCombatWorld world)
    {
        _ecsWorld = world;
        world.SpiritCollected += OnSpiritCollected;
    }

    /// <summary>
    /// 从敌人耐久计算灵息价值，并延迟到安全时机创建或合并掉落。
    /// </summary>
    public void SpawnForEnemy(Vector2 position, EnemyDefinition enemy)
    {
        int value = SpiritValueCalculator.Calculate(enemy);
        Callable.From(() => Spawn(position, value)).CallDeferred();
    }

    /// <summary>
    /// 在指定位置生成固定价值灵息，供奖励逻辑和集成测试复用。
    /// </summary>
    public void Spawn(Vector2 position, int value)
    {
        if (_ecsWorld is not null)
        {
            _ecsWorld.SpawnSpirit(position, value);
            SpawnedCount++;
            return;
        }

        if (SpiritScene is null || _container is null || _player is null ||
            _modifiers is null || _progression is null || value <= 0)
        {
            return;
        }

        if (AliveCount >= MaximumAlive && TryMerge(position, value))
        {
            return;
        }

        var spirit = SpiritScene.Instantiate<SpiritDropActor>();
        spirit.Configure(_player, value,
            () => BaseAttractionRange * _modifiers.SpiritAttractionMultiplier);
        spirit.Collected += OnSpiritCollected;
        _container.AddChild(spirit);
        spirit.GlobalPosition = position;
        SpawnedCount++;
    }

    /// <summary>
    /// 把新价值并入距离生成点最近的现有灵息，确保达到节点上限后仍不损失经验。
    /// </summary>
    private bool TryMerge(Vector2 position, int value)
    {
        SpiritDropActor? nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (Node child in _container!.GetChildren())
        {
            if (child is not SpiritDropActor spirit)
            {
                continue;
            }

            float distance = spirit.GlobalPosition.DistanceSquaredTo(position);
            if (distance < nearestDistance)
            {
                nearest = spirit;
                nearestDistance = distance;
            }
        }

        nearest?.AddValue(value);
        return nearest is not null;
    }

    /// <summary>
    /// 将拾取值写入纯经验状态并广播反馈事件，实体本身不依赖升级界面。
    /// </summary>
    private void OnSpiritCollected(int value)
    {
        _progression!.AddExperience(value);
        SpiritCollected?.Invoke(value);
    }
}
