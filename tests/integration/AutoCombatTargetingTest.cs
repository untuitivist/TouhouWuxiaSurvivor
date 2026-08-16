using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Combat.Targeting;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Encounters;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证普通弹保持最近目标与预测收束，中心弹幕独立形成辐射和二至四重螺旋。
/// </summary>
public partial class AutoCombatTargetingTest : Node
{
    /// <summary>组合普通敌人和 Boss 后检查最近索敌，并验证每颗收束弹都朝向预判交点。</summary>
    public override void _Ready()
    {
        var world = new EcsCombatWorld();
        var fallbackContainer = new Node2D();
        try
        {
            VerifyNearestTargetRemainsAuthoritative(world, fallbackContainer);
            VerifyConvergingOrdinaryPattern();
            VerifyCenteredBarragePatterns();
            GD.Print("Auto combat targeting test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
        finally
        {
            world.Free();
            fallbackContainer.Free();
        }
    }

    /// <summary>Boss 出现后仍先攻击最近小怪，清场能力而非特殊穿透规则负责打开火线。</summary>
    private static void VerifyNearestTargetRemainsAuthoritative(
        EcsCombatWorld world,
        Node2D fallbackContainer)
    {
        EnemyDefinition ordinary = EnemyCatalog.All.First(item => !item.IsBoss);
        world.SpawnEnemy(new Vector2(20.0f, 0.0f), ordinary);
        world.SpawnBoss(new Vector2(100.0f, 0.0f),
            BossDefinitionFactory.Create(CharacterCatalog.Default));
        var fallback = new NearestEnemyTargetFinder(fallbackContainer);
        bool found = AutoTargetSelector.TrySelect(
            world, fallback, Vector2.Zero, 200.0f, out TargetMotion motion);
        Require(found && Math.Abs(motion.Position.X - 20.0f) < 0.001f,
            "Boss presence changed the nearest-target combat rule.");
    }

    /// <summary>普通弹的两翼收束属于定向表现，每颗弹都必须指向同一预测交点。</summary>
    private static void VerifyConvergingOrdinaryPattern()
    {
        PlayerBarrageSnapshot plan = PlayerBarrageCurve.Evaluate(
            true, 0, 0, 0, ordinaryProjectileBonus: 3);
        Vector2 origin = new(10.0f, -5.0f);
        Vector2 target = new(180.0f, 70.0f);
        for (int index = 0; index < plan.OrdinaryProjectileCount; index++)
        {
            ProjectileLaunchPlan launch = PlayerVolleyPattern.Resolve(
                origin, Vector2.Right, target, 18.0f, plan, index);
            Vector2 toTarget = (target - launch.Position).Normalized();
            Require(launch.Channel == PlayerProjectileChannel.Ordinary &&
                launch.Direction.Dot(toTarget) > 0.9999f,
                "Converging ordinary projectile no longer points at the intercept.");
            Require(Math.Abs((launch.Position - origin).Dot(Vector2.Right) - 18.0f) < 0.001f,
                "Ordinary projectile no longer starts on the forward launch line.");
        }

        Require(plan.OrdinaryProjectileCount == 4 && plan.BarrageProjectileCount == 0,
            "Converging ordinary form unexpectedly created centered barrage projectiles.");
    }

    /// <summary>逐项验证辐射、二重、三重和四重螺旋均从自机圆周向外发射且无需目标。</summary>
    private static void VerifyCenteredBarragePatterns()
    {
        foreach (int arms in new[] { 0, 2, 3, 4 })
        {
            PlayerBarrageSnapshot full = PlayerBarrageCurve.Evaluate(
                false, arms, 3, 0, barrageProjectileBonus: 12);
            PlayerBarrageSnapshot plan = full.WithoutOrdinaryProjectiles();
            Require(!plan.RequiresTarget && plan.BarrageProjectileCount == 12 &&
                plan.BarrageMode == (arms == 0
                    ? PlayerBarrageMode.Radial
                    : PlayerBarrageMode.Spiral),
                $"Centered barrage mode is incorrect for {arms} spiral arms.");
            VerifyOutwardGeometry(plan, arms);
        }
    }

    /// <summary>确认一组中心弹幕的出生半径、向外方向和首层臂间夹角符合阵形定义。</summary>
    private static void VerifyOutwardGeometry(PlayerBarrageSnapshot plan, int arms)
    {
        Vector2 origin = new(10.0f, -5.0f);
        var firstLayer = new List<Vector2>();
        int sampleCount = arms == 0 ? plan.BarrageProjectileCount : arms;
        for (int index = 0; index < plan.BarrageProjectileCount; index++)
        {
            ProjectileLaunchPlan launch = PlayerVolleyPattern.Resolve(
                origin, Vector2.Right, origin + Vector2.Right, 18.0f, plan, index);
            Vector2 radial = (launch.Position - origin).Normalized();
            Require(launch.Channel == PlayerProjectileChannel.Barrage &&
                launch.Direction.Dot(radial) > 0.9999f,
                "A centered barrage projectile did not travel away from the player.");
            if (index < sampleCount)
            {
                firstLayer.Add(launch.Direction);
            }
        }

        double expected = Math.Tau / sampleCount;
        double actual = NormalizeAngle(firstLayer[0].AngleTo(firstLayer[1]));
        Require(Math.Abs(actual - expected) < 0.001,
            $"Centered barrage first-layer spacing drifted for {arms} spiral arms.");
    }

    /// <summary>把 Godot 的有符号夹角整理为零至整圆，便于比较阵形相邻臂间距。</summary>
    private static double NormalizeAngle(double angle) => angle < 0.0
        ? angle + Math.Tau
        : angle;

    /// <summary>把任一自动战斗契约失败转换为带明确原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
