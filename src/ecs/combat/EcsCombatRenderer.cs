using Godot;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Ecs.Combat.Bosses;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 将 ECS 战斗数据批量绘制为原作纹理；素材不可用时保留中文文字回退。
/// </summary>
public sealed class EcsCombatRenderer
{
    private const string BaseSourceId = "base";
    private readonly InternalVisualCatalog _visuals = new();
    private readonly EcsCombatItemRenderer _items = new();
    private Font? _font;

    public int LastMappedEnemyCount { get; private set; }
    public int LastFallbackEnemyCount { get; private set; }
    public int LastMappedBossCount { get; private set; }
    public int LastFallbackBossCount { get; private set; }
    public int LastPickupIconCount => _items.LastPickupIconCount;
    public int LastSpiritIconCount => _items.LastSpiritIconCount;
    public int LastProjectileIconCount => _items.LastProjectileIconCount;
    public int LastEnemyProjectileIconCount => _items.LastEnemyProjectileIconCount;
    public int LastVisibleEnemyCount { get; private set; }
    public int LastVisibleProjectileCount { get; private set; }
    public int LastCulledEntityCount { get; private set; }

    /// <summary>
    /// 从共享内部清单加载东方道具、红魔乡弹幕图集和默认字体，不直接持有资源路径。
    /// </summary>
    public void Configure()
    {
        _items.Configure(_visuals);
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
        double animationTime,
        float interpolationFraction = 1.0f)
    {
        LastMappedEnemyCount = LastFallbackEnemyCount = 0;
        LastMappedBossCount = LastFallbackBossCount = 0;
        _items.ResetCounts();
        LastVisibleEnemyCount = LastVisibleProjectileCount = LastCulledEntityCount = 0;
        CombatVisibilityBounds visibility = CombatVisibilityBounds.FromCanvas(canvas);
        foreach (PickupComponent pickup in pickups)
        {
            if (visibility.Intersects(pickup.Position, 12.0f)) _items.DrawPickup(canvas, pickup);
            else LastCulledEntityCount++;
        }

        enemies.ForEach(enemy => DrawEnemyIfVisible(
            canvas, enemy, animationTime, interpolationFraction, visibility));
        foreach (SpiritComponent spirit in spirits)
        {
            Vector2 position = spirit.GetRenderPosition(interpolationFraction);
            if (visibility.Intersects(position, 12.0f))
                _items.DrawSpirit(canvas, spirit, position);
            else LastCulledEntityCount++;
        }

        projectiles.ForEach(projectile =>
        {
            Vector2 position = projectile.GetRenderPosition(interpolationFraction);
            if (visibility.Intersects(position, projectile.Radius))
            {
                _items.DrawProjectile(canvas, projectile, position);
                LastVisibleProjectileCount++;
            }
            else LastCulledEntityCount++;
        });
    }

    /// <summary>
    /// 先统计敌人素材覆盖，再只提交可视敌人；死亡文字和 Boss 血条共用扩大后的实体半径。
    /// </summary>
    private void DrawEnemyIfVisible(
        Node2D canvas,
        EnemyComponent enemy,
        double animationTime,
        float interpolationFraction,
        CombatVisibilityBounds visibility)
    {
        CountEnemyVisual(enemy);
        Vector2 position = enemy.GetRenderPosition(interpolationFraction);
        float radius = enemy.Definition.IsBoss
            ? enemy.Definition.CollisionRadius + 24.0f
            : enemy.Definition.CollisionRadius + 12.0f;
        if (!visibility.Intersects(position, radius))
        {
            LastCulledEntityCount++;
            return;
        }

        LastVisibleEnemyCount++;
        DrawEnemy(canvas, enemy, position, animationTime);
    }

    /// <summary>
    /// 统计目录定义是否具备运行素材，不把镜头位置混入图鉴完整性诊断，也不提交任何绘制指令。
    /// </summary>
    private void CountEnemyVisual(EnemyComponent enemy)
    {
        if (!enemy.Alive)
        {
            return;
        }

        if (enemy.Definition.IsBoss)
        {
            if (TryResolveBossVisual(enemy.Definition, out _)) LastMappedBossCount++;
            else LastFallbackBossCount++;
            return;
        }

        string sourceId = enemy.Definition.RequiredContentPack ?? BaseSourceId;
        bool mapped = _visuals.TryGet(sourceId, InternalVisualCategory.Enemy,
                enemy.Definition.DisplayName, out InternalVisualDefinition visual) &&
            visual.Kind == InternalVisualKind.ActorStrip &&
            _visuals.TryGetTexture(visual, out _);
        if (mapped) LastMappedEnemyCount++;
        else LastFallbackEnemyCount++;
    }

    /// <summary>
    /// 使用图鉴共享映射切换四帧敌人动画，受击时以红色调制，死亡时显示消散文字。
    /// </summary>
    private void DrawEnemy(
        Node2D canvas,
        EnemyComponent enemy,
        Vector2 position,
        double animationTime)
    {
        if (!enemy.Alive)
        {
            DrawText(canvas, position, "消散", new Color("c5c5bf"));
            return;
        }

        if (enemy.Definition.IsBoss)
        {
            DrawBoss(canvas, enemy, position, animationTime);
            return;
        }

        string sourceId = enemy.Definition.RequiredContentPack ?? BaseSourceId;
        if (!_visuals.TryGet(sourceId, InternalVisualCategory.Enemy,
                enemy.Definition.DisplayName, out InternalVisualDefinition visual) ||
            visual.Kind != InternalVisualKind.ActorStrip ||
            !_visuals.TryGetTexture(visual, out Texture2D texture))
        {
            DrawText(canvas, position, enemy.Definition.DisplayName,
                new Color("c8e7ff"));
            return;
        }

        int frame = (int)(animationTime * 6.0 + visual.Variant) & 3;
        Vector2 textureSize = texture.GetSize();
        float frameWidth = textureSize.X / 4.0f;
        var source = new Rect2(frame * frameWidth, 0.0f, frameWidth, textureSize.Y);
        float drawSize = enemy.Definition.CollisionRadius >= 10.0f ? 36.0f : 24.0f;
        var destination = new Rect2(
            (position - Vector2.One * drawSize * 0.5f).Round(),
            Vector2.One * drawSize);
        Color modulate = enemy.HurtTime > 0.0f
            ? new Color(1.0f, 0.42f, 0.42f)
            : Colors.White;
        canvas.DrawTextureRectRegion(texture, destination, source, modulate);
    }

    /// <summary>
    /// 使用角色分类映射绘制 Boss；支持四帧 ActorStrip 与完整 Portrait，两者缺失时回退中文名并始终绘制紧凑血条。
    /// </summary>
    private void DrawBoss(
        Node2D canvas,
        EnemyComponent enemy,
        Vector2 position,
        double animationTime)
    {
        if (!TryResolveBossVisual(enemy.Definition, out InternalVisualDefinition visual) ||
            !_visuals.TryGetTexture(visual, out Texture2D texture))
        {
            DrawText(canvas, position, enemy.Definition.DisplayName,
                new Color("ffd6e6"), 11, 72.0f);
            DrawBossHealth(canvas, enemy, position);
            BossSpellPresentationRenderer.Draw(canvas, _font, enemy, position);
            return;
        }

        const float drawSize = 48.0f;
        var destination = new Rect2(
            (position - Vector2.One * drawSize * 0.5f).Round(),
            Vector2.One * drawSize);
        Color modulate = enemy.HurtTime > 0.0f
            ? new Color(1.0f, 0.45f, 0.45f)
            : Colors.White;
        if (visual.Kind == InternalVisualKind.ActorStrip)
        {
            int frame = (int)(animationTime * 6.0 + visual.Variant) & 3;
            Vector2 textureSize = texture.GetSize();
            float frameWidth = textureSize.X / 4.0f;
            canvas.DrawTextureRectRegion(texture, destination,
                new Rect2(frame * frameWidth, 0.0f, frameWidth, textureSize.Y), modulate);
        }
        else
        {
            canvas.DrawTextureRect(texture, GetAspectFitRect(texture, position, drawSize),
                false, modulate);
        }

        DrawBossHealth(canvas, enemy, position);
        BossSpellPresentationRenderer.Draw(canvas, _font, enemy, position);
    }

    /// <summary>查询角色 Boss 的共享视觉定义，只接受适合运行时绘制的立绘或四帧角色条。</summary>
    public bool TryResolveBossVisual(
        TouhouWuxiaSurvivor.Actors.Enemies.EnemyDefinition definition,
        out InternalVisualDefinition visual)
    {
        visual = null!;
        string sourceId = definition.RequiredContentPack ?? BaseSourceId;
        return definition.IsBoss &&
            _visuals.TryGet(sourceId, InternalVisualCategory.Character,
                definition.DisplayName, out visual) &&
            visual.Kind is InternalVisualKind.ActorStrip or InternalVisualKind.Portrait;
    }

    /// <summary>在 Boss 头顶绘制固定宽度生命条，血量变化不会挤占 HUD 或改变实体版式。</summary>
    private static void DrawBossHealth(
        Node2D canvas,
        EnemyComponent enemy,
        Vector2 position)
    {
        const float width = 48.0f;
        const float height = 4.0f;
        float ratio = Mathf.Clamp(enemy.Health /
            (float)Math.Max(1, enemy.Definition.MaxHealth), 0.0f, 1.0f);
        Vector2 origin = (position + new Vector2(-width * 0.5f,
            -enemy.Definition.CollisionRadius - 12.0f)).Round();
        canvas.DrawRect(new Rect2(origin, new Vector2(width, height)),
            new Color("151719"));
        canvas.DrawRect(new Rect2(origin + Vector2.One,
            new Vector2((width - 2.0f) * ratio, height - 2.0f)),
            new Color("d84a57"));
    }

    /// <summary>把完整立绘按原始宽高比缩入固定方形范围，避免裁掉半身或把角色强行拉伸。</summary>
    private static Rect2 GetAspectFitRect(
        Texture2D texture,
        Vector2 center,
        float maximumSize)
    {
        Vector2 sourceSize = texture.GetSize();
        float scale = maximumSize / Math.Max(1.0f, Math.Max(sourceSize.X, sourceSize.Y));
        Vector2 size = (sourceSize * scale).Round();
        return new Rect2((center - size * 0.5f).Round(), size);
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
