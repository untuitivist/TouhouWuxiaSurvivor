using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private bool touchHudVisible;
    public Rect2 MinimapBounds => HudLayout.Minimap(touchHudVisible);

    public void SetTouchHudVisible(bool visible)
    {
        if (touchHudVisible == visible) return;
        touchHudVisible = visible;
        hudLayer.QueueRedraw();
    }

    private void DrawHud()
    {
        if (Run == null) return;
        surface.DrawStyleBox(PixelSkin.Frame("dark"), new(0, 0, 1280, 94));
        surface.DrawStyleBox(PixelSkin.Frame("dark"), new(0, 650, 1280, 70));
        Text(Run.Hero == HeroKind.Reimu ? "博丽灵梦" : "雾雨魔理沙", new(28, 32), 22, Palette.Paper, TitleFont);
        Text($"修习 {Run.Level}", new(178, 31), 16, Palette.Gold);
        Bar(new(28, 45, 222, 9), Run.Health / Run.MaxHealth, Palette.Red);
        Text($"{MathF.Ceiling(Run.Health)} / {Run.MaxHealth}", new(28, 76), 14, Palette.Muted);
        Text($"退治  {Run.Kills}", new(165, 76), 14, Palette.Muted);
        CenterText(FormatTime(Run.Time), new(640, 37), 29, Palette.Paper);
        CenterText(Run.BossSpawned ? "终章 · 雾中来客" : Run.Time < 60 ? "一之卷 · 夜行" : Run.Time < 150 ? "二之卷 · 妖潮" : "三之卷 · 破阵", new(640, 64), 15, Palette.Gold);
        Bar(new(430, 76, 420, 3), Math.Min(1, Run.Time / RunState.BossArrival), Palette.Gold);
        Text("符卡蓄势", new(989, 31), 17, Palette.Jade);
        Text($"{(int)Run.SpellCharge} / 100", new(1162, 31), 15, Palette.Paper);
        Bar(new(989, 45, 263, 8), Run.SpellCharge / 100, Palette.Jade);
        Text(Run.Build.SignatureUnlocked ? "擦弹蓄势 · 满槽自动清弹" : "梦想封印 · 尚未解锁", new(989, 77), 14, Palette.Muted);
        Text("历 练", new(28, 679), 13, Palette.Jade);
        Bar(new(84, 668, 242, 7), (float)Run.Experience / Run.NextLevelExperience, Palette.Jade);
        Text($"{Run.Experience} / {Run.NextLevelExperience}", new(84, 699), 13, Palette.Muted);
        var horizontal = 370;
        foreach (var art in ArtCatalog.Abilities(Run.Hero))
        {
            var rank = Run.Ranks[(int)art.Id];
            var color = new Color(art.Color);
            surface.DrawStyleBox(PixelSkin.Frame("dark"), new(horizontal - 2, 661, 43, 43));
            CenterText(art.Symbol, new(horizontal + 20, 689), 22, rank > 0 ? color : Palette.Muted, TitleFont);
            for (var dot = 0; dot < 5; dot++) surface.DrawRect(new(horizontal + dot * 8, 708, 5, 3), Palette.Alpha(color, dot < rank ? 1 : 0.12f));
            horizontal += 58;
        }
        FittedText($"{GameControls.Hint(GameControls.Dash)} 闪身 · {(Run.DashCooldown <= 0 ? "就绪" : $"{Run.DashCooldown:0.0}s")}", new(654, 683), 173, 16, Run.DashCooldown <= 0 ? Palette.Paper : Palette.Muted);
        Bar(new(654, 697, 173, 3), 1 - Run.DashCooldown / Run.DashInterval, Palette.Gold);
        FittedText($"{GameControls.Hint(GameControls.Focus)} 慢移   Esc 暂停", new(1032, 690), 218, 14, Palette.Muted);
        DrawMinimap();
        DrawObjective();
        var boss = Run.Boss;
        if (boss != null)
        {
            surface.DrawRect(new(395, 108, 490, 54), Palette.Alpha(Palette.Deep, 0.85f));
            CenterText("结界残影  /  异变的回声", new(640, 129), 15, Palette.Violet);
            Bar(new(413, 142, 454, 5), boss.Health / boss.MaxHealth, Palette.Violet);
        }
        if (Run.Time < 10)
        {
            surface.DrawRect(new(383, 551, 514, 65), Palette.Alpha(Palette.Deep, 0.8f));
            CenterText(Run.Hero == HeroKind.Reimu ? "基础御札起步，修习解锁阵与玉。" : "星弹开路，魔炮锁向。", new(640, 578), 20, Palette.Paper, TitleFont);
            FittedText($"{GameControls.Hint(GameControls.Up)} / {GameControls.Hint(GameControls.Left)} / {GameControls.Hint(GameControls.Down)} / {GameControls.Hint(GameControls.Right)} 移动 · {GameControls.Hint(GameControls.Inspect)} 构筑 · Esc 暂停", new(397, 603), 486, 15, Palette.Muted);
        }
        if (Run.SpellFlash > 0) CenterText(ArtCatalog.SignatureName(Run.Hero), new(640, 213), 30, Palette.Alpha(Palette.Gold, Run.SpellFlash / 0.65f), TitleFont);
        if (Run.Health < Run.MaxHealth * 0.25f)
        {
            surface.DrawRect(new(0, 93, 7, 557), Palette.Alpha(Palette.Red, 0.5f));
            surface.DrawRect(new(1273, 93, 7, 557), Palette.Alpha(Palette.Red, 0.5f));
        }
    }

    private void DrawObjective()
    {
        if (Run == null) return;
        surface.DrawStyleBox(PixelSkin.Frame("dark"), new(24, 108, 233, 78));
        Text($"净化古印  {Run.PurifiedSeals} / 3", new(39, 136), 18, Palette.Gold);
        Text(Run.BossSpawned ? "击破雾中来客，平息异变" : $"距终章  {FormatTime(RunState.BossArrival - Run.Time)}", new(39, 164), 14, Palette.Muted);
        var nearest = Run.Seals.Where(seal => !seal.Complete).OrderBy(seal => System.Numerics.Vector2.DistanceSquared(seal.Position, Run.PlayerPosition)).FirstOrDefault();
        if (nearest == null) return;
        var direction = Palette.Vector(nearest.Position - Run.PlayerPosition);
        if (direction.Length() < 140) return;
        var arrow = new Vector2(640, 370) + Palette.Vector(Run.PlayerPosition) - camera + direction.Normalized() * 160;
        arrow = arrow.Clamp(new Vector2(275, 193), new Vector2(1070, 580));
        var heading = direction.Normalized();
        surface.DrawColoredPolygon([arrow + heading * 11, arrow - heading * 5 + heading.Orthogonal() * 6, arrow - heading * 5 - heading.Orthogonal() * 6], Palette.Gold);
        CenterText(nearest.Name, arrow + new Vector2(0, 27), 14, Palette.Gold);
    }

    private void DrawMinimap()
    {
        if (Run == null) return;
        var bounds = MinimapBounds;
        surface.DrawStyleBox(PixelSkin.Frame("dark"), bounds);
        Vector2 Map(System.Numerics.Vector2 position) => bounds.GetCenter() + new Vector2(position.X / RunState.ArenaHalfWidth * 62, position.Y / RunState.ArenaHalfHeight * 46);
        foreach (var seal in Run.Seals) Diamond(Map(seal.Position), 3, seal.Complete ? Palette.Jade : Palette.Gold);
        if (Run.Boss != null) surface.DrawCircle(Map(Run.Boss.Position), 3, Palette.Red);
        surface.DrawCircle(Map(Run.PlayerPosition), 3, Palette.Paper);
        Text("博 丽 夜 境", bounds.Position + new Vector2(31, 131), 12, Palette.Muted);
    }

    private void Bar(Rect2 rectangle, float fraction, Color color)
    {
        surface.DrawRect(rectangle.Grow(2), new Color("1d2425"));
        surface.DrawRect(rectangle, new Color("655340"));
        var width = MathF.Floor(rectangle.Size.X * Math.Clamp(fraction, 0, 1) / 2) * 2;
        surface.DrawRect(new(rectangle.Position, new(width, rectangle.Size.Y)), color);
        if (rectangle.Size.Y >= 7 && width > 0) surface.DrawRect(new(rectangle.Position, new(width, 2)), color.Lightened(0.2f));
    }

    public static string FormatTime(float seconds) => $"{(int)Math.Max(0, seconds) / 60:00}:{(int)Math.Max(0, seconds) % 60:00}";
}
