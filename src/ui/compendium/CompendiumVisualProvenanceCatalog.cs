using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 将正式视觉清单中的内容包归属、代理原因与审核状态投影到图鉴，不重新维护素材说明。
/// </summary>
public sealed class CompendiumVisualProvenanceCatalog
{
    public const string Placeholder = "{visual_provenance}";
    private readonly InternalVisualCatalog _visuals = new();

    /// <summary>
    /// 为目录条目补全实际素材来源；规则类条目没有纹理身份，保持原始展示不变。
    /// </summary>
    public CompendiumEntry Enrich(CompendiumEntry entry)
    {
        if (entry.Category == CompendiumCategory.Build)
        {
            return entry;
        }

        InternalVisualDefinition? definition = Resolve(entry);
        string detail = BuildDetail(definition);
        string sourceLabel = definition is null
            ? $"{entry.SourceName}\n中文图标回退"
            : definition.ProxySourceWork is null
                ? $"{entry.SourceName}\n本包原生素材"
                : $"{entry.SourceName}\n审核代理 {FormatSourceId(definition.ProxySourceWork)}";
        CompendiumFact[] facts = entry.Facts.Select(fact =>
            fact.Value.Contains(Placeholder, StringComparison.Ordinal)
                ? new CompendiumFact(fact.Label,
                    fact.Value.Replace(Placeholder, detail, StringComparison.Ordinal),
                    fact.IsWide)
                : fact).ToArray();
        return entry.WithPresentation(facts, sourceLabel);
    }

    /// <summary>
    /// 普通条目按精确复合键取图；奥义在本包存在原生图集时遵守正式战斗的原生优先规则。
    /// </summary>
    private InternalVisualDefinition? Resolve(CompendiumEntry entry)
    {
        if (!Enum.TryParse(entry.Category.ToString(), false,
                out InternalVisualCategory category) ||
            !_visuals.TryGet(entry.SourceId, category, entry.Name,
                out InternalVisualDefinition exact))
        {
            return null;
        }

        if (entry.SpellCard is null || exact.ProxySourceWork is null)
        {
            return exact;
        }

        return _visuals.GetDefinitions(entry.SpellCard.SourcePackId,
                InternalVisualCategory.SpellCard)
            .FirstOrDefault(candidate =>
                candidate.Kind == InternalVisualKind.BulletAtlas &&
                candidate.ProxySourceWork is null) ?? exact;
    }

    /// <summary>生成完整来源说明；代理必须把来源作品、审核状态和登记原因同时展示。</summary>
    private static string BuildDetail(InternalVisualDefinition? definition)
    {
        if (definition is null)
        {
            return "暂无素材映射，使用中文动态图标";
        }

        if (definition.ProxySourceWork is null)
        {
            return "优先使用本内容包原生同语义素材";
        }

        string review = definition.ReviewStatus == "proxy-reviewed"
            ? "已审核代理"
            : "登记代理";
        string reason = string.IsNullOrWhiteSpace(definition.ReasonZh)
            ? "清单未提供代理原因"
            : definition.ReasonZh;
        return $"{review} {definition.ProxySourceWork}：{reason}";
    }

    /// <summary>把稳定内容包 ID 压缩成窄栏可完整显示的本体或 TH 编号。</summary>
    private static string FormatSourceId(string sourceId)
    {
        if (string.Equals(sourceId, ContentPackCatalog.Base.Id, StringComparison.Ordinal))
        {
            return "本体";
        }

        ContentPackDefinition? source = ContentPackCatalog.All.FirstOrDefault(pack =>
            string.Equals(pack.Id, sourceId, StringComparison.Ordinal));
        return source?.Number > 0 ? $"TH{source.Number:00}" : sourceId;
    }
}
