using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Pacing;
using TouhouWuxiaSurvivor.World.Biomes;

namespace TouhouWuxiaSurvivor.Gameplay.Spawning;

/// <summary>
/// 在镜头外沿按连续生成信用刷新普通敌人；总供给不受场上存活数阻断，强度档由统一配比调度。
/// </summary>
public partial class EnemySpawner : Node
{
    private readonly RandomNumberGenerator _random = new();
    private readonly EnemyTierScheduler _tierScheduler = new();
    private PlayerController? _player;
    private Node2D? _enemyContainer;
    private ContentPackSelection _content = ContentPackSelection.BaseOnly;
    private Func<Vector2, BiomeId>? _biomeAtPosition;
    private Func<double>? _difficultySeconds;
    private EcsCombatWorld? _ecsWorld;
    private double _elapsedSeconds;
    private double _spawnCredit;
    private double _cleanupCooldown;

    [Export]
    public PackedScene? EnemyScene { get; set; }

    [Export(PropertyHint.Range, "0,100,1")]
    public int InitialSpawnCount { get; set; } = EnemySpawnPacing.DefaultInitialSpawnCount;

    public int AliveCount => _ecsWorld?.EnemyCount ?? _enemyContainer?.GetChildCount() ?? 0;
    public int SpawnedCount { get; private set; }
    public int DefeatedCount { get; private set; }
    public double ElapsedSeconds => _ecsWorld?.ElapsedSeconds ?? _elapsedSeconds;
    public event Action<Vector2, EnemyDefinition>? EnemyDefeated;
    public event Action? EnemyDamaged;
    public event Action? EnemyExploded;

    /// <summary>
    /// 注入动态难度时间；未配置时保留真实时间兼容语义，便于独立测试与旧场景继续运行。
    /// </summary>
    public void ConfigurePacing(Func<double> difficultySeconds)
    {
        ArgumentNullException.ThrowIfNull(difficultySeconds);
        _difficultySeconds = difficultySeconds;
    }

    /// <summary>
    /// 绑定玩家、敌人容器和群系查询，重置节奏并在屏幕四周创建首批地区敌人。
    /// </summary>
    public void Configure(
        PlayerController player,
        Node2D enemyContainer,
        ContentPackSelection content,
        Func<Vector2, BiomeId> biomeAtPosition)
    {
        _player = player;
        _enemyContainer = enemyContainer;
        _content = content;
        _biomeAtPosition = biomeAtPosition;
        _random.Randomize();
        _elapsedSeconds = 0.0;
        _spawnCredit = 0.0;
        SpawnedCount = 0;
        DefeatedCount = 0;
        for (int index = 0; index < InitialSpawnCount; index++)
        {
            SpawnOne(EnemyStrengthTier.Common, 0.0, true);
        }
    }

    /// <summary>绑定 ECS 战斗世界；之后刷怪只写入敌人组件池，不创建 EnemyActor 节点。</summary>
    public void ConfigureEcs(EcsCombatWorld world)
    {
        _ecsWorld = world;
        world.EnemyDefeated += OnEnemyDefeated;
        world.EnemyDamaged += OnEnemyDamaged;
        world.EnemyExploded += OnEnemyExploded;
    }

    /// <summary>
    /// 推进生存时间与刷怪冷却，并定期清除远离玩家、已失去玩法意义的实体。
    /// </summary>
    public override void _Process(double delta)
    {
        if (_player is null ||
            (_ecsWorld is null && (_enemyContainer is null || EnemyScene is null)))
        {
            return;
        }

        if (_ecsWorld is null)
        {
            _elapsedSeconds += delta;
        }

        double elapsedSeconds = ElapsedSeconds;
        double pacingSeconds = GetDifficultySeconds(elapsedSeconds);
        EnemyPressureSnapshot pressure = EnemySpawnPacing.GetPressure(pacingSeconds);
        _spawnCredit += Math.Max(0.0, delta) * pressure.SpawnRatePerSecond;
        _cleanupCooldown -= delta;
        int spawnedThisFrame = 0;
        while (_spawnCredit >= 1.0 && spawnedThisFrame < 32)
        {
            EnemyStrengthTier tier = _tierScheduler.Select(
                pressure.GearIndex, pressure.TierMix);
            if (!SpawnOne(tier, elapsedSeconds, true, pacingSeconds))
            {
                break;
            }

            _spawnCredit -= 1.0;
            spawnedThisFrame++;
        }

        if (_cleanupCooldown <= 0.0)
        {
            RecycleDistantEnemies();
            _cleanupCooldown = 2.0;
        }
    }

    /// <summary>
    /// 按指定强度选择当前地区敌人并放在镜头外；返回值只表示本次实体是否真正进入世界。
    /// </summary>
    private bool SpawnOne(
        EnemyStrengthTier tier,
        double elapsedSeconds,
        bool countsTowardsPacing,
        double? pacingOverride = null)
    {
        double pacingSeconds = pacingOverride ?? GetDifficultySeconds(elapsedSeconds);
        if (_player is null)
        {
            return false;
        }

        Vector2 spawnPosition = ChooseSpawnPosition();
        BiomeId biome = _biomeAtPosition?.Invoke(spawnPosition) ?? BiomeId.Common;
        EnemyDefinition baseDefinition = EnemyCatalog.Choose(
            _random, pacingSeconds, biome, _content, tier);
        EnemyDefinition definition = baseDefinition;
        if (_ecsWorld is not null)
        {
            _ecsWorld.SpawnEnemy(spawnPosition, definition);
            if (countsTowardsPacing) SpawnedCount++;
            return true;
        }

        if (_enemyContainer is null || EnemyScene is null)
        {
            return false;
        }

        var enemy = EnemyScene.Instantiate<EnemyActor>();
        enemy.Configure(definition, _player);
        enemy.Defeated += OnEnemyDefeated;
        enemy.Damaged += OnEnemyDamaged;
        enemy.Exploded += OnEnemyExploded;
        _enemyContainer.AddChild(enemy);
        enemy.GlobalPosition = spawnPosition;
        if (countsTowardsPacing) SpawnedCount++;
        return true;
    }

    /// <summary>读取阶段导演映射后的难度时钟，并把非法结果安全回退到真实生存时间。</summary>
    private double GetDifficultySeconds(double elapsedSeconds)
    {
        double value = _difficultySeconds?.Invoke() ?? elapsedSeconds;
        return double.IsFinite(value) ? Math.Max(0.0, value) : Math.Max(0.0, elapsedSeconds);
    }

    /// <summary>
    /// 从可见矩形的四条边随机选择一边，再增加随机外侧留白以隐藏生成瞬间。
    /// </summary>
    private Vector2 ChooseSpawnPosition()
    {
        Vector2 halfView = GetViewport().GetVisibleRect().Size * 0.5f;
        float padding = _random.RandfRange(24.0f, 64.0f);
        int side = _random.RandiRange(0, 3);
        return side switch
        {
            0 => _player!.GlobalPosition + new Vector2(-halfView.X - padding,
                _random.RandfRange(-halfView.Y, halfView.Y)),
            1 => _player!.GlobalPosition + new Vector2(halfView.X + padding,
                _random.RandfRange(-halfView.Y, halfView.Y)),
            2 => _player!.GlobalPosition + new Vector2(
                _random.RandfRange(-halfView.X, halfView.X), -halfView.Y - padding),
            _ => _player!.GlobalPosition + new Vector2(
                _random.RandfRange(-halfView.X, halfView.X), halfView.Y + padding),
        };
    }

    /// <summary>
    /// 回收距离超过当前视口对角线约两倍的敌人，限制玩家持续单向移动造成的场外积压。
    /// </summary>
    private void RecycleDistantEnemies()
    {
        if (_ecsWorld is not null)
        {
            float ecsRecycleDistance = GetViewport().GetVisibleRect().Size.Length() * 1.8f;
            _ecsWorld.RecycleDistant(_player!.GlobalPosition, ecsRecycleDistance);
            return;
        }

        if (_player is null || _enemyContainer is null)
        {
            return;
        }

        float recycleDistance = GetViewport().GetVisibleRect().Size.Length() * 1.8f;
        float recycleDistanceSquared = recycleDistance * recycleDistance;
        foreach (Node child in _enemyContainer.GetChildren())
        {
            if (child is EnemyActor enemy &&
                enemy.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition) > recycleDistanceSquared)
            {
                enemy.QueueFree();
            }
        }
    }

    /// <summary>
    /// 汇总击破数并向掉落系统转发准确死亡位置与定义，不让敌人直接依赖掉落实现。
    /// </summary>
    private void OnEnemyDefeated(Vector2 position, EnemyDefinition definition)
    {
        if (!definition.IsBoss)
        {
            DefeatedCount++;
        }
        EnemyDefeated?.Invoke(position, definition);
    }

    /// <summary>
    /// 汇总所有存活敌人的非致命受击事件，使音频系统无需追踪不断生成和回收的实体。
    /// </summary>
    private void OnEnemyDamaged() => EnemyDamaged?.Invoke();

    /// <summary>
    /// 汇总自爆敌人正式进入爆炸阶段的事件，确保爆炸音与动画起点保持同步。
    /// </summary>
    private void OnEnemyExploded() => EnemyExploded?.Invoke();
}
