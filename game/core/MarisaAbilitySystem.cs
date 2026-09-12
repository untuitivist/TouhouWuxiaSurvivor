namespace Rebirth.Core;

internal static class MarisaAbilitySystem
{
    internal static void Step(RunState run)
    {
        var state = run.Marisa;
        state.ShotCooldown -= RunState.StepSeconds * run.CastSpeed;
        if (run.Beam == null) state.BeamCooldown -= RunState.StepSeconds * run.CastSpeed;
        var stars = run.Ranks[(int)ArtKind.Stars];
        var spark = run.Ranks[(int)ArtKind.MasterSpark];
        if (stars > 0 && state.ShotCooldown <= 0)
        {
            MarisaProjectileSystem.Cast(run, stars);
            state.ShotCooldown = Math.Max(0, state.ShotCooldown + MarisaTuning.StarInterval);
        }
        if (spark > 0 && run.Beam == null && state.BeamCooldown <= 0 && run.NearestEnemy(run.PlayerPosition, 1200) != null)
            MarisaBeamSystem.Start(run, false);
        MarisaBeamSystem.Step(run);
        MarisaHerbSystem.Step(run);
    }
}
