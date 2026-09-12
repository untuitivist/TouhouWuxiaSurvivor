using System.Numerics;

namespace Rebirth.Core;

public static class BossAbilitySystem
{
    public const int StarLimit = 24;
    public const float BeamWarmup = 1.15f;
    public const float BeamTurnRate = MathF.PI / 5;

    internal static void Step(RunState run, Enemy enemy, Vector2 direction, float distance)
    {
        if (enemy.Abilities is not { } state) return;
        state.Elapsed += RunState.StepSeconds;
        state.Phase = enemy.Health > enemy.MaxHealth * 0.66f ? 0 : enemy.Health > enemy.MaxHealth * 0.33f ? 1 : 2;
        var advance = distance > 360 ? 1 : distance < 260 ? -0.7f : 0;
        var strafe = MathF.Sin(state.Elapsed * 0.45f) * 0.35f;
        enemy.Velocity = (direction * advance + new Vector2(-direction.Y, direction.X) * strafe) * enemy.Speed;
        state.ShotCooldown -= RunState.StepSeconds;
        state.SpecialCooldown -= RunState.StepSeconds;
        state.FieldCooldown -= RunState.StepSeconds;
        state.OrbitPulse -= RunState.StepSeconds;
        if (state.Hero == HeroKind.Reimu) StepReimu(run, enemy, state, direction);
        else StepMarisa(run, enemy, state, direction);
    }

    private static void StepReimu(RunState run, Enemy enemy, BossAbilityState state, Vector2 direction)
    {
        enemy.Telegraph = state.ShotCooldown is > 0 and < 0.65f ? 0.65f - state.ShotCooldown : 0;
        if (state.ShotCooldown <= 0)
        {
            CharacterAttackRules.FireOfuda(run, enemy.Position, direction, 3 + state.Phase * 2, 180 + state.Phase * 15,
                14, 3.2f, state.Phase > 0 ? 0.65f : 0, false, 0, state.VolleyCount++, enemy.Id);
            state.ShotCooldown = 1.65f - state.Phase * 0.16f;
        }
        if (state.Phase > 0 && state.Field == null && state.FieldCooldown <= 0)
        {
            state.Field = new() { Position = run.PlayerPosition, HalfSize = 105, Warmup = 1.1f, Remaining = 3, Damage = 1.4f };
            state.FieldCooldown = 8;
        }
        if (state.Field is { } field)
        {
            if (field.Warmup > 0) field.Warmup = Math.Max(0, field.Warmup - RunState.StepSeconds);
            else
            {
                field.Remaining -= RunState.StepSeconds;
                field.PulseTimer -= RunState.StepSeconds;
                if (field.PulseTimer <= 0)
                {
                    field.PulseTimer += AbilityTuning.BoundaryPulse;
                    var offset = Vector2.Abs(run.PlayerPosition - field.Position);
                    if (Math.Max(offset.X, offset.Y) < field.HalfSize + 5)
                    {
                        run.HurtContinuous(field.Damage);
                        if (run.DashDuration <= 0) run.PlayerBoundRemaining = Math.Max(run.PlayerBoundRemaining, 0.2f);
                    }
                }
                if (field.Remaining <= 0) state.Field = null;
            }
        }
        if (state.Phase < 2 || state.OrbitPulse > 0) return;
        state.OrbitPulse = 0.55f;
        var clearBudget = 3;
        for (var index = 0; index < 3; index++)
        {
            var position = state.OrbitPosition(enemy.Position, index);
            if (Geometry.DistanceSquared(position, run.PlayerPosition) < 30 * 30) run.Hurt(12);
            foreach (ref var projectile in run.Projectiles.Active)
            {
                if (clearBudget <= 0) break;
                if (projectile.Hostile || projectile.Life <= 0 || Geometry.DistanceSquared(projectile.Position, position) > 24 * 24) continue;
                projectile.Life = 0;
                clearBudget--;
            }
        }
    }

    private static void StepMarisa(RunState run, Enemy enemy, BossAbilityState state, Vector2 direction)
    {
        state.RecoveryCooldown -= RunState.StepSeconds;
        if (state.RecoveryRemaining > 0)
        {
            enemy.Velocity *= 0.25f;
            state.RecoveryRemaining -= RunState.StepSeconds;
            if (state.RecoveryRemaining <= 0) enemy.Health = Math.Min(enemy.MaxHealth, enemy.Health + enemy.MaxHealth * 0.04f);
            return;
        }
        if (state.Beam == null && state.RemediesUsed < 2 && state.RecoveryCooldown <= 0 && enemy.Health < enemy.MaxHealth * 0.8f)
        {
            state.RemediesUsed++;
            state.RecoveryRemaining = 2.4f;
            state.RecoveryCooldown = 18;
            return;
        }
        if (state.ShotCooldown <= 0)
        {
            MarisaProjectileSystem.CastFrom(run, enemy.Position, state.Build, state.Marisa, state.Build.Ranks[(int)ArtKind.Stars], 0.8f, enemy.Id);
            state.ShotCooldown = 0.42f - state.Phase * 0.045f;
        }
        if (state.Phase > 0 && state.Beam == null && state.SpecialCooldown <= 0)
        {
            state.Beam = new() { Direction = direction, AimDirection = direction, Steering = true,
                Warmup = BeamWarmup, Remaining = 2.4f, Duration = 2.4f, Length = 900,
                HalfWidth = state.Phase == 2 ? 45 : 32, Damage = 18 };
            state.SpecialCooldown = 8.5f;
        }
        var beam = state.Beam;
        if (beam == null) return;
        enemy.Velocity *= 0.25f;
        beam.AimDirection = direction;
        beam.Direction = CharacterAttackRules.TurnToward(beam.Direction, run.PlayerPosition - enemy.Position, BeamTurnRate);
        if (beam.Warmup > 0)
        {
            beam.Warmup = Math.Max(0, beam.Warmup - RunState.StepSeconds);
            return;
        }
        beam.Remaining -= RunState.StepSeconds;
        if (beam.Remaining <= 0) { state.Beam = null; return; }
        beam.PulseTimer -= RunState.StepSeconds;
        if (beam.PulseTimer > 0) return;
        beam.PulseTimer += AbilityTuning.BeamPulse;
        if (CharacterAttackRules.BeamContains(beam, enemy.Position, run.PlayerPosition, 5)) run.Hurt(beam.Damage);
    }
}
