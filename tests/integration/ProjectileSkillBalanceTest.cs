using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

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
            VerifyIndependentBarrageForms();
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
    /// 验证两类弹丸共享单弹伤害，同时允许普通弹数量、弹幕数量与贯穿分别成长。
    /// </summary>
    private static void VerifySharedProjectileStats(int piercingTwoTargetDamage)
    {
        PlayerBarrageSnapshot split = PlayerBarrageCurve.Evaluate(
            false, 0, 0, 0,
            ordinaryProjectileBonus: 1, barrageProjectileBonus: 4);
        PlayerAttackDamageSnapshot damage = PlayerAttackDamageProjector.Project(
            10.0, split, sharedDamageMultiplier: 1.35f, ordinaryMaximumHits: 2);
        Require(piercingTwoTargetDamage == 13,
            "The physical piercing collision no longer applies ten plus three damage.");
        Require(split.OrdinaryProjectileCount == 2 && split.BarrageProjectileCount == 4 &&
            damage.Ordinary.ProjectileCount == 2 &&
            damage.Barrage.ProjectileCount == 4,
            "Independent projectile-count upgrades did not reach both channels.");
        Require(damage.Ordinary.MinimumPrimaryDamage == 14 &&
            damage.Ordinary.MaximumPrimaryDamage == 14 &&
            damage.Barrage.MinimumPrimaryDamage == 14 &&
            damage.Barrage.MaximumPrimaryDamage == 14 &&
            damage.Ordinary.PrimaryTotalDamage == 28 &&
            damage.Barrage.PrimaryTotalDamage == 56,
            "Ordinary and barrage projectiles no longer share the same per-projectile damage.");
        Require(damage.Ordinary.SecondaryTotalDamage > 0 &&
            damage.Barrage.SecondaryTotalDamage == 0,
            "Piercing presentation leaked from ordinary shots into the barrage channel.");
    }

    /// <summary>
    /// 从正式构筑取得辐射、二重、三重和四重螺旋，确认阵形选择不改变八发弹幕数量。
    /// </summary>
    private static void VerifyIndependentBarrageForms()
    {
        RunUpgradeDefinition route = RunUpgradeCatalog.FindById("wind_riding")!;
        Require(route.Specializations.Count == 2,
            "Barrage route no longer exposes the two composable form upgrades.");
        RunModifierState radial = CreateBarrageModifiers(route, []);
        RunModifierState doubleSpiral = CreateBarrageModifiers(route, [0]);
        RunModifierState tripleSpiral = CreateBarrageModifiers(route, [1]);
        RunModifierState quadrupleSpiral = CreateBarrageModifiers(route, [0, 1]);
        Require(new[] { radial, doubleSpiral, tripleSpiral, quadrupleSpiral }.All(
                item => item.BarrageProjectileBonus == 8) &&
            radial.BarrageSpiralArmCount == 0 &&
            doubleSpiral.BarrageSpiralArmCount == 2 &&
            tripleSpiral.BarrageSpiralArmCount == 3 &&
            quadrupleSpiral.BarrageSpiralArmCount == 4,
            "A barrage form upgrade changed quantity or lost its authored spiral arm count.");
    }

    /// <summary>建立二重天罗弹阵并应用指定阵式索引，返回正式运行倍率投影。</summary>
    private static RunModifierState CreateBarrageModifiers(
        RunUpgradeDefinition route,
        IReadOnlyList<int> specializationIndices)
    {
        var build = new RunBuildState();
        Require(build.Apply(route) && build.Apply(route),
            "Could not reach the barrage form prerequisite.");
        foreach (int index in specializationIndices)
        {
            Require(build.ApplySpecialization(route, route.Specializations[index], 4),
                "Could not apply an authored barrage form specialization.");
        }

        var modifiers = new RunModifierState();
        modifiers.Refresh(build);
        return modifiers;
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
