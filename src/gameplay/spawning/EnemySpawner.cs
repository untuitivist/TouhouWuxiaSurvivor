using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.World.Biomes;

namespace TouhouWuxiaSurvivor.Gameplay.Spawning;

/// <summary>
/// 参考生存者玩法在镜头外沿持续生成追踪敌人，并随时间提高生成频率和单批数量。
/// </summary>
public partial class EnemySpawner : Node
{
    private readonly RandomNumberGenerator _random = new();
    private readonly Dictionary<EnemyDefinition, EnemyDefinition> _scaledDefinitions = new();
    private PlayerController? _player;
    private Node2D? _enemyContainer;
    private ContentPackSelection _content = ContentPackSelection.BaseOnly;
    private Func<Vector2, BiomeId>? _biomeAtPosition;
    private Func<double>? _difficultySeconds;
    private EcsCombatWorld? _ecsWorld;
    private double _elapsedSeconds;
    private double _spawnCooldown;
    private double _cleanupCooldown;
    private long _difficultyTier = -1L;

    [Export]
    public PackedScene? EnemyScene { get; set; }

    [Export(PropertyHint.Range, "0,100,1")]
    public int InitialSpawnCount { get; set; } = EnemySpawnPacing.DefaultInitialSpawnCount;

    [Export(PropertyHint.Range, "10,500,1")]
    public int MaximumAlive { get; set; } = EnemySpawnPacing.DefaultAliveHardLimit;

    public int AliveCount => _ecsWorld?.EnemyCount ?? _enemyContainer?.GetChildCount() ?? 0;
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
        _spawnCooldown = 0.8;
        _difficultyTier = -1L;
        _scaledDefinitions.Clear();
        for (int index = 0; index < InitialSpawnCount; index++)
        {
            SpawnOne();
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
        _spawnCooldown -= delta;
        _cleanupCooldown -= delta;
        if (_spawnCooldown <= 0.0)
        {
            SpawnBatch(elapsedSeconds, pacingSeconds);
            _spawnCooldown = EnemySpawnPacing.GetSpawnInterval(pacingSeconds);
        }

        if (_cleanupCooldown <= 0.0)
        {
            RecycleDistantEnemies();
            _cleanupCooldown = 2.0;
        }
    }

    /// <summary>
    /// 按集中节奏曲线提高单批数量，并同时遵守动态存活上限和场景硬上限。
    /// </summary>
    private void SpawnBatch(double elapsedSeconds, double pacingSeconds)
    {
        int batchSize = EnemySpawnPacing.GetBatchSize(pacingSeconds);
        int aliveLimit = EnemySpawnPacing.GetAliveLimit(pacingSeconds, MaximumAlive);
        for (int index = 0; index < batchSize && AliveCount < aliveLimit; index++)
        {
            SpawnOne(elapsedSeconds, pacingSeconds);
        }
    }

    /// <summary>
    /// 选择当前时间已解锁的敌人，并把它放在随机镜头边缘之外而非玩家脚下。
    /// </summary>
    private void SpawnOne(
        double? elapsedOverride = null,
        double? pacingOverride = null)
    {
        double elapsedSeconds = elapsedOverride ?? ElapsedSeconds;
        double pacingSeconds = pacingOverride ?? GetDifficultySeconds(elapsedSeconds);
        int aliveLimit = EnemySpawnPacing.GetAliveLimit(pacingSeconds, MaximumAlive);
        if (_player is null || AliveCount >= aliveLimit)
        {
            return;
        }

        Vector2 spawnPosition = ChooseSpawnPosition();
        BiomeId biome = _biomeAtPosition?.Invoke(spawnPosition) ?? BiomeId.Common;
        EnemyDefinition baseDefinition = EnemyCatalog.Choose(
            _random, pacingSeconds, biome, _content);
        EnemyDefinition definition = GetScaledDefinition(baseDefinition, elapsedSeconds);
        if (_ecsWorld is not null)
        {
            _ecsWorld.SpawnEnemy(spawnPosition, definition);
            return;
        }

        if (_enemyContainer is null || EnemyScene is null)
        {
            return;
        }

        var enemy = EnemyScene.Instantiate<EnemyActor>();
        enemy.Configure(definition, _player);
        enemy.Defeated += OnEnemyDefeated;
        enemy.Damaged += OnEnemyDamaged;
        enemy.Exploded += OnEnemyExploded;
        _enemyContainer.AddChild(enemy);
        enemy.GlobalPosition = spawnPosition;
    }

    /// <summary>读取阶段导演映射后的难度时钟，并把非法结果安全回退到真实生存时间。</summary>
    private double GetDifficultySeconds(double elapsedSeconds)
    {
        double value = _difficultySeconds?.Invoke() ?? elapsedSeconds;
        return double.IsFinite(value) ? Math.Max(0.0, value) : Math.Max(0.0, elapsedSeconds);
    }

    /// <summary>
    /// 在同一十秒档位复用缩放定义；进入新档位时丢弃旧缓存，使无尽数值持续增长且不累积缓存。
    /// </summary>
    private EnemyDefinition GetScaledDefinition(EnemyDefinition definition, double elapsedSeconds)
    {
        long tier = EnemyDifficultyScaler.GetTier(elapsedSeconds);
        if (tier != _difficultyTier)
        {
            _difficultyTier = tier;
            _scaledDefinitions.Clear();
        }

        if (!_scaledDefinitions.TryGetValue(definition, out EnemyDefinition? scaled))
        {
            scaled = EnemyDifficultyScaler.Scale(definition, tier);
            _scaledDefinitions.Add(definition, scaled);
        }

        return scaled;
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
        DefeatedCount++;
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
