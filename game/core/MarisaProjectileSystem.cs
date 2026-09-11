using System.Numerics;

namespace Rebirth.Core;

internal static class MarisaProjectileSystem
{
    internal static bool Cast(RunState run, int rank)
    {
        if (run.Stars.Count >= MarisaTuning.StarLimit) return false;
        var angle = run.Marisa.EmissionAngle;
        run.Marisa.EmissionAngle = (angle + MarisaTuning.EmissionAngleStep) % MathF.Tau;
        var direction = Geometry.Angle(angle);
        var lifetime = MarisaTuning.Lifetime(run.Build);
        var mass = MarisaTuning.RollMass(run.Marisa.MassRandom, run.Build);
        run.Stars.Add(new()
        {
            Position = RunState.ClampToArena(run.PlayerPosition + direction * 12),
            Velocity = direction * MarisaTuning.StarSpeed,
            Mass = mass,
            DamageRate = AbilityTuning.Get(ArtKind.Stars, rank).Damage * mass * run.Power,
            Life = lifetime, Duration = lifetime, PulseTimer = MarisaTuning.StarPulse, Rotation = angle,
            VisualSeed = run.Marisa.VisualRandom.Next()
        });
        return true;
    }

    internal static void Step(RunState run)
    {
        StarGravitySystem.Step(run);
        foreach (ref var star in run.Stars.Active)
        {
            star.Life = Math.Max(0, star.Life - RunState.StepSeconds);
            if (star.Life <= 0) continue;
            star.Velocity = MarisaTuning.IntegrateGravity(star.Velocity, star.Acceleration, MarisaTuning.StarDrag, MarisaTuning.StarVelocityLimit);
            star.Position = RunState.ClampToArena(Geometry.Advance(star.Position, star.Velocity, RunState.StepSeconds));
            star.Resonating = run.Build.Has(AbilityTraits.SparkResonance) && run.BeamContains(star.Position);
            star.PulseTimer -= RunState.StepSeconds;
            if (star.PulseTimer > 0) continue;
            star.PulseTimer += MarisaTuning.StarPulse;
            Tear(run, star);
        }
        run.Stars.RemoveAll(static star => star.Life <= 0);
    }

    private static void Tear(RunState run, StarBody star)
    {
        var radius = MarisaTuning.DamageRadius(star.Mass);
        foreach (var enemy in run.World.Grid.Query(star.Position, radius + 36))
        {
            if (enemy.Health <= 0) continue;
            var reach = radius + enemy.Radius;
            var distanceSquared = Geometry.DistanceSquared(star.Position, enemy.Position);
            if (distanceSquared >= reach * reach) continue;
            var falloff = 1 - 0.4f * Math.Clamp(MathF.Sqrt(distanceSquared) / reach, 0, 1);
            var damage = star.DamageRate * MarisaTuning.StarPulse * falloff * (star.Resonating ? MarisaTuning.ResonanceMultiplier : 1);
            run.DamageEnemy(enemy, damage, Vector2.Zero);
        }
    }
}
