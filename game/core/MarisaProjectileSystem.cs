using System.Numerics;

namespace Rebirth.Core;

internal static class MarisaProjectileSystem
{
    internal static Enemy? FindAttractor(RunState run, Vector2 origin, float range)
    {
        Enemy? selected = null;
        var best = float.MaxValue;
        foreach (var enemy in run.World.Grid.Query(origin, range + 36))
        {
            if (enemy.Health <= 0) continue;
            var distance = Geometry.DistanceSquared(enemy.Position, origin);
            if (distance > range * range) continue;
            var mass = enemy.Kind == EnemyKind.Boss ? 16 : enemy.Kind == EnemyKind.Elite ? 3 : 1;
            var score = distance / mass;
            if (score >= best) continue;
            best = score;
            selected = enemy;
        }
        return selected;
    }

    internal static bool Cast(RunState run, Enemy target, int rank)
    {
        var count = MarisaTuning.StarCount(run.Build);
        if (run.Stars.Count + count > MarisaTuning.StarLimit) return false;
        Span<int> groups = stackalloc int[MarisaTuning.GroupLimit];
        var groupCount = 0;
        foreach (ref readonly var star in run.Stars.Active)
        {
            if (star.Life <= 0 || groups[..groupCount].Contains(star.GroupId)) continue;
            if (groupCount == groups.Length) return false;
            groups[groupCount++] = star.GroupId;
        }
        if (groupCount >= MarisaTuning.GroupLimit) return false;
        var planet = run.Build.Has(AbilityTraits.StarPlanet);
        var increasedMass = run.Build.Has(AbilityTraits.StarMass);
        var damage = AbilityTuning.Get(ArtKind.Stars, rank).Damage * run.Power;
        var lifetime = MarisaTuning.Lifetime(run.Build);
        var group = ++run.Marisa.NextGroup;
        for (var index = 0; index < count; index++)
        {
            var angle = index * MathF.Tau / count;
            var mass = MarisaTuning.RollMass(run.Marisa.MassRandom, increasedMass, planet && index == 0);
            run.Stars.Add(new() { Position = run.PlayerPosition + Geometry.Angle(angle) * 16,
                Anchor = target.Position, TargetId = target.Id, GroupId = group, Mass = mass,
                DamageRate = damage, Life = lifetime, Duration = lifetime, PulseTimer = MarisaTuning.StarPulse,
                OrbitAngle = angle, OrbitScale = run.Focused ? 0.35f : 1, Planet = planet && index == 0 });
        }
        return true;
    }

    internal static void Step(RunState run)
    {
        foreach (var enemy in run.Enemies) enemy.GravityVelocity = Vector2.Zero;
        foreach (ref var star in run.Stars.Active)
        {
            star.Life = Math.Max(0, star.Life - RunState.StepSeconds);
            if (star.Life <= 0) continue;
            Move(run, ref star);
            star.Resonating = run.Build.Has(AbilityTraits.SparkResonance) && run.BeamContains(star.Position);
            if (star.Planet && Geometry.DistanceSquared(star.Position, star.Anchor) <= 24 * 24) Pull(run, star);
            star.PulseTimer -= RunState.StepSeconds;
            if (star.PulseTimer > 0) continue;
            star.PulseTimer += MarisaTuning.StarPulse;
            Tear(run, star);
        }
        run.Stars.RemoveAll(static star => star.Life <= 0);
    }

    private static void Move(RunState run, ref StarBody star)
    {
        var destination = star.Anchor;
        if (!star.Planet)
        {
            star.TargetTimer -= RunState.StepSeconds;
            var target = run.World.Grid.FindById(star.TargetId);
            if (target == null || target.Health <= 0 || Geometry.DistanceSquared(target.Position, star.Position) > MarisaTuning.TargetRange * MarisaTuning.TargetRange)
            {
                target = null;
                if (star.TargetTimer <= 0)
                {
                    target = FindAttractor(run, star.Position, MarisaTuning.TargetRange);
                    star.TargetId = target?.Id ?? 0;
                    star.TargetTimer = MarisaTuning.TargetInterval;
                }
            }
            if (target != null) star.Anchor = target.Position;
            star.OrbitAngle += RunState.StepSeconds * 2.5f / MathF.Sqrt(star.Mass);
            destination = star.Anchor + Geometry.Angle(star.OrbitAngle) * (18 + MathF.Sqrt(star.Mass) * 3) * star.OrbitScale;
        }
        var offset = destination - star.Position;
        var speed = star.Planet ? MarisaTuning.PlanetSpeed : MarisaTuning.StarSpeed;
        star.Position = RunState.ClampToArena(star.Position + MarisaTuning.LimitVector(offset, speed * RunState.StepSeconds));
    }

    private static void Pull(RunState run, StarBody star)
    {
        var radius = MarisaTuning.PullRadius(star.Mass);
        foreach (var enemy in run.World.Grid.Query(star.Position, radius + 36))
        {
            var offset = star.Position - enemy.Position;
            var distance = Geometry.Length(offset);
            if (distance <= 8 || distance >= radius) continue;
            var resistance = enemy.Kind == EnemyKind.Boss ? MarisaTuning.BossPullMultiplier : enemy.Kind == EnemyKind.Elite ? 0.45f : 1;
            var strength = MarisaTuning.PullSpeed * Math.Clamp(star.Mass / 10, 0.4f, 1.4f) * (1 - distance / radius) * resistance;
            enemy.GravityVelocity = MarisaTuning.LimitVector(enemy.GravityVelocity + offset / distance * strength, MarisaTuning.PullSpeed * resistance);
        }
    }

    private static void Tear(RunState run, StarBody star)
    {
        var radius = MarisaTuning.DamageRadius(star.Mass);
        var primary = run.World.Grid.FindById(star.TargetId);
        var targets = primary != null && ApplyTear(run, star, primary, radius) ? 1 : 0;
        foreach (var enemy in run.World.Grid.Query(star.Position, radius + 36))
        {
            if (enemy == primary || !ApplyTear(run, star, enemy, radius)) continue;
            if (++targets >= MarisaTuning.StarTargetLimit) break;
        }
    }

    private static bool ApplyTear(RunState run, StarBody star, Enemy enemy, float radius)
    {
        if (enemy.Health <= 0) return false;
        var reach = radius + enemy.Radius;
        var distanceSquared = Geometry.DistanceSquared(star.Position, enemy.Position);
        if (distanceSquared >= reach * reach) return false;
        var falloff = 1 - 0.4f * Math.Clamp(MathF.Sqrt(distanceSquared) / reach, 0, 1);
        var damage = star.DamageRate * MarisaTuning.StarPulse * falloff * (star.Resonating ? MarisaTuning.ResonanceMultiplier : 1);
        run.DamageEnemy(enemy, damage, Vector2.Zero);
        return true;
    }
}
