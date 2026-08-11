using Godot;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 将 ECS 战斗数据批量绘制为原作纹理；素材不可用时保留中文文字回退。
/// </summary>
public sealed class EcsCombatRenderer
{
    private const string BaseSourceId = "base";
    private const string Th06SourceId = "th06_eosd";
    private const string DefaultBulletName = "灵符「梦想封印」";
    private readonly InternalVisualCatalog _visuals = new();
    private Texture2D? _itemAtlas;
    private Texture2D? _bulletAtlas;
    private Font? _font;

    public int LastMappedEnemyCount { get; private set; }
    public int LastFallbackEnemyCount { get; private set; }
    public int LastPickupIconCount { get; private set; }
    public int LastSpiritIconCount { get; private set; }
    public int LastProjectileIconCount { get; private set; }

    /// <summary>
    /// 从共享内部清单加载东方道具、红魔乡弹幕图集和默认字体，不直接持有资源路径。
    /// </summary>
    public void Configure()
    {
        _itemAtlas = LoadTexture(
            BaseSourceId,
            InternalVisualCategory.Pickup,
            PickupCatalog.Get(PickupKind.RapidFire).DisplayName,
            InternalVisualKind.ItemAtlas);
        _bulletAtlas = LoadTexture(
            Th06SourceId,
            InternalVisualCategory.SpellCard,
            DefaultBulletName,
            InternalVisualKind.BulletAtlas);
        _font = ThemeDB.FallbackFont;
    }

    /// <summary>
    /// 按固定层次绘制掉落物、敌人、灵息和玩家弹幕，并记录本帧素材覆盖统计。
    /// </summary>
    public void Draw(
        Node2D canvas,
        EnemyPool enemies,
        IReadOnlyList<PickupComponent> pickups,
        IReadOnlyList<SpiritComponent> spirits,
        ProjectilePool projectiles,
        double animationTime)
    {
        LastMappedEnemyCount = 0;
        LastFallbackEnemyCount = 0;
        LastPickupIconCount = 0;
        LastSpiritIconCount = 0;
        LastProjectileIconCount = 0;
        foreach (PickupComponent pickup in pickups)
        {
            DrawPickup(canvas, pickup);
        }

        enemies.ForEach(enemy => DrawEnemy(canvas, enemy, animationTime));
        foreach (SpiritComponent spirit in spirits)
        {
            DrawSpirit(canvas, spirit);
        }

        projectiles.ForEach(projectile => DrawProjectile(canvas, projectile));
    }

    /// <summary>
    /// 使用图鉴共享映射切换四帧敌人动画，受击时以红色调制，死亡时显示消散文字。
    /// </summary>
    private void DrawEnemy(Node2D canvas, EnemyComponent enemy, double animationTime)
    {
        if (!enemy.Alive)
        {
            DrawText(canvas, enemy.Position, "消散", new Color("c5c5bf"));
            return;
        }

        string sourceId = enemy.Definition.RequiredContentPack ?? BaseSourceId;
        if (!_visuals.TryGet(sourceId, InternalVisualCategory.Enemy,
                enemy.Definition.DisplayName, out InternalVisualDefinition visual) ||
            visual.Kind != InternalVisualKind.ActorStrip ||
            !_visuals.TryGetTexture(visual, out Texture2D texture))
        {
            DrawText(canvas, enemy.Position, enemy.Definition.DisplayName,
                new Color("c8e7ff"));
            LastFallbackEnemyCount++;
            return;
        }

        int frame = (int)(animationTime * 6.0 + visual.Variant) & 3;
        Vector2 textureSize = texture.GetSize();
        float frameWidth = textureSize.X / 4.0f;
        var source = new Rect2(frame * frameWidth, 0.0f, frameWidth, textureSize.Y);
        float drawSize = enemy.Definition.CollisionRadius >= 10.0f ? 36.0f : 24.0f;
        var destination = new Rect2(
            (enemy.Position - Vector2.One * drawSize * 0.5f).Round(),
            Vector2.One * drawSize);
        Color modulate = enemy.HurtTime > 0.0f
            ? new Color(1.0f, 0.42f, 0.42f)
            : Colors.White;
        canvas.DrawTextureRectRegion(texture, destination, source, modulate);
        LastMappedEnemyCount++;
    }

    /// <summary>
    /// 从天空璋道具图集选择 P、星和 F 图标，分别表达射速、移速和螺旋弹道构筑。
    /// </summary>
    private void DrawPickup(Node2D canvas, PickupComponent pickup)
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

    /// <summary>
    /// 用天空璋道具图集中的青色季节点表示经验；合并价值大于一时附加紧凑数字。
    /// </summary>
    private void DrawSpirit(Node2D canvas, SpiritComponent spirit)
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

    /// <summary>从红魔乡弹幕图集选择一枚 16 像素灵弹，并缩放到稳定的八像素显示尺寸。</summary>
    private void DrawProjectile(Node2D canvas, ProjectileComponent projectile)
    {
        var destination = new Rect2(
            (projectile.Position - Vector2.One * 4.0f).Round(), Vector2.One * 8.0f);
        if (_bulletAtlas is not null)
        {
            canvas.DrawTextureRectRegion(
                _bulletAtlas, destination, new Rect2(16.0f, 32.0f, 16.0f, 16.0f));
            LastProjectileIconCount++;
        }
        else
        {
            canvas.DrawRect(destination, new Color("f4df7d"));
        }
    }

    /// <summary>
    /// 按稳定内容键加载指定版式的共享纹理；缺失或版式错误时返回空值并由绘制层回退文字。
    /// </summary>
    private Texture2D? LoadTexture(
        string sourceId,
        InternalVisualCategory category,
        string name,
        InternalVisualKind expectedKind)
    {
        return _visuals.TryGet(sourceId, category, name, out InternalVisualDefinition definition) &&
            definition.Kind == expectedKind &&
            _visuals.TryGetTexture(definition, out Texture2D texture)
                ? texture
                : null;
    }

    /// <summary>在素材回退时绘制居中的短中文标识，不改变实体数据或碰撞尺寸。</summary>
    private void DrawText(
        Node2D canvas,
        Vector2 position,
        string text,
        Color color,
        int size = 10,
        float width = 56.0f)
    {
        canvas.DrawString(_font, position + new Vector2(-width * 0.5f, 4.0f),
            text, HorizontalAlignment.Center, width, size, color);
    }
}
