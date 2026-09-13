using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private ShaderMaterial bossSparkMaterial = null!;

    private void DrawBossGround()
    {
        if (Run?.Boss?.Abilities?.Volley is not { } volley) return;
        var position = Palette.Vector(volley.Origin);
        var opacity = volley.Warmup > 0 ? 0.35f : 0.18f;
        OriginalEffect(volley.Hero == HeroKind.Reimu ? "reimu_seal_ink" : "marisa_cast", position,
            Vector2.One * (110 + volley.Phase * 25), Palette.Alpha(Colors.White, opacity), ReducedMotion ? 0 : Clock * 0.2f);
    }

    private void DrawBossBeam()
    {
        if (Run?.Boss?.Abilities?.Beam is not { } beam) return;
        var opacity = beam.Warmup > 0 ? 0.12f : Math.Min(0.85f, beam.Remaining / 0.2f);
        for (var index = 0; index < beam.Count; index++)
            OriginalBeam(Palette.Vector(beam.Origin), Palette.Vector(beam.Bearing(index)), beam.Length, beam.HalfWidth,
                beam.HalfWidth * 2, Palette.Alpha(Colors.White, opacity));
    }

    private void DrawBossDanger()
    {
        if (Run?.Boss?.Abilities is not { } state) return;
        if (state.Volley is { Warmup: > 0 } volley)
        {
            var position = Palette.Vector(volley.Origin);
            var radius = 65 + (1 - volley.Warmup / BossVolleyState.TelegraphSeconds) * 30;
            for (var lane = 0; lane < 2; lane++)
            {
                var angle = volley.GapAngle + lane * MathF.PI;
                surface.DrawArc(position, radius, angle + BossVolleyState.GapHalfAngle,
                    angle + MathF.PI - BossVolleyState.GapHalfAngle, 24, Palette.Alpha(Palette.Red, 0.8f), 2);
                for (var side = -1; side <= 1; side += 2)
                {
                    var direction = Vector2.FromAngle(angle + side * BossVolleyState.WarningHalfAngle);
                    surface.DrawLine(position + direction * BossVolleyState.WarningStart, position + direction * 360, Palette.Alpha(Palette.Jade, 0.65f), 1.5f);
                }
            }
        }
        if (state.Beam is not { } beam) return;
        var origin = Palette.Vector(beam.Origin);
        for (var index = 0; index < beam.Count; index++)
        {
            var direction = Palette.Vector(beam.Bearing(index));
            var target = origin + direction * beam.Length;
            var side = direction.Orthogonal() * beam.HalfWidth;
            surface.DrawLine(origin + side, target + side, Palette.Alpha(Palette.Red, 0.9f), 2);
            surface.DrawLine(origin - side, target - side, Palette.Alpha(Palette.Red, 0.9f), 2);
            if (beam.Warmup > 0)
                for (var distance = 25f; distance < beam.Length; distance += 42)
                    surface.DrawLine(origin + direction * distance, origin + direction * (distance + 21), Palette.Gold, 2);
        }
        OriginalEffect("marisa_cast", origin, Vector2.One * 52, Palette.Alpha(Palette.Red, 0.7f), ReducedMotion ? 0 : Clock);
    }
}
