using System.Numerics;

namespace Rebirth.Core;

public sealed partial class RunState
{
    private void UpdateWeapons()
    {
        if (Hero == HeroKind.Reimu) ReimuAbilitySystem.Step(this);
        else MarisaAbilitySystem.Step(this);
    }

    public bool BeamContains(Vector2 position, float radius = 0)
        => MarisaBeamSystem.Contains(this, position, radius);

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
