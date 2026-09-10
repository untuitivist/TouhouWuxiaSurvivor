using System.Numerics;

namespace Rebirth.Core;

internal static class MarisaBeamSystem
{
    internal static void Start(RunState run, bool signature)
    {
        var rank = run.Ranks[(int)ArtKind.MasterSpark];
        if (rank <= 0 || signature && !run.Build.SignatureUnlocked) return;
        var stats = AbilityTuning.Get(ArtKind.MasterSpark, rank);
        var target = run.NearestEnemy(run.PlayerPosition, 1200);
        var aim = target == null ? run.Facing : Geometry.Direction(target.Position - run.PlayerPosition);
        var sweep = run.Build.Has(AbilityTraits.SparkSweep);
        var duration = signature ? 2.4f : stats.Duration;
        run.Beam = new()
        {
            AimDirection = aim, Direction = sweep ? Geometry.Rotate(aim, -MarisaTuning.SweepDegrees * MathF.PI / 180) : aim,
            Sweep = sweep, ClearsBullets = run.Build.Has(AbilityTraits.SparkClear),
            Warmup = AbilityTuning.BeamWarmup, Remaining = duration, Duration = duration,
            HalfWidth = AbilityTuning.BeamHalfWidth(rank) + (signature ? 16 : 0),
            Length = signature ? 1100 : stats.Range,
            Damage = stats.Damage * run.Power * (signature ? 1.7f : 1), Signature = signature
        };
        run.Marisa.BeamCooldown = stats.Interval;
    }

    internal static bool Contains(RunState run, Vector2 position, float radius)
    {
        var beam = run.Beam;
        if (beam == null || beam.Warmup > 0) return false;
        var relative = position - run.PlayerPosition;
        var along = Vector2.Dot(relative, beam.Direction);
        var across = Math.Abs(relative.X * beam.Direction.Y - relative.Y * beam.Direction.X);
        return along >= -radius && along <= beam.Length + radius && across <= beam.HalfWidth + radius;
    }

    internal static void Step(RunState run)
    {
        var beam = run.Beam;
        if (beam == null) return;
        if (beam.Warmup > 0)
        {
            beam.Warmup -= RunState.StepSeconds;
            if (beam.Warmup <= 0) run.Emit(EffectKind.Beam, run.PlayerPosition);
            return;
        }
        beam.Remaining -= RunState.StepSeconds;
        if (beam.Remaining <= 0) { run.Beam = null; return; }
        if (beam.Sweep)
        {
            var progress = Math.Clamp(1 - beam.Remaining / beam.Duration, 0, 1);
            beam.Direction = Geometry.Rotate(beam.AimDirection, (progress * 2 - 1) * MarisaTuning.SweepDegrees * MathF.PI / 180);
        }
        beam.PulseTimer -= RunState.StepSeconds;
        if (beam.PulseTimer > 0) return;
        beam.PulseTimer += AbilityTuning.BeamPulse;
        if (beam.ClearsBullets) ClearPulse(run);
        foreach (var enemy in run.Enemies)
            if (enemy.Health > 0 && Contains(run, enemy.Position, enemy.Radius)) run.DamageEnemy(enemy, beam.Damage, Vector2.Zero);
    }

    private static void ClearPulse(RunState run)
    {
        var budget = MarisaTuning.BeamClearLimit;
        foreach (ref var projectile in run.Projectiles.Active)
        {
            if (!projectile.Hostile || projectile.Life <= 0 || !Contains(run, projectile.Position, projectile.Radius)) continue;
            projectile.Life = 0;
            if (--budget == 0) break;
        }
    }
}
