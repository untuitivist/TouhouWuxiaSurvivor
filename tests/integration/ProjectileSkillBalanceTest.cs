using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证构筑驱动的贯穿弹会依次伤害不同敌人，且普通一击弹仍保持原有回收语义。
/// </summary>
public partial class ProjectileSkillBalanceTest : Node
{
    /// <summary>
    /// 建立两名重叠测试敌人与一颗二次命中弹，连续解析两帧后核对目标身份和池回收。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            var enemies = new EnemyPool();
            EnemyDefinition definition = CreateEnemy();
            enemies.Add(Vector2.Zero, definition);
            enemies.Add(Vector2.Zero, definition);
            var projectiles = new ProjectilePool();
            Require(projectiles.TryAdd(Vector2.Zero, Vector2.Right, 0.0f, 10,
                ProjectileFaction.Player, 2.0f, 4.0f, 0, out _, maximumHits: 2),
                "Could not create the piercing projectile.");
            var hits = new List<(int EnemyIndex, int Damage)>();
            var collision = new ProjectileCollisionSystem();
            collision.Resolve(projectiles, enemies, new Vector2(999.0f, 999.0f),
                1.0f, (index, damage) => hits.Add((index, damage)), _ => { });
            Require(projectiles.Count == 1 && hits.SequenceEqual([(0, 10)]),
                "The first hit consumed the piercing projectile or selected the wrong enemy.");
            collision.Resolve(projectiles, enemies, new Vector2(999.0f, 999.0f),
                1.0f, (index, damage) => hits.Add((index, damage)), _ => { });
            Require(projectiles.Count == 0 && hits.SequenceEqual([(0, 10), (1, 3)]),
                "The second hit repeated one enemy or failed to consume the projectile.");
            VerifySharedProjectileStats(hits.Sum(item => item.Damage));
            VerifyProjectileSpeedPolicy();
            GD.Print("Projectile skill balance test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认运行时公开的弹速边界会处理负值、正常值与正无穷，防止面板显示不可兑现速度。</summary>
    private static void VerifyProjectileSpeedPolicy()
    {
        Require(ProjectileKinematicsPolicy.NormalizeSpeed(-5.0f) == 0.0f,
            "Negative projectile speed was not clamped to zero.");
        Require(ProjectileKinematicsPolicy.NormalizeSpeed(720.0f) == 720.0f,
            "A normal projectile speed was changed by the effective policy.");
        float firstOverflow = ProjectileKinematicsPolicy.NormalizeSpeed(2400.0f);
        float laterOverflow = ProjectileKinematicsPolicy.NormalizeSpeed(10000.0f);
        Require(firstOverflow > ProjectileKinematicsPolicy.SoftCapStartSpeed &&
            laterOverflow > firstOverflow &&
            laterOverflow < ProjectileKinematicsPolicy.MaximumEffectiveSpeed,
            "Endless projectile speed stopped growing or crossed its soft safety ceiling.");
        Require(ProjectileKinematicsPolicy.NormalizeSpeed(float.PositiveInfinity) ==
            ProjectileKinematicsPolicy.MaximumEffectiveSpeed,
            "Positive overflow did not resolve to the effective projectile speed ceiling.");
    }

    /// <summary>
    /// 验证两类弹丸共享单弹伤害，同时允许自瞄数量、弹幕数量与贯穿表现分别成长。
    /// </summary>
    private static void VerifySharedProjectileStats(int piercingTwoTargetDamage)
    {
        PlayerBarrageSnapshot split = PlayerBarrageCurve.Evaluate(
            false, 0, aimedProjectileBonus: 1, barrageProjectileBonus: 2);
        PlayerAttackDamageSnapshot damage = PlayerAttackDamageProjector.Project(
            10.0, split, sharedDamageMultiplier: 1.35f, aimedMaximumHits: 2);
        Require(piercingTwoTargetDamage == 13,
            "The physical piercing collision no longer applies ten plus three damage.");
        Require(split.AimedProjectileCount == 2 && split.BarrageProjectileCount == 2 &&
            damage.PredictiveAim.ProjectileCount == 2 &&
            damage.Barrage.ProjectileCount == 2,
            "Independent projectile-count upgrades did not reach both channels.");
        Require(damage.PredictiveAim.MinimumPrimaryDamage == 14 &&
            damage.PredictiveAim.MaximumPrimaryDamage == 14 &&
            damage.Barrage.MinimumPrimaryDamage == 14 &&
            damage.Barrage.MaximumPrimaryDamage == 14 &&
            damage.PredictiveAim.PrimaryTotalDamage == 28 &&
            damage.Barrage.PrimaryTotalDamage == 28,
            "Aimed and barrage projectiles no longer share the same per-projectile damage.");
        Require(damage.PredictiveAim.SecondaryTotalDamage > 0 &&
            damage.Barrage.SecondaryTotalDamage == 0,
            "Piercing presentation leaked from aimed shots into the barrage channel.");
    }

    /// <summary>
    /// 建立不会移动或反击的高生命敌人，使测试只观察投射物身份与命中次数。
    /// </summary>
    private static EnemyDefinition CreateEnemy() => new(
        EnemyArchetype.Fairy, "贯穿试靶", 100, 0.0f, 6.0f,
        1.0f, 0.0f, 0.0f, []);

    /// <summary>将技能契约失败转换为包含明确原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
