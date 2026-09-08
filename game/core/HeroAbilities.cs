using System.Numerics;

namespace Rebirth.Core;

public sealed partial class RunState
{
    private void UpdateWeapons()
    {
        primaryTimer -= StepSeconds * CastSpeed;
        var target = NearestEnemy(PlayerPosition, 950);
        if (Hero == HeroKind.Reimu)
        {
            ReimuAbilitySystem.Step(this);
        }
        else
        {
            var rank = Ranks[(int)ArtKind.Stars];
            if (rank > 0 && target != null && primaryTimer <= 0)
            {
                CastStars(target, rank);
                primaryTimer = AbilityTuning.Get(ArtKind.Stars, rank).Interval;
            }
            stardustTimer -= StepSeconds * CastSpeed;
            rank = Ranks[(int)ArtKind.Stardust];
            if (rank > 0 && target != null && stardustTimer <= 0)
            {
                var stats = AbilityTuning.Get(ArtKind.Stardust, rank);
                for (var index = 0; index < stats.Count; index++)
                    AddProjectile(new() { Art = ArtKind.Stardust, Position = PlayerPosition, Velocity = Geometry.Angle(Time + index * MathF.Tau / stats.Count) * 215, Damage = stats.Damage * Power, Life = stats.Range / 215, Radius = 8, Pierce = 1 });
                stardustTimer = stats.Interval;
            }
            if (Beam == null)
            {
                beamCooldown -= StepSeconds * CastSpeed;
                if (Ranks[(int)ArtKind.MasterSpark] > 0 && target != null && beamCooldown <= 0) StartBeam(false);
            }
            UpdateBeam();
        }
    }



    private void CastStars(Enemy target, int rank)
    {
        var stats = AbilityTuning.Get(ArtKind.Stars, rank);
        var heading = Geometry.Direction(target.Position - PlayerPosition);
        var spread = Focused ? 0.045f : 0.16f;
        for (var index = 0; index < stats.Count; index++)
            AddProjectile(new() { Art = ArtKind.Stars, Position = PlayerPosition, Velocity = Geometry.Rotate(heading, (index - (stats.Count - 1) / 2f) * spread) * 620, Damage = stats.Damage * Power, Life = stats.Range / 620, Radius = 8 });
    }



    private void StartBeam(bool signature)
    {
        var rank = Math.Max(1, Ranks[(int)ArtKind.MasterSpark]);
        var stats = AbilityTuning.Get(ArtKind.MasterSpark, rank);
        var target = NearestEnemy(PlayerPosition, 1200);
        Beam = new()
        {
            Direction = target == null ? Facing : Geometry.Direction(target.Position - PlayerPosition),
            Warmup = AbilityTuning.BeamWarmup,
            Remaining = signature ? 2.4f : stats.Duration,
            HalfWidth = AbilityTuning.BeamHalfWidth(rank) + (signature ? 16 : 0),
            Length = signature ? 1100 : stats.Range,
            Damage = stats.Damage * Power * (signature ? 1.7f : 1),
            Signature = signature
        };
        beamCooldown = stats.Interval;
    }

    public bool BeamContains(Vector2 position, float radius = 0)
    {
        if (Beam == null || Beam.Warmup > 0) return false;
        var relative = position - PlayerPosition;
        var along = Vector2.Dot(relative, Beam.Direction);
        var across = Math.Abs(relative.X * Beam.Direction.Y - relative.Y * Beam.Direction.X);
        return along >= -radius && along <= Beam.Length + radius && across <= Beam.HalfWidth + radius;
    }

    private void UpdateBeam()
    {
        if (Beam == null) return;
        if (Beam.Warmup > 0)
        {
            Beam.Warmup -= StepSeconds;
            if (Beam.Warmup <= 0) Emit(EffectKind.Beam, PlayerPosition);
            return;
        }
        Beam.Remaining -= StepSeconds;
        if (Beam.Remaining <= 0) { Beam = null; return; }
        Projectiles.RemoveAll(projectile => projectile.Hostile && BeamContains(projectile.Position, projectile.Radius));
        Beam.PulseTimer -= StepSeconds;
        if (Beam.PulseTimer > 0) return;
        Beam.PulseTimer += AbilityTuning.BeamPulse;
        foreach (var enemy in Enemies)
            if (BeamContains(enemy.Position, enemy.Radius)) DamageEnemy(enemy, Beam.Damage, Vector2.Zero);
    }

    private void CastDreamSeal()
    {
        var targets = Enemies.Where(enemy => enemy.Health > 0).OrderBy(enemy => Vector2.DistanceSquared(enemy.Position, PlayerPosition)).Take(7).ToArray();
        for (var index = 0; index < 7; index++)
        {
            var target = targets.Length > 0 ? targets[index % targets.Length] : null;
            AddProjectile(new() { Art = ArtKind.Ofuda, DreamOrb = true, TintIndex = index, Position = PlayerPosition, Velocity = Geometry.Angle(index * MathF.Tau / 7) * 430, Radius = 14, Life = 4, Damage = (55 + Level * 5) * Power, TurnRate = 7, TargetId = target?.Id ?? 0 });
        }
    }
}
