using System.Numerics;

namespace Rebirth.Core;

internal static class MarisaProjectileSystem
{
    internal static bool Cast(RunState run, int rank)
        => CastFrom(run, run.PlayerPosition, run.Build, run.Marisa, rank, run.Power);

    internal static int CountOwned(RunState run, int ownerId)
    {
        var count = 0;
        foreach (ref readonly var star in run.Stars.Active) if (star.Life > 0 && star.OwnerId == ownerId) count++;
        return count;
    }

    internal static bool CastFrom(RunState run, Vector2 origin, BuildState build, MarisaAbilityState state, int rank, float power, int ownerId = 0)
    {
        if (CountOwned(run, ownerId) >= (ownerId == 0 ? MarisaTuning.StarLimit : BossAbilitySystem.StarLimit))
        {
            state.BlockedEmissions = Math.Min(int.MaxValue - 1, state.BlockedEmissions) + 1;
            return false;
        }
        var angle = state.EmissionAngle;
        state.EmissionAngle = (angle + MarisaTuning.EmissionAngleStep) % MathF.Tau;
        var direction = Geometry.Angle(angle);
        var lifetime = MarisaTuning.Lifetime(build);
        var mass = MarisaTuning.RollMass(state.MassRandom, build);
        run.Stars.Add(new()
        {
            OwnerId = ownerId, Hostile = ownerId != 0, Stance = build.Stance,
            Position = RunState.ClampToArena(origin + direction * 12),
            Velocity = direction * MarisaTuning.StarSpeed,
            Mass = mass,
            DamageRate = AbilityTuning.Get(ArtKind.Stars, rank).Damage * mass * power * MainlineGrowth.Power(build),
            Life = lifetime, Duration = lifetime, PulseTimer = MarisaTuning.StarPulse, Rotation = angle,
            VisualSeed = state.VisualRandom.Next()
        });
        return true;
    }

    internal static void Step(RunState run)
    {
        var measured = run.Timings != null;
        var started = measured ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        StarGravitySystem.Step(run);
        if (measured)
        {
            run.Marisa.GravityMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            started = System.Diagnostics.Stopwatch.GetTimestamp();
        }
        var boss = run.Boss;
        foreach (ref var star in run.Stars.Active)
        {
            star.Life = Math.Max(0, star.Life - RunState.StepSeconds);
            if (star.Life <= 0) continue;
            star.Velocity = MarisaTuning.IntegrateGravity(star.Velocity, star.Acceleration, MarisaTuning.StarDrag, MarisaTuning.StarVelocityLimit);
            star.Position = RunState.ClampToArena(Geometry.Advance(star.Position, star.Velocity, RunState.StepSeconds));
            star.Resonating = star.Hostile
                ? boss?.Id == star.OwnerId && boss.Abilities is { Phase: 2, Beam: { } beam } && CharacterAttackRules.BeamContains(beam, boss.Position, star.Position, 0)
                : run.Build.Has(AbilityTraits.SparkResonance) && run.BeamContains(star.Position);
            star.PulseTimer -= RunState.StepSeconds;
            if (star.PulseTimer > 0) continue;
            star.PulseTimer += MarisaTuning.StarPulse;
            Tear(run, star);
        }
        foreach (var enemy in run.Enemies.Active)
        {
            if (enemy.StarHitDisplayDamage <= 0) continue;
            run.Emit(EffectKind.Hit, enemy.Position, enemy.StarHitDisplayDamage);
            enemy.StarHitDisplayDamage = 0;
        }
        run.Stars.RemoveWhere(static (in StarBody star) => star.Life <= 0);
        if (measured) run.Marisa.StarUpdateMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    private static void Tear(RunState run, StarBody star)
    {
        star.DamageRate *= MainlineGrowth.StarAgeMultiplier(star.Stance, star.Duration, star.Life);
        var radius = MarisaTuning.DamageRadius(star.Mass);
        if (star.Hostile)
        {
            var reach = radius + 5;
            var distance = Geometry.DistanceSquared(star.Position, run.PlayerPosition);
            if (distance < reach * reach) run.HurtContinuous(star.DamageRate * MarisaTuning.StarPulse
                * (1 - 0.4f * Math.Clamp(MathF.Sqrt(distance) / reach, 0, 1)) * (star.Resonating ? MarisaTuning.ResonanceMultiplier : 1));
            return;
        }
        foreach (var enemy in run.World.Grid.Query(star.Position, radius + 36))
        {
            if (enemy.Health <= 0) continue;
            var reach = radius + enemy.Radius;
            var distanceSquared = Geometry.DistanceSquared(star.Position, enemy.Position);
            if (distanceSquared >= reach * reach) continue;
            var falloff = 1 - 0.4f * Math.Clamp(MathF.Sqrt(distanceSquared) / reach, 0, 1);
            var damage = star.DamageRate * MarisaTuning.StarPulse * falloff * (star.Resonating ? MarisaTuning.ResonanceMultiplier : 1);
            run.DamageEnemy(enemy, damage, Vector2.Zero, false);
            enemy.StarHitDisplayDamage += damage;
        }
    }
}
