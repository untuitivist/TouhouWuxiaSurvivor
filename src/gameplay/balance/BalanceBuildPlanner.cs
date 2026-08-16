using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Balance;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.Balance;

/// <summary>
/// 只通过正式升级与特化接口推进四种构筑，保证模拟结果不会获得玩家实际无法选择的数值。
/// </summary>
internal static class BalanceBuildPlanner
{
    private static readonly int[] OffensiveSpellLevels = [7, 14, 22, 34];
    private static readonly int[] SupportSpellLevels = [12, 28];

    /// <summary>
    /// 为一次升级机会依次尝试应得奥义、已解锁特化、有限修行和无尽延续，且最多应用一项。
    /// </summary>
    public static void ApplyLevelChoice(
        RunBuildState build,
        BalanceBuildKind kind,
        int runLevel,
        ContentPackSelection content)
    {
        if (TryApplyScheduledSpell(build, kind, runLevel, content) ||
            TryApplySpecialization(build, kind, runLevel) ||
            TryApplyFiniteUpgrade(build, kind) ||
            TryApplyEndlessUpgrade(build, kind))
        {
            return;
        }

        throw new InvalidOperationException(
            $"No legal balance choice at level {runLevel} for {kind}.");
    }

    /// <summary>
    /// 按独立的四主攻与二护持里程碑补奥义；未满足前置时保留缺口并在后续等级重试。
    /// </summary>
    private static bool TryApplyScheduledSpell(
        RunBuildState build,
        BalanceBuildKind kind,
        int runLevel,
        ContentPackSelection content)
    {
        int desiredOffensive = OffensiveSpellLevels.Count(level => runLevel >= level);
        int desiredSupport = SupportSpellLevels.Count(level => runLevel >= level);
        if (SpellCardSlotPolicy.CountOccupied(build, SpellCardSlotKind.Offensive) <
            desiredOffensive && TryApplySpell(build, kind, content, SpellCardSlotKind.Offensive))
        {
            return true;
        }

        return SpellCardSlotPolicy.CountOccupied(build, SpellCardSlotKind.Support) <
            desiredSupport && TryApplySpell(build, kind, content, SpellCardSlotKind.Support);
    }

    /// <summary>
    /// 从当前内容包中选择指定槽类且前置合法的最佳契合奥义；评分只决定选择顺序，不改变卡牌数值。
    /// </summary>
    private static bool TryApplySpell(
        RunBuildState build,
        BalanceBuildKind kind,
        ContentPackSelection content,
        SpellCardSlotKind slotKind)
    {
        SpellCardDefinition? selected = SpellCardCatalog.GetEnabled(content)
            .Where(card => SpellCardSlotPolicy.Classify(card) == slotKind)
            .Where(card => build.GetRank(card.UnlockUpgradeId) == 0)
            .Where(card => RunUpgradeCatalog.FindById(card.UnlockUpgradeId) is { } upgrade &&
                build.CanUpgrade(upgrade))
            .OrderByDescending(card => ScoreSpell(card, kind, slotKind))
            .ThenBy(card => card.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        return selected is not null &&
            build.Apply(RunUpgradeCatalog.FindById(selected.UnlockUpgradeId)!);
    }

    /// <summary>
    /// 以共享贡献预算和路线偏好计算确定性选择分；该分数只选择横向招式，不会直接增加最终战力。
    /// </summary>
    private static double ScoreSpell(
        SpellCardDefinition card,
        BalanceBuildKind kind,
        SpellCardSlotKind slotKind)
    {
        double contribution = SpellCardContributionModel.CalculateBudget(card);
        double routeFactor = kind switch
        {
            BalanceBuildKind.Assault => card.Combat.DamageScale,
            BalanceBuildKind.Rapid => 1.0 / card.Combat.IntervalScale,
            BalanceBuildKind.Utility => card.Combat.RangeScale + card.Combat.DefenseScale,
            _ => 1.0,
        };
        return contribution * routeFactor;
    }

    /// <summary>
    /// 按路线声明的稳定分支顺序寻找第一个已经满足境界与基础重数的特化。
    /// </summary>
    private static bool TryApplySpecialization(
        RunBuildState build,
        BalanceBuildKind kind,
        int runLevel)
    {
        foreach (string specializationId in GetSpecializationOrder(kind))
        {
            RunUpgradeDefinition? owner = RunUpgradeCatalog.All.FirstOrDefault(definition =>
                definition.Specializations.Any(item => item.Id == specializationId));
            RunUpgradeSpecialization? specialization = owner?.Specializations.FirstOrDefault(
                item => item.Id == specializationId);
            if (owner is not null && specialization is not null &&
                build.ApplySpecialization(owner, specialization, runLevel))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 基础与效用路线按最大重数归一后铺开修行；专门输出路线才优先练满核心项。
    /// </summary>
    private static bool TryApplyFiniteUpgrade(RunBuildState build, BalanceBuildKind kind)
    {
        RunUpgradeKind[] order = GetFiniteOrder(kind);
        IEnumerable<RunUpgradeDefinition> candidates = order.Select(
            RunUpgradeCatalog.GetRequiredByKind).Where(build.CanUpgrade);
        RunUpgradeDefinition? selected = kind is BalanceBuildKind.Baseline or BalanceBuildKind.Utility
            ? candidates.OrderBy(item => build.GetRank(item.Id) /
                (double)Math.Max(1, item.MaxRank)).FirstOrDefault()
            : candidates.FirstOrDefault();
        return selected is not null && build.Apply(selected);
    }

    /// <summary>
    /// 在有限修行练满后按路线权重轮转六种无尽延续，使成长无上限但不会只堆单一维度。
    /// </summary>
    private static bool TryApplyEndlessUpgrade(RunBuildState build, BalanceBuildKind kind)
    {
        RunUpgradeKind[] order = GetEndlessOrder(kind);
        float[] weights = GetEndlessWeights(kind);
        RunUpgradeDefinition? selected = order.Select((upgradeKind, index) => new
            {
                Definition = RunUpgradeCatalog.GetRequiredByKind(upgradeKind),
                Priority = (build.GetRank(upgradeKind) + 1.0f) / weights[index],
                Index = index,
            })
            .Where(item => build.CanUpgrade(item.Definition))
            .OrderBy(item => item.Priority)
            .ThenBy(item => item.Index)
            .Select(item => item.Definition)
            .FirstOrDefault();
        return selected is not null && build.Apply(selected);
    }

    /// <summary>返回六项有限修行在对应路线中的稳定优先级。</summary>
    private static RunUpgradeKind[] GetFiniteOrder(BalanceBuildKind kind) => kind switch
    {
        BalanceBuildKind.Assault => [RunUpgradeKind.NeedleDamage, RunUpgradeKind.FireRate,
            RunUpgradeKind.TargetRange, RunUpgradeKind.ProjectileSpeed,
            RunUpgradeKind.MoveSpeed, RunUpgradeKind.SpiritAttraction],
        BalanceBuildKind.Rapid => [RunUpgradeKind.FireRate, RunUpgradeKind.ProjectileSpeed,
            RunUpgradeKind.MoveSpeed, RunUpgradeKind.TargetRange,
            RunUpgradeKind.NeedleDamage, RunUpgradeKind.SpiritAttraction],
        BalanceBuildKind.Utility => [RunUpgradeKind.SpiritAttraction, RunUpgradeKind.NeedleDamage,
            RunUpgradeKind.TargetRange, RunUpgradeKind.FireRate,
            RunUpgradeKind.MoveSpeed, RunUpgradeKind.ProjectileSpeed],
        _ => [RunUpgradeKind.NeedleDamage, RunUpgradeKind.FireRate,
            RunUpgradeKind.MoveSpeed, RunUpgradeKind.TargetRange,
            RunUpgradeKind.ProjectileSpeed, RunUpgradeKind.SpiritAttraction],
    };

    /// <summary>返回与六项有限修行一一对应的无尽延续顺序。</summary>
    private static RunUpgradeKind[] GetEndlessOrder(BalanceBuildKind kind) =>
        GetFiniteOrder(kind).Select(item => item switch
        {
            RunUpgradeKind.NeedleDamage => RunUpgradeKind.EndlessDamage,
            RunUpgradeKind.FireRate => RunUpgradeKind.EndlessFireRate,
            RunUpgradeKind.MoveSpeed => RunUpgradeKind.EndlessMoveSpeed,
            RunUpgradeKind.TargetRange => RunUpgradeKind.EndlessTargetRange,
            RunUpgradeKind.ProjectileSpeed => RunUpgradeKind.EndlessProjectileSpeed,
            _ => RunUpgradeKind.EndlessSpiritAttraction,
        }).ToArray();

    /// <summary>给强攻、速射和效用路线的核心无尽维度更高配额，基础路线保持完全均衡。</summary>
    private static float[] GetEndlessWeights(BalanceBuildKind kind) => kind switch
    {
        BalanceBuildKind.Assault => [3.0f, 2.0f, 1.5f, 1.0f, 1.0f, 1.0f],
        BalanceBuildKind.Rapid => [3.0f, 2.0f, 1.5f, 1.0f, 1.0f, 1.0f],
        BalanceBuildKind.Utility => [3.0f, 2.0f, 1.5f, 1.0f, 1.0f, 1.0f],
        _ => [1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f],
    };

    /// <summary>返回每条路线偏好的互斥特化，未列出的同组分支不会被模拟器偷偷取得。</summary>
    private static string[] GetSpecializationOrder(BalanceBuildKind kind) => kind switch
    {
        BalanceBuildKind.Assault => ["needle_piercing", "breathing_focus", "soul_lock",
            "wind_breaker", "tengu_awareness", "spirit_tide"],
        BalanceBuildKind.Rapid => ["breathing_swift", "needle_rain", "tengu_gale",
            "soul_net", "wind_thunder", "spirit_flow"],
        BalanceBuildKind.Utility => ["spirit_tide", "soul_lock", "tengu_awareness",
            "wind_breaker", "breathing_swift", "needle_rain"],
        _ => ["needle_rain", "breathing_swift", "tengu_awareness",
            "soul_net", "wind_breaker", "spirit_tide"],
    };
}
