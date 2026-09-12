using Rebirth.Core;
using Rebirth.Diagnostics;

namespace Rebirth.Tests;

internal static class LongRunBalance
{
    public static int Run()
    {
        var failures = 0;
        foreach (var stance in Enum.GetValues<MainlineStance>().Where(stance => stance != MainlineStance.None))
        {
            var hero = stance is MainlineStance.RapidOfuda or MainlineStance.ChargedOfuda ? HeroKind.Reimu : HeroKind.Marisa;
            var wins = 0;
            foreach (var seed in new[] { 42, 260906, 781 })
            {
                var run = new RunState(hero, seed);
                var bossChoices = -1;
                for (var tick = 0; tick < 24 * 60 / RunState.StepSeconds && run.Phase is not (RunPhase.Won or RunPhase.Lost); tick++)
                {
                    RunPilot.ResolveChoices(run, stance);
                    run.Step(RunPilot.Input(run, tick));
                    if (run.BossSpawned && bossChoices < 0) bossChoices = run.Build.AllocatedPoints;
                }
                if (run.Phase == RunPhase.Won) wins++;
                if (bossChoices >= 0 && bossChoices is not (>= 20 and <= 26)) failures++;
                Console.WriteLine($"LONG_BALANCE hero={hero} stance={stance} seed={seed} result={run.Phase} time={run.Time:0.0} choices={run.Build.AllocatedPoints} bossChoices={bossChoices} health={run.Health:0.0} kills={run.Kills} ranks={string.Join(',', run.Ranks)} traits={run.Build.Traits}");
            }
            if (wins == 0) failures++;
            Console.WriteLine($"LONG_ROUTE stance={stance} wins={wins}/3 healingAssisted=false");
        }
        Console.WriteLine($"LONG_BALANCE failures={failures}; automated input is not human playtesting or proof of equal win rates");
        return failures == 0 ? 0 : 1;
    }
}
