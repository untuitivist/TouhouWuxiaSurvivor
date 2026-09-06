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
        DrawRect(new(0, 0, 1280, 720), new Color("101f27"));
        for (var index = 0; index < 90; index++)
        {
            var position = new Vector2(TileHash(index, 3) % 1280, TileHash(index, 8) % 550);
            DrawCircle(position, index % 4 == 0 ? 1.5f : 0.8f, Palette.Alpha(Palette.Paper, 0.15f + 0.12f * MathF.Sin(Clock + index)));
        }
        DrawCircle(new(954, 233), 140, new Color("192e36"));
        DrawCircle(new(954, 233), 124, new Color("607973"));
        DrawCircle(new(954, 233), 120, new Color("b2b7a0"));
        for (var layer = 0; layer < 4; layer++)
        {
            var points = new List<Vector2> { new(-50, 720) };
            for (var index = 0; index < 17; index++)
                points.Add(new(index * 90 - 50, 380 + layer * 72 + MathF.Sin(index * 1.34f + layer) * (70 - layer * 9)));
            points.Add(new(1400, 720));
            DrawColoredPolygon(points.ToArray(), new Color(0.075f + layer * 0.004f, 0.16f - layer * 0.018f, 0.18f - layer * 0.018f));
        }
        DrawArc(new(953, 406), 187, 0, MathF.Tau, 96, Palette.Alpha(Palette.Gold, 0.22f), 1);
        DrawArc(new(953, 406), 176, -Clock * 0.08f, 3.9f - Clock * 0.08f, 72, Palette.Alpha(Palette.Gold, 0.35f), 2);
        DrawTorii(new(953, 474), 1.35f);
        DrawTree(new(1220, 620), 1.8f, true);
        DrawTree(new(705, 627), 1.0f, true);
        Sprite("players/reimu", new(939, 532 + MathF.Sin(Clock * 1.5f) * 5), 2.9f);
        Sprite("players/marisa", new(1043, 547 + MathF.Sin(Clock * 1.5f + 1) * 5), 2.6f, 1);
        for (var index = 0; index < 28; index++)
        {
            var position = new Vector2((index * 193 + Clock * 18) % 1280, (index * 113 + Clock * 24) % 720);
            DrawLine(position, position + new Vector2(5, 2), Palette.Alpha(new Color("d59fa8"), 0.5f), 2);
        }
        DrawRect(new(0, 0, 650, 720), new Color(0.035f, 0.075f, 0.095f, 0.65f));
        DrawLine(new(48, 35), new(1232, 35), Palette.Alpha(Palette.Gold, 0.25f), 1);
        DrawLine(new(48, 670), new(1232, 670), Palette.Alpha(Palette.Gold, 0.25f), 1);
    }
}
