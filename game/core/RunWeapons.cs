using System.Numerics;

namespace Rebirth.Core;

public sealed partial class RunState
{

    internal Enemy? NearestEnemy(Vector2 origin, float range, HitHistory excluded = default)
    {
        Enemy? nearest = null;
        var distance = range * range;
        foreach (var enemy in Enemies)
        {
            if (enemy.Health <= 0) continue;
            var candidate = Geometry.DistanceSquared(enemy.Position, origin);
            if (candidate >= distance || excluded.Contains(enemy.Id)) continue;
            distance = candidate;
            nearest = enemy;
        }
        return nearest;
    }

    internal void Graze()
    {
        Grazes++;
        SpellCharge = Math.Min(100, SpellCharge + (Hero == HeroKind.Reimu ? 5 : 3.8f));
        Emit(EffectKind.Graze, PlayerPosition);
    }

    internal void Explode(Vector2 position, float damage)
    {
        const float radius = 72;
        Emit(EffectKind.Explosion, position, radius);
        foreach (var enemy in grid.Query(position, radius + 35))
            if (Vector2.DistanceSquared(position, enemy.Position) < MathF.Pow(radius + enemy.Radius, 2))
                DamageEnemy(enemy, damage, Geometry.Direction(enemy.Position - position) * 12);
    }
}
