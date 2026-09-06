using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private void DrawHud()
    {
        if (Run == null) return;
        DrawRect(new(0, 0, 1280, 92), Palette.Alpha(Palette.Deep, 0.93f));
        DrawLine(new(24, 91), new(1256, 91), Palette.Alpha(Palette.Gold, 0.3f), 1);
        DrawRect(new(0, 650, 1280, 70), Palette.Alpha(Palette.Deep, 0.94f));
        DrawLine(new(24, 650), new(1256, 650), Palette.Alpha(Palette.Gold, 0.3f), 1);
        Text(Run.Hero == HeroKind.Reimu ? "博丽灵梦" : "雾雨魔理沙", new(28, 32), 22, Palette.Paper, TitleFont);
        Text($"第 {Run.Level} 境", new(178, 31), 16, Palette.Gold);
        Bar(new(28, 45, 222, 9), Run.Health / Run.MaxHealth, Palette.Red);
        Text($"{MathF.Ceiling(Run.Health)} / {Run.MaxHealth}", new(28, 76), 14, Palette.Muted);
        Text($"退治  {Run.Kills}", new(165, 76), 14, Palette.Muted);
        CenterText(FormatTime(Run.Time), new(640, 37), 29, Palette.Paper);
        CenterText(Run.BossSpawned ? "终章 · 雾中来客" : Run.Time < 60 ? "一之卷 · 夜行" : Run.Time < 150 ? "二之卷 · 妖潮" : "三之卷 · 破阵", new(640, 64), 15, Palette.Gold);
        Bar(new(430, 76, 420, 3), Math.Min(1, Run.Time / RunState.BossArrival), Palette.Gold);
        Text("剑意", new(989, 31), 17, Palette.Jade);
        Text($"{(int)Run.Qi} / 100", new(1162, 31), 15, Palette.Paper);
        Bar(new(989, 45, 263, 8), Run.Qi / 100, Palette.Jade);
        Text("擦弹蓄势 · 满槽自动清弹", new(989, 77), 14, Palette.Muted);
        Text("修 为", new(28, 679), 13, Palette.Jade);
        Bar(new(84, 668, 242, 7), (float)Run.Experience / Run.NextLevelExperience, Palette.Jade);
        Text($"{Run.Experience} / {Run.NextLevelExperience}", new(84, 699), 13, Palette.Muted);
        var horizontal = 370;
        for (var index = 0; index < 4; index++)
        {
            var rank = Run.Ranks[index];
            var color = new Color(ArtCatalog.All[index].Color);
            DrawRect(new(horizontal, 663, 39, 39), Palette.Alpha(color, rank > 0 ? 0.12f : 0.025f));
            DrawRect(new(horizontal, 663, 39, 39), Palette.Alpha(color, rank > 0 ? 0.7f : 0.13f), false, 1);
            CenterText(new[] { "剑", "阵", "符", "雷" }[index], new(horizontal + 20, 689), 22, rank > 0 ? color : Palette.Muted, TitleFont);
            for (var dot = 0; dot < 5; dot++) DrawRect(new(horizontal + dot * 8, 708, 5, 3), Palette.Alpha(color, dot < rank ? 1 : 0.12f));
            horizontal += 58;
        }
        Text(Run.DashCooldown <= 0 ? "SPACE  闪身 · 就绪" : $"SPACE  闪身 · {Run.DashCooldown:0.0}s", new(654, 683), 16, Run.DashCooldown <= 0 ? Palette.Paper : Palette.Muted);
        Bar(new(654, 697, 173, 3), 1 - Run.DashCooldown / Run.DashInterval, Palette.Gold);
        Text("SHIFT 慢移   ESC 暂停", new(1032, 690), 14, Palette.Muted);
        DrawMinimap();
        DrawObjective();
        var boss = Run.Boss;
        if (boss != null)
        {
            DrawRect(new(395, 108, 490, 54), Palette.Alpha(Palette.Deep, 0.85f));
            CenterText("雾中魔理沙  /  借一场弹幕，试你的剑", new(640, 129), 15, Palette.Violet);
            Bar(new(413, 142, 454, 5), boss.Health / boss.MaxHealth, Palette.Violet);
        }
        if (Run.Time < 10)
        {
            DrawRect(new(383, 551, 514, 65), Palette.Alpha(Palette.Deep, 0.8f));
            CenterText("无需瞄准，飞剑会为你开路。", new(640, 578), 20, Palette.Paper, TitleFont);
            CenterText("WASD / 方向键 移动  ·  E 构筑  ·  Esc / P 暂停", new(640, 603), 15, Palette.Muted);
        }
        if (Run.BurstGlow > 0) CenterText("剑 意  ·  归 一", new(640, 213), 32, Palette.Alpha(Palette.Gold, Run.BurstGlow / 0.65f), TitleFont);
        if (Run.Health < Run.MaxHealth * 0.25f)
        {
            DrawRect(new(0, 93, 7, 557), Palette.Alpha(Palette.Red, 0.5f));
            DrawRect(new(1273, 93, 7, 557), Palette.Alpha(Palette.Red, 0.5f));
        }
    }

    private void DrawObjective()
    {
        if (Run == null) return;
        DrawRect(new(24, 108, 233, 78), Palette.Alpha(Palette.Deep, 0.8f));
        Text($"净化古印  {Run.PurifiedSeals} / 3", new(39, 136), 18, Palette.Gold);
        Text(Run.BossSpawned ? "击破雾中来客，平息异变" : $"距终章  {FormatTime(RunState.BossArrival - Run.Time)}", new(39, 164), 14, Palette.Muted);
        var nearest = Run.Seals.Where(seal => !seal.Complete).OrderBy(seal => System.Numerics.Vector2.DistanceSquared(seal.Position, Run.PlayerPosition)).FirstOrDefault();
        if (nearest == null) return;
        var direction = Palette.Vector(nearest.Position - Run.PlayerPosition);
        if (direction.Length() < 140) return;
        var arrow = new Vector2(640, 370) + Palette.Vector(Run.PlayerPosition) - camera + direction.Normalized() * 160;
        arrow = arrow.Clamp(new Vector2(275, 193), new Vector2(1070, 580));
        var heading = direction.Normalized();
        DrawColoredPolygon([arrow + heading * 11, arrow - heading * 5 + heading.Orthogonal() * 6, arrow - heading * 5 - heading.Orthogonal() * 6], Palette.Gold);
        CenterText(nearest.Name, arrow + new Vector2(0, 27), 14, Palette.Gold);
    }

    private void DrawMinimap()
    {
        if (Run == null) return;
        var bounds = new Rect2(1111, 108, 141, 111);
        DrawRect(bounds, Palette.Alpha(Palette.Deep, 0.85f));
        DrawRect(bounds.Grow(-5), Palette.Alpha(Palette.Gold, 0.2f), false, 1);
        Vector2 Map(System.Numerics.Vector2 position) => bounds.GetCenter() + new Vector2(position.X / RunState.ArenaHalfWidth * 62, position.Y / RunState.ArenaHalfHeight * 46);
        foreach (var seal in Run.Seals) Diamond(Map(seal.Position), 3, seal.Complete ? Palette.Jade : Palette.Gold);
        if (Run.Boss != null) DrawCircle(Map(Run.Boss.Position), 3, Palette.Red);
        DrawCircle(Map(Run.PlayerPosition), 3, Palette.Paper);
        Text("博 丽 夜 境", new(1142, 239), 12, Palette.Muted);
    }

    private void Bar(Rect2 rectangle, float fraction, Color color)
    {
        DrawRect(rectangle, new Color("293638"));
        DrawRect(new(rectangle.Position, new(rectangle.Size.X * Math.Clamp(fraction, 0, 1), rectangle.Size.Y)), color);
    }

    public static string FormatTime(float seconds) => $"{(int)Math.Max(0, seconds) / 60:00}:{(int)Math.Max(0, seconds) % 60:00}";
}
