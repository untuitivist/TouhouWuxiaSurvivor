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
        var ready = stars > 0 && state.ShotCooldown <= 0 || spark > 0 && run.Beam == null && state.BeamCooldown <= 0;
        var target = ready ? MarisaProjectileSystem.FindAttractor(run, run.PlayerPosition, MarisaTuning.TargetRange) : null;
        if (target != null)
        {
            if (stars > 0 && state.ShotCooldown <= 0 && MarisaProjectileSystem.Cast(run, target, stars))
                state.ShotCooldown = MarisaTuning.StarInterval;
            if (spark > 0 && run.Beam == null && state.BeamCooldown <= 0) MarisaBeamSystem.Start(run, false);
        }
        MarisaBeamSystem.Step(run);
        MarisaProjectileSystem.Step(run);
        MarisaHerbSystem.Step(run);
    }
}
