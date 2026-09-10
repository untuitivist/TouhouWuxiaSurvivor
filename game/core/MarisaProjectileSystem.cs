namespace Rebirth.Core;

internal static class MarisaProjectileSystem
{
    internal static void Move(RunState run, ref Projectile projectile)
    {
        if (projectile.RecallRemaining == 0 || projectile.Hostile) return;
        if (projectile.RecallRemaining > 0)
        {
            projectile.RecallRemaining -= RunState.StepSeconds;
            if (projectile.RecallRemaining > 0) return;
            projectile.RecallRemaining = -1;
        }
        var offset = run.PlayerPosition - projectile.Position;
        if (offset.LengthSquared() <= 14 * 14) { projectile.Life = 0; return; }
        projectile.Velocity = Geometry.Direction(offset) * MarisaTuning.RecallSpeed;
    }

    internal static void OnHit(RunState run, ref Projectile projectile, int targetId)
    {
        if (!projectile.StarSplit) return;
        projectile.StarSplit = false;
        var state = run.Marisa;
        if (state.FragmentCount + MarisaTuning.FragmentCount > state.Fragments.Length) return;
        var direction = Geometry.Direction(projectile.Velocity);
        for (var index = 0; index < MarisaTuning.FragmentCount; index++)
        {
            var fragment = new Projectile { Art = ArtKind.Stars, Position = projectile.Position, Radius = 5,
                Velocity = Geometry.Rotate(direction, (index - (MarisaTuning.FragmentCount - 1) / 2f) * MarisaTuning.FragmentSpread) * MarisaTuning.FragmentSpeed,
                Life = MarisaTuning.FragmentRange / MarisaTuning.FragmentSpeed,
                Damage = projectile.Damage * MarisaTuning.FragmentDamageMultiplier };
            fragment.HitIds.Add(targetId);
            state.Fragments[state.FragmentCount++] = fragment;
        }
    }

    internal static void Flush(RunState run)
    {
        var state = run.Marisa;
        for (var index = 0; index < state.FragmentCount; index++) run.AddProjectile(state.Fragments[index]);
        state.FragmentCount = 0;
    }
}
