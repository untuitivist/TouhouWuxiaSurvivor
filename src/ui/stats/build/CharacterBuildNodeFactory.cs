using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 把升级与特化定义投影为完整节点状态，并集中生成人类可读的锁定原因。
/// </summary>
public static class CharacterBuildNodeFactory
{
    /// <summary>
    /// 投影一个升级节点；已习得优先于后续锁定条件，未习得时按可解释顺序判断阻塞来源。
    /// </summary>
    public static CharacterBuildNodeView CreateUpgrade(
        RunUpgradeDefinition definition,
        RunBuildState build,
        ContentPackSelection content,
        int sortOrder)
    {
        int rank = build.GetRank(definition.Id);
        (CharacterBuildNodeState state, string reason) = ResolveUpgradeState(
            definition, build, content, rank);
        bool contentEnabled = definition.RequiredContentPack is null ||
            content.IsEnabled(definition.RequiredContentPack);
        string trigger = definition.SpellCardId is null
            ? string.Empty
            : FormatSpellTrigger(definition.SpellCardId);
        return new CharacterBuildNodeView(
            definition.Id, null, CharacterBuildNodeKind.Upgrade, state,
            definition.DisplayName, definition.Category, definition.GetCategoryName(),
            definition.EffectText,
            CharacterBuildProgressText.FormatCurrent(definition, rank),
            CharacterBuildProgressText.FormatNext(definition, rank),
            trigger, reason, rank, definition.MaxRank,
            definition.IsRepeatable, contentEnabled && build.CanUpgrade(definition),
            definition.RequiredContentPack ?? "base",
            definition.Affinities, (int)definition.Category, sortOrder);
    }

    /// <summary>
    /// 投影一个特化节点，并将同门分支与显式互斥、基础重数和境界门槛分别呈现。
    /// </summary>
    public static CharacterBuildNodeView CreateSpecialization(
        RunUpgradeDefinition parent,
        RunUpgradeSpecialization specialization,
        RunBuildState build,
        ContentPackSelection content,
        int runLevel,
        int sortOrder)
    {
        (CharacterBuildNodeState state, string reason) = ResolveSpecializationState(
            parent, specialization, build, content, runLevel);
        bool contentEnabled = parent.RequiredContentPack is null ||
            content.IsEnabled(parent.RequiredContentPack);
        bool selected = build.HasSpecialization(specialization.Id);
        return new CharacterBuildNodeView(
            specialization.Id, parent.Id, CharacterBuildNodeKind.Specialization,
            state, specialization.DisplayName, parent.Category,
            FormatSpecializationCategory(specialization),
            specialization.EffectText,
            CharacterBuildProgressText.FormatSpecializationCurrent(
                specialization, selected),
            CharacterBuildProgressText.FormatSpecializationNext(
                specialization, selected),
            string.Empty, reason,
            selected ? 1 : 0, 1, false,
            contentEnabled && build.CanSpecialize(parent, specialization, runLevel),
            parent.RequiredContentPack ?? "base", specialization.Affinities,
            3, sortOrder);
    }

    /// <summary>
    /// 按已取得、内容、互斥、前置与满重次序解析普通升级状态和具体原因。
    /// </summary>
    private static (CharacterBuildNodeState, string) ResolveUpgradeState(
        RunUpgradeDefinition definition,
        RunBuildState build,
        ContentPackSelection content,
        int rank)
    {
        if (rank > 0)
        {
            bool mastered = !definition.IsRepeatable && rank >= definition.MaxRank;
            return (mastered ? CharacterBuildNodeState.Mastered :
                CharacterBuildNodeState.Learned, string.Empty);
        }

        if (definition.RequiredContentPack is not null &&
            !content.IsEnabled(definition.RequiredContentPack))
        {
            return (CharacterBuildNodeState.LockedContent, "本局未启用所属作品");
        }

        string[] conflicts = definition.ExcludedUpgradeIds
            .Where(id => build.GetRank(id) > 0).Select(GetUpgradeName).ToArray();
        if (conflicts.Length > 0)
        {
            return (CharacterBuildNodeState.LockedExclusion,
                $"与已习得的{string.Join("、", conflicts)}互斥");
        }

        string[] missing = definition.Requirements.Where(requirement =>
                build.GetRank(requirement.RequiredUpgradeId) < requirement.MinimumRank)
            .Select(requirement =>
                $"{GetUpgradeName(requirement.RequiredUpgradeId)} {requirement.MinimumRank}重")
            .ToArray();
        return missing.Length > 0
            ? (CharacterBuildNodeState.LockedPrerequisite,
                $"需先修习{string.Join("、", missing)}")
            : (CharacterBuildNodeState.Available, string.Empty);
    }

    /// <summary>
    /// 按已选、内容、同门互斥、基础重数和境界依次解析特化状态，便于节点显示唯一主因。
    /// </summary>
    private static (CharacterBuildNodeState, string) ResolveSpecializationState(
        RunUpgradeDefinition parent,
        RunUpgradeSpecialization specialization,
        RunBuildState build,
        ContentPackSelection content,
        int runLevel)
    {
        if (build.HasSpecialization(specialization.Id))
        {
            return (CharacterBuildNodeState.Mastered, string.Empty);
        }

        if (parent.RequiredContentPack is not null &&
            !content.IsEnabled(parent.RequiredContentPack))
        {
            return (CharacterBuildNodeState.LockedContent, "本局未启用所属作品");
        }

        string? conflict = parent.Specializations.FirstOrDefault(item =>
            build.HasSpecialization(item.Id))?.DisplayName;
        if (conflict is not null)
        {
            return (CharacterBuildNodeState.LockedExclusion, $"已选择互斥特化{conflict}");
        }

        if (build.GetRank(parent.Id) < specialization.RequiredRank)
        {
            return (CharacterBuildNodeState.LockedPrerequisite,
                $"需先将{parent.DisplayName}修至{specialization.RequiredRank}重");
        }

        return runLevel < specialization.MinimumRunLevel
            ? (CharacterBuildNodeState.LockedLevel,
                $"需达到境界 {specialization.MinimumRunLevel}")
            : (CharacterBuildNodeState.Available, string.Empty);
    }

    /// <summary>
    /// 按稳定 ID 返回升级显示名；目录损坏时保留 ID，确保面板仍能诊断断裂关系。
    /// </summary>
    private static string GetUpgradeName(string id) =>
        RunUpgradeCatalog.FindById(id)?.DisplayName ?? id;

    /// <summary>按实际效果区分弹幕行为、收益与常规属性特化，使节点标题能表达分支性质。</summary>
    private static string FormatSpecializationCategory(
        RunUpgradeSpecialization specialization) => specialization.Effect switch
        {
            RunSpecializationEffect.BarrageProjectiles or
            RunSpecializationEffect.ProjectilePierce or
            RunSpecializationEffect.ConvergingBarrage => "弹幕特化",
            RunSpecializationEffect.SpiritYield => "收益特化",
            RunSpecializationEffect.ContinuousFireMomentum or
            RunSpecializationEffect.StationaryFocus or
            RunSpecializationEffect.MovementMomentum or
            RunSpecializationEffect.SpiritFlowMomentum => "蓄势特化",
            _ => "属性特化",
        };

    /// <summary>
    /// 从奥义唯一数据源取得定时缩放说明，缺失定义时给出明确的数据错误提示。
    /// </summary>
    private static string FormatSpellTrigger(string spellCardId)
    {
        SpellCardDefinition? card = SpellCardCatalog.FindById(spellCardId);
        return card is null ? "符卡定义缺失" : SpellCardTriggerTextFormatter.Format(card);
    }
}
