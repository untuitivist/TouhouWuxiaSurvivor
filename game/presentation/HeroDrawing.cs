using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private static readonly Color[] DreamColors = [new("ef9fb3"), new("e6c786"), new("8cdcc8"), new("a9cadb"), new("bab0f0"), new("ecb9ed"), new("f3e7ce")];

    private void DrawHeroFields()
    {
        if (Run?.Field is { } field)
        {
            var center = Palette.Vector(field.Position);
            var bounds = new Rect2(center - Vector2.One * field.HalfSize, Vector2.One * field.HalfSize * 2);
            var fieldOpacity = Math.Min(1, field.Remaining / 0.3f);
            DrawRect(bounds, Palette.Alpha(Palette.Red, 0.055f * fieldOpacity));
            DrawRect(bounds, Palette.Alpha(Palette.Red, 0.7f * fieldOpacity), false, 2);
            DrawRect(bounds.Grow(-12), Palette.Alpha(Palette.Paper, 0.22f * fieldOpacity), false, 1);
            DrawArc(center, field.HalfSize * 0.8f, 0, MathF.Tau, 64, Palette.Alpha(Palette.Red, 0.16f * fieldOpacity), 1);
            foreach (var corner in new[] { bounds.Position, bounds.Position + new Vector2(bounds.Size.X, 0), bounds.End, bounds.Position + new Vector2(0, bounds.Size.Y) })
            {
                DrawRect(new(corner - new Vector2(6, 10), new(12, 20)), Palette.Alpha(Palette.Paper, fieldOpacity));
                DrawLine(corner - Vector2.Down * 6, corner + Vector2.Down * 6, Palette.Red, 3);
            }
        }
        if (Run?.Beam is not { } beam) return;
        var origin = Palette.Vector(Run.PlayerPosition);
        var direction = Palette.Vector(beam.Direction);
        var target = origin + direction * beam.Length;
        var side = direction.Orthogonal() * beam.HalfWidth;
        if (beam.Warmup > 0)
        {
            DrawLine(origin + side, target + side, Palette.Alpha(Palette.Gold, 0.24f), 1);
            DrawLine(origin - side, target - side, Palette.Alpha(Palette.Gold, 0.24f), 1);
            for (var distance = 22f; distance < beam.Length; distance += 38)
                DrawLine(origin + direction * distance, origin + direction * (distance + 17), Palette.Alpha(Palette.Gold, 0.58f), 2);
            Star(origin, 18 + 14 * (1 - beam.Warmup / AbilityTuning.BeamWarmup), Palette.Gold, Clock * 3);
            return;
        }
        var opacity = Math.Min(1, beam.Remaining / 0.2f);
        DrawColoredPolygon([origin + side, target + side, target - side, origin - side], Palette.Alpha(Palette.Violet, 0.26f * opacity));
        DrawLine(origin, target, Palette.Alpha(Palette.Gold, 0.28f * opacity), beam.HalfWidth * 1.45f);
        DrawLine(origin, target, Palette.Alpha(Palette.Paper, 0.6f * opacity), beam.HalfWidth * 0.65f);
        DrawLine(origin + side, target + side, Palette.Alpha(Palette.Jade, 0.7f * opacity), 2);
        DrawLine(origin - side, target - side, Palette.Alpha(Palette.Violet, 0.7f * opacity), 2);
        Star(origin, beam.HalfWidth * 0.85f, Palette.Alpha(Palette.Gold, opacity), Clock * 2);
    }

    private void Star(Vector2 position, float radius, Color color, float rotation)
    {
        if (radius < 0.25f || color.A <= 0) return;
        var points = new Vector2[10];
        for (var index = 0; index < points.Length; index++)
            points[index] = position + Vector2.FromAngle(rotation + index * MathF.PI / 5) * (index % 2 == 0 ? radius : radius * 0.43f);
        DrawColoredPolygon(points, color);
    }
}
