using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 从内部原作弹幕图集选择一个完整弹型帧，供奥义实体在素材缺失时安全回退文字表现。
/// </summary>
public partial class InternalSpellBulletVisual : Sprite2D
{
    private static readonly InternalVisualCatalog Catalog = new();

    /// <summary>
    /// 按符卡中文名、弹型语义和颜色变体配置完整帧，资源缺失时交由文字节点显示。
    /// </summary>
    public void Configure(
        string sourceId,
        string spellCardName,
        SpellBulletStyleKind bulletStyle,
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
        SpellBulletVisualSelection selection = SpellBulletAtlasRegionResolver.Resolve(
            definition, bulletStyle, variant, texture);
        RegionRect = selection.Source;
        Scale = selection.CreateSpriteScale();
        TextureFilter = TextureFilterEnum.Nearest;
    }
}
