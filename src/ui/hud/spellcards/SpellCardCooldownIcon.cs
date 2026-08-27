using System.Text;
using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

namespace TouhouWuxiaSurvivor.Ui.Hud.SpellCards;

/// <summary>
/// 绘制单张奥义的文字印章、槽位色与向上收缩冷却遮罩，不保存任何独立计时状态。
/// </summary>
public partial class SpellCardCooldownIcon : Control
{
    private static readonly Color OffensiveFill = new("4b1918");
    private static readonly Color SupportFill = new("163a2b");
    private static readonly Color OffensiveBorder = new("c56351");
    private static readonly Color SupportBorder = new("67a77b");
    private static readonly Color ReadyBorder = new("e5c66b");
    private static readonly Color TextColor = new("f1ead4");
    private static readonly Color CooldownMask = new(0.015f, 0.02f, 0.017f, 0.76f);
    private SpellCardTimerSnapshot? _timer;
    private Font? _font;

    public string CardId => _timer?.Card.Id ?? string.Empty;
    public float CooldownRatio => ResolveCooldownRatio(_timer);
    public bool IsWaitingForCondition => _timer?.IsWaitingForCondition == true;

    /// <summary>配置固定图标尺寸、最近邻画布语义和可透传鼠标的完整名称提示。</summary>
    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(28.0f, 28.0f);
        MouseFilter = MouseFilterEnum.Pass;
        _font = ThemeDB.FallbackFont;
    }

    /// <summary>接收协调器生成的单帧快照，并同步提示文字与下一绘制帧。</summary>
    public void SetTimer(SpellCardTimerSnapshot timer)
    {
        _timer = timer ?? throw new ArgumentNullException(nameof(timer));
        TooltipText = BuildTooltip(timer);
        QueueRedraw();
    }

    /// <summary>
    /// 返回顶部固定、下边缘向上移动的遮罩矩形；剩余比例归零时矩形高度也归零。
    /// </summary>
    public Rect2 GetCooldownMaskRect()
    {
        float height = MathF.Ceiling(Math.Max(0.0f, Size.Y) * CooldownRatio);
        return new Rect2(Vector2.Zero, new Vector2(Math.Max(0.0f, Size.X), height));
    }

    /// <summary>依次绘制削角印章、文字、受内框裁切的冷却遮罩、待机标记与槽位包边。</summary>
    public override void _Draw()
    {
        if (_timer is null || _font is null)
        {
            return;
        }

        Rect2 bounds = new(Vector2.Zero, Size);
        bool support = SpellCardSlotPolicy.Classify(_timer.Card) == SpellCardSlotKind.Support;
        Color fill = support ? SupportFill : OffensiveFill;
        Color border = IsWaitingForCondition
            ? ReadyBorder
            : support ? SupportBorder : OffensiveBorder;
        DrawSealFrame(bounds, fill, border);
        DrawGlyph(_timer.Card.ShortName, bounds);
        Rect2 mask = GetCooldownMaskRect().Intersection(bounds.Grow(-3.0f));
        if (mask.Size.Y > 0.0f)
        {
            DrawRect(mask, CooldownMask);
        }

        if (IsWaitingForCondition)
        {
            DrawRect(new Rect2(3.0f, Size.Y - 3.0f, Size.X - 6.0f, 2.0f), ReadyBorder);
        }
    }

    /// <summary>
    /// 绘制偏移阴影、硬削角底、内沿高光与四枚榫钉，形成可辨识的微型文字印章。
    /// </summary>
    private void DrawSealFrame(Rect2 bounds, Color fill, Color border)
    {
        DrawRect(new Rect2(bounds.Position + new Vector2(2, 2), bounds.Size),
            new Color(0.01f, 0.015f, 0.01f, 0.82f));
        DrawRect(new Rect2(bounds.Position + new Vector2(2, 0),
            bounds.Size - new Vector2(4, 0)), border);
        DrawRect(new Rect2(bounds.Position + new Vector2(0, 2),
            bounds.Size - new Vector2(0, 4)), border);
        DrawRect(bounds.Grow(-2.0f), fill);
        DrawLine(new Vector2(4, 3), new Vector2(Size.X - 4, 3),
            border.Lightened(0.22f), 1.0f);
        DrawLine(new Vector2(4, Size.Y - 3), new Vector2(Size.X - 4, Size.Y - 3),
            border.Darkened(0.28f), 1.0f);
        DrawRect(new Rect2(2, 2, 2, 2), ReadyBorder.Darkened(0.18f));
        DrawRect(new Rect2(Size.X - 4, Size.Y - 4, 2, 2), ReadyBorder.Darkened(0.18f));
    }

    /// <summary>把中英文短名压缩为两字或两个英文首字母，并居中绘制在印章中。</summary>
    private void DrawGlyph(string shortName, Rect2 bounds)
    {
        const int fontSize = 10;
        string glyph = BuildGlyph(shortName);
        Vector2 textSize = _font!.GetStringSize(glyph, HorizontalAlignment.Left, -1.0f, fontSize);
        Vector2 baseline = new(
            bounds.Position.X + (bounds.Size.X - textSize.X) * 0.5f,
            bounds.Position.Y + (bounds.Size.Y - textSize.Y) * 0.5f + _font.GetAscent(fontSize));
        DrawString(_font, baseline, glyph, HorizontalAlignment.Left, -1.0f, fontSize, TextColor);
    }

    /// <summary>优先用英文词首字母，否则保留短名中的前两个字母或数字。</summary>
    private static string BuildGlyph(string value)
    {
        string[] words = value.Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1 && words.All(word => word.All(character => character <= 127)))
        {
            return string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
        }

        return string.Concat(value.EnumerateRunes()
            .Where(Rune.IsLetterOrDigit)
            .Take(2)
            .Select(rune => rune.ToString()));
    }

    /// <summary>以权威周期计算剩余比例，保护零周期、超期和异常浮点输入。</summary>
    private static float ResolveCooldownRatio(SpellCardTimerSnapshot? timer)
    {
        if (timer is null || !float.IsFinite(timer.IntervalSeconds) ||
            timer.IntervalSeconds <= 0.0f || !float.IsFinite(timer.RemainingSeconds))
        {
            return 0.0f;
        }

        return Math.Clamp(timer.RemainingSeconds / timer.IntervalSeconds, 0.0f, 1.0f);
    }

    /// <summary>在悬停提示中区分冷却、已就绪待条件和即将施放三种运行状态。</summary>
    private static string BuildTooltip(SpellCardTimerSnapshot timer)
    {
        string state = timer.IsWaitingForCondition
            ? "周天已就绪，等待战况条件"
            : timer.IsReady
                ? "奥义已就绪"
                : $"剩余 {Math.Max(0.0f, timer.RemainingSeconds):0.0} 秒";
        return $"{timer.Card.FullName}\n{state}";
    }
}
