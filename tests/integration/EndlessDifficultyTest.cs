using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Gameplay.Pacing;

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
            VerifyFixedEnemyRuntimeStats();
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
    /// 验证普通敌人的实际出生定义不消费全局时间倍率，阶段只负责更换敌群构成。
    /// </summary>
    private static void VerifyFixedEnemyRuntimeStats()
    {
        EnemyDefinition baseEnemy = EnemyCatalog.All.First(definition => !definition.IsBoss);
        EnemyDefinition opening = EnemyDifficultyScaler.Scale(baseEnemy, 0L);
        long lateTier = EnemyDifficultyScaler.GetTier(60.0 * 60.0);
        EnemyDefinition late = EnemyDifficultyScaler.Scale(baseEnemy, lateTier);
        Require(opening.MaxHealth == baseEnemy.MaxHealth &&
            opening.ContactDamage == baseEnemy.ContactDamage,
            "Opening enemy scaling changed the authored base values.");
        Require(ReferenceEquals(opening, late) && late.MaxHealth == opening.MaxHealth &&
            Mathf.IsEqualApprox(late.MoveSpeed, opening.MoveSpeed) &&
            late.ContactDamage == opening.ContactDamage,
            "A global stage modified fixed ordinary-enemy attributes.");
    }

    /// <summary>
    /// 比较零、十、六十和一千分钟快照，要求刷新压力严格增长且始终保持有限正数。
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
                current.ScheduledSpawnsPerSecond > previous.ScheduledSpawnsPerSecond,
                $"Endless pressure stopped growing at {current.ElapsedMinutes} minutes.");
            Require(double.IsFinite(current.Intensity) && current.Intensity > 0.0,
                "Difficulty intensity became invalid.");
        }

        EndlessDifficultySnapshot extreme = EndlessDifficultyCurve.EvaluateSeconds(
            double.PositiveInfinity, int.MaxValue);
        Require(double.IsFinite(extreme.Intensity) &&
            double.IsFinite(extreme.ScheduledSpawnsPerSecond) &&
            extreme.ScheduledSpawnsPerSecond > 0.0,
            "Extreme elapsed time produced invalid endless difficulty values.");
    }

    /// <summary>
    /// 确认连续刷新率始终上升且完整传给正式生成器；四档占比始终构成完整百分比。
    /// </summary>
    private static void VerifyEntitySafetyLimits()
    {
        double previousRate = 0.0;
        foreach (double minutes in TestMinutes)
        {
            double seconds = minutes * 60.0;
            EndlessDifficultySnapshot snapshot = EndlessDifficultyCurve.EvaluateSeconds(seconds, 140);
            Require(snapshot.ScheduledSpawnsPerSecond >= previousRate &&
                Math.Abs(EnemySpawnPacing.GetScheduledSpawnsPerSecond(seconds) -
                snapshot.ScheduledSpawnsPerSecond) < 0.000001,
                "Continuous spawn pacing diverged from the difficulty snapshot.");
            double tierTotal = snapshot.TierMix.Common + snapshot.TierMix.Veteran +
                snapshot.TierMix.Elite + snapshot.TierMix.Champion;
            Require(Math.Abs(tierTotal - 1.0) < 0.000001,
                "Enemy tier shares no longer form a complete population mix.");
            previousRate = snapshot.ScheduledSpawnsPerSecond;
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
            RunLevelCurve.GetRequiredExperience(2) == 13,
            "Opening level requirements drifted from eight and thirteen.");
    }

    /// <summary>
    /// 验证时间不赠送火力，并锁定普通弹数量、中心弹幕数量与阵形的独立成长。
    /// </summary>
    private static void VerifyPlayerBarrageStages()
    {
        PlayerBarrageSnapshot opening = PlayerBarrageCurve.EvaluateSeconds(0.0, false, 0, 0);
        PlayerBarrageSnapshot lateWithoutBuild = PlayerBarrageCurve.EvaluateSeconds(
            RunPacingTimeline.FinalEncounterSeconds, false, 0, 0);
        PlayerBarrageSnapshot radialFour = PlayerBarrageCurve.EvaluateSeconds(
            0.0, false, 0, 0, 4);
        PlayerBarrageSnapshot radialEight = PlayerBarrageCurve.EvaluateSeconds(
            0.0, false, 0, 0, 8);
        PlayerBarrageSnapshot radialTwelve = PlayerBarrageCurve.EvaluateSeconds(
            0.0, false, 0, 0, 12);
        PlayerBarrageSnapshot ordinary = PlayerBarrageCurve.EvaluateSeconds(
            0.0, false, 0, 0, 0, 2);
        PlayerBarrageSnapshot combined = PlayerBarrageCurve.EvaluateSeconds(
            0.0, false, 0, 0, 8, 1);
        PlayerBarrageSnapshot sixtyMinutes = PlayerBarrageCurve.EvaluateSeconds(3600.0, false, 1, 0);
        PlayerBarrageSnapshot thousandMinutes = PlayerBarrageCurve.EvaluateSeconds(60000.0, false, 1, 0);
        Require(opening.ProjectileCount == 1 && lateWithoutBuild.ProjectileCount == 1 &&
            radialFour.ProjectileCount == 5 && radialEight.ProjectileCount == 9 &&
            radialTwelve.ProjectileCount == 13 && ordinary.OrdinaryProjectileCount == 3 &&
            ordinary.BarrageProjectileCount == 0 &&
            combined.OrdinaryProjectileCount == 2 && combined.BarrageProjectileCount == 8,
            "Independent ordinary or barrage projectile ranks are incorrect.");
        Require(sixtyMinutes.ProjectileCount == 1 &&
            thousandMinutes.ProjectileCount == 1 &&
            sixtyMinutes.BarrageMode == PlayerBarrageMode.None &&
            thousandMinutes.BarrageMode == PlayerBarrageMode.None,
            "Elapsed time still grants automatic barrage power.");

        PlayerBarrageSnapshot convergingOrdinary = PlayerBarrageCurve.EvaluateSeconds(
            0.0, true, 0, 0, 0, 2);
        PlayerBarrageSnapshot doubleSpiral = PlayerBarrageCurve.EvaluateSeconds(
            0.0, false, 0, 0, 8, 0, 2);
        PlayerBarrageSnapshot tripleSpiral = PlayerBarrageCurve.EvaluateSeconds(
            0.0, false, 0, 0, 8, 0, 3);
        PlayerBarrageSnapshot quadrupleSpiral = PlayerBarrageCurve.EvaluateSeconds(
            0.0, false, 0, 0, 8, 0, 4);
        PlayerBarrageSnapshot degraded = PlayerBarrageCurve.EvaluateSeconds(
            0.0, false, 1, PlayerBarrageCurve.ProjectileSoftLimit - 1, 12);
        PlayerBarrageSnapshot saturated = PlayerBarrageCurve.EvaluateSeconds(
            0.0, false, 1, PlayerBarrageCurve.ProjectileSoftLimit, 12);
        Require(convergingOrdinary.OrdinaryProjectileCount == 3 &&
            convergingOrdinary.BarrageProjectileCount == 0 &&
            convergingOrdinary.OrdinaryMode == PlayerOrdinaryShotMode.ConvergingFormation &&
            convergingOrdinary.RequiresTarget,
            "Converging specialization did not remain an ordinary-projectile form.");
        Require(radialEight.BarrageMode == PlayerBarrageMode.Radial &&
            doubleSpiral.BarrageSpiralArmCount == 2 &&
            tripleSpiral.BarrageSpiralArmCount == 3 &&
            quadrupleSpiral.BarrageSpiralArmCount == 4 &&
            doubleSpiral.BarrageProjectileCount == radialEight.BarrageProjectileCount &&
            quadrupleSpiral.BarrageProjectileCount == radialEight.BarrageProjectileCount,
            "Barrage form upgrades changed projectile quantity or lost a spiral tier.");
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
