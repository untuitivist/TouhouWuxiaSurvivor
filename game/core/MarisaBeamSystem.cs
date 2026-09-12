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
        var offset = target == null ? Vector2.Zero : target.Position - run.PlayerPosition;
        var aim = offset.LengthSquared() > 0.0001f ? Geometry.Direction(offset) : run.Facing;
        var steering = run.Build.Has(AbilityTraits.SparkSteer);
        var duration = signature ? 2.4f : stats.Duration;
        run.Beam = new()
        {
            AimDirection = aim, Direction = aim,
            Steering = steering, ClearsBullets = run.Build.Has(AbilityTraits.SparkClear),
            Warmup = AbilityTuning.BeamWarmup, Remaining = duration, Duration = duration,
            HalfWidth = (AbilityTuning.BeamHalfWidth(rank) + (signature ? 16 : 0)) * (run.Build.Has(AbilityTraits.SparkWide) ? MarisaTuning.WideMultiplier : 1),
            Length = signature ? 1100 : stats.Range,
            Damage = stats.Damage * run.Power * (signature ? 1.7f : 1), Signature = signature
        };
        run.Marisa.BeamCooldown = stats.Interval;
    }

    internal static bool Contains(RunState run, Vector2 position, float radius)
    {
        var beam = run.Beam;
        if (beam == null || beam.Warmup > 0) return false;
        return CharacterAttackRules.BeamContains(beam, run.PlayerPosition, position, radius);
    }

    internal static void Step(RunState run)
    {
        var beam = run.Beam;
        if (beam == null) return;
        if (beam.Steering && run.NearestEnemy(run.PlayerPosition, 1200) is { } target)
        {
            var offset = new Vector2(target.Position.X - run.PlayerPosition.X, target.Position.Y - run.PlayerPosition.Y);
            beam.AimDirection = offset.X * offset.X + offset.Y * offset.Y > 0.0001f ? Geometry.Direction(offset) : beam.Direction;
            beam.Direction = CharacterAttackRules.TurnToward(beam.Direction, offset, MarisaTuning.SteeringRadians);
        }
        if (beam.Warmup > 0)
        {
            beam.Warmup -= RunState.StepSeconds;
            if (beam.Warmup <= 0) run.Emit(EffectKind.Beam, run.PlayerPosition);
            return;
        }
        beam.Remaining -= RunState.StepSeconds;
        if (beam.Remaining <= 0) { run.Beam = null; return; }
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
