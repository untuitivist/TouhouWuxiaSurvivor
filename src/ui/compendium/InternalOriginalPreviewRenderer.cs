using Godot;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 按逐条映射绘制内部原作预览；资源被公开导出排除时把控制权交还文字预览。
/// </summary>
public sealed class InternalOriginalPreviewRenderer
{
    private readonly InternalPreviewCatalog _catalog = new();
    private readonly Dictionary<string, Texture2D?> _textures = new(StringComparer.Ordinal);

    public bool AssetsReady => _catalog.Count > 0;
    public int DefinitionCount => _catalog.Count;

    /// <summary>
    /// 检查条目是否具有内部映射，供覆盖测试与预览状态检查共同使用。
    /// </summary>
    public bool HasDefinition(CompendiumEntry entry) => _catalog.Contains(entry);

    /// <summary>
    /// 查找当前条目的映射和纹理，再分派到与素材版式匹配的动画渲染函数。
    /// </summary>
    public bool TryDraw(Control canvas, CompendiumEntry entry, Rect2 area, double animationTime)
    {
        if (!_catalog.TryGet(entry, out InternalPreviewDefinition definition) ||
            !TryGetTexture(definition.AssetPath, out Texture2D texture))
        {
            return false;
        }

        switch (definition.Kind)
        {
            case InternalPreviewKind.Scene:
                DrawScene(canvas, texture, area, animationTime, definition.Variant);
                break;
            case InternalPreviewKind.ActorStrip:
                DrawActor(canvas, texture, area, animationTime, definition.Variant);
                break;
            case InternalPreviewKind.Portrait:
                DrawPortrait(canvas, texture, area, animationTime, definition.Variant);
                break;
            case InternalPreviewKind.BulletAtlas:
                DrawSpellBullets(canvas, texture, area, animationTime, definition.Variant);
                break;
            default:
                return false;
        }

        return true;
    }

    /// <summary>
    /// 以轻微往返裁切展示 128x80 场景，使静态地区也保留可感知但不喧宾夺主的日常运动。
    /// </summary>
    private static void DrawScene(
        Control canvas, Texture2D texture, Rect2 area, double time, int variant)
    {
        Vector2 size = texture.GetSize();
        float inset = Math.Min(2.0f, Math.Min(size.X, size.Y) * 0.02f);
        float phase = (MathF.Sin((float)time * 0.45f + variant) + 1.0f) * 0.5f;
        var source = new Rect2(
            Mathf.Floor(phase * inset * 2.0f), inset,
            Math.Max(1.0f, size.X - inset * 2.0f), Math.Max(1.0f, size.Y - inset * 2.0f));
        canvas.DrawTextureRectRegion(texture, area, source);
        DrawSceneLife(canvas, area, time, variant);
    }

    /// <summary>
    /// 在场景安全边界内绘制三像素行人和闪烁灯火，表达日常活动而不让文字标签被边框裁断。
    /// </summary>
    private static void DrawSceneLife(Control canvas, Rect2 area, double time, int variant)
    {
        float travel = (float)((time * 0.12 + variant * 0.17) % 1.0);
        float x = Mathf.Lerp(area.Position.X + 8.0f, area.End.X - 8.0f, travel);
        float y = area.End.Y - 13.0f + Mathf.Round(MathF.Sin((float)time * 6.0f));
        Color robe = variant % 2 == 0
            ? new Color(0.88f, 0.35f, 0.23f)
            : new Color(0.72f, 0.82f, 0.58f);
        canvas.DrawRect(new Rect2(Mathf.Floor(x), y, 3.0f, 5.0f), robe);
        canvas.DrawRect(new Rect2(Mathf.Floor(x) + 1.0f, y - 2.0f, 1.0f, 2.0f),
            new Color(0.95f, 0.86f, 0.67f));
        float glow = 0.45f + 0.35f * MathF.Sin((float)time * 3.0f + variant);
        canvas.DrawCircle(new Vector2(area.End.X - 10.0f, area.Position.Y + 10.0f), 2.0f,
            new Color(1.0f, 0.63f, 0.22f, glow));
    }

    /// <summary>
    /// 从 192x48 横向动画条切换四帧，并沿往返路线移动，模拟局内敌人的持续逼近感。
    /// </summary>
    private static void DrawActor(
        Control canvas, Texture2D texture, Rect2 area, double time, int variant)
    {
        int frame = (int)(time * 6.0 + variant) % 4;
        var source = new Rect2(frame * 48.0f, 0.0f, 48.0f, 48.0f);
        float phase = (float)(time % 4.0 / 4.0);
        float ping = phase < 0.5f ? phase * 2.0f : 2.0f - phase * 2.0f;
        float x = Mathf.Lerp(area.Position.X + 36.0f, area.End.X - 36.0f, ping);
        float y = area.GetCenter().Y - 5.0f + MathF.Sin((float)time * 7.0f + variant) * 3.0f;
        var destination = new Rect2(
            new Vector2(Mathf.Floor(x - 24.0f), Mathf.Floor(y - 24.0f)),
            new Vector2(48.0f, 48.0f));
        canvas.DrawTextureRectRegion(texture, destination, source);
    }

    /// <summary>
    /// 居中绘制 80x80 立绘并作两像素呼吸起伏，中文名仍由上层按项目既有规则叠加。
    /// </summary>
    private static void DrawPortrait(
        Control canvas, Texture2D texture, Rect2 area, double time, int variant)
    {
        float bob = Mathf.Round(MathF.Sin((float)time * 2.2f + variant) * 2.0f);
        var destination = new Rect2(
            area.GetCenter() - new Vector2(40.0f, 44.0f - bob), new Vector2(80.0f, 80.0f));
        canvas.DrawTextureRect(texture, destination, false);
    }

    /// <summary>
    /// 从原作弹幕图集选择颜色行，让不同符卡变体呈现追踪收束、环形封魔等运动。
    /// </summary>
    private static void DrawSpellBullets(
        Control canvas, Texture2D texture, Rect2 area, double time, int variant)
    {
        Vector2 center = area.GetCenter() - new Vector2(0.0f, 5.0f);
        int count = variant % 2 == 0 ? 8 : 6;
        float close = variant % 2 == 0 ? (float)(time % 2.0 / 2.0) : 0.2f;
        for (int index = 0; index < count; index++)
        {
            int column = 1 + (variant * 5 + index * 2) % 14;
            var source = new Rect2(column * 16.0f, 32.0f, 16.0f, 16.0f);
            float angle = (float)time * (variant % 2 == 0 ? 2.4f : -1.7f) +
                Mathf.Tau * index / count;
            Vector2 orbit = center + Vector2.FromAngle(angle) * (22.0f + variant * 3.0f);
            Vector2 position = orbit.Lerp(center, close * close * 0.7f);
            canvas.DrawTextureRectRegion(texture,
                new Rect2(position.Round() - new Vector2(8.0f, 8.0f), new Vector2(16.0f, 16.0f)),
                source);
        }
    }

    /// <summary>
    /// 首次使用时加载纹理并缓存空结果，避免公开包每帧重复查询已被排除的内部路径。
    /// </summary>
    private bool TryGetTexture(string path, out Texture2D texture)
    {
        if (!_textures.TryGetValue(path, out Texture2D? cached))
        {
            cached = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
            _textures[path] = cached;
        }

        texture = cached!;
        return cached is not null;
    }
}
