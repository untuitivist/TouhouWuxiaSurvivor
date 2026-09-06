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
            var distance = Vector2.DistanceSquared(pickup.Position, run.PlayerPosition);
            if (distance < attractionRadius) pickup.Attracted = true;
            if (!pickup.Attracted) continue;
            var offset = run.PlayerPosition - pickup.Position;
            pickup.Position += Geometry.Direction(offset) * Math.Min(offset.Length(), 540 * RunState.StepSeconds);
            if (Vector2.DistanceSquared(pickup.Position, run.PlayerPosition) >= 20 * 20) continue;
            run.CollectPickup(pickup);
            pickup.Collected = true;
        }
        pickups.RemoveAll(static pickup => pickup.Collected);
    }
}
