using System.Numerics;

namespace Rebirth.Core;

internal static class MarisaHerbSystem
{
    internal static void Step(RunState run)
    {
        if (run.Phase != RunPhase.Playing) return;
        var state = run.Marisa;
        if (state.PendingHealing > 0)
        {
            var amount = Math.Min(state.PendingHealing, MarisaTuning.BrewRate * RunState.StepSeconds);
            run.Heal(amount);
            state.PendingHealing -= amount;
        }
        var rank = run.Ranks[(int)ArtKind.Herbs];
        if (rank <= 0) return;
        state.HerbCooldown -= RunState.StepSeconds;
        if (state.HerbCooldown > 0) return;
        state.HerbCooldown = MarisaTuning.HerbInterval;
        var reserve = run.Build.Has(AbilityTraits.HerbReserve);
        var count = 0;
        foreach (ref readonly var pickup in run.Pickups.Active)
            if (pickup.Herbal && !pickup.Collected) count++;
        if (count >= MarisaTuning.HerbLimit + (reserve ? 2 : 0) || run.Pickups.Count >= RunState.PickupLimit) return;
        var heading = run.PlayerVelocity.LengthSquared() > 1 ? Geometry.Direction(run.PlayerVelocity) : run.Facing;
        var position = RunState.ClampToArena(run.PlayerPosition - heading * 42 + new Vector2(-heading.Y, heading.X) * 24);
        run.Pickups.Add(new() { Position = position, Healing = true, Herbal = true,
            Value = (int)AbilityTuning.Get(ArtKind.Herbs, rank).Damage,
            Life = MarisaTuning.HerbLifetime + (reserve ? 16 : 0) });
    }

    internal static void Collect(RunState run, Pickup pickup)
    {
        run.Heal(pickup.Value);
        if (run.Build.Has(AbilityTraits.HerbBrew))
            run.Marisa.PendingHealing = Math.Min(MarisaTuning.BrewLimit, run.Marisa.PendingHealing + pickup.Value * 0.5f);
    }
}
