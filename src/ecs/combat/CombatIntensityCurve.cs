namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 保留旧扩展 API 的固定倍率策略；阶段系统不得用时间改写任一怪物或敌弹基础属性。
/// </summary>
public static class CombatIntensityCurve
{
    /// <summary>返回固定 Boss 生命倍率；参数只用于兼容历史调用。</summary>
    public static float GetBossHealthMultiplier(double ignoredElapsedSeconds) => 1.0f;

    /// <summary>返回固定移动倍率；参数只用于兼容历史调用。</summary>
    public static float GetMoveSpeedMultiplier(double ignoredElapsedSeconds) => 1.0f;

    /// <summary>返回固定敌弹速度倍率；参数只用于兼容历史调用。</summary>
    public static float GetBulletSpeedMultiplier(double ignoredElapsedSeconds) => 1.0f;

    /// <summary>返回零伤害增量，确保阶段不会暗中提高同种怪物伤害。</summary>
    public static int GetDamageBonus(double ignoredElapsedSeconds) => 0;

    /// <summary>返回零弹数增量，敌弹密度必须由具体怪物档案或独立增幅种类定义。</summary>
    public static int GetWaveBonus(double ignoredElapsedSeconds) => 0;

    /// <summary>返回固定射击间隔倍率；参数只用于兼容历史调用。</summary>
    public static float GetFireIntervalMultiplier(double ignoredElapsedSeconds) => 1.0f;
}
