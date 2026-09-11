using System.Numerics;

namespace Rebirth.Core;

internal static class PickupSystem
{
    internal static void Step(RunState run)
    {
        var pickups = run.World.Pickups;
        var attractionRadius = run.PickupRadius * run.PickupRadius;
        for (var index = pickups.Count - 1; index >= 0; index--)
        {
            ref var pickup = ref pickups[index];
            if (pickup.Herbal)
            {
                pickup.Life -= RunState.StepSeconds;
                if (pickup.Life <= 0) { pickup.Collected = true; continue; }
                if (run.Health >= run.MaxHealth) { pickup.Attracted = false; continue; }
            }
            var distance = Geometry.DistanceSquared(pickup.Position, run.PlayerPosition);
            if (distance < attractionRadius) pickup.Attracted = true;
            if (!pickup.Attracted) continue;
            var offset = new Vector2(run.PlayerPosition.X - pickup.Position.X, run.PlayerPosition.Y - pickup.Position.Y);
            pickup.Position = Geometry.Advance(pickup.Position, Geometry.Direction(offset), Math.Min(Geometry.Length(offset), 540 * RunState.StepSeconds));
            if (Geometry.DistanceSquared(pickup.Position, run.PlayerPosition) >= 20 * 20) continue;
            run.CollectPickup(pickup);
            pickup.Collected = true;
        }
        pickups.RemoveAll(static pickup => pickup.Collected);
    }
}
