using Godot;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 从内部原作弹幕图集选择一个像素格，供符卡实体在公开包缺图时安全回退文字表现。
/// </summary>
public partial class InternalSpellBulletVisual : Sprite2D
{
    private readonly InternalVisualCatalog _catalog = new();

    /// <summary>
    /// 按符卡中文名和颜色变体配置 16×16 弹幕区域，资源缺失时隐藏自身交由文字节点显示。
    /// </summary>
    public void Configure(string sourceId, string spellCardName, int variant)
    {
        if (!_catalog.TryGet(
                sourceId, InternalVisualCategory.SpellCard, spellCardName, out var definition) ||
            definition.Kind != InternalVisualKind.BulletAtlas ||
            !_catalog.TryGetTexture(definition, out Texture2D texture))
        {
            Visible = false;
            return;
        }

        Visible = true;
        Texture = texture;
        RegionEnabled = true;
        RegionRect = new Rect2((1 + variant % 14) * 16.0f, 32.0f, 16.0f, 16.0f);
        TextureFilter = TextureFilterEnum.Nearest;
    }
}
