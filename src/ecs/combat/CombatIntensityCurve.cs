namespace TouhouWuxiaSurvivor.Ecs.Combat;

using TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 把共享无尽难度快照投影为 ECS 弹幕参数；这里不维护第二条时间轴，只负责数值类型与性能边界。
/// </summary>
public static class CombatIntensityCurve
{
    /// <summary>读取共享时间轴上的 Boss 原生命曲线，不受有限流程普通怪清场缓冲影响。</summary>
    public static float GetBossHealthMultiplier(double elapsedSeconds) =>
        ToFiniteFloat(Evaluate(elapsedSeconds).BossHealthMultiplier);

    /// <summary>直接读取共享快照中受性能约束的敌人速度倍率，避免极长局穿越碰撞体。</summary>
    public static float GetMoveSpeedMultiplier(double elapsedSeconds) =>
        ToFiniteFloat(Evaluate(elapsedSeconds).EnemySpeedMultiplier);

    /// <summary>从共享无界强度派生缓慢对数弹速，避免另设分钟公式并保持后期持续增强。</summary>
    public static float GetBulletSpeedMultiplier(double elapsedSeconds) =>
        ToFiniteFloat(1.0 + Math.Log(Math.Max(1.0,
            Evaluate(elapsedSeconds).Intensity)) * 0.12);

    /// <summary>把共享伤害倍率投影为整数增量，并做饱和保护防止极端存档转换溢出。</summary>
    public static int GetDamageBonus(double elapsedSeconds) =>
        SaturatingInt(Math.Floor(Evaluate(elapsedSeconds).EnemyDamageMultiplier - 1.0));

    /// <summary>从共享强度的对数增加每波弹数，增长无固定分钟终点且由投射物池限制实际实体数。</summary>
    public static int GetWaveBonus(double elapsedSeconds) =>
        SaturatingInt(Math.Floor(Math.Log2(Math.Max(1.0,
            Evaluate(elapsedSeconds).Intensity))));

    /// <summary>从共享强度缩短射击间隔并保留性能下限，余下难度由同一快照的弹速、伤害和波次承接。</summary>
    public static float GetFireIntervalMultiplier(double elapsedSeconds) =>
        Math.Max(0.38f, ToFiniteFloat(1.0 /
            (1.0 + Math.Log(Math.Max(1.0,
                Evaluate(elapsedSeconds).Intensity)) * 0.18)));

    /// <summary>通过统一入口取得快照，硬存活上限只影响快照中的实体预算，不改变战斗倍率。</summary>
    private static EndlessDifficultySnapshot Evaluate(double elapsedSeconds) =>
        EndlessDifficultyCurve.EvaluateSeconds(elapsedSeconds, int.MaxValue);

    /// <summary>将有限双精度倍率安全压缩为正浮点数，极端输入采用最大有限值而非无穷。</summary>
    private static float ToFiniteFloat(double value) =>
        (float)Math.Clamp(value, 0.0, float.MaxValue);

    /// <summary>把非负双精度档位饱和为可安全参与基础伤害和弹数加法的整数。</summary>
    private static int SaturatingInt(double value) =>
        (int)Math.Clamp(value, 0.0, int.MaxValue / 4.0);
}
