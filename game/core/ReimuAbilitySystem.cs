using System.Numerics;

namespace Rebirth.Core;

public static class ReimuAbilitySystem
{
    internal static void Step(RunState run)
    {
        var state = run.Reimu;
        state.ShotCooldown -= RunState.StepSeconds * run.CastSpeed;
        var target = run.NearestEnemy(run.PlayerPosition, 950);
        var rank = run.Ranks[(int)ArtKind.Ofuda];
        if (rank > 0 && target != null && state.ShotCooldown <= 0)
        {
            var stats = AbilityTuning.Get(ArtKind.Ofuda, rank);
            var heading = Geometry.Direction(target.Position - run.PlayerPosition);
            var homing = run.Build.Has(AbilityTraits.Homing);
            var side = state.VolleyCount++ % 2 == 0 ? 1 : -1;
            for (var index = 0; index < stats.Count; index++)
                run.AddProjectile(new() { Art = ArtKind.Ofuda, Position = run.PlayerPosition,
                    Velocity = Geometry.Rotate(heading, index == 0 ? 0 : ((index + 1) / 2) * (index % 2 == 1 ? side : -side) * 0.12f) * 510,
                    Damage = stats.Damage * run.Power * (homing ? ReimuTuning.HomingDamageMultiplier : 1), Life = stats.Range / 510 + 0.6f, Radius = 8,
                    TurnRate = homing ? 5.5f : 0, TargetId = target.Id,
                    Blast = run.Build.Has(AbilityTraits.Blast) });
            state.ShotCooldown = stats.Interval;
        }
        UpdateOrbit(run, target);
        UpdateBoundary(run, target);
    }

    public static int OrbitCount(RunState run)
    {
        var rank = run.Ranks[(int)ArtKind.YinYang];
        return rank <= 0 ? 0 : AbilityTuning.Get(ArtKind.YinYang, rank).Count - (run.Reimu.OrbAbsentRemaining > 0 ? 1 : 0);
    }

    public static Vector2 OrbitPosition(RunState run, int index)
    {
        var stats = AbilityTuning.Get(ArtKind.YinYang, run.Ranks[(int)ArtKind.YinYang]);
        return run.PlayerPosition + Geometry.Angle(run.OrbitAngle + index * MathF.Tau / stats.Count) * stats.Range;
    }

    private static void UpdateOrbit(RunState run, Enemy? target)
    {
        var state = run.Reimu;
        state.OrbAbsentRemaining = Math.Max(0, state.OrbAbsentRemaining - RunState.StepSeconds);
        var rank = run.Ranks[(int)ArtKind.YinYang];
        if (rank <= 0) return;
        var stats = AbilityTuning.Get(ArtKind.YinYang, rank);
        if (run.Build.Has(AbilityTraits.Launch))
        {
            state.LaunchCooldown -= RunState.StepSeconds * run.CastSpeed;
            if (!state.Charging && state.OrbAbsentRemaining <= 0 && state.LaunchCooldown <= 0 && target != null)
            {
                state.Charging = true;
                state.ChargeRemaining = ReimuTuning.OrbChargeDuration;
            }
            if (state.Charging)
            {
                state.ChargeRemaining -= RunState.StepSeconds;
                if (state.ChargeRemaining <= 0)
                {
                    state.Charging = false;
                    var position = OrbitPosition(run, stats.Count - 1);
                    var direction = target == null ? run.Facing : Geometry.Direction(target.Position - position);
                    run.AddProjectile(new() { Art = ArtKind.YinYang, Position = position,
                        Velocity = direction * ReimuTuning.OrbLaunchSpeed, Radius = 18, Pierce = 2,
                        Life = ReimuTuning.OrbFlightDuration, Damage = stats.Damage * run.Power * ReimuTuning.OrbDamageMultiplier,
                        ClearBudget = run.Build.Has(AbilityTraits.Clear) ? ReimuTuning.ClearLimit : 0 });
                    state.OrbAbsentRemaining = ReimuTuning.OrbFlightDuration;
                    state.LaunchCooldown = ReimuTuning.OrbLaunchInterval;
                }
            }
        }
        state.OrbitCooldown -= RunState.StepSeconds * run.CastSpeed;
        if (state.OrbitCooldown > 0) return;
        var clearBudget = run.Build.Has(AbilityTraits.Clear) ? ReimuTuning.ClearLimit : 0;
        for (var index = 0; index < OrbitCount(run); index++)
        {
            var position = OrbitPosition(run, index);
            foreach (var enemy in run.World.Grid.Query(position, 75))
                if (Vector2.DistanceSquared(position, enemy.Position) < MathF.Pow(enemy.Radius + 28, 2))
                    run.DamageEnemy(enemy, stats.Damage * run.Power, Geometry.Direction(enemy.Position - run.PlayerPosition) * 9);
            ClearProjectiles(run, position, position, ref clearBudget);
        }
        state.OrbitCooldown = stats.Interval;
    }

    internal static void ClearProjectiles(RunState run, Vector2 start, Vector2 end, ref int budget)
    {
        if (budget <= 0) return;
        foreach (ref var projectile in run.Projectiles.Active)
        {
            if (!projectile.Hostile || projectile.Life <= 0) continue;
            var radius = ReimuTuning.ClearRadius + projectile.Radius;
            if (Geometry.SegmentDistanceSquared(projectile.Position, start, end) > radius * radius) continue;
            projectile.Life = 0;
            if (--budget <= 0) break;
        }
    }

    private static void UpdateBoundary(RunState run, Enemy? target)
    {
        var state = run.Reimu;
        state.FieldCooldown -= RunState.StepSeconds * run.CastSpeed;
        var rank = run.Ranks[(int)ArtKind.Boundary];
        var clustered = run.Build.Has(AbilityTraits.Cluster);
        var range = clustered ? ReimuTuning.ClusterRange : 260;
        if (run.Field == null && rank > 0 && state.FieldCooldown <= 0 && target != null
            && Vector2.DistanceSquared(target.Position, run.PlayerPosition) < range * range)
        {
            var stats = AbilityTuning.Get(ArtKind.Boundary, rank);
            run.Field = new() { Position = clustered ? ClusterPosition(run, stats.Range) : run.PlayerPosition,
                HalfSize = stats.Range, Remaining = stats.Duration, Damage = stats.Damage * run.Power };
            state.FieldCooldown = stats.Interval;
        }
        var field = run.Field;
        if (field == null) return;
        field.Remaining -= RunState.StepSeconds;
        if (field.Remaining <= 0) { run.Field = null; return; }
        field.PulseTimer -= RunState.StepSeconds;
        if (field.PulseTimer > 0) return;
        field.PulseTimer += AbilityTuning.BoundaryPulse;
        foreach (var enemy in run.Enemies)
        {
            var offset = Vector2.Abs(enemy.Position - field.Position);
            if (enemy.Health <= 0 || Math.Max(offset.X, offset.Y) > field.HalfSize + enemy.Radius) continue;
            if (run.Build.Has(AbilityTraits.Bind)) enemy.BoundRemaining = ReimuTuning.BindDuration;
            run.DamageEnemy(enemy, field.Damage, Vector2.Zero);
        }
    }

    private static Vector2 ClusterPosition(RunState run, float halfSize)
    {
        var best = run.PlayerPosition;
        var bestCount = -1;
        var checkedCount = 0;
        foreach (var candidate in run.Enemies)
        {
            if (candidate.Health <= 0 || Vector2.DistanceSquared(candidate.Position, run.PlayerPosition) > ReimuTuning.ClusterRange * ReimuTuning.ClusterRange) continue;
            var count = 0;
            foreach (var neighbor in run.World.Grid.Query(candidate.Position, halfSize * 1.5f + 40))
            {
                var offset = Vector2.Abs(neighbor.Position - candidate.Position);
                if (neighbor.Health > 0 && Math.Max(offset.X, offset.Y) <= halfSize + neighbor.Radius) count++;
            }
            if (count > bestCount) { best = candidate.Position; bestCount = count; }
            if (++checkedCount == 32) break;
        }
        return best;
    }

    internal static void Blast(RunState run, Vector2 position, float damage, int directTargetId)
    {
        run.Emit(EffectKind.Explosion, position, ReimuTuning.BlastRadius);
        var remaining = ReimuTuning.BlastTargetLimit;
        foreach (var enemy in run.World.Grid.Query(position, ReimuTuning.BlastRadius + 40))
        {
            if (enemy.Id == directTargetId || Vector2.DistanceSquared(enemy.Position, position) > MathF.Pow(ReimuTuning.BlastRadius + enemy.Radius, 2)) continue;
            run.DamageEnemy(enemy, damage * ReimuTuning.BlastMultiplier, Vector2.Zero);
            if (--remaining == 0) break;
        }
    }
}
