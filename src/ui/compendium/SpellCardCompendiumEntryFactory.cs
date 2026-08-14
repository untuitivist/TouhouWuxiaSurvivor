using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Ui.SpellCards;
using TouhouWuxiaSurvivor.Ui.Stats.Build;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 将运行时符卡目录转换为图鉴条目，保证原作名、构筑条件和实战数值只有一个数据来源。
/// </summary>
public static class SpellCardCompendiumEntryFactory
{
    /// <summary>
    /// 为全部内容包符卡生成来源条目，并保持运行目录中的作品与清单顺序。
    /// </summary>
    public static IReadOnlyList<CompendiumEntry> CreateAll()
    {
        return SpellCardCatalog.All
            .Select(card => CreateEntry(card, ResolveSource(card.SourcePackId)))
            .ToArray();
    }

    /// <summary>
    /// 在本体和可选正作的统一命名空间解析符卡来源，避免常驻奥义被误当成不存在的 DLC。
    /// </summary>
    private static ContentPackDefinition ResolveSource(string sourcePackId) =>
        string.Equals(sourcePackId, ContentPackCatalog.Base.Id, StringComparison.Ordinal)
            ? ContentPackCatalog.Base
            : ContentPackCatalog.All.Single(pack => pack.Id == sourcePackId);

    /// <summary>
    /// 组合一张符卡的原作身份、武侠定位、自动规则与完整战斗参数，长文本独占整行。
    /// </summary>
    private static CompendiumEntry CreateEntry(
        SpellCardDefinition card,
        ContentPackDefinition source)
    {
        RunUpgradeDefinition unlock = RunUpgradeCatalog.FindById(card.UnlockUpgradeId) ??
            throw new InvalidDataException($"Spell unlock is missing: {card.Id}");
        string prerequisite = FormatRequirement(unlock.Requirement);
        string targets = card.Combat.TargetScale > 0.0f
            ? $"角色奥义承载 ×{card.Combat.TargetScale:0.##}"
            : "角色奥义承载 ×1";
        string defense = card.Combat.DefenseScale > 0.0f
            ? $"受击无敌 ×{card.Combat.DefenseScale:0.##}"
            : "不提供护持";
        string slot = SpellCardSlotPolicy.Classify(card) == SpellCardSlotKind.Support
            ? $"护持奥义 · 共享 {SpellCardSlotPolicy.MaximumSupportSlots} 槽"
            : $"主攻奥义 · 共享 {SpellCardSlotPolicy.MaximumOffensiveSlots} 槽";
        return new CompendiumEntry(
            CompendiumCategory.SpellCard,
            card.FullName,
            source.Id,
            source.Number > 0
                ? $"TH{source.Number:00} {source.DisplayName}"
                : source.DisplayName,
            $"{slot} · {SpellCardActivationText.GetShortName(card.ActivationKind)}",
            [
                new("所属角色", card.OwnerName),
                new("设定来源", (card.CanonLevel == SpellCardCanonLevel.Official
                    ? "原作正式符卡 · " : "旧作攻击意象的武侠化拟制 · ") +
                    card.SourceNote, true),
                new("定位与槽位", $"{card.WuxiaStyle} · {slot} · 弹幕形态 " +
                    SpellCardGeometryText.GetName(card.GeometryKind), true),
                new("前置构筑", prerequisite, true),
                new("自动触发", SpellCardTriggerTextFormatter.DescribeAutomaticTrigger(card), true),
                new("周天换算", $"当前角色奥义周天 ×{card.Combat.IntervalScale:0.##}"),
                new("攻势换算", $"当前实效攻势 ×{card.Combat.DamageScale:0.##}"),
                new("范围换算", $"当前实效索敌 ×{card.Combat.RangeScale:0.##}"),
                new("弹速换算", $"当前实效弹速 ×{card.Combat.ProjectileSpeedScale:0.##}"),
                new("目标承载", targets),
                new("护身换算", defense),
                new("奥义效果", card.EffectDescription, true),
            ],
            TileId.ShrineGrassBase,
            (int)card.EffectKind,
            null,
            card);
    }

    /// <summary>
    /// 把稳定前置类型还原为玩家可读的修炼名称与最低重数；缺失条件时明确写成无。
    /// </summary>
    private static string FormatRequirement(RunUpgradeRequirement? requirement)
    {
        if (requirement is null)
        {
            return "无";
        }

        RunUpgradeDefinition prerequisite = RunUpgradeCatalog.FindById(
            requirement.RequiredUpgradeId) ?? throw new InvalidDataException(
                $"Spell prerequisite is missing: {requirement.RequiredUpgradeId}");
        return $"{prerequisite.DisplayName} {requirement.MinimumRank} 重";
    }

}
