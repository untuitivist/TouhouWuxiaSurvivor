using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 把紧凑符卡视觉编号解析为原作弹幕图集区域；高频渲染只做字典查询，不扫描内容目录。
/// </summary>
public sealed class SpellCardProjectileVisualResolver
{
    private readonly Dictionary<int,
        (InternalVisualDefinition Definition, SpellBulletStyleKind Style)> _visuals = new();
    private InternalVisualCatalog? _catalog;

    /// <summary>一次性加载全部有效符卡图集；缺少映射的条目由正式渲染器继续使用通用弹幕。</summary>
    public void Configure(InternalVisualCatalog visuals)
    {
        _catalog = visuals;
        _visuals.Clear();
        foreach (SpellCardDefinition card in SpellCardCatalog.All)
        {
            if (!visuals.TryGet(card.SourcePackId, InternalVisualCategory.SpellCard,
                    card.FullName, out InternalVisualDefinition definition) ||
                definition.Kind != InternalVisualKind.BulletAtlas)
            {
                continue;
            }

            int binding = SpellCardVisualBindingCatalog.GetBindingId(card.Id);
            _visuals.Add(binding, (definition, card.BulletStyleKind));
        }
    }

    /// <summary>返回该符卡和弹丸序号对应的完整帧及显示尺寸；编号零或缺图时返回 false。</summary>
    public bool TryResolve(
        int visualStyleId,
        int projectileVariant,
        out Texture2D texture,
        out SpellBulletVisualSelection selection)
    {
        texture = null!;
        selection = default;
        if (visualStyleId <= 0 || _catalog is null ||
            !_visuals.TryGetValue(visualStyleId, out var visual) ||
            !_catalog.TryGetTexture(visual.Definition, out texture))
        {
            return false;
        }

        selection = SpellBulletAtlasRegionResolver.Resolve(
            visual.Definition, visual.Style, projectileVariant, texture);
        return true;
    }
}
