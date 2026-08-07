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
    /// 为灵梦当前实装的全部符卡生成 TH06 来源条目，并保持运行目录中的稳定顺序。
    /// </summary>
    public static IReadOnlyList<CompendiumEntry> CreateAll()
    {
        ContentPackDefinition source = ContentPackCatalog.All.Single(pack => pack.Number == 6);
        return SpellCardCatalog.ReimuLoadout
            .Select(card => CreateEntry(card, source))
            .ToArray();
    }

    /// <summary>
    /// 组合一张符卡的原作身份、武侠定位、自动规则与完整战斗参数，长文本独占整行。
    /// </summary>
    private static CompendiumEntry CreateEntry(
        SpellCardDefinition card,
        ContentPackDefinition source)
    {
        RunUpgradeDefinition unlock = RunUpgradeCatalog.All.Single(
            upgrade => upgrade.Kind == card.UnlockKind);
        string prerequisite = FormatRequirement(unlock.Requirement);
        string trigger = card.EffectKind == SpellCardEffectKind.FantasySeal
            ? "灵力足够且射程内至少有 3 个目标"
            : "灵力足够且近身至少 3 敌，或半血以下遭近身";
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
                new("初出作品", card.SourceWork.Replace("初出：", string.Empty)),
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

        RunUpgradeDefinition prerequisite = RunUpgradeCatalog.All.Single(
            upgrade => upgrade.Kind == requirement.RequiredKind);
        return $"{prerequisite.DisplayName} {requirement.MinimumRank} 重";
    }
}
