using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Ecs.Combat;

namespace TouhouWuxiaSurvivor.Gameplay.Spawning;

/// <summary>
/// 将敌人击破事件转换为概率掉落，并把实例统一放入可随世界原点重定位的容器。
/// </summary>
public partial class PickupSpawner : Node
{
    private readonly RandomNumberGenerator _random = new();
    private Node2D? _pickupContainer;
    private EcsCombatWorld? _ecsWorld;

    [Export]
    public PackedScene? PickupScene { get; set; }

    public int SpawnedCount { get; private set; }
    public event Action? PickupCollected;

    /// <summary>
    /// 绑定本局掉落物容器并随机化独立随机源，避免影响地图或敌人生成序列。
    /// </summary>
    public void Configure(Node2D pickupContainer)
    {
        _pickupContainer = pickupContainer;
        _random.Randomize();
    }

    /// <summary>绑定 ECS 世界并接收批量拾取事件。</summary>
    public void ConfigureEcs(EcsCombatWorld world)
    {
        _ecsWorld = world;
        world.PickupCollected += OnPickupCollected;
    }

    /// <summary>
    /// 根据被击破敌人的独立掉落概率决定是否抽取并生成一种强化物。
    /// </summary>
    public void TrySpawnForEnemy(Vector2 position, EnemyDefinition enemy)
    {
        if (_random.Randf() <= enemy.DropChance)
        {
            PickupDefinition definition = PickupCatalog.Choose(_random);
            Callable.From(() => Spawn(definition, position)).CallDeferred();
        }
    }

    /// <summary>
    /// 在指定世界位置生成指定种类掉落物，供固定奖励和集成验证复用。
    /// </summary>
    public void Spawn(PickupKind kind, Vector2 position) =>
        Spawn(PickupCatalog.Get(kind), position);

    /// <summary>
    /// 实例化配置完成的掉落实体并设置全局坐标，确保父容器重定位后仍可正确拾取。
    /// </summary>
    private void Spawn(PickupDefinition definition, Vector2 position)
    {
        if (_ecsWorld is not null)
        {
            _ecsWorld.SpawnPickup(definition.Kind, position);
            SpawnedCount++;
            return;
        }

        if (PickupScene is null || _pickupContainer is null)
        {
            return;
        }

        var pickup = PickupScene.Instantiate<PickupActor>();
        pickup.Configure(definition);
        pickup.Collected += OnPickupCollected;
        _pickupContainer.AddChild(pickup);
        pickup.GlobalPosition = position;
        SpawnedCount++;
    }

    /// <summary>
    /// 将任意动态掉落实体的拾取通知汇总为生成器级事件，供音频等外围系统统一订阅。
    /// </summary>
    private void OnPickupCollected() => PickupCollected?.Invoke();
}
