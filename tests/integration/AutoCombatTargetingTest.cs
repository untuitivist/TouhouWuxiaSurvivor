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
/// 验证自动武器保持最近目标规则，同时让后期收束阵围绕真实预判交点造成有效伤害。
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
            VerifyConvergingPattern();
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

    /// <summary>环绕出生只改变视觉起点；每个方向都必须与同一预判交点相交而不是向外空转。</summary>
    private static void VerifyConvergingPattern()
    {
        var barrage = new PlayerBarrageSnapshot(
            4.0, PlayerBarrageMode.ConvergingOrbit, 5, 5,
            Math.Tau / 5.0, 0.37, true, 0.0, 2.25);
        Vector2 origin = new(10.0f, -5.0f);
        Vector2 target = new(180.0f, 70.0f);
        for (int index = 0; index < barrage.ProjectileCount; index++)
        {
            ProjectileLaunchPlan launch = PlayerVolleyPattern.Resolve(
                origin, Vector2.Right, target, 18.0f, barrage, index);
            Vector2 toTarget = (target - launch.Position).Normalized();
            Require(launch.Direction.Dot(toTarget) > 0.9999f,
                "Converging projectile no longer points at the predicted intercept.");
            Require(Math.Abs(launch.Position.DistanceTo(origin) - 18.0f) < 0.001f,
                "Converging projectile did not preserve the rotating spawn ring.");
        }
    }

    /// <summary>把任一自动战斗契约失败转换为带明确原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
