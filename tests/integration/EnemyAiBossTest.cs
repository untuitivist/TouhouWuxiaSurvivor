using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.Encounters;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证普通敌人 AI、敌我投射物、角色 Boss 候选与阶段弹幕都遵守正式 ECS 契约。
/// </summary>
public partial class EnemyAiBossTest : Node
{
    /// <summary>依次执行纯数据系统与目录集成断言，任一失败都会让无窗口 Godot 以非零状态退出。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyDistinctEnemyAi();
            VerifyProjectileFactions();
            VerifyBossPhases();
            VerifyBossSelectionAndVisual();
            VerifyBossRecyclingAndEvents();
            VerifyProjectileCapacity();
            GD.Print("Enemy AI and boss test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>让追击、绕射和突进原型在相同目标附近推进，确认轨迹和射击职责不会退化为同一种行为。</summary>
    private static void VerifyDistinctEnemyAi()
    {
        var pool = new EnemyPool();
        pool.Add(new Vector2(-120.0f, 0.0f), CreateEnemy("追击", EnemyAiProfile.Chase));
        pool.Add(new Vector2(-120.0f, 0.0f), CreateEnemy("绕射",
            EnemyAiProfile.OrbitShooter, EnemyProjectileProfile.Aimed));
        pool.Add(new Vector2(-120.0f, 0.0f), CreateEnemy("突进", EnemyAiProfile.Charger));
        var movement = new EnemyMovementSystem();
        movement.Step(pool, Vector2.Zero, 2.0f, _ => { });
        Vector2 chase = pool.Get(0).Position;
        Vector2 orbit = pool.Get(1).Position;
        Vector2 charger = pool.Get(2).Position;
        Require(!chase.IsEqualApprox(orbit) && !chase.IsEqualApprox(charger) &&
            !orbit.IsEqualApprox(charger), "Three enemy AI profiles collapsed to the same movement.");
        int shots = 0;
        var projectiles = new EnemyProjectileSystem();
        projectiles.Step(pool, Vector2.Zero, 3.0f,
            _ => { shots++; return true; });
        Require(shots == 1, $"Only the orbit shooter should fire in the opening profile: {shots}");
    }

    /// <summary>把两种阵营投射物同时放入命中位置，确认玩家弹只伤敌、敌弹只伤玩家且均被消费。</summary>
    private static void VerifyProjectileFactions()
    {
        var enemies = new EnemyPool();
        enemies.Add(Vector2.Zero, CreateEnemy("靶子", EnemyAiProfile.Chase));
        var projectiles = new ProjectilePool();
        Require(projectiles.TryAdd(Vector2.Zero, Vector2.Right, 0.0f, 3,
            ProjectileFaction.Player, 2.0f, 4.0f, 0, out _), "Player projectile was rejected.");
        Require(projectiles.TryAdd(new Vector2(80.0f, 0.0f), Vector2.Left, 0.0f, 2,
            ProjectileFaction.Enemy, 2.0f, 4.0f, 1, out _), "Enemy projectile was rejected.");
        int enemyDamage = 0;
        int playerDamage = 0;
        new ProjectileCollisionSystem().Resolve(projectiles, enemies,
            new Vector2(80.0f, 0.0f), 7.0f,
            (_, amount) => enemyDamage += amount,
            amount => playerDamage += amount);
        Require(enemyDamage == 3 && playerDamage == 2 && projectiles.Count == 0,
            "Projectile factions did not resolve to their exclusive targets.");
    }

    /// <summary>逐次设置 Boss 高、中、低血量并触发射击，确认扇形、环形和交错旋转三个阶段全部可达。</summary>
    private static void VerifyBossPhases()
    {
        var pool = new EnemyPool();
        EnemyDefinition definition = CreateBossDefinition();
        pool.Add(new Vector2(100.0f, 0.0f), definition);
        var system = new EnemyProjectileSystem();
        int fanShots = FireBossAtHealth(pool, system, 90);
        Require(pool.Get(0).BossPhase == BossBulletPhase.AimedFan,
            "High-health boss did not select aimed fan phase.");
        int ringShots = FireBossAtHealth(pool, system, 50);
        Require(pool.Get(0).BossPhase == BossBulletPhase.Ring && ringShots > fanShots,
            "Mid-health boss did not expand into a ring pattern.");
        int spiralShots = FireBossAtHealth(pool, system, 20);
        float firstDirection = pool.Get(0).PatternDirection;
        FireBossAtHealth(pool, system, 20);
        Require(pool.Get(0).BossPhase == BossBulletPhase.AlternatingSpiral &&
            spiralShots >= 2 && pool.Get(0).PatternDirection != firstDirection,
            "Low-health boss did not alternate its rotating pattern.");
    }

    /// <summary>启用全部正作后验证候选严格排除自机，并确认至少一名 Boss 能查询到角色分类素材。</summary>
    private static void VerifyBossSelectionAndVisual()
    {
        var content = new ContentPackSelection(ContentPackCatalog.All.Select(pack => pack.Id));
        CharacterDefinition selected = CharacterCatalog.Default;
        IReadOnlyList<CharacterDefinition> candidates = CharacterBossCatalog.GetCandidates(
            content, selected.CharacterId);
        Require(candidates.Count > 0 && candidates.All(character =>
            character.CharacterId != selected.CharacterId),
            "Boss candidates leaked the selected player character.");
        IReadOnlyList<CharacterDefinition> baseCandidates = CharacterBossCatalog.GetCandidates(
            ContentPackSelection.BaseOnly, selected.CharacterId);
        Require(baseCandidates.Count == 1 && baseCandidates[0].DisplayName == "雾雨魔理沙",
            "Base-only boss pool must offer Marisa while preserving selected-player exclusion.");
        var renderer = new EcsCombatRenderer();
        renderer.Configure();
        bool visualFound = candidates.Any(character => renderer.TryResolveBossVisual(
            BossDefinitionFactory.Create(character), out _));
        Require(visualFound, "No registered boss character resolved to Portrait or ActorStrip visuals.");
        var world = new EcsCombatWorld();
        var director = new BossEncounterDirector();
        director.Configure(world, new RunContentContext(content,
            new CharacterSelection(selected)), () => Vector2.Zero);
        Require(director.TrySpawn(new Vector2(300.0f, 0.0f), 120.0, 0) &&
            director.LastSpawnedCharacter?.CharacterId != selected.CharacterId,
            "Boss director failed to preserve selected-character exclusion.");
        director._ExitTree();
        director.Free();
        world.Free();
    }

    /// <summary>把普通敌人与 Boss 放在回收半径外，确认只清理普通敌人，并验证 Boss 生灭事件与计数。</summary>
    private static void VerifyBossRecyclingAndEvents()
    {
        var world = new EcsCombatWorld();
        int spawned = 0;
        int defeated = 0;
        world.BossSpawned += (_, _) => spawned++;
        world.BossDefeated += (_, _) => defeated++;
        world.SpawnEnemy(new Vector2(1000.0f, 0.0f),
            CreateEnemy("远敌", EnemyAiProfile.Chase));
        world.SpawnBoss(new Vector2(1000.0f, 0.0f), CreateBossDefinition());
        world.RecycleDistant(Vector2.Zero, 100.0f);
        Require(world.EnemyCount == 1 && world.AliveBossCount == 1 && spawned == 1,
            "Distant recycling removed a live boss or retained an ordinary enemy.");
        world.DamageEnemies(new Vector2(1000.0f, 0.0f), 20.0f, 1000);
        Require(world.AliveBossCount == 0 && defeated == 1,
            "Boss defeat did not update the dedicated event and alive count.");
        world.Free();
    }

    /// <summary>尝试生成超过硬上限的敌弹，确认连续池只接受固定最大数量且统计阵营准确。</summary>
    private static void VerifyProjectileCapacity()
    {
        var pool = new ProjectilePool();
        int accepted = 0;
        for (int index = 0; index < ProjectilePool.MaximumActive + 100; index++)
        {
            if (pool.TryAdd(Vector2.Zero, Vector2.Right, 80.0f, 1,
                    ProjectileFaction.Enemy, 2.0f, 3.0f, 0, out _))
            {
                accepted++;
            }
        }

        Require(accepted == ProjectilePool.MaximumActive &&
            pool.CountFaction(ProjectileFaction.Enemy) == ProjectilePool.MaximumActive,
            "Projectile pool exceeded or undershot its hard active limit.");
    }

    /// <summary>触发指定生命值的一次 Boss 波次，并返回发射委托收到的子弹数量。</summary>
    private static int FireBossAtHealth(
        EnemyPool pool,
        EnemyProjectileSystem system,
        int health)
    {
        EnemyComponent boss = pool.Get(0);
        boss.Health = health;
        boss.FireCooldown = 0.0f;
        pool.Set(0, boss);
        int shots = 0;
        system.Step(pool, Vector2.Zero, 0.01f,
            _ => { shots++; return true; });
        return shots;
    }

    /// <summary>建立带指定移动和射击档案的普通测试敌人，使测试参数不依赖正式目录权重。</summary>
    private static EnemyDefinition CreateEnemy(
        string name,
        EnemyAiProfile ai,
        EnemyProjectileProfile? projectile = null) =>
        new(EnemyArchetype.Fairy, name, 10, 20.0f, 5.0f,
            1.0f, 0.0f, 0.0f, [], aiProfile: ai,
            projectileProfile: projectile ?? EnemyProjectileProfile.None);

    /// <summary>建立固定一百生命的角色 Boss 测试定义，便于精确覆盖三个血量分界。</summary>
    private static EnemyDefinition CreateBossDefinition() =>
        new(EnemyArchetype.CharacterBoss, "测试角色", 100, 30.0f, 16.0f,
            0.0f, 0.0f, 1.0f, [], requiredContentPack: "base",
            contactDamage: 5, aiProfile: EnemyAiProfile.BossPhased,
            projectileProfile: EnemyProjectileProfile.Boss,
            isBoss: true, characterId: "character_test");

    /// <summary>将战斗契约失败转换为带明确原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
