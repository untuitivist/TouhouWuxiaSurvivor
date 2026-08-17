using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>将正式升级和特化目录投影为可检索武学图鉴，避免复制另一套静态技能表。</summary>
public static class RunUpgradeCompendiumEntryFactory
{
    /// <summary>返回全部非奥义修行及其特化；奥义保留在符卡分页，避免同一内容重复出现。</summary>
    public static IReadOnlyList<CompendiumEntry> CreateAll()
    {
        var entries = new List<CompendiumEntry>();
        foreach (RunUpgradeDefinition upgrade in RunUpgradeCatalog.All.Where(
                     definition => definition.Category != RunUpgradeCategory.SpellCard))
        {
            ContentPackDefinition source = ResolveSource(upgrade.RequiredContentPack);
            entries.Add(CreateUpgrade(upgrade, source));
            entries.AddRange(upgrade.Specializations.Select(specialization =>
                CreateSpecialization(upgrade, specialization, source)));
        }

        return entries;
    }

    /// <summary>创建基础或无尽修行条目，显示上限、前置、亲和与满重后的候选行为。</summary>
    private static CompendiumEntry CreateUpgrade(
        RunUpgradeDefinition upgrade,
        ContentPackDefinition source)
    {
        string rank = upgrade.IsRepeatable ? "无上限" : $"0/{upgrade.MaxRank}";
        string candidate = upgrade.IsRepeatable
            ? "满足前置后持续进入候选，边际收益递减"
            : "达到等级上限后不再进入升级候选";
        string specializations = upgrade.Specializations.Count == 0
            ? "无"
            : string.Join("、", upgrade.Specializations.Select(item => item.DisplayName));
        return new CompendiumEntry(
            CompendiumCategory.Build, upgrade.DisplayName, CompendiumSourceText.GetId(source),
            CompendiumSourceText.GetLabel(source),
            $"{upgrade.GetCategoryName()} · {rank} · " +
            RunUpgradeAffinityFormatter.FormatMany(upgrade.Affinities),
            [
                new("修行类型", upgrade.GetCategoryName()),
                new("等级上限", rank),
                new("生效规则", upgrade.EffectText, true),
                new("构筑亲和", RunUpgradeAffinityFormatter.FormatMany(upgrade.Affinities)),
                new("前置要求", FormatRequirements(upgrade.Requirements)),
                new("候选规则", candidate, true),
                new("可选特化", specializations, true),
                new("内容规则", "通用横向构筑；内容包只增加可选项，不提高等级上限", true),
            ], GetPreviewTile(upgrade), (int)upgrade.Kind);
    }

    /// <summary>创建行为特化条目，明确当前版本允许并行取得，不把分支误画成互斥选择。</summary>
    private static CompendiumEntry CreateSpecialization(
        RunUpgradeDefinition parent,
        RunUpgradeSpecialization specialization,
        ContentPackDefinition source) => new(
            CompendiumCategory.Build, specialization.DisplayName,
            CompendiumSourceText.GetId(source), CompendiumSourceText.GetLabel(source),
            $"{parent.DisplayName}特化 · 境界 {specialization.MinimumRunLevel} · " +
            RunUpgradeAffinityFormatter.FormatMany(specialization.Affinities),
            [
                new("所属修行", parent.DisplayName),
                new("解锁境界", specialization.MinimumRunLevel.ToString()),
                new("要求重数", $"{parent.DisplayName} {specialization.RequiredRank} 重"),
                new("构筑亲和", RunUpgradeAffinityFormatter.FormatMany(
                    specialization.Affinities)),
                new("行为变化", specialization.EffectText, true),
                new("互斥规则", specialization.ExcludedSpecializationIds.Count == 0
                    ? "当前无互斥，可与同修行其他特化并行取得"
                    : "与目录明确登记的特化互斥", true),
                new("候选规则", "满足境界与重数后进入三选一；取得后不再重复出现", true),
            ], GetPreviewTile(parent), 20 + (int)specialization.Effect);

    /// <summary>把一个或多个前置转换为紧凑中文，空前置稳定显示为无。</summary>
    private static string FormatRequirements(IReadOnlyList<RunUpgradeRequirement> requirements)
    {
        if (requirements.Count == 0) return "无";
        return string.Join("、", requirements.Select(requirement =>
        {
            RunUpgradeDefinition? parent = RunUpgradeCatalog.FindById(
                requirement.RequiredUpgradeId);
            return $"{parent?.DisplayName ?? requirement.RequiredUpgradeId} " +
                $"{requirement.MinimumRank} 重";
        }));
    }

    /// <summary>解析定义的来源包；没有可选包要求的升级属于始终可用的幻想乡本体。</summary>
    private static ContentPackDefinition ResolveSource(string? sourceId) =>
        sourceId is null
            ? ContentPackCatalog.Base
            : ContentPackCatalog.All.Single(source => source.Id == sourceId);

    /// <summary>武学使用神社草地，心法使用结界土色，使文字动态图标保持两类可辨。</summary>
    private static TileId GetPreviewTile(RunUpgradeDefinition upgrade) =>
        upgrade.Category == RunUpgradeCategory.MartialArt
            ? TileId.ShrineGrassBase
            : TileId.BoundarySoilBase;
}
