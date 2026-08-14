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
    public float AttackPowerMultiplier { get; private set; } = 1.0f;
    public float FireRateMultiplier { get; private set; } = 1.0f;
    public float MoveSpeedMultiplier { get; private set; } = 1.0f;
    public float TargetRangeMultiplier { get; private set; } = 1.0f;
    public float ProjectileSpeedMultiplier { get; private set; } = 1.0f;
    public float SpiritAttractionMultiplier { get; private set; } = 1.0f;
    public float SpiritYieldMultiplier { get; private set; } = 1.0f;
    public int ExtraProjectiles { get; private set; }
    public int ProjectilePierceCount { get; private set; }
    public bool UsesSpiralPattern { get; private set; }
    public bool UsesContinuousFireMomentum { get; private set; }
    public bool UsesStationaryFocus { get; private set; }
    public bool UsesMovementMomentum { get; private set; }
    public bool UsesSpiritFlow { get; private set; }

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
        int endlessRange = build.GetRank(RunUpgradeKind.EndlessTargetRange);
        int endlessProjectileSpeed = build.GetRank(RunUpgradeKind.EndlessProjectileSpeed);
        int endlessAttraction = build.GetRank(RunUpgradeKind.EndlessSpiritAttraction);
        float specializationDamage = GetSpecializationBonus(
            build, RunSpecializationEffect.Damage);
        float specializationFireRate = GetSpecializationBonus(
            build, RunSpecializationEffect.FireRate);
        float specializationMoveSpeed = GetSpecializationBonus(
            build, RunSpecializationEffect.MoveSpeed);
        float specializationTargetRange = GetSpecializationBonus(
            build, RunSpecializationEffect.TargetRange);
        float specializationProjectileSpeed = GetSpecializationBonus(
            build, RunSpecializationEffect.ProjectileSpeed);
        float specializationAttraction = GetSpecializationBonus(
            build, RunSpecializationEffect.SpiritAttraction);
        DamageBonus = _baseDamageBonus;
        AttackPowerMultiplier = (1.0f +
            build.GetRank(RunUpgradeKind.NeedleDamage) * 0.10f + specializationDamage) *
            SqrtGrowth(endlessDamage, 0.06f);
        FireRateMultiplier = (1.0f + build.GetRank(RunUpgradeKind.FireRate) * 0.09f +
            specializationFireRate) *
            SqrtGrowth(endlessFireRate, 0.06f);
        MoveSpeedMultiplier = _baseMoveSpeedMultiplier *
            (1.0f + build.GetRank(RunUpgradeKind.MoveSpeed) * 0.07f +
                specializationMoveSpeed) *
            SqrtGrowth(endlessMoveSpeed, 0.04f);
        TargetRangeMultiplier = (1.0f +
            build.GetRank(RunUpgradeKind.TargetRange) * 0.08f +
            specializationTargetRange) * SqrtGrowth(endlessRange, 0.04f);
        ProjectileSpeedMultiplier = 1.0f +
            build.GetRank(RunUpgradeKind.ProjectileSpeed) * 0.08f +
            specializationProjectileSpeed;
        ProjectileSpeedMultiplier *= SqrtGrowth(endlessProjectileSpeed, 0.04f);
        SpiritAttractionMultiplier = _baseSpiritAttractionMultiplier *
            (1.0f + build.GetRank(RunUpgradeKind.SpiritAttraction) * 0.18f +
                specializationAttraction) * SqrtGrowth(endlessAttraction, 0.08f);
        ExtraProjectiles = (int)MathF.Round(GetSpecializationBonus(
            build, RunSpecializationEffect.ExtraProjectiles));
        ProjectilePierceCount = (int)MathF.Round(GetSpecializationBonus(
            build, RunSpecializationEffect.ProjectilePierce));
        UsesSpiralPattern = GetSpecializationBonus(
            build, RunSpecializationEffect.SpiralPattern) > 0.0f;
        SpiritYieldMultiplier = 1.0f + GetSpecializationBonus(
            build, RunSpecializationEffect.SpiritYield);
        UsesContinuousFireMomentum = HasSpecializationEffect(
            build, RunSpecializationEffect.ContinuousFireMomentum);
        UsesStationaryFocus = HasSpecializationEffect(
            build, RunSpecializationEffect.StationaryFocus);
        UsesMovementMomentum = HasSpecializationEffect(
            build, RunSpecializationEffect.MovementMomentum);
        UsesSpiritFlow = HasSpecializationEffect(
            build, RunSpecializationEffect.SpiritFlowMomentum);
    }

    /// <summary>
    /// 汇总已选特化对某项倍率的绝对加成，目录数据是唯一效果来源且不会重复累积漂移。
    /// </summary>
    private static float GetSpecializationBonus(
        RunBuildState build,
        RunSpecializationEffect effect) => RunUpgradeCatalog.All
            .SelectMany(definition => definition.Specializations)
            .Where(item => item.Effect == effect && build.HasSpecialization(item.Id))
            .Sum(item => item.EffectValue);

    private static bool HasSpecializationEffect(
        RunBuildState build,
        RunSpecializationEffect effect) => RunUpgradeCatalog.All
            .SelectMany(definition => definition.Specializations)
            .Any(item => item.Effect == effect && build.HasSpecialization(item.Id));

    /// <summary>
    /// 用平方根提供无上限但边际递减的统一延续，避免固定加值令单一路线永久压倒其他选择。
    /// </summary>
    private static float SqrtGrowth(int rank, float step) =>
        1.0f + MathF.Sqrt(Math.Max(0, rank)) * step;
}
