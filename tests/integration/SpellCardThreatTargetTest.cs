using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Combat;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证集中型符卡使用稳定威胁排序，而不是把完整弹丸预算浪费在最近的低生命杂兵上。
/// </summary>
public partial class SpellCardThreatTargetTest : Node
{
    /// <summary>依次验证生命优先、Boss 优先、射程边界和同分稳定结果。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyHealthBeforeDistance();
            VerifyBossBeforeHealth();
            VerifyRangeAndStableTie();
            GD.Print("Spell card threat target test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认远处高耐久敌人优先于近处低耐久敌人，集中重招不会按最近距离浪费。</summary>
    private static void VerifyHealthBeforeDistance()
    {
        var world = new EcsCombatWorld();
        world.SpawnEnemy(new Vector2(12, 0), CreateEnemy("杂兵", 12));
        world.SpawnEnemy(new Vector2(80, 0), CreateEnemy("强敌", 120));
        Require(world.TryFindHighestThreat(Vector2.Zero, 100.0f, out Vector2 target) &&
            target.IsEqualApprox(new Vector2(80, 0)),
            "Highest-health threat was not selected before the nearest weak enemy.");
        world.Free();
    }

    /// <summary>确认角色 Boss 始终优先于更高生命的普通敌人，体现遭遇中的真正威胁身份。</summary>
    private static void VerifyBossBeforeHealth()
    {
        var world = new EcsCombatWorld();
        world.SpawnEnemy(new Vector2(20, 0), CreateEnemy("巨型杂兵", 500));
        world.SpawnBoss(new Vector2(70, 0), CreateEnemy("角色首领", 90, true));
        Require(world.TryFindHighestThreat(Vector2.Zero, 100.0f, out Vector2 target) &&
            target.IsEqualApprox(new Vector2(70, 0)),
            "Boss identity did not outrank ordinary health in focused targeting.");
        world.Free();
    }

    /// <summary>确认射程外 Boss 不会被选择，并以距离、实体号稳定打破完全同分目标。</summary>
    private static void VerifyRangeAndStableTie()
    {
        var world = new EcsCombatWorld();
        world.SpawnEnemy(new Vector2(40, 0), CreateEnemy("同分甲", 60));
        world.SpawnEnemy(new Vector2(32, 0), CreateEnemy("同分乙", 60));
        world.SpawnBoss(new Vector2(140, 0), CreateEnemy("射程外首领", 200, true));
        Require(world.TryFindHighestThreat(Vector2.Zero, 100.0f, out Vector2 first) &&
            world.TryFindHighestThreat(Vector2.Zero, 100.0f, out Vector2 second) &&
            first.IsEqualApprox(new Vector2(32, 0)) && second.IsEqualApprox(first),
            "Focused target ignored range or produced an unstable tied result.");
        world.Free();
    }

    /// <summary>建立普通或 Boss 测试定义；Boss 带稳定角色编号以满足正式生成契约。</summary>
    private static EnemyDefinition CreateEnemy(string name, int health, bool boss = false) =>
        new(EnemyArchetype.Fairy, name, health, 30.0f, 6.0f,
            1.0f, 0.0f, 0.0f, [], isBoss: boss,
            characterId: boss ? "threat_test_boss" : null);

    /// <summary>将威胁排序违约转换为包含目标语义的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
