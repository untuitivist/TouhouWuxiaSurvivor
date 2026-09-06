using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private void DrawBattlefield()
    {
        if (Run == null) return;
        var minimumX = (int)MathF.Floor((camera.X - 700) / 48);
        var minimumY = (int)MathF.Floor((camera.Y - 420) / 48);
        for (var column = minimumX; column < minimumX + 30; column++)
        for (var row = minimumY; row < minimumY + 19; row++)
        {
            var location = new Vector2(column * 48, row * 48);
            var hash = TileHash(column, row);
            var outside = Math.Abs(location.X) > RunState.ArenaHalfWidth || Math.Abs(location.Y) > RunState.ArenaHalfHeight;
            var path = Math.Abs(location.X + 24) < 75 || Math.Abs(location.Y + 24) < 70 || Math.Abs(location.X - location.Y * 1.45f) < 48;
            var tint = outside ? new Color("101f26") : path ? new Color("526563") : new Color("345348");
            var brightness = 0.88f + hash % 5 * 0.025f;
            DrawTextureRect(textures[path ? "path" : "grass"], new(location, new(48, 48)), false, tint * new Color(brightness, brightness, brightness));
            if (!path && !outside && hash % 7 == 0)
            {
                var tuft = location + new Vector2(12 + hash % 21, 14 + hash % 13);
                DrawLine(tuft, tuft + new Vector2(-4, -7), new("55735c"), 2);
                DrawLine(tuft, tuft + new Vector2(3, -10), new("49654e"), 2);
            }
            if (!path && !outside && hash % 19 == 0)
            {
                var flower = location + new Vector2(20, 22);
                DrawRect(new(flower, new(3, 3)), new("bb8590"));
                DrawRect(new(flower + new Vector2(7, 5), new(2, 2)), new("c79e9c"));
            }
        }
        DrawRect(new(-RunState.ArenaHalfWidth, -RunState.ArenaHalfHeight, RunState.ArenaHalfWidth * 2, RunState.ArenaHalfHeight * 2), Palette.Alpha(Palette.Jade, 0.45f), false, 4);
        for (var index = 0; index < 72; index++)
        {
            var hash = TileHash(index, 7);
            var location = new Vector2((int)(hash % 2800) - 1400, (int)(TileHash(index, 12) % 2000) - 1000);
            if (Math.Abs(location.X) < 150 || Math.Abs(location.Y) < 100 || Math.Abs(location.X - location.Y * 1.45f) < 100) continue;
            if (location.DistanceSquaredTo(camera) > 900 * 900 || Run.Seals.Any(seal => Palette.Vector(seal.Position).DistanceSquaredTo(location) < 180 * 180)) continue;
            DrawTree(location, 0.7f + hash % 4 * 0.1f, false);
        }
        DrawTorii(new(0, -175), 1);
        foreach (var seal in Run.Seals) DrawSeal(seal);
        for (var index = 0; index < 25; index++)
        {
            var location = camera + new Vector2(((index * 179 + Clock * (7 + index % 3)) % 1420) - 710, ((index * 137 + Clock * 12) % 820) - 410);
            DrawLine(location, location + new Vector2(4, 2), Palette.Alpha(index % 3 == 0 ? Palette.Gold : Palette.Paper, 0.2f), 2);
        }
    }

    private static uint TileHash(int column, int row)
    {
        var hash = unchecked((uint)(column * 374761393 + row * 668265263));
        hash = (hash ^ (hash >> 13)) * 1274126177u;
        return hash ^ (hash >> 16);
    }

    private void DrawSeal(Seal seal)
    {
        var position = Palette.Vector(seal.Position);
        var color = seal.Complete ? Palette.Jade : Palette.Gold;
        DrawCircle(position, 91, Palette.Alpha(color, 0.035f));
        DrawArc(position, 90, 0, MathF.Tau, 64, Palette.Alpha(color, 0.3f), 2);
        DrawArc(position, 74, -MathF.PI / 2, -MathF.PI / 2 + MathF.Tau * Math.Max(0.002f, seal.Charge), 64, color, 3);
        for (var index = 0; index < 8; index++)
        {
            var angle = Clock * 0.12f + index * MathF.Tau / 8;
            var point = position + Vector2.FromAngle(angle) * 81;
            DrawLine(point - Vector2.FromAngle(angle + MathF.PI / 2) * 6, point + Vector2.FromAngle(angle + MathF.PI / 2) * 6, Palette.Alpha(color, 0.6f), 2);
        }
        DrawRect(new(position + new Vector2(-15, -36), new(30, 44)), new Color("243638"));
        DrawRect(new(position + new Vector2(-18, 5), new(36, 8)), new Color("6a7970"));
        CenterText(seal.Complete ? "定" : "封", position + new Vector2(0, -7), 23, color, TitleFont);
        CenterText(seal.Name, position + new Vector2(0, 113), 18, color);
        if (!seal.Complete && Run != null && position.DistanceSquaredTo(Palette.Vector(Run.PlayerPosition)) < 160 * 160)
            CenterText("停留净化 · 悟道 / 回血", position + new Vector2(0, 138), 15, Palette.Paper);
    }

    private void DrawTorii(Vector2 position, float scale)
    {
        void Block(float left, float top, float width, float height, string color) => DrawRect(new(position + new Vector2(left, top) * scale, new Vector2(width, height) * scale), new Color(color));
        Block(-76, 0, 152, 18, "233330");
        Block(-60, -121, 13, 130, "793c3b");
        Block(47, -121, 13, 130, "793c3b");
        Block(-59, -117, 4, 120, "ac6151");
        Block(48, -117, 4, 120, "ac6151");
        Block(-69, -18, 30, 26, "526460");
        Block(39, -18, 30, 26, "526460");
        Block(-84, -132, 168, 13, "713b3a");
        Block(-90, -137, 180, 6, "b2755b");
        Block(-82, -101, 164, 9, "a15c4e");
        Block(-94, -145, 188, 9, "1c2c30");
        Block(-11, -122, 22, 39, "272e29");
        Block(-7, -116, 14, 27, "bca375");
        for (var index = 0; index < 5; index++)
        {
            var origin = position + new Vector2(-40 + index * 20, -78 + MathF.Sin(index) * 3) * scale;
            DrawLine(origin, origin + new Vector2(2, 15) * scale, new Color("c9c7aa"), 3 * scale);
        }
    }

    private void DrawTree(Vector2 position, float scale, bool blossom)
    {
        var foliage = blossom ? new Color("624a61") : new Color("243f3d");
        DrawCircle(position + new Vector2(0, 4), 40 * scale, new Color(0.03f, 0.06f, 0.07f, 0.23f));
        DrawRect(new(position + new Vector2(-6, -64) * scale, new Vector2(12, 74) * scale), new Color("39443e"));
        for (var index = 0; index < 5; index++)
        {
            var offset = new Vector2((index % 3 - 1) * 28, -64 - (index / 3) * 27) * scale;
            DrawCircle(position + offset, (34 - index % 2 * 5) * scale, foliage);
            DrawCircle(position + offset + new Vector2(-6, -10) * scale, 23 * scale, blossom ? new Color("815b70") : new Color("304d45"));
        }
    }

    private void DrawTitleLandscape()
    {
        DrawTextureRect(titleLandscape, new(0, 0, 1280, 720), false);
        Sprite("players/reimu", new(949, 539), 3);
        Sprite("players/marisa", new(1037, 559), 3, 1);
        if (!ReducedMotion) for (var index = 0; index < 18; index++)
        {
            var horizontal = MathF.Floor(((index * 193 + Clock * 12) % 1280) / 4) * 4;
            var vertical = MathF.Floor(((index * 113 + Clock * 17) % 720) / 4) * 4;
            DrawRect(new(horizontal, vertical, 4, 4), new Color("dfa5a1"));
        }
    }
}
