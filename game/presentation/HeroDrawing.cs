using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private static readonly Color[] DreamColors = [new("ef9fb3"), new("e6c786"), new("8cdcc8"), new("a9cadb"), new("bab0f0"), new("ecb9ed"), new("f3e7ce")];

    private void DrawReimuField()
    {
        if (Run?.Field is { } field)
        {
            var center = Palette.Vector(field.Position);
            var bounds = new Rect2(center - Vector2.One * field.HalfSize, Vector2.One * field.HalfSize * 2);
            var fieldOpacity = Math.Min(1, field.Remaining / 0.3f);
            EffectSprite("reimu_seal_ink", center, bounds.Size, Palette.Alpha(Colors.White, 0.52f * fieldOpacity));
            EffectSprite("reimu_aura", center, bounds.Size * 0.9f, Palette.Alpha(new Color("ffb4c4"), 0.16f * fieldOpacity));
            surface.DrawRect(bounds, Palette.Alpha(Palette.Red, 0.3f * fieldOpacity), false, 1);
            foreach (var corner in new[] { bounds.Position, bounds.Position + new Vector2(bounds.Size.X, 0), bounds.End, bounds.Position + new Vector2(0, bounds.Size.Y) })
            {
                EffectSprite("reimu_talisman", corner, Vector2.One * 26, Palette.Alpha(Colors.White, fieldOpacity));
            }
        }
    }

    private void DrawMarisaBeam()
    {
        if (Run?.Beam is not { } beam) return;
        var origin = Palette.Vector(Run.PlayerPosition);
        var direction = Palette.Vector(beam.Direction);
        var target = origin + direction * beam.Length;
        var side = direction.Orthogonal() * beam.HalfWidth;
        if (beam.Warmup > 0)
        {
            var charge = 1 - beam.Warmup / AbilityTuning.BeamWarmup;
            SpellBeam(origin, direction, beam.Length, beam.HalfWidth, Palette.Alpha(SparkColor(), 0.06f + charge * 0.08f));
            surface.DrawLine(origin + side, target + side, Palette.Alpha(Palette.Gold, 0.24f), 1);
            surface.DrawLine(origin - side, target - side, Palette.Alpha(Palette.Gold, 0.24f), 1);
            for (var distance = 22f; distance < beam.Length; distance += 38)
                surface.DrawLine(origin + direction * distance, origin + direction * (distance + 17), Palette.Alpha(Palette.Gold, 0.58f), 2);
            EffectSprite("marisa_cast", origin, Vector2.One * (32 + charge * 36), Palette.Alpha(Colors.White, 0.35f + charge * 0.5f), ReducedMotion ? 0 : -Clock);
            return;
        }
        var opacity = Math.Min(1, beam.Remaining / 0.2f);
        SpellBeam(origin, direction, beam.Length, beam.HalfWidth * 1.12f, Palette.Alpha(SparkColor(), 0.4f * opacity));
        SpellBeam(origin, direction, beam.Length, beam.HalfWidth, Palette.Alpha(SparkColor(1.5f) * new Color(1.2f, 1.2f, 1.2f), 0.85f * opacity));
        SpellBeam(origin, direction, beam.Length, beam.HalfWidth * 0.65f, Palette.Alpha(Colors.White, 0.4f * opacity));
        EffectSprite("marisa_cast", origin, Vector2.One * Math.Max(48, beam.HalfWidth * 3), Palette.Alpha(Colors.White, 0.8f * opacity), ReducedMotion ? 0 : -Clock * 0.6f);
    }

    private void Star(Vector2 position, float radius, Color color, float rotation)
    {
        if (radius < 0.25f || color.A <= 0) return;
        EffectSprite("star", position, Vector2.One * radius * 2, color, rotation);
    }
}
