using Godot;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 把紧凑符卡视觉编号解析为原作弹幕图集区域；高频渲染只做字典查询，不扫描内容目录。
/// </summary>
public sealed class SpellCardProjectileVisualResolver
{
    private readonly Dictionary<int,
        (InternalVisualDefinition Definition, Texture2D Texture,
            SpellBulletStyleKind Style)> _spellVisuals = new();
    private readonly Dictionary<(int Source, SpellBulletStyleKind Style),
        (InternalVisualDefinition Definition, Texture2D Texture)> _sourceVisuals = new();

    /// <summary>
    /// 一次性建立精确符卡与通用内容包索引；本包原生图集优先，明确代理只在本包无原生图集时使用。
    /// </summary>
    public void Configure(InternalVisualCatalog visuals)
    {
        ArgumentNullException.ThrowIfNull(visuals);
        _spellVisuals.Clear();
        _sourceVisuals.Clear();
        SpellCardDefinition[] cards = SpellCardCatalog.All
            .OrderBy(card => card.Id, StringComparer.Ordinal).ToArray();
        foreach (IGrouping<string, SpellCardDefinition> group in cards.GroupBy(
                     card => card.SourcePackId, StringComparer.Ordinal))
        {
            InternalVisualDefinition[] candidates = group
                .Select(card => FindDefinition(visuals, card))
                .Where(definition => definition is not null)
                .Cast<InternalVisualDefinition>()
                .DistinctBy(definition => definition.AssetPath, StringComparer.Ordinal)
                .ToArray();
            int sourceBinding = ProjectileVisualSourceBindingCatalog.GetBindingId(group.Key);
            foreach (SpellBulletStyleKind style in Enum.GetValues<SpellBulletStyleKind>())
            {
                if (TrySelectPackVisual(visuals, candidates, style,
                        out InternalVisualDefinition definition, out Texture2D texture))
                {
                    _sourceVisuals.Add((sourceBinding, style), (definition, texture));
                }
            }
        }

        foreach (SpellCardDefinition card in cards)
        {
            InternalVisualDefinition? exact = FindDefinition(visuals, card);
            if (exact is null) continue;
            InternalVisualDefinition selected = SelectExactOrNative(card, exact);
            if (!TryLoad(visuals, selected, card.BulletStyleKind, out Texture2D texture)) continue;
            int binding = SpellCardVisualBindingCatalog.GetBindingId(card.Id);
            _spellVisuals.Add(binding, (selected, texture, card.BulletStyleKind));
        }
    }

    /// <summary>返回该符卡和弹丸序号对应的完整帧及显示尺寸；编号零或缺图时返回 false。</summary>
    public bool TryResolve(
        int visualStyleId,
        int projectileVariant,
        out Texture2D texture,
        out SpellBulletVisualSelection selection) => TryResolve(
            visualStyleId, projectileVariant, out texture, out selection, out _);

    /// <summary>以同一符卡图集解析复合演出的指定弹型，保证五行或曳尾不会借用其他内容包。</summary>
    public bool TryResolve(
        int visualStyleId,
        SpellBulletStyleKind style,
        int projectileVariant,
        out Texture2D texture,
        out SpellBulletVisualSelection selection)
    {
        texture = null!;
        selection = default;
        if (visualStyleId <= 0 || !_spellVisuals.TryGetValue(visualStyleId, out var visual))
            return false;
        texture = visual.Texture;
        selection = SpellBulletAtlasRegionResolver.Resolve(
            visual.Definition, style, projectileVariant, texture);
        return true;
    }

    /// <summary>解析精确符卡视觉并返回实际采用的来源定义，供来源契约测试与诊断读取。</summary>
    public bool TryResolve(
        int visualStyleId,
        int projectileVariant,
        out Texture2D texture,
        out SpellBulletVisualSelection selection,
        out InternalVisualDefinition definition)
    {
        texture = null!;
        selection = default;
        definition = null!;
        if (visualStyleId <= 0 || !_spellVisuals.TryGetValue(visualStyleId, out var visual))
        {
            return false;
        }

        texture = visual.Texture;
        definition = visual.Definition;
        selection = SpellBulletAtlasRegionResolver.Resolve(
            visual.Definition, visual.Style, projectileVariant, texture);
        return true;
    }

    /// <summary>
    /// 按弹丸所属内容包解析同语义素材；未知来源或本包没有原生/已声明代理时返回 false。
    /// </summary>
    public bool TryResolveSource(
        int visualSourceId,
        SpellBulletStyleKind style,
        int projectileVariant,
        out Texture2D texture,
        out SpellBulletVisualSelection selection,
        out InternalVisualDefinition definition)
    {
        texture = null!;
        selection = default;
        definition = null!;
        if (!_sourceVisuals.TryGetValue((visualSourceId, style), out var visual)) return false;
        texture = visual.Texture;
        definition = visual.Definition;
        selection = SpellBulletAtlasRegionResolver.Resolve(
            definition, style, projectileVariant, texture);
        return true;
    }

    /// <summary>读取条目的精确映射；非弹幕图集不参与任何弹丸回退候选。</summary>
    private static InternalVisualDefinition? FindDefinition(
        InternalVisualCatalog visuals,
        SpellCardDefinition card) =>
        visuals.TryGet(card.SourcePackId, InternalVisualCategory.SpellCard,
            card.FullName, out InternalVisualDefinition definition) &&
        definition.Kind == InternalVisualKind.BulletAtlas
            ? definition
            : null;

    /// <summary>代理卡在本包已有原生同语义图集时改用原生图集，否则保留清单明确声明的代理。</summary>
    private InternalVisualDefinition SelectExactOrNative(
        SpellCardDefinition card,
        InternalVisualDefinition exact)
    {
        int source = ProjectileVisualSourceBindingCatalog.GetBindingId(card.SourcePackId);
        return exact.ProxySourceWork is not null &&
            _sourceVisuals.TryGetValue((source, card.BulletStyleKind), out var native) &&
            native.Definition.ProxySourceWork is null
                ? native.Definition
                : exact;
    }

    /// <summary>在一包候选中先找原生图集，再找显式代理；同层按稳定素材路径决定结果。</summary>
    private static bool TrySelectPackVisual(
        InternalVisualCatalog visuals,
        IReadOnlyList<InternalVisualDefinition> candidates,
        SpellBulletStyleKind style,
        out InternalVisualDefinition definition,
        out Texture2D texture)
    {
        foreach (bool proxy in new[] { false, true })
        {
            foreach (InternalVisualDefinition candidate in candidates
                         .Where(item => (item.ProxySourceWork is not null) == proxy)
                         .OrderBy(item => item.AssetPath, StringComparer.Ordinal))
            {
                if (!TryLoad(visuals, candidate, style, out texture)) continue;
                definition = candidate;
                return true;
            }
        }

        definition = null!;
        texture = null!;
        return false;
    }

    /// <summary>加载图集并验证该图集家族能解析目标语义，损坏区域由统一切片器立即报告。</summary>
    private static bool TryLoad(
        InternalVisualCatalog visuals,
        InternalVisualDefinition definition,
        SpellBulletStyleKind style,
        out Texture2D texture)
    {
        if (!visuals.TryGetTexture(definition, out texture)) return false;
        _ = SpellBulletAtlasRegionResolver.Resolve(definition, style, 0, texture);
        return true;
    }
}
