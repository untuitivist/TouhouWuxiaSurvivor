using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Targeting;
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
    private readonly EnemyTargetAccess _enemyTargets = new();
    private readonly EnemyProjectileSystem _enemyProjectiles = new();
    private readonly ProjectileMovementSystem _projectileMovement = new();
    private readonly ProjectileCollisionSystem _projectileCollision = new();
    private readonly PickupSystem _pickupSystem = new();
    private readonly SpiritSystem _spiritSystem = new();
    private readonly AreaDamageSystem _areaDamage = new();
    private readonly EcsCombatRenderer _renderer = new();
    private double _elapsedSeconds;
    private PlayerController? _player;
    private PlayerHealth? _health;
    private PlayerBuffController? _buffs;
    private RunModifierState? _modifiers;
    private int _playerVisualSourceId;

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
    /// <summary>角色 Boss 正式写入 ECS 池时发出，参数为生成位置与完整定义。</summary>
    public event Action<Vector2, EnemyDefinition>? BossSpawned;
    /// <summary>角色 Boss 生命归零时发出，供遭遇导演、HUD 和结算独立订阅。</summary>
    public event Action<Vector2, EnemyDefinition>? BossDefeated;
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
    /// <summary>获取当前仍存活的角色 Boss 数量，不包含死亡消散反馈。</summary>
    public int AliveBossCount => _enemies.AliveBossCount;
    /// <summary>获取当前敌方阵营投射物数量，供弹幕密度 HUD 与性能测试读取。</summary>
    public int EnemyProjectileCount => _projectiles.CountFaction(ProjectileFaction.Enemy);
    /// <summary>获取玩家与敌人共享的投射物硬上限。</summary>
    public int ProjectileCapacity => ProjectilePool.MaximumActive;
    /// <summary>获取从本局开始累计生成的投射物数量。</summary>
    public int TotalProjectilesSpawned { get; private set; }
    /// <summary>获取本局累计成功写入池中的敌方弹幕数量。</summary>
    public int TotalEnemyProjectilesSpawned { get; private set; }
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
    /// <summary>获取上一绘制帧使用原作弹幕图集的玩家与敌方投射物总数。</summary>
    public int ProjectileIconVisualCount => _renderer.LastProjectileIconCount;
    /// <summary>获取上一绘制帧使用角色立绘或角色条的 Boss 数量。</summary>
    public int MappedBossVisualCount => _renderer.LastMappedBossCount;
    /// <summary>获取上一绘制帧因缺少角色素材而回退中文名的 Boss 数量。</summary>
    public int FallbackBossVisualCount => _renderer.LastFallbackBossCount;
    /// <summary>获取上一绘制帧使用内部弹幕图集的敌方投射物数量。</summary>
    public int EnemyProjectileIconVisualCount => _renderer.LastEnemyProjectileIconCount;
    /// <summary>获取上一物理帧玩家弹实际检查的空间索引候选数量。</summary>
    public int ProjectileCollisionCandidateChecks => _projectileCollision.LastCandidateChecks;
    /// <summary>兼容性能快照使用的稳定命名，返回上一物理帧玩家弹候选检查数。</summary>
    public int LastPlayerCollisionCandidateChecks => _projectileCollision.LastCandidateChecks;
    /// <summary>获取上一物理帧旧版全量碰撞遍历对应的比较次数上界。</summary>
    public long ProjectileCollisionNaiveUpperBound =>
        _projectileCollision.LastNaiveComparisonUpperBound;
    /// <summary>获取上一绘制帧真正提交绘制的敌人数量。</summary>
    public int VisibleEnemyRenderCount => _renderer.LastVisibleEnemyCount;
    /// <summary>获取上一绘制帧真正提交绘制的投射物数量。</summary>
    public int VisibleProjectileRenderCount => _renderer.LastVisibleProjectileCount;
    /// <summary>获取上一绘制帧被 CPU 可视矩形拒绝的战斗实体数量。</summary>
    public int CulledCombatRenderCount => _renderer.LastCulledEntityCount;

    /// <summary>绑定玩家和局内状态，使批量系统不依赖场景查找。</summary>
    public void Configure(PlayerController player, PlayerHealth health, PlayerBuffController buffs,
        RunModifierState modifiers, string playerVisualSourcePackId)
    {
        _player = player;
        _health = health;
        _buffs = buffs;
        _modifiers = modifiers;
        _playerVisualSourceId = ProjectileVisualSourceBindingCatalog.GetBindingId(
            playerVisualSourcePackId);
        _renderer.Configure();
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
    }

    /// <summary>添加一个敌人数据项，不创建 EnemyActor 节点。</summary>
    public Core.EcsEntity SpawnEnemy(Vector2 position, EnemyDefinition definition) =>
        _enemies.Add(position, definition);

    /// <summary>
    /// 把角色 Boss 定义写入独立语义入口；拒绝普通定义，防止遭遇系统意外绕过 Boss 约束。
    /// </summary>
    public Core.EcsEntity SpawnBoss(Vector2 position, EnemyDefinition definition)
    {
        if (!definition.IsBoss || string.IsNullOrWhiteSpace(definition.CharacterId))
        {
            throw new ArgumentException("Boss definition requires a stable character id.", nameof(definition));
        }

        Core.EcsEntity entity = _enemies.Add(position, definition);
        BossSpawned?.Invoke(position, definition);
        return entity;
    }

    /// <summary>添加一颗玩家投射物到连续数据池。</summary>
    public void SpawnProjectile(
        Vector2 position,
        Vector2 direction,
        float speed,
        int damage,
        int maximumHits = 1, int secondaryHitDamage = -1, int visualVariant = 0,
        int visualSourceId = 0)
    {
        int source = visualSourceId > 0 ? visualSourceId : _playerVisualSourceId;
        if (EcsProjectileSpawner.TrySpawnPlayer(_projectiles, position, direction,
                speed, damage, maximumHits, secondaryHitDamage, visualVariant, source))
        {
            TotalProjectilesSpawned++;
        }
    }

    /// <summary>
    /// 在敌方四百发软上限内生成敌弹，为玩家后期弹幕预留至少一千六百发容量。
    /// </summary>
    public bool SpawnEnemyProjectile(
        Vector2 position,
        Vector2 direction,
        float speed,
        int damage,
        int visualVariant = 0,
        int visualStyleId = 0,
        int visualSourceId = 0)
    {
        bool spawned = EcsProjectileSpawner.TrySpawnEnemy(_projectiles,
            position, direction, speed, damage, visualVariant,
            visualStyleId, visualSourceId);
        if (spawned) TotalEnemyProjectilesSpawned++;
        return spawned;
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
        => _enemyTargets.TryFindNearest(_enemies, origin, range, out position);

    /// <summary>返回射程内最近存活敌人的当前位置与速度，供通用预判索敌消费。</summary>
    public bool TryFindNearestTarget(Vector2 origin, float range, out TargetMotion motion) =>
        _enemyTargets.TryFindNearestMotion(_enemies, origin, range, out motion);

    /// <summary>返回范围内存活敌人的位置，供符卡范围效果复用。</summary>
    public IReadOnlyList<Vector2> SelectEnemies(Vector2 origin, float range, int maximum = int.MaxValue)
        => _enemyTargets.Select(_enemies, origin, range, maximum)
            .Select(target => target.Position).ToArray();

    /// <summary>
    /// 返回范围内敌人的稳定句柄与当前位置，供低数量跨帧效果追踪而不暴露连续池索引。
    /// </summary>
    public IReadOnlyList<(Core.EcsEntity Entity, Vector2 Position)> SelectEnemyTargets(
        Vector2 origin,
        float range,
        int maximum = int.MaxValue) =>
        _enemyTargets.Select(_enemies, origin, range, maximum);

    /// <summary>按稳定威胁规则返回集中型攻势目标，具体排序由独立选择器维护。</summary>
    public bool TryFindHighestThreat(Vector2 origin, float range, out Vector2 position)
        => EnemyThreatTargetSelector.TrySelect(_enemies, origin, range, out position);

    /// <summary>按稳定威胁规则返回目标句柄与当前位置，使投射物能跨越池交换持续追踪同一敌人。</summary>
    public bool TryFindHighestThreatTarget(
        Vector2 origin,
        float range,
        out Core.EcsEntity entity,
        out Vector2 position) =>
        EnemyThreatTargetSelector.TrySelect(_enemies, origin, range, out entity, out position);

    /// <summary>按实体句柄读取活体敌人的最新位置；死亡或回收后返回 false。</summary>
    public bool TryGetEnemyPosition(Core.EcsEntity entity, out Vector2 position)
        => _enemyTargets.TryGetPosition(_enemies, entity, out position);

    /// <summary>按稳定实体句柄施加一次伤害，避免追踪弹在尾部交换后误伤占用旧索引的敌人。</summary>
    public bool DamageEnemy(Core.EcsEntity entity, int damage)
        => _enemyTargets.Damage(_enemies, entity, damage, ApplyDamageByIndex);

    /// <summary>无分配统计范围内存活敌人，供低频奥义触发判定读取而不创建索敌数组。</summary>
    public int CountEnemiesInRange(Vector2 origin, float range) =>
        _enemies.CountAliveInRange(origin, range);

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

    /// <summary>
    /// 对范围内最近目标施加距离衰减伤害，供奥义等低频范围效果遵守明确命中预算。
    /// </summary>
    public int DamageNearestEnemies(
        Vector2 origin,
        float range,
        int damage,
        int maximumTargets,
        float minimumMultiplier) => _areaDamage.Apply(
            _enemies, origin, range, damage, maximumTargets,
            minimumMultiplier, ApplyDamageByIndex);

    /// <summary>同步世界重定位，所有 ECS 实体保持相对玩家的局部距离。</summary>
    public void Rebase(Vector2 offset)
    {
        for (int index = 0; index < _enemies.Count; index++) { var item = _enemies.Get(index); item.Translate(-offset); _enemies.Set(index, item); }
        for (int index = 0; index < _projectiles.Count; index++) { var item = _projectiles.Get(index); item.Translate(-offset); _projectiles.Set(index, item); }
        for (int index = 0; index < _pickups.Count; index++) { var item = _pickups[index]; item.Position -= offset; _pickups[index] = item; }
        for (int index = 0; index < _spirits.Count; index++) { var item = _spirits[index]; item.Translate(-offset); _spirits[index] = item; }
    }

    /// <summary>回收远离玩家的敌人，防止无限移动时死亡反馈或场外实体长期积压。</summary>
    public void RecycleDistant(Vector2 playerPosition, float distance)
    {
        float squared = distance * distance;
        for (int index = _enemies.Count - 1; index >= 0; index--)
        {
            EnemyComponent enemy = _enemies.Get(index);
            if (enemy.Definition.IsBoss ||
                enemy.Position.DistanceSquaredTo(playerPosition) <= squared) continue;
            _enemies.RemoveSwap(index);
            _enemies.TrimLast();
        }
    }

    /// <summary>按固定系统顺序推进敌人、投射物、掉落物和灵息。</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_player is null || _health is null || _buffs is null || _modifiers is null) return;
        _elapsedSeconds += delta;
        _enemyMovement.Step(_enemies, _player.GlobalPosition, (float)delta,
            amount => _health.ApplyDamage(amount));
        _enemyProjectiles.Step(_enemies, _player.GlobalPosition, (float)delta,
            request => SpawnEnemyProjectile(request.Position, request.Direction,
                request.Speed, request.Damage, request.VisualVariant,
                request.VisualStyleId, request.VisualSourceId));
        _projectileMovement.Step(_projectiles, (float)delta);
        ResolveProjectileHits();
        _pickupSystem.Step(_pickups, _player.GlobalPosition, _buffs, (float)delta, () => PickupCollected?.Invoke());
        _spiritSystem.Step(_spirits, _player.GlobalPosition, 72.0f * _modifiers.SpiritAttractionMultiplier,
            (float)delta, value => SpiritCollected?.Invoke(value));
    }

    /// <summary>每个渲染帧请求重绘，使高刷新率画面可以取样两个固定物理状态之间的位置。</summary>
    public override void _Process(double delta) => QueueRedraw();

    /// <summary>把当前 ECS 数据按物理插值比例交给批量渲染器，不为单个实体创建节点。</summary>
    public override void _Draw() =>
        _renderer.Draw(this, _enemies, _pickups, _spirits, _projectiles,
            _elapsedSeconds, (float)Engine.GetPhysicsInterpolationFraction());

    /// <summary>遍历投射物并在首次命中时消费数据。</summary>
    private void ResolveProjectileHits()
    {
        if (_player is null || _health is null) return;
        _projectileCollision.Resolve(_projectiles, _enemies, _player.GlobalPosition, 7.0f,
            ApplyDamageByIndex, amount => _health.ApplyDamage(amount));
    }

    /// <summary>按池索引重新读取最新敌人快照，再交给统一伤害入口处理命中和死亡事件。</summary>
    private void ApplyDamageByIndex(int index, int amount) =>
        ApplyDamage(index, amount, _enemies.Get(index));

    /// <summary>应用伤害并转换为受击、死亡、掉落事件。</summary>
    private void ApplyDamage(int index, int amount, EnemyComponent enemy)
    {
        if (amount <= 0 || !enemy.Alive) return;
        enemy.Health -= amount;
        if (enemy.Health > 0) { enemy.HurtTime = 0.12f; EnemyDamaged?.Invoke(); _enemies.Set(index, enemy); return; }
        enemy.Alive = false; enemy.DeathTime = enemy.Definition.ExplodesOnDeath ? 0.28f : 0.18f; DefeatedCount++;
        EnemyDefeated?.Invoke(enemy.Position, enemy.Definition);
        if (enemy.Definition.IsBoss) BossDefeated?.Invoke(enemy.Position, enemy.Definition);
        if (enemy.Definition.ExplodesOnDeath) EnemyExploded?.Invoke(); _enemies.Set(index, enemy);
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
