using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private void DrawScenery()
    {
        if (Run == null) return;
        surface.DrawRect(new(-RunState.ArenaHalfWidth, -RunState.ArenaHalfHeight, RunState.ArenaHalfWidth * 2, RunState.ArenaHalfHeight * 2), Palette.Alpha(Palette.Jade, 0.45f), false, 4);
        for (var index = 0; index < 72; index++)
        {
            var hash = TileHash(index, 7);
            var location = new Vector2((int)(hash % 2800) - 1400, (int)(TileHash(index, 12) % 2000) - 1000);
            if (Math.Abs(location.X) < 150 || Math.Abs(location.Y) < 100 || Math.Abs(location.X - location.Y * 1.45f) < 100) continue;
            if (Run.Seals.Any(seal => Palette.Vector(seal.Position).DistanceSquaredTo(location) < 180 * 180)) continue;
            DrawTree(location, 0.7f + hash % 4 * 0.1f, (int)(hash % 2));
        }
        DrawTorii(new(0, -80), 1);
    }

    private void DrawWorldDynamic()
    {
        if (Run == null) return;
        foreach (var seal in Run.Seals) DrawSeal(seal);
        for (var index = 0; index < 25; index++)
        {
            var location = camera + new Vector2(((index * 179 + Clock * (7 + index % 3)) % 1420) - 710, ((index * 137 + Clock * 12) % 820) - 410);
            surface.DrawTextureRect(PixelSkin.Artwork("petal"), new(location, new(8, 8)), false, Palette.Alpha(Colors.White, 0.35f));
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
        EffectSprite("ritual_array", position, Vector2.One * 184, Palette.Alpha(Colors.White, seal.Complete ? 0.18f : 0.38f), ReducedMotion ? 0 : Clock * 0.06f);
        surface.DrawArc(position, 74, -MathF.PI / 2, -MathF.PI / 2 + MathF.Tau * Math.Max(0.002f, seal.Charge), 64, color, 3);
        surface.DrawTextureRect(PixelSkin.Artwork("shrine_marker"), new(position + new Vector2(-20, -48), new(40, 60)), false);
    }

    private void DrawTorii(Vector2 position, float scale)
    {
        var size = toriiTexture.GetSize() * (0.7f * scale);
        surface.DrawTextureRect(toriiTexture, new(position - new Vector2(size.X / 2, size.Y - 10 * scale), size), false, new Color("c9b6b1"));
    }

    private void DrawTree(Vector2 position, float scale, int variant)
    {
        var size = Vector2.One * (148 * scale);
        surface.DrawTextureRect(treeCanopies[variant], new(position - new Vector2(size.X / 2, size.Y - 8 * scale), size), false, new Color("b2c3be"));
    }

    private void DrawTitleLandscape()
    {
        surface.DrawTextureRect(titleLandscape, new(0, 0, 1280, 720), false);
        Sprite("players/reimu", new(949, 539), 3);
        Sprite("players/marisa", new(1037, 559), 3, 1);
        surface.DrawRect(new(780, 610, 468, 40), Palette.Alpha(Palette.Deep, 0.86f));
        surface.DrawRect(new(0, 672, 1280, 48), Palette.Alpha(Palette.Deep, 0.86f));
    }
}
