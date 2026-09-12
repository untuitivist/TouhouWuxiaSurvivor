using System.Numerics;

namespace Rebirth.Core;

public static class CharacterAttackRules
{
    internal static void FireOfuda(RunState run, Vector2 origin, Vector2 heading, int count, float speed,
        float damage, float life, float turnRate, bool blast, int targetId, int volley, int ownerId = 0, int pierce = 0)
    {
        var side = volley % 2 == 0 ? 1 : -1;
        for (var index = 0; index < count; index++)
        {
            var angle = index == 0 ? 0 : ((index + 1) / 2) * (index % 2 == 1 ? side : -side) * 0.12f;
            run.AddProjectile(new() { Art = ArtKind.Ofuda, Position = origin, Velocity = Geometry.Rotate(heading, angle) * speed,
                Damage = damage, Life = life, Radius = ownerId == 0 ? 8 : 6, TurnRate = turnRate,
                TargetId = targetId, Blast = blast, Hostile = ownerId != 0, OwnerId = ownerId, Pierce = pierce });
        }
    }

    public static Vector2 TurnToward(Vector2 direction, Vector2 targetOffset, float radiansPerSecond)
    {
        if (targetOffset.LengthSquared() <= 0.0001f) return direction;
        var heading = MathF.Atan2(direction.Y, direction.X);
        var delta = Geometry.AngleDelta(heading, MathF.Atan2(targetOffset.Y, targetOffset.X));
        return Geometry.Angle(heading + Math.Clamp(delta, -radiansPerSecond * RunState.StepSeconds, radiansPerSecond * RunState.StepSeconds));
    }

    public static bool BeamContains(BeamState beam, Vector2 origin, Vector2 position, float radius)
    {
        if (beam.Warmup > 0 || beam.Remaining <= 0) return false;
        var relativeX = position.X - origin.X;
        var relativeY = position.Y - origin.Y;
        var along = relativeX * beam.Direction.X + relativeY * beam.Direction.Y;
        var across = Math.Abs(relativeX * beam.Direction.Y - relativeY * beam.Direction.X);
        return along >= -radius && along <= beam.Length + radius && across <= beam.HalfWidth + radius;
    }
}
