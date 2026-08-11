using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Spawning;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证无尽难度、经验需求和玩家自动弹幕在千分钟长局中保持单调、安全且完全不依赖主动输入。
/// </summary>
public partial class EndlessDifficultyTest : Node
{
    private static readonly double[] TestMinutes = [0.0, 10.0, 60.0, 1000.0];

    /// <summary>
    /// 依次执行纯数据契约和自动射击源码边界检查，任一失败均以非零退出码报告。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyEndlessDifficulty();
            VerifyEntitySafetyLimits();
            VerifyEnemyRuntimeScaling();
            VerifyLevelCurve();
            VerifyPlayerBarrageStages();
            VerifyNoActiveFireInput();
            GD.Print("Endless difficulty test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 验证普通敌人的实际出生定义消费共享曲线，并在生命、移速、接触伤害和灵息基数上持续增强。
    /// </summary>
    private static void VerifyEnemyRuntimeScaling()
    {
        EnemyDefinition baseEnemy = EnemyCatalog.All.First(definition => !definition.IsBoss);
        EnemyDefinition opening = EnemyDifficultyScaler.Scale(baseEnemy, 0L);
        long lateTier = EnemyDifficultyScaler.GetTier(60.0 * 60.0);
        EnemyDefinition late = EnemyDifficultyScaler.Scale(baseEnemy, lateTier);
        Require(opening.MaxHealth == baseEnemy.MaxHealth &&
            opening.ContactDamage == baseEnemy.ContactDamage,
            "Opening enemy scaling changed the authored base values.");
        Require(late.MaxHealth > opening.MaxHealth &&
            late.MoveSpeed > opening.MoveSpeed &&
            late.ContactDamage > opening.ContactDamage,
            "Ordinary enemy runtime stats stopped before the endless curve.");
    }

    /// <summary>
    /// 比较零、十、六十和一千分钟快照，要求所有无界威胁字段严格增长且始终保持有限正数。
    /// </summary>
    private static void VerifyEndlessDifficulty()
    {
        EndlessDifficultySnapshot[] snapshots = TestMinutes
            .Select(minutes => EndlessDifficultyCurve.EvaluateSeconds(minutes * 60.0, 140))
            .ToArray();
        for (int index = 1; index < snapshots.Length; index++)
        {
            EndlessDifficultySnapshot previous = snapshots[index - 1];
            EndlessDifficultySnapshot current = snapshots[index];
            Require(current.Intensity > previous.Intensity &&
                current.SpawnBudgetPerSecond > previous.SpawnBudgetPerSecond &&
                current.EnemyHealthMultiplier > previous.EnemyHealthMultiplier &&
                current.EnemyDamageMultiplier > previous.EnemyDamageMultiplier &&
                current.RewardMultiplier > previous.RewardMultiplier,
                $"Endless pressure stopped growing at {current.ElapsedMinutes} minutes.");
            Require(double.IsFinite(current.Intensity) && current.Intensity > 0.0,
                "Difficulty intensity became invalid.");
        }

        EndlessDifficultySnapshot extreme = EndlessDifficultyCurve.EvaluateSeconds(
            double.PositiveInfinity, int.MaxValue);
        Require(double.IsFinite(extreme.Intensity) &&
            double.IsFinite(extreme.SpawnBudgetPerSecond) &&
            double.IsFinite(extreme.EnemyHealthMultiplier) &&
            double.IsFinite(extreme.EnemyDamageMultiplier) &&
            double.IsFinite(extreme.RewardMultiplier),
            "Extreme elapsed time produced a non-finite endless multiplier.");
    }

    /// <summary>
    /// 确认批次、间隔、速度和存活数遵守性能边界，同时公开刷怪预算在实体封顶后仍继续增长。
    /// </summary>
    private static void VerifyEntitySafetyLimits()
    {
        double previousBudget = 0.0;
        foreach (double minutes in TestMinutes)
        {
            double seconds = minutes * 60.0;
            EndlessDifficultySnapshot snapshot = EndlessDifficultyCurve.EvaluateSeconds(seconds, 140);
            Require(snapshot.SpawnBatchSize is >= 1 and <=
                EndlessDifficultyCurve.MaximumSpawnBatchSize,
                "Spawn batch exceeded its safety limit.");
            Require(snapshot.SpawnIntervalSeconds >=
                EndlessDifficultyCurve.MinimumSpawnIntervalSeconds,
                "Spawn interval crossed its safety floor.");
            Require(snapshot.AliveLimit is >= 1 and <= 140 &&
                EnemySpawnPacing.GetAliveLimit(seconds, 140) == snapshot.AliveLimit,
                "Alive limit escaped the scene hard limit.");
            Require(snapshot.EnemySpeedMultiplier <=
                EndlessDifficultyCurve.MaximumEnemySpeedMultiplier,
                "Enemy speed escaped its readability limit.");
            Require(snapshot.SpawnBudgetPerSecond > previousBudget || minutes == 0.0,
                "Spawn budget stopped after an entity safety cap.");
            Require(Math.Abs(EnemySpawnPacing.GetSpawnBudgetPerSecond(seconds) -
                snapshot.SpawnBudgetPerSecond) < 0.000001,
                "Spawn pacing facade diverged from the endless budget.");
            previousBudget = snapshot.SpawnBudgetPerSecond;
        }
    }

    /// <summary>
    /// 覆盖开局、百万级和 int 最大等级，确认需求非递减、开局数值兼容且没有负数或算术溢出。
    /// </summary>
    private static void VerifyLevelCurve()
    {
        int previous = RunLevelCurve.GetRequiredExperience(1);
        for (int level = 2; level <= 1_000_000; level++)
        {
            int required = RunLevelCurve.GetRequiredExperience(level);
            Require(required >= previous && required > 0,
                $"Level requirement overflowed or decreased at level {level}.");
            previous = required;
        }

        Require(RunLevelCurve.GetRequiredExperience(int.MaxValue) >= previous,
            "Maximum representable level overflowed after the million-level sample.");
        Require(RunLevelCurve.GetRequiredExperience(1) == 8 &&
            RunLevelCurve.GetRequiredExperience(2) == 10,
            "Opening level requirements drifted from eight and ten.");
    }

    /// <summary>
    /// 验证自动武器依次形成一、三、五、七发，并在十分钟后交错旋转环且在池临界时退化而非越界。
    /// </summary>
    private static void VerifyPlayerBarrageStages()
    {
        PlayerBarrageSnapshot opening = PlayerBarrageCurve.EvaluateSeconds(0.0, false, 0, 0);
        PlayerBarrageSnapshot three = PlayerBarrageCurve.EvaluateSeconds(120.0, false, 0, 0);
        PlayerBarrageSnapshot five = PlayerBarrageCurve.EvaluateSeconds(300.0, false, 0, 0);
        PlayerBarrageSnapshot seven = PlayerBarrageCurve.EvaluateSeconds(600.0, false, 0, 0);
        PlayerBarrageSnapshot rotating = PlayerBarrageCurve.EvaluateSeconds(600.0, false, 1, 0);
        PlayerBarrageSnapshot sixtyMinutes = PlayerBarrageCurve.EvaluateSeconds(3600.0, false, 1, 0);
        PlayerBarrageSnapshot thousandMinutes = PlayerBarrageCurve.EvaluateSeconds(60000.0, false, 1, 0);
        Require(opening.ProjectileCount == 1 && three.ProjectileCount == 3 &&
            five.ProjectileCount == 5 && seven.ProjectileCount == 7,
            "Player barrage did not progress through 1/3/5/7 projectiles.");
        Require(seven.Mode == PlayerBarrageMode.AlternatingFan &&
            rotating.Mode == PlayerBarrageMode.RotatingRing && !rotating.RequiresTarget,
            "Late volleys do not alternate fan and rotating patterns.");
        Require(sixtyMinutes.ProjectileCount == PlayerBarrageCurve.MaximumProjectilesPerVolley &&
            thousandMinutes.ProjectileCount == PlayerBarrageCurve.MaximumProjectilesPerVolley &&
            sixtyMinutes.Mode == PlayerBarrageMode.RotatingRing &&
            thousandMinutes.Mode == PlayerBarrageMode.RotatingRing,
            "Sixty- or thousand-minute barrage escaped its stable late-game stage.");

        PlayerBarrageSnapshot spiral = PlayerBarrageCurve.EvaluateSeconds(0.0, true, 0, 0);
        PlayerBarrageSnapshot degraded = PlayerBarrageCurve.EvaluateSeconds(600.0,
            false, 1, PlayerBarrageCurve.ProjectileSoftLimit - 1);
        PlayerBarrageSnapshot saturated = PlayerBarrageCurve.EvaluateSeconds(600.0,
            false, 1, PlayerBarrageCurve.ProjectileSoftLimit);
        Require(spiral.ProjectileCount == 2 && spiral.Mode == PlayerBarrageMode.RotatingRing,
            "Existing opposite spiral pair was not preserved.");
        Require(degraded.ProjectileCount == 1 && saturated.ProjectileCount == 0 &&
            saturated.RetryIntervalSeconds > 0.0,
            "Projectile saturation did not degrade to one shot and a bounded retry.");
    }

    /// <summary>
    /// 审计自动射击实现不读取输入，并拒绝为开火、攻击或施放符卡增加新的项目动作。
    /// </summary>
    private static void VerifyNoActiveFireInput()
    {
        string shooterSource = Godot.FileAccess.GetFileAsString(
            "res://src/combat/weapons/AutoShooter.cs");
        Require(shooterSource.Length > 0 &&
            !shooterSource.Contains("Input" + ".", StringComparison.Ordinal),
            "Auto shooter must not read player input.");
        foreach (string action in new[] { "fire", "shoot", "attack", "cast_spell" })
        {
            Require(!InputMap.HasAction(action), $"Forbidden active combat action exists: {action}.");
        }
    }

    /// <summary>
    /// 将任一无尽数值契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
