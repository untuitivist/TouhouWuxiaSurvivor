using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 从内部原作弹幕图集选择一个像素格，供符卡实体在公开包缺图时安全回退文字表现。
/// </summary>
public partial class InternalSpellBulletVisual : Sprite2D
{
    private static readonly InternalVisualCatalog Catalog = new();

    /// <summary>
    /// 按符卡中文名和颜色变体配置 16×16 弹幕区域，资源缺失时隐藏自身交由文字节点显示。
    /// </summary>
    public void Configure(
        string sourceId,
        string spellCardName,
        SpellCardGeometryKind geometryKind,
        int variant)
    {
        if (!Catalog.TryGet(
                sourceId, InternalVisualCategory.SpellCard, spellCardName, out var definition) ||
            definition.Kind != InternalVisualKind.BulletAtlas ||
            !Catalog.TryGetTexture(definition, out Texture2D texture))
        {
            Visible = false;
            return;
        }

        Visible = true;
        Texture = texture;
        RegionEnabled = true;
        RegionRect = SpellBulletAtlasRegionResolver.Resolve(
            definition, geometryKind, variant, texture);
        TextureFilter = TextureFilterEnum.Nearest;
    }
}
