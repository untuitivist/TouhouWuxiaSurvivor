using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
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
            .Select(card => CreateEntry(card, ContentPackCatalog.All.Single(
                pack => pack.Id == card.SourcePackId)))
            .ToArray();
    }

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
        string trigger = FormatTrigger(card.TriggerKind);
        string targets = card.Combat.TargetCount > 0
            ? card.Combat.TargetCount.ToString()
            : "范围内全部";
        string defense = card.Combat.DefenseSeconds > 0.0f
            ? $"{card.Combat.DefenseSeconds:0.##} 秒"
            : "无";
        return new CompendiumEntry(
            CompendiumCategory.SpellCard,
            card.FullName,
            source.Id,
            $"TH{source.Number:00} {source.DisplayName}",
            $"{card.WuxiaStyle} · 消耗 {card.Combat.PowerCost} 灵力",
            [
                new("所属角色", card.OwnerName),
                new("设定来源", (card.CanonLevel == SpellCardCanonLevel.Official
                    ? "原作正式符卡 · " : "旧作攻击意象的武侠化拟制 · ") +
                    card.SourceNote, true),
                new("武侠定位", card.WuxiaStyle, true),
                new("前置构筑", prerequisite, true),
                new("自动触发", trigger, true),
                new("灵力消耗", card.Combat.PowerCost.ToString()),
                new("公共冷却", $"{card.Combat.CooldownSeconds:0.#} 秒"),
                new("作用范围", $"{card.Combat.EffectRange:0} 像素"),
                new("单次伤害", card.Combat.Damage.ToString()),
                new("目标数量", targets),
                new("护身时间", defense),
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

    /// <summary>
    /// 把自动条件枚举转换为图鉴说明，所有分支都明确不需要玩家按键。
    /// </summary>
    private static string FormatTrigger(SpellCardTriggerKind trigger) => trigger switch
    {
        SpellCardTriggerKind.Crowd => "灵力足够且范围内至少有 3 个目标",
        SpellCardTriggerKind.Danger => "灵力足够且受围，或半血以下遭近身",
        SpellCardTriggerKind.SingleTarget => "灵力足够且范围内存在目标",
        _ => "不满足自动施展条件",
    };
}
