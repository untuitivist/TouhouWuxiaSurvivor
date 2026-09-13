using System.Numerics;

namespace Rebirth.Core;

public static class BossAbilitySystem
{
    public const int ProjectileBudget = 320;

    internal static void Step(RunState run, Enemy enemy, Vector2 direction, float distance)
    {
        if (enemy.Abilities is not { } state) return;
        state.Elapsed += RunState.StepSeconds;
        var phase = enemy.Health > enemy.MaxHealth * 0.66f ? 0 : enemy.Health > enemy.MaxHealth * 0.33f ? 1 : 2;
        if (phase != state.Phase)
        {
            state.Phase = phase;
            state.Volley = null;
            state.Beam = null;
            state.ShotCooldown = 1.2f;
            state.SpecialCooldown = 3;
            ClearOwned(run, enemy.Id);
        }
        var advance = distance > 360 ? 1 : distance < 260 ? -0.7f : 0;
        var strafe = MathF.Sin(state.Elapsed * 0.45f) * 0.35f;
        enemy.Velocity = (direction * advance + new Vector2(-direction.Y, direction.X) * strafe) * enemy.Speed;
        state.ShotCooldown -= RunState.StepSeconds;
        state.SpecialCooldown -= RunState.StepSeconds;
        if (state.Beam == null && state.Volley == null)
        {
            if (state.Hero == HeroKind.Marisa && state.Phase > 0 && state.SpecialCooldown <= 0)
            {
                ClearOwned(run, enemy.Id);
                state.Beam = new(enemy.Position, direction, state.Phase);
                state.SpecialCooldown = 10;
            }
            else if (state.ShotCooldown <= 0)
                state.Volley = new(enemy.Position, direction, state.Hero, state.Phase, state.VolleyCount++);
        }
        enemy.Telegraph = state.Volley is { Warmup: > 0 } volley ? BossVolleyState.TelegraphSeconds - volley.Warmup
            : state.Beam is { Warmup: > 0 } beam ? BossBeamState.TelegraphSeconds - beam.Warmup : 0;
        if (state.Beam != null || state.Volley != null) enemy.Velocity = Vector2.Zero;
        StepVolley(run, enemy, state);
        StepBeam(run, state);
    }

    private static void ClearOwned(RunState run, int ownerId)
    {
        foreach (ref var projectile in run.Projectiles.Active)
            if (projectile.Hostile && projectile.OwnerId == ownerId) projectile.Life = 0;
    }

    private static void StepVolley(RunState run, Enemy enemy, BossAbilityState state)
    {
        if (state.Volley is not { } volley) return;
        if (volley.Warmup > 0)
        {
            volley.Warmup = Math.Max(0, volley.Warmup - RunState.StepSeconds);
            return;
        }
        volley.WaveTimer -= RunState.StepSeconds;
        if (volley.WaveTimer > 0) return;
        var owned = 0;
        foreach (ref readonly var projectile in run.Projectiles.Active)
            if (projectile.Hostile && projectile.OwnerId == enemy.Id && projectile.Life > 0) owned++;
        if (owned + volley.BulletCount <= ProjectileBudget && run.Projectiles.Count + volley.BulletCount <= RunState.ProjectileLimit)
        {
            for (var index = 0; index < volley.BulletCount; index++)
            {
                var bearing = volley.Bearing(index);
                if (volley.IsGap(bearing)) continue;
                var direction = Geometry.Angle(bearing);
                run.AddProjectile(new()
                {
                    OwnerId = enemy.Id, Hostile = true, Art = volley.Art(volley.WavesFired),
                    Position = volley.Origin + direction * 24, Velocity = direction * volley.Speed(index),
                    Radius = volley.Radius(volley.WavesFired), Damage = 12 + state.Phase * 2, Life = 6,
                    TintIndex = (index + volley.WavesFired) % 8, Alternate = volley.WavesFired % 2 == 1
                });
            }
        }
        volley.WavesFired++;
        volley.WaveTimer = 0.36f;
        if (volley.WavesFired < volley.WaveCount) return;
        state.Volley = null;
        state.ShotCooldown = 2.4f - state.Phase * 0.25f;
    }

    private static void StepBeam(RunState run, BossAbilityState state)
    {
        if (state.Beam is not { } beam) return;
        if (beam.Warmup > 0)
        {
            beam.Warmup = Math.Max(0, beam.Warmup - RunState.StepSeconds);
            return;
        }
        beam.Remaining -= RunState.StepSeconds;
        if (beam.Remaining <= 0)
        {
            state.Beam = null;
            state.ShotCooldown = 1.8f;
            return;
        }
        beam.PulseTimer -= RunState.StepSeconds;
        if (beam.PulseTimer > 0) return;
        beam.PulseTimer += 0.12f;
        if (beam.Contains(run.PlayerPosition, 5)) run.Hurt(beam.Damage);
    }
}
