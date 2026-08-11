using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 高数量战斗实体的唯一运行时数据入口；Godot 场景只保留这个批量桥接节点。
/// </summary>
public partial class EcsCombatWorld : Node2D
{
    private readonly EnemyPool _enemies = new();
    private readonly ProjectilePool _projectiles = new();
    private readonly List<PickupComponent> _pickups = new();
    private readonly List<SpiritComponent> _spirits = new();
    private readonly EnemyMovementSystem _enemyMovement = new();
    private readonly ProjectileMovementSystem _projectileMovement = new();
    private readonly ProjectileCollisionSystem _projectileCollision = new();
    private readonly PickupSystem _pickupSystem = new();
    private readonly SpiritSystem _spiritSystem = new();
    private readonly EcsCombatRenderer _renderer = new();
    private double _elapsedSeconds;
    private PlayerController? _player;
    private PlayerHealth? _health;
    private PlayerBuffController? _buffs;
    private RunModifierState? _modifiers;

    /// <summary>敌人击破事件，参数为位置和定义。</summary>
    public event Action<Vector2, EnemyDefinition>? EnemyDefeated;
    /// <summary>非致命敌人受击事件。</summary>
    public event Action? EnemyDamaged;
    /// <summary>自爆敌人进入死亡状态事件。</summary>
    public event Action? EnemyExploded;
    /// <summary>强化掉落物被玩家拾取事件。</summary>
    public event Action? PickupCollected;
    /// <summary>灵息被玩家吸收事件。</summary>
    public event Action<int>? SpiritCollected;
    /// <summary>当前活跃敌人数量。</summary>
    public int EnemyCount => _enemies.Count;
    /// <summary>获取当前仍可被索敌和命中的敌人数量。</summary>
    public int AliveEnemyCount
    {
        get
        {
            int count = 0;
            for (int index = 0; index < _enemies.Count; index++)
                if (_enemies.Get(index).Alive) count++;
            return count;
        }
    }
    /// <summary>当前活跃投射物数量。</summary>
    public int ProjectileCount => _projectiles.Count;
    /// <summary>获取从本局开始累计生成的投射物数量。</summary>
    public int TotalProjectilesSpawned { get; private set; }
    /// <summary>当前活跃强化掉落物数量。</summary>
    public int PickupCount => _pickups.Count;
    /// <summary>当前活跃灵息数量。</summary>
    public int SpiritCount => _spirits.Count;
    /// <summary>累计击破数。</summary>
    public int DefeatedCount { get; private set; }
    /// <summary>获取本局 ECS 世界运行时间，供刷怪节奏和结算读取。</summary>
    public double ElapsedSeconds => _elapsedSeconds;
    /// <summary>获取上一绘制帧使用图鉴内部素材的敌人数。</summary>
    public int MappedEnemyVisualCount => _renderer.LastMappedEnemyCount;
    /// <summary>获取上一绘制帧回退为中文名的敌人数。</summary>
    public int FallbackEnemyVisualCount => _renderer.LastFallbackEnemyCount;
    /// <summary>获取上一绘制帧使用图集图标的强化掉落数。</summary>
    public int PickupIconVisualCount => _renderer.LastPickupIconCount;
    /// <summary>获取上一绘制帧使用东方道具图集的灵息数。</summary>
    public int SpiritIconVisualCount => _renderer.LastSpiritIconCount;
    /// <summary>获取上一绘制帧使用原作弹幕图集的玩家弹数。</summary>
    public int ProjectileIconVisualCount => _renderer.LastProjectileIconCount;

    /// <summary>绑定玩家和局内状态，使批量系统不依赖场景查找。</summary>
    public void Configure(PlayerController player, PlayerHealth health, PlayerBuffController buffs,
        RunModifierState modifiers)
    {
        _player = player;
        _health = health;
        _buffs = buffs;
        _modifiers = modifiers;
        _renderer.Configure();
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
    }

    /// <summary>添加一个敌人数据项，不创建 EnemyActor 节点。</summary>
    public void SpawnEnemy(Vector2 position, EnemyDefinition definition) => _enemies.Add(position, definition);

    /// <summary>添加一颗玩家投射物到连续数据池。</summary>
    public void SpawnProjectile(Vector2 position, Vector2 direction, float speed, int damage)
    {
        _projectiles.Add(position, direction, speed, damage);
        TotalProjectilesSpawned++;
    }

    /// <summary>添加一个强化掉落物到数据池。</summary>
    public void SpawnPickup(PickupKind kind, Vector2 position) =>
        _pickups.Add(new PickupComponent(new Core.EcsEntity(_pickups.Count + 200000),
            position, PickupCatalog.Get(kind)));

    /// <summary>添加或合并一份灵息奖励，避免节点上限造成经验丢失。</summary>
    public void SpawnSpirit(Vector2 position, int value)
    {
        if (value <= 0) return;
        if (_spirits.Count >= 240 && TryMergeSpirit(position, value)) return;
        _spirits.Add(new SpiritComponent(new Core.EcsEntity(_spirits.Count + 300000), position, value));
    }

    /// <summary>对指定位置最近敌人执行批量索敌。</summary>
    public bool TryFindNearest(Vector2 origin, float range, out Vector2 position)
    {
        position = default;
        float best = range * range;
        bool found = false;
        for (int index = 0; index < _enemies.Count; index++)
        {
            EnemyComponent enemy = _enemies.Get(index);
            if (!enemy.Alive) continue;
            float distance = origin.DistanceSquaredTo(enemy.Position);
            if (distance >= best) continue;
            best = distance;
            position = enemy.Position;
            found = true;
        }
        return found;
    }

    /// <summary>返回范围内存活敌人的位置，供符卡范围效果复用。</summary>
    public IReadOnlyList<Vector2> SelectEnemies(Vector2 origin, float range, int maximum = int.MaxValue)
    {
        var result = new List<(float distance, Vector2 position)>();
        float squared = range * range;
        for (int index = 0; index < _enemies.Count; index++)
        {
            EnemyComponent enemy = _enemies.Get(index);
            if (enemy.Alive && origin.DistanceSquaredTo(enemy.Position) <= squared)
                result.Add((origin.DistanceSquaredTo(enemy.Position), enemy.Position));
        }
        return result.OrderBy(item => item.distance).Take(maximum).Select(item => item.position).ToArray();
    }

    /// <summary>对范围内敌人逐项施加伤害并发出击破事件。</summary>
    public int DamageEnemies(Vector2 origin, float range, int damage)
    {
        int hitCount = 0;
        float squared = range * range;
        for (int index = _enemies.Count - 1; index >= 0; index--)
        {
            EnemyComponent enemy = _enemies.Get(index);
            if (!enemy.Alive || origin.DistanceSquaredTo(enemy.Position) > squared) continue;
            ApplyDamage(index, damage, enemy);
            hitCount++;
        }
        return hitCount;
    }

    /// <summary>同步世界重定位，所有 ECS 实体保持相对玩家的局部距离。</summary>
    public void Rebase(Vector2 offset)
    {
        for (int index = 0; index < _enemies.Count; index++) { var item = _enemies.Get(index); item.Position -= offset; _enemies.Set(index, item); }
        for (int index = 0; index < _projectiles.Count; index++) { var item = _projectiles.Get(index); item.Position -= offset; _projectiles.Set(index, item); }
        for (int index = 0; index < _pickups.Count; index++) { var item = _pickups[index]; item.Position -= offset; _pickups[index] = item; }
        for (int index = 0; index < _spirits.Count; index++) { var item = _spirits[index]; item.Position -= offset; _spirits[index] = item; }
    }

    /// <summary>回收远离玩家的敌人，防止无限移动时死亡反馈或场外实体长期积压。</summary>
    public void RecycleDistant(Vector2 playerPosition, float distance)
    {
        float squared = distance * distance;
        for (int index = _enemies.Count - 1; index >= 0; index--)
        {
            if (_enemies.Get(index).Position.DistanceSquaredTo(playerPosition) <= squared) continue;
            _enemies.RemoveSwap(index);
            _enemies.TrimLast();
        }
    }

    /// <summary>按固定系统顺序推进敌人、投射物、掉落物和灵息。</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_player is null || _health is null || _buffs is null || _modifiers is null) return;
        _elapsedSeconds += delta;
        _enemyMovement.Step(_enemies, _player.GlobalPosition, (float)delta, amount => _health.ApplyDamage(amount));
        _projectileMovement.Step(_projectiles, (float)delta);
        ResolveProjectileHits();
        _pickupSystem.Step(_pickups, _player.GlobalPosition, _buffs, (float)delta, () => PickupCollected?.Invoke());
        _spiritSystem.Step(_spirits, _player.GlobalPosition, 72.0f * _modifiers.SpiritAttractionMultiplier,
            (float)delta, value => SpiritCollected?.Invoke(value));
        QueueRedraw();
    }

    /// <summary>把当前 ECS 数据交给共享素材批量渲染器，不为单个实体创建节点。</summary>
    public override void _Draw() =>
        _renderer.Draw(this, _enemies, _pickups, _spirits, _projectiles, _elapsedSeconds);

    /// <summary>遍历投射物并在首次命中时消费数据。</summary>
    private void ResolveProjectileHits()
    {
        for (int projectileIndex = _projectiles.Count - 1; projectileIndex >= 0; projectileIndex--)
        {
            ProjectileComponent projectile = _projectiles.Get(projectileIndex);
            for (int enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
            {
                EnemyComponent enemy = _enemies.Get(enemyIndex);
                float radius = projectile.Radius + enemy.Definition.CollisionRadius;
                if (!enemy.Alive || projectile.Position.DistanceSquaredTo(enemy.Position) > radius * radius) continue;
                ApplyDamage(enemyIndex, projectile.Damage, enemy); _projectiles.RemoveSwap(projectileIndex); _projectiles.TrimLast(); break;
            }
        }
    }

    /// <summary>应用伤害并转换为受击、死亡、掉落事件。</summary>
    private void ApplyDamage(int index, int amount, EnemyComponent enemy)
    {
        if (amount <= 0 || !enemy.Alive) return;
        enemy.Health -= amount;
        if (enemy.Health > 0) { enemy.HurtTime = 0.12f; EnemyDamaged?.Invoke(); _enemies.Set(index, enemy); return; }
        enemy.Alive = false; enemy.DeathTime = enemy.Definition.ExplodesOnDeath ? 0.28f : 0.18f; DefeatedCount++;
        EnemyDefeated?.Invoke(enemy.Position, enemy.Definition); if (enemy.Definition.ExplodesOnDeath) EnemyExploded?.Invoke(); _enemies.Set(index, enemy);
    }

    /// <summary>把新灵息价值并入距离最近的灵息。</summary>
    private bool TryMergeSpirit(Vector2 position, int value)
    {
        if (_spirits.Count == 0) return false;
        int nearest = 0; float distance = float.MaxValue;
        for (int index = 0; index < _spirits.Count; index++) { float current = _spirits[index].Position.DistanceSquaredTo(position); if (current < distance) { distance = current; nearest = index; } }
        var spirit = _spirits[nearest]; spirit.Value += value; _spirits[nearest] = spirit; return true;
    }
}
