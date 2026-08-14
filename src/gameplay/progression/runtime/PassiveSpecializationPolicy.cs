namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 集中定义四种条件特化的满层收益、蓄势节奏与模拟兑现率，避免战斗和策划各写一套常量。
/// </summary>
public static class PassiveSpecializationPolicy
{
    public const float ContinuousFireBonus = 0.20f;
    public const float StationaryFocusBonus = 0.22f;
    public const float MovementMomentumBonus = 0.18f;
    public const float SpiritFlowBonus = 0.18f;
    public const double FireChargeSeconds = 1.20;
    public const double FireDecaySeconds = 0.70;
    public const double FireGraceSeconds = 0.42;
    public const double FocusChargeSeconds = 0.90;
    public const double FocusDecaySeconds = 0.25;
    public const double MovementChargeSeconds = 1.10;
    public const double MovementDecaySeconds = 0.45;
    public const double SpiritFlowDurationSeconds = 3.0;
    public const double ExpectedContinuousFireUptime = 0.82;
    public const double ExpectedFocusUptime = 0.55;
    public const double ExpectedMovementUptime = 0.70;
    public const double ExpectedSpiritFlowUptime = 0.45;

    /// <summary>把零至一的蓄势进度转换为以一为基准的最终倍率。</summary>
    public static float Scale(float progress, float maximumBonus) =>
        1.0f + Math.Clamp(progress, 0.0f, 1.0f) * maximumBonus;
}
