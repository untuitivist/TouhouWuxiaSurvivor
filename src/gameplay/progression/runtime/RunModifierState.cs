using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 把构筑重数投影为玩家、武器和灵息掉落物可直接消费的只读运行时倍率。
/// </summary>
public sealed class RunModifierState
{
    private int _baseDamageBonus;
    private float _baseMoveSpeedMultiplier = 1.0f;
    private float _baseSpiritAttractionMultiplier = 1.0f;
    public int DamageBonus { get; private set; }
    public float FireRateMultiplier { get; private set; } = 1.0f;
    public float MoveSpeedMultiplier { get; private set; } = 1.0f;
    public float TargetRangeMultiplier { get; private set; } = 1.0f;
    public float ProjectileSpeedMultiplier { get; private set; } = 1.0f;
    public float SpiritAttractionMultiplier { get; private set; } = 1.0f;

    /// <summary>
    /// 配置局外档案提供的基础伤害、移动和吸附加成，并立即更新尚无构筑时的公开值。
    /// </summary>
    public void ConfigureBase(
        int damageBonus,
        float moveSpeedMultiplier,
        float spiritAttractionMultiplier)
    {
        _baseDamageBonus = Math.Max(0, damageBonus);
        _baseMoveSpeedMultiplier = Math.Max(1.0f, moveSpeedMultiplier);
        _baseSpiritAttractionMultiplier = Math.Max(1.0f, spiritAttractionMultiplier);
        DamageBonus = _baseDamageBonus;
        MoveSpeedMultiplier = _baseMoveSpeedMultiplier;
        SpiritAttractionMultiplier = _baseSpiritAttractionMultiplier;
    }

    /// <summary>
    /// 从完整构筑重新计算所有倍率，避免重复应用选择导致浮点累积漂移。
    /// </summary>
    public void Refresh(RunBuildState build)
    {
        int endlessDamage = build.GetRank(RunUpgradeKind.EndlessDamage);
        int endlessFireRate = build.GetRank(RunUpgradeKind.EndlessFireRate);
        int endlessMoveSpeed = build.GetRank(RunUpgradeKind.EndlessMoveSpeed);
        DamageBonus = checked(_baseDamageBonus +
            build.GetRank(RunUpgradeKind.NeedleDamage) + endlessDamage);
        FireRateMultiplier = (1.0f + build.GetRank(RunUpgradeKind.FireRate) * 0.12f) *
            (1.0f + MathF.Log2(1.0f + endlessFireRate) * 0.08f);
        MoveSpeedMultiplier = _baseMoveSpeedMultiplier *
            (1.0f + build.GetRank(RunUpgradeKind.MoveSpeed) * 0.08f) *
            (1.0f + MathF.Log2(1.0f + endlessMoveSpeed) * 0.04f);
        TargetRangeMultiplier = 1.0f + build.GetRank(RunUpgradeKind.TargetRange) * 0.10f;
        ProjectileSpeedMultiplier = 1.0f + build.GetRank(RunUpgradeKind.ProjectileSpeed) * 0.12f;
        SpiritAttractionMultiplier = _baseSpiritAttractionMultiplier *
            (1.0f + build.GetRank(RunUpgradeKind.SpiritAttraction) * 0.25f);
    }
}
