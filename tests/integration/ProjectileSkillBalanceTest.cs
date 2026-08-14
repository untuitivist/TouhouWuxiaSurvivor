using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
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
            VerifyHorizontalVolleyBudget(hits.Sum(item => item.Damage));
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
    /// 对照开局破甲的两敌十三点与十分钟散华七弹十三点，锁定两条分支的横向伤害预算。
    /// </summary>
    private static void VerifyHorizontalVolleyBudget(int piercingTwoTargetDamage)
    {
        PlayerBarrageSnapshot scatter = PlayerBarrageCurve.EvaluateSeconds(
            600.0, false, 0L, 0, bonusProjectiles: 2);
        ProjectileVolleyDamageSnapshot scatterDamage = ProjectileDamageBudget.Project(
            10.0, scatter.VolleyDamageBudget, scatter.ProjectileCount, maximumHits: 1);
        int distributed = Enumerable.Range(0, scatter.ProjectileCount)
            .Sum(scatterDamage.GetPrimaryDamage);
        Require(scatter.ProjectileCount == 7 &&
            scatterDamage.PrimaryTotalDamage == 13 &&
            scatterDamage.MinimumPrimaryDamage == 1 &&
            scatterDamage.MaximumPrimaryDamage == 2 &&
            distributed == 13,
            "The seven-projectile scatter volley did not preserve its authored integer budget.");
        Require(piercingTwoTargetDamage == scatterDamage.TwoTargetTotalDamage,
            $"Piercing and scatter budgets diverged: {piercingTwoTargetDamage}/" +
            $"{scatterDamage.TwoTargetTotalDamage}.");
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
