using Godot;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 集中绘制强化、灵息与双方投射物，和敌人视觉分离后仍只使用共享纹理及批量 Canvas 指令。
/// </summary>
public sealed class EcsCombatItemRenderer
{
    private const string BaseSourceId = "base";
    private const string DefaultBulletName = "灵符「梦想封印」";
    private Texture2D? _itemAtlas;
    private Texture2D? _bulletAtlas;
    private Font? _font;

    public int LastPickupIconCount { get; private set; }
    public int LastSpiritIconCount { get; private set; }
    public int LastProjectileIconCount { get; private set; }
    public int LastEnemyProjectileIconCount { get; private set; }

    /// <summary>从共享内部清单加载道具与弹幕图集，缺失时保留中文或纯色回退。</summary>
    public void Configure(InternalVisualCatalog visuals)
    {
        _itemAtlas = LoadTexture(visuals, BaseSourceId,
            InternalVisualCategory.Pickup,
            PickupCatalog.Get(PickupKind.RapidFire).DisplayName,
            InternalVisualKind.ItemAtlas);
        _bulletAtlas = LoadTexture(visuals, BaseSourceId,
            InternalVisualCategory.SpellCard, DefaultBulletName,
            InternalVisualKind.BulletAtlas);
        _font = ThemeDB.FallbackFont;
    }

    /// <summary>清空本帧实际提交的图标数量，不把 CPU 剔除实体误计为已绘制。</summary>
    public void ResetCounts()
    {
        LastPickupIconCount = 0;
        LastSpiritIconCount = 0;
        LastProjectileIconCount = 0;
        LastEnemyProjectileIconCount = 0;
    }

    /// <summary>从东方道具图集选择 P、星和 F 图标，表达三种临时构筑强化。</summary>
    public void DrawPickup(Node2D canvas, PickupComponent pickup)
    {
        if (_itemAtlas is null)
        {
            DrawText(canvas, pickup.Position, pickup.Definition.DisplayName,
                new Color("f0d477"));
            return;
        }

        Rect2 source = pickup.Definition.Kind switch
        {
            PickupKind.MoveSpeed => new Rect2(96.0f, 0.0f, 32.0f, 32.0f),
            PickupKind.RapidFire => new Rect2(0.0f, 0.0f, 32.0f, 32.0f),
            PickupKind.SpiralShot => new Rect2(160.0f, 0.0f, 32.0f, 32.0f),
            _ => new Rect2(0.0f, 0.0f, 32.0f, 32.0f),
        };
        var destination = new Rect2(
            (pickup.Position - Vector2.One * 8.0f).Round(), Vector2.One * 16.0f);
        canvas.DrawTextureRectRegion(_itemAtlas, destination, source);
        LastPickupIconCount++;
    }

    /// <summary>用道具图集中的青色季节点表示经验，合并价值大于一时附加数字。</summary>
    public void DrawSpirit(Node2D canvas, SpiritComponent spirit)
    {
        if (_itemAtlas is not null)
        {
            var destination = new Rect2(
                (spirit.Position - Vector2.One * 5.0f).Round(), Vector2.One * 10.0f);
            canvas.DrawTextureRectRegion(
                _itemAtlas, destination, new Rect2(128.0f, 0.0f, 32.0f, 32.0f));
            LastSpiritIconCount++;
        }
        else
        {
            DrawText(canvas, spirit.Position, "灵息", new Color("b8efcf"));
        }

        if (spirit.Value > 1)
        {
            DrawText(canvas, spirit.Position + new Vector2(7.0f, 5.0f),
                spirit.Value.ToString(), new Color("b8efcf"), 8, 18.0f);
        }
    }

    /// <summary>从共享弹幕图集选择阵营与变体对应的灵弹，并固定为八像素显示尺寸。</summary>
    public void DrawProjectile(Node2D canvas, ProjectileComponent projectile)
    {
        var destination = new Rect2(
            (projectile.Position - Vector2.One * 4.0f).Round(), Vector2.One * 8.0f);
        if (_bulletAtlas is not null)
        {
            int column = projectile.Faction == ProjectileFaction.Player
                ? 1
                : 3 + projectile.VisualVariant % 4;
            canvas.DrawTextureRectRegion(
                _bulletAtlas, destination, new Rect2(column * 16.0f, 32.0f, 16.0f, 16.0f));
            LastProjectileIconCount++;
            if (projectile.Faction == ProjectileFaction.Enemy)
            {
                LastEnemyProjectileIconCount++;
            }
        }
        else
        {
            Color fallback = projectile.Faction == ProjectileFaction.Player
                ? new Color("f4df7d")
                : new Color("ef7898");
            canvas.DrawRect(destination, fallback);
        }
    }

    /// <summary>按稳定内容键加载指定版式纹理，版式不匹配时让调用方使用回退视觉。</summary>
    private static Texture2D? LoadTexture(
        InternalVisualCatalog visuals,
        string sourceId,
        InternalVisualCategory category,
        string name,
        InternalVisualKind expectedKind) =>
        visuals.TryGet(sourceId, category, name, out InternalVisualDefinition definition) &&
        definition.Kind == expectedKind && visuals.TryGetTexture(definition, out Texture2D texture)
            ? texture
            : null;

    /// <summary>在素材缺失时绘制居中的短中文标识，不改变实体数据或碰撞尺寸。</summary>
    private void DrawText(
        Node2D canvas,
        Vector2 position,
        string text,
        Color color,
        int size = 10,
        float width = 56.0f) =>
        canvas.DrawString(_font, position + new Vector2(-width * 0.5f, 4.0f),
            text, HorizontalAlignment.Center, width, size, color);
}
