using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 使用与游戏世界一致的中文名称，实时绘制地区日常、结构活动和敌人移动。
/// </summary>
public partial class CompendiumPreview : Control
{
    private static readonly string[] DailyLabels = ["村民", "妖精", "巫女", "旅人"];
    private CompendiumEntry? _entry;
    private Texture2D? _ground;
    private Font? _font;
    private InternalOriginalPreviewRenderer? _internalOriginal;
    private double _animationTime;

    public bool AssetsReady => _font is not null;
    public bool InternalOriginalAssetsReady => _internalOriginal?.AssetsReady ?? false;
    public bool InternalOriginalActive { get; private set; }
    public double AnimationTime => _animationTime;
    public CompendiumCategory? CurrentCategory => _entry?.Category;

    /// <summary>
    /// 缓存 Godot 回退字体并启用处理，具体地表在选择条目时按需加载。
    /// </summary>
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        _font = ThemeDB.FallbackFont;
        _internalOriginal = new InternalOriginalPreviewRenderer();
        SetProcess(true);
    }

    /// <summary>
    /// 切换当前图鉴条目、加载代表地表并从动画起点开始新的展示循环。
    /// </summary>
    public void SetEntry(CompendiumEntry? entry)
    {
        _entry = entry;
        _animationTime = 0.0;
        InternalOriginalActive = false;
        _ground = entry is null
            ? null
            : GD.Load<Texture2D>(TileCatalog.GetResourcePath(entry.PreviewTile));
        QueueRedraw();
    }

    /// <summary>
    /// 在暂停菜单中仍推进轻量预览动画，但只在可见且已有条目时请求重绘。
    /// </summary>
    public override void _Process(double delta)
    {
        if (!IsVisibleInTree() || _entry is null)
        {
            return;
        }

        _animationTime += delta;
        QueueRedraw();
    }

    /// <summary>
    /// 绘制平铺地表和分类专属活动层，四周留出九宫格边框的安全内边距。
    /// </summary>
    public override void _Draw()
    {
        if (_entry is null || _ground is null || _font is null)
        {
            return;
        }

        var area = new Rect2(2.0f, 2.0f, Math.Max(0.0f, Size.X - 4.0f), Math.Max(0.0f, Size.Y - 4.0f));
        InternalOriginalActive = _internalOriginal?.TryDraw(
            this, _entry, area, _animationTime) ?? false;
        if (!InternalOriginalActive)
        {
            DrawGround(area);
        }

        DrawRect(area, new Color(0.02f, 0.04f, 0.025f, InternalOriginalActive ? 0.16f : 0.32f));
        switch (_entry.Category)
        {
            case CompendiumCategory.Biome:
                if (!InternalOriginalActive)
                {
                    DrawDailyScene(area, false);
                }
                break;
            case CompendiumCategory.Structure:
                if (!InternalOriginalActive)
                {
                    DrawDailyScene(area, true);
                }
                break;
            case CompendiumCategory.Enemy:
                if (InternalOriginalActive)
                {
                    DrawOriginalCaption(area);
                }
                else
                {
                    DrawEnemyScene(area);
                }
                break;
            case CompendiumCategory.Character:
                if (InternalOriginalActive)
                {
                    DrawOriginalCaption(area);
                }
                else
                {
                    DrawCharacterScene(area);
                }
                break;
            case CompendiumCategory.Build:
                CompendiumBuildPreviewRenderer.Draw(
                    this, _entry, area, _animationTime, _font);
                break;
            case CompendiumCategory.SpellCard:
                if (InternalOriginalActive)
                {
                    DrawOriginalCaption(area);
                }
                else
                {
                    DrawSpellCardScene(area);
                }
                break;
        }

        DrawAmbientParticles(area);
    }

    /// <summary>
    /// 在内部图集动画底部叠加当前中文条目名，保留项目以文字识别实体的既有视觉语言。
    /// </summary>
    private void DrawOriginalCaption(Rect2 area)
    {
        DrawEntityText(_entry!.Name, new Vector2(area.GetCenter().X, area.End.Y - 7.0f), 9,
            new Color(0.98f, 0.93f, 0.79f));
    }

    /// <summary>
    /// 以原始 16x16 尺寸平铺代表地表，确保最近邻像素不会产生非整数拉伸。
    /// </summary>
    private void DrawGround(Rect2 area)
    {
        for (float y = area.Position.Y; y < area.End.Y; y += 16.0f)
        {
            for (float x = area.Position.X; x < area.End.X; x += 16.0f)
            {
                DrawTextureRect(_ground!, new Rect2(x, y, 16.0f, 16.0f), false,
                    new Color(0.64f, 0.68f, 0.61f));
            }
        }
    }

    /// <summary>
    /// 绘制移动的日常中文名；结构条目额外绘制屋顶、门和交替闪烁的朱砂灯。
    /// </summary>
    private void DrawDailyScene(Rect2 area, bool includeStructure)
    {
        if (includeStructure)
        {
            DrawStructure(area);
        }

        float progress = (float)(_animationTime % 6.0 / 6.0);
        float x = Mathf.Lerp(area.Position.X - 24.0f, area.End.X + 8.0f, progress);
        float bob = MathF.Sin((float)_animationTime * 7.0f) * 1.0f;
        int actor = Math.Abs(_entry!.PreviewVariant) % DailyLabels.Length;
        DrawEntityText(DailyLabels[actor], new Vector2(x, area.End.Y - 13.0f + bob), 10,
            new Color(0.94f, 0.91f, 0.78f));
        if (includeStructure)
        {
            float reverseX = Mathf.Lerp(area.End.X + 8.0f, area.Position.X - 24.0f, progress);
            DrawEntityText(DailyLabels[(actor + 1) % DailyLabels.Length],
                new Vector2(reverseX, area.End.Y - 28.0f - bob), 9,
                new Color(0.71f, 0.82f, 0.68f));
        }
    }

    /// <summary>
    /// 让敌人中文名像游戏实体一样往返逼近阴阳目标，并以半透明文字形成速度尾迹。
    /// </summary>
    private void DrawEnemyScene(Rect2 area)
    {
        if (_entry!.Enemy is null)
        {
            return;
        }

        float cycle = (float)(_animationTime % 4.0 / 4.0);
        float ping = cycle < 0.5f ? cycle * 2.0f : 2.0f - cycle * 2.0f;
        float x = Mathf.Lerp(area.End.X - 42.0f, area.Position.X + 47.0f, ping);
        float y = area.GetCenter().Y - 13.0f + MathF.Sin((float)_animationTime * 8.0f) * 3.0f;
        DrawEntityText(_entry.Name, new Vector2(x + 8.0f, y), 11,
            new Color(0.92f, 0.88f, 0.74f, 0.24f));
        DrawEntityText(_entry.Name, new Vector2(x, y), 11,
            new Color(0.96f, 0.94f, 0.84f));
        Vector2 target = new(area.Position.X + 24.0f, area.GetCenter().Y);
        DrawCircle(target, 9.0f, new Color(0.08f, 0.07f, 0.05f, 0.92f));
        DrawCircle(target, 6.0f, new Color(0.79f, 0.72f, 0.56f, 0.9f));
        DrawCircle(target + new Vector2(0.0f, -3.0f), 2.5f, new Color(0.48f, 0.09f, 0.07f));
        DrawCircle(target + new Vector2(0.0f, 3.0f), 2.5f, new Color(0.04f, 0.08f, 0.05f));
    }

    /// <summary>
    /// 让目录角色名称缓慢起伏，严格保持当前游戏使用中文文字代替角色图标的表现。
    /// </summary>
    private void DrawCharacterScene(Rect2 area)
    {
        float bob = MathF.Sin((float)_animationTime * 3.5f) * 2.0f;
        DrawEntityText(_entry!.Name,
            area.GetCenter() + new Vector2(0.0f, bob), 12,
            new Color(0.97f, 0.9f, 0.72f));
    }

    /// <summary>
    /// 按真实效果类型播放文字符卡缩影：追踪灵玉向妖怪收束，封魔阵由灵梦中心扩散淡出。
    /// </summary>
    private void DrawSpellCardScene(Rect2 area)
    {
        if (_entry!.SpellCard is null)
        {
            return;
        }

        Vector2 center = area.GetCenter();
        if (_entry.SpellCard.EffectKind is SpellCardEffectKind.HomingVolley or
            SpellCardEffectKind.FocusedVolley)
        {
            Vector2 target = center + new Vector2(28.0f, 0.0f);
            DrawEntityText("妖", target, 12, new Color(0.92f, 0.78f, 0.72f));
            float close = (float)(_animationTime % 2.0 / 2.0);
            for (int index = 0; index < 6; index++)
            {
                float angle = (float)_animationTime * 2.4f + Mathf.Tau * index / 6.0f;
                Vector2 orbit = center + Vector2.FromAngle(angle) * 24.0f;
                Vector2 position = orbit.Lerp(target, close * close);
                Color color = Color.FromHsv(index / 6.0f, 0.52f, 1.0f, 0.95f);
                DrawEntityText("灵", position, 11, color);
            }

            return;
        }

        float pulse = (float)(_animationTime % 1.4 / 1.4);
        int fontSize = 10 + (int)(pulse * 7.0f);
        DrawEntityText("灵梦", center, 11, new Color(0.98f, 0.9f, 0.72f));
        DrawEntityText("封魔阵", center, fontSize,
            new Color(0.96f, 0.83f, 0.45f, 1.0f - pulse));
    }

    /// <summary>
    /// 按地区层级画出据点、殿堂或秘境轮廓，并让两盏灯以相反相位呼吸。
    /// </summary>
    private void DrawStructure(Rect2 area)
    {
        float centerX = area.GetCenter().X;
        float baseY = area.End.Y - 23.0f;
        Color ink = new(0.035f, 0.055f, 0.04f, 0.95f);
        Color trim = new(0.53f, 0.45f, 0.29f, 0.95f);
        float width = 58.0f + _entry!.PreviewVariant * 10.0f;
        DrawRect(new Rect2(centerX - width * 0.5f, baseY - 28.0f, width, 28.0f), ink);
        DrawLine(new Vector2(centerX - width * 0.62f, baseY - 28.0f),
            new Vector2(centerX, baseY - 43.0f), trim, 3.0f);
        DrawLine(new Vector2(centerX, baseY - 43.0f),
            new Vector2(centerX + width * 0.62f, baseY - 28.0f), trim, 3.0f);
        DrawRect(new Rect2(centerX - 7.0f, baseY - 17.0f, 14.0f, 17.0f), trim);
        float glow = 0.55f + MathF.Sin((float)_animationTime * 4.0f) * 0.25f;
        DrawCircle(new Vector2(centerX - width * 0.34f, baseY - 17.0f), 3.0f,
            new Color(0.86f, 0.23f, 0.14f, glow));
        DrawCircle(new Vector2(centerX + width * 0.34f, baseY - 17.0f), 3.0f,
            new Color(0.86f, 0.23f, 0.14f, 1.1f - glow));
    }

    /// <summary>
    /// 以给定位置为中心绘制中文实体名，并增加单像素暗影保证复杂地表上的可读性。
    /// </summary>
    private void DrawEntityText(string text, Vector2 center, int fontSize, Color color)
    {
        Vector2 size = _font!.GetStringSize(
            text, HorizontalAlignment.Left, -1.0f, fontSize);
        Vector2 baseline = center + new Vector2(-size.X * 0.5f, size.Y * 0.35f);
        DrawString(_font, baseline + Vector2.One, text,
            HorizontalAlignment.Left, -1.0f, fontSize, new Color(0.0f, 0.0f, 0.0f, color.A));
        DrawString(_font, baseline, text,
            HorizontalAlignment.Left, -1.0f, fontSize, color);
    }

    /// <summary>
    /// 绘制少量确定性漂浮粒子，使静态地表具备风、萤火或尘埃般的环境节奏。
    /// </summary>
    private void DrawAmbientParticles(Rect2 area)
    {
        for (int index = 0; index < 7; index++)
        {
            float speed = 5.0f + index * 1.7f;
            float x = area.Position.X + (index * 43.0f + (float)_animationTime * speed) % area.Size.X;
            float y = area.Position.Y + 8.0f + (index * 19 % Math.Max(12.0f, area.Size.Y - 18.0f));
            float alpha = 0.25f + index % 3 * 0.12f;
            DrawRect(new Rect2(Mathf.Floor(x), Mathf.Floor(y), 2.0f, 2.0f),
                new Color(0.79f, 0.72f, 0.46f, alpha));
        }
    }
}
