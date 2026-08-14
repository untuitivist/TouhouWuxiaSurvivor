using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证正式 ECS 在二千投射物与一百四十敌人负载下维持常量计数、局部碰撞和可视剔除契约。
/// </summary>
public partial class EcsPerformanceContractTest : Node2D
{
    private readonly EcsCombatRenderer _renderer = new();
    private readonly EnemyPool _renderEnemies = new();
    private readonly ProjectilePool _renderProjectiles = new();
    private bool _renderFixtureReady;

    /// <summary>
    /// 执行纯数据压力测试，再通过真实绘制通知验证屏内实体保留、屏外实体不提交。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            VerifyFactionCounts();
            VerifyIndexedCollisionAgainstNaiveReference();
            VerifyMaximumLoadContract();
            PrepareRenderFixture();
            QueueRedraw();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            VerifyRenderCulling();
            GD.Print("ECS performance contract test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>只在 Godot 允许绘图的通知内调用批量渲染器，避免测试绕过正式 Canvas API。</summary>
    public override void _Draw()
    {
        if (_renderFixtureReady)
        {
            _renderer.Draw(this, _renderEnemies, [], [], _renderProjectiles, 0.0);
        }
    }

    /// <summary>覆盖新增、改变阵营、尾交换删除和直接删尾，确认 O(1) 缓存始终等于实际池内容。</summary>
    private static void VerifyFactionCounts()
    {
        var pool = new ProjectilePool();
        AddProjectile(pool, ProjectileFaction.Player, Vector2.Zero);
        AddProjectile(pool, ProjectileFaction.Player, Vector2.One);
        AddProjectile(pool, ProjectileFaction.Enemy, Vector2.One * 2.0f);
        RequireCounts(pool, 2, 1, "after add");

        ProjectileComponent changed = pool.Get(0);
        changed.Faction = ProjectileFaction.Enemy;
        pool.Set(0, changed);
        RequireCounts(pool, 1, 2, "after faction change");

        pool.RemoveSwap(0);
        pool.TrimLast();
        RequireCounts(pool, 1, 1, "after swap removal");
        pool.TrimLast();
        RequireCounts(pool, 0, 1, "after direct tail removal");
    }

    /// <summary>
    /// 用二千发稀疏玩家弹对一百四十敌人进行命中，要求结果等同朴素顺序且候选远少于全量笛卡尔积。
    /// </summary>
    private static void VerifyIndexedCollisionAgainstNaiveReference()
    {
        EnemyPool enemies = CreateEnemies(140);
        var projectiles = new ProjectilePool();
        for (int index = 0; index < ProjectilePool.MaximumActive; index++)
        {
            Vector2 position = index % 20 == 0
                ? enemies.Get((index / 20) % enemies.Count).Position
                : new Vector2(20000.0f + index * 9.0f, -18000.0f - index * 7.0f);
            AddProjectile(projectiles, ProjectileFaction.Player, position);
        }

        int[] expected = GetNaiveHits(projectiles, enemies);
        var actual = new List<int>();
        var collision = new ProjectileCollisionSystem();
        collision.Resolve(projectiles, enemies, Vector2.One * -50000.0f, 7.0f,
            (enemyIndex, _) => actual.Add(enemyIndex), _ => { });
        Require(expected.SequenceEqual(actual),
            "Spatial collision selected a different target than the naive pool-order reference.");
        Require(collision.LastNaiveComparisonUpperBound ==
                (long)ProjectilePool.MaximumActive * enemies.Count,
            "Naive comparison upper bound did not describe the original P*E workload.");
        Require(collision.LastCandidateChecks < collision.LastNaiveComparisonUpperBound / 10,
            $"Spatial index did not reduce sparse candidates enough: " +
            $"{collision.LastCandidateChecks}/{collision.LastNaiveComparisonUpperBound}.");
    }

    /// <summary>确认硬上限准确接纳二千投射物，敌人正式负载上限可保存一百四十项且不会创建节点。</summary>
    private static void VerifyMaximumLoadContract()
    {
        var pool = new ProjectilePool();
        for (int index = 0; index < ProjectilePool.MaximumActive; index++)
        {
            AddProjectile(pool, index < ProjectilePool.MaximumEnemyActive
                ? ProjectileFaction.Enemy : ProjectileFaction.Player, new Vector2(index, 0.0f));
        }

        bool overflow = pool.TryAdd(Vector2.Zero, Vector2.Right, 0.0f, 1,
            ProjectileFaction.Player, 1.0f, 2.0f, 0, out _);
        Require(!overflow && pool.Count == 2000 &&
                pool.CountFaction(ProjectileFaction.Enemy) == 400 &&
                pool.CountFaction(ProjectileFaction.Player) == 1600,
            "Projectile hard limit or cached faction totals are inconsistent.");
        Require(CreateEnemies(140).Count == 140,
            "Enemy ECS pool did not retain the formal 140-entity load contract.");
    }

    /// <summary>准备一组屏内和远离镜头的敌人与投射物，交由下一次正式绘制通知处理。</summary>
    private void PrepareRenderFixture()
    {
        _renderer.Configure();
        EnemyDefinition enemy = CreateEnemy();
        _renderEnemies.Add(new Vector2(120.0f, 120.0f), enemy);
        _renderEnemies.Add(new Vector2(12000.0f, 12000.0f), enemy);
        AddProjectile(_renderProjectiles, ProjectileFaction.Player,
            new Vector2(160.0f, 120.0f));
        AddProjectile(_renderProjectiles, ProjectileFaction.Enemy,
            new Vector2(-12000.0f, -12000.0f));
        _renderFixtureReady = true;
    }

    /// <summary>确认屏内敌人与投射物均提交，两个远端实体被 CPU 剔除且素材覆盖统计仍完整。</summary>
    private void VerifyRenderCulling()
    {
        Require(_renderer.LastVisibleEnemyCount == 1 &&
                _renderer.LastVisibleProjectileCount == 1,
            "Visibility culling removed an on-screen combat entity.");
        Require(_renderer.LastCulledEntityCount == 2,
            $"Off-screen combat entities were not culled: {_renderer.LastCulledEntityCount}.");
        Require(_renderer.LastMappedEnemyCount + _renderer.LastFallbackEnemyCount == 2,
            "Visibility culling corrupted full-catalog visual coverage diagnostics.");
    }

    /// <summary>建立间隔二百五十六像素的敌人，使每个空间桶保持稀疏并覆盖正负坐标。</summary>
    private static EnemyPool CreateEnemies(int count)
    {
        var enemies = new EnemyPool();
        EnemyDefinition definition = CreateEnemy();
        for (int index = 0; index < count; index++)
        {
            float x = (index % 20 - 10) * 256.0f;
            float y = (index / 20 - 3) * 256.0f;
            enemies.Add(new Vector2(x, y), definition);
        }
        return enemies;
    }

    /// <summary>按旧版投射物倒序、敌人正序规则生成首个重叠索引序列，作为行为参考。</summary>
    private static int[] GetNaiveHits(ProjectilePool projectiles, EnemyPool enemies)
    {
        var result = new List<int>();
        for (int projectileIndex = projectiles.Count - 1; projectileIndex >= 0; projectileIndex--)
        {
            ProjectileComponent projectile = projectiles.Get(projectileIndex);
            for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
            {
                EnemyComponent enemy = enemies.Get(enemyIndex);
                float radius = projectile.Radius + enemy.Definition.CollisionRadius;
                if (projectile.Position.DistanceSquaredTo(enemy.Position) > radius * radius) continue;
                result.Add(enemyIndex);
                break;
            }
        }
        return result.ToArray();
    }

    /// <summary>向池中加入固定寿命与半径的测试投射物，并拒绝任何意外容量失败。</summary>
    private static void AddProjectile(
        ProjectilePool pool,
        ProjectileFaction faction,
        Vector2 position)
    {
        Require(pool.TryAdd(position, Vector2.Right, 0.0f, 1,
            faction, 5.0f, 4.0f, 0, out _), "Projectile fixture exceeded pool capacity.");
    }

    /// <summary>建立静止低半径测试敌人，隔离 AI 和正式目录权重。</summary>
    private static EnemyDefinition CreateEnemy() => new(
        EnemyArchetype.Fairy, "性能靶", 10, 0.0f, 6.0f,
        0.0f, 0.0f, 0.0f, []);

    /// <summary>比较池总数与两阵营缓存，错误中保留操作阶段。</summary>
    private static void RequireCounts(ProjectilePool pool, int players, int enemies, string phase) =>
        Require(pool.Count == players + enemies &&
            pool.CountFaction(ProjectileFaction.Player) == players &&
            pool.CountFaction(ProjectileFaction.Enemy) == enemies,
            $"Faction counts diverged {phase}.");

    /// <summary>把性能契约失败转换为包含明确原因的异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
