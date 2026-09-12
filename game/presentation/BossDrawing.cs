using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private ShaderMaterial bossSparkMaterial = null!;

    private void DrawBossGround()
    {
        if (Run?.Boss is not { Abilities: { } state } boss) return;
        if (state.Field is { } field)
        {
            var center = Palette.Vector(field.Position);
            var size = Vector2.One * field.HalfSize * 2;
            var opacity = field.Warmup > 0 ? 0.2f : 0.6f;
            OriginalEffect("reimu_seal_ink", center, size, Palette.Alpha(new Color("ff9c8d"), opacity));
            surface.DrawRect(new(center - size * 0.5f, size), Palette.Alpha(Palette.Red, field.Warmup > 0 ? 0.7f : 0.95f), false, 2);
        }
        if (state.RecoveryRemaining > 0)
        {
            var position = Palette.Vector(boss.Position);
            OriginalEffect("marisa_cast", position, Vector2.One * 82, Palette.Alpha(Palette.Jade, 0.45f), -Clock);
            OriginalEffect("marisa_mushroom", position + new Vector2(0, -54), Vector2.One * 34, Colors.White);
        }
    }

    private void DrawBossBeam()
    {
        if (Run?.Boss is not { Abilities.Beam: { } beam } boss) return;
        var opacity = beam.Warmup > 0 ? 0.12f : Math.Min(0.85f, beam.Remaining / 0.2f);
        OriginalBeam(Palette.Vector(boss.Position), Palette.Vector(beam.Direction), beam.Length, beam.HalfWidth,
            Math.Min(beam.Length * 0.2f, beam.HalfWidth * 2), Palette.Alpha(Colors.White, opacity));
    }

    private void DrawBossDanger()
    {
        if (Run == null) return;
        foreach (ref readonly var star in Run.Stars.Active)
        {
            if (!star.Hostile || star.Life <= 0 || !InView(Palette.Vector(star.Position), 120)) continue;
            surface.DrawArc(Palette.Vector(star.Position), MarisaTuning.DamageRadius(star.Mass), 0, MathF.Tau, 20,
                Palette.Alpha(Palette.Red, 0.55f), 1.5f);
        }
        if (Run.Boss is not { Abilities: { } state } boss) return;
        var position = Palette.Vector(boss.Position);
        if (state.Hero == HeroKind.Reimu && state.Phase == 2)
        {
            for (var index = 0; index < 3; index++)
            {
                var orbit = Palette.Vector(state.OrbitPosition(boss.Position, index));
                Sprite("actors/yin_yang_orb", orbit + new Vector2(0, 7), 0.7f);
                surface.DrawArc(orbit, 24, 0, MathF.Tau, 20, Palette.Alpha(Palette.Red, 0.55f), 1.5f);
            }
        }
        if (state.Beam is not { } beam) return;
        var direction = Palette.Vector(beam.Direction);
        var target = position + direction * beam.Length;
        var side = direction.Orthogonal() * beam.HalfWidth;
        surface.DrawLine(position + side, target + side, Palette.Alpha(Palette.Red, 0.9f), 2);
        surface.DrawLine(position - side, target - side, Palette.Alpha(Palette.Red, 0.9f), 2);
        if (beam.Warmup > 0)
            for (var distance = 25f; distance < beam.Length; distance += 42)
                surface.DrawLine(position + direction * distance, position + direction * (distance + 21), Palette.Gold, 2);
        OriginalEffect("marisa_cast", position, Vector2.One * 52, Palette.Alpha(Palette.Red, 0.7f), ReducedMotion ? 0 : Clock);
    }
}
