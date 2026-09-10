using System.Numerics;

namespace Rebirth.Core;

internal static class MarisaAbilitySystem
{
    internal static void Step(RunState run)
    {
        var state = run.Marisa;
        state.ShotCooldown -= RunState.StepSeconds * run.CastSpeed;
        state.StardustCooldown -= RunState.StepSeconds * run.CastSpeed;
        if (run.Beam == null) state.BeamCooldown -= RunState.StepSeconds * run.CastSpeed;
        if (state.EchoCount > 0)
        {
            state.EchoRemaining -= RunState.StepSeconds;
            if (state.EchoRemaining <= 0)
            {
                EmitRing(run, state.EchoTemplate, state.EchoCount, state.EchoAngle);
                state.EchoCount = 0;
            }
        }
        var stars = run.Ranks[(int)ArtKind.Stars];
        var dust = run.Ranks[(int)ArtKind.Stardust];
        var spark = run.Ranks[(int)ArtKind.MasterSpark];
        var ready = stars > 0 && state.ShotCooldown <= 0 || dust > 0 && state.StardustCooldown <= 0
            || spark > 0 && run.Beam == null && state.BeamCooldown <= 0;
        var target = ready ? run.NearestEnemy(run.PlayerPosition, 950) : null;
        if (target != null)
        {
            if (stars > 0 && state.ShotCooldown <= 0) CastStars(run, target, stars);
            if (dust > 0 && state.StardustCooldown <= 0) CastStardust(run, dust);
            if (spark > 0 && run.Beam == null && state.BeamCooldown <= 0) MarisaBeamSystem.Start(run, false);
        }
        MarisaBeamSystem.Step(run);
    }

    private static void CastStars(RunState run, Enemy target, int rank)
    {
        var stats = AbilityTuning.Get(ArtKind.Stars, rank);
        var heading = Geometry.Direction(target.Position - run.PlayerPosition);
        var spread = run.Focused ? 0.045f : 0.16f;
        var pierce = run.Build.Has(AbilityTraits.StarPierce);
        for (var index = 0; index < stats.Count; index++)
            run.AddProjectile(new() { Art = ArtKind.Stars, Position = run.PlayerPosition,
                Velocity = Geometry.Rotate(heading, (index - (stats.Count - 1) / 2f) * spread) * MarisaTuning.StarSpeed,
                Damage = stats.Damage * run.Power * (pierce ? MarisaTuning.PierceDamageMultiplier : 1),
                Pierce = pierce ? 1 : 0, StarSplit = run.Build.Has(AbilityTraits.StarSplit),
                Life = stats.Range / MarisaTuning.StarSpeed, Radius = 8 });
        run.Marisa.ShotCooldown = stats.Interval;
    }

    private static void CastStardust(RunState run, int rank)
    {
        var stats = AbilityTuning.Get(ArtKind.Stardust, rank);
        var recall = run.Build.Has(AbilityTraits.StardustRecall);
        var echo = run.Build.Has(AbilityTraits.StardustEcho);
        var template = new Projectile { Art = ArtKind.Stardust, Position = run.PlayerPosition,
            Damage = stats.Damage * run.Power * (recall ? MarisaTuning.RecallDamageMultiplier : 1)
                * (echo ? MarisaTuning.EchoDamageMultiplier : 1),
            Life = stats.Range / MarisaTuning.StardustSpeed, Radius = 8, Pierce = recall ? 2 : 1,
            RecallRemaining = recall ? MarisaTuning.RecallDelay : 0 };
        EmitRing(run, template, stats.Count, run.Time);
        if (echo)
        {
            run.Marisa.EchoRemaining = MarisaTuning.EchoDelay;
            run.Marisa.EchoTemplate = template;
            run.Marisa.EchoCount = stats.Count;
            run.Marisa.EchoAngle = run.Time + MathF.PI / stats.Count;
        }
        run.Marisa.StardustCooldown = stats.Interval;
    }

    private static void EmitRing(RunState run, Projectile template, int count, float angle)
    {
        for (var index = 0; index < count; index++)
        {
            template.Velocity = Geometry.Angle(angle + index * MathF.Tau / count) * MarisaTuning.StardustSpeed;
            run.AddProjectile(template);
        }
    }
}
