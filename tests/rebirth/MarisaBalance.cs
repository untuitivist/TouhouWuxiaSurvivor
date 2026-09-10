using System.Numerics;
using System.Text.Json;
using Rebirth.Core;
using Rebirth.Diagnostics;
using static Rebirth.Tests.MarisaGrowthTests;

namespace Rebirth.Tests;

internal static class MarisaBalance
{
    private static readonly string[] Routes = ["refine", "stars", "dust", "spark"];
    private static readonly string[] Scenarios = ["distant-single", "moving-single", "dense-cluster", "close-ring"];
    private sealed record ArenaResult(string Route, string Scenario, int Points, double Dps, int PeakProjectiles);

    public static int Run()
    {
        var arena = Routes.SelectMany(route => Scenarios.Select(scenario => Measure(route, scenario))).ToArray();
        var journeys = Routes.Skip(1).SelectMany(route => new[] { 42, 260906, 781 }.Select(seed => Journey(route, seed))).ToArray();
        Console.WriteLine(JsonSerializer.Serialize(new { arena, journeys }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    public static void Guardrails()
    {
        var arena = Routes.SelectMany(route => Scenarios.Select(scenario => Measure(route, scenario))).ToArray();
        MarisaGrowthTests.Check(arena.All(result => result.Points == 6 && result.Dps > 0 && result.PeakProjectiles < 250), "Equal legal budget, useful damage and bounded projectiles");
        double Damage(string route, string scenario) => arena.Single(result => result.Route == route && result.Scenario == scenario).Dps;
        MarisaGrowthTests.Check(Damage("refine", "distant-single") > Damage("stars", "distant-single"), "Pierce and split trade single-target damage for coverage");
        MarisaGrowthTests.Check(Damage("stars", "dense-cluster") > Damage("refine", "dense-cluster"), "Star branches reward grouped targets");
        MarisaGrowthTests.Check(Damage("dust", "close-ring") > Damage("spark", "close-ring"), "Returning rings retain a close-surround niche");
        MarisaGrowthTests.Check(MarisaTuning.PierceDamageMultiplier * (2 + MarisaTuning.FragmentCount * MarisaTuning.FragmentDamageMultiplier) < 2.4f, "Combined split and pierce have a finite per-star damage ceiling");
        MarisaGrowthTests.Check(MarisaTuning.EchoDamageMultiplier * 2 < 1.3f, "Echo is not a free double-damage multiplier");
    }

    private static string[] Nodes(string route) => route switch
    {
        "stars" => [UpgradeCatalog.StarPierce, UpgradeCatalog.StarSplit],
        "dust" => [UpgradeCatalog.StardustUnlock, UpgradeCatalog.StardustRecall, UpgradeCatalog.StardustEcho],
        "spark" => [UpgradeCatalog.MasterSparkUnlock, UpgradeCatalog.SparkSweep, UpgradeCatalog.SparkClear],
        _ => []
    };

    private static ArenaResult Measure(string route, string scenario)
    {
        var run = new RunState(HeroKind.Marisa, 42);
        Learn(run, Nodes(route));
        var art = route == "dust" ? ArtKind.Stardust : route == "spark" ? ArtKind.MasterSpark : ArtKind.Stars;
        var refine = UpgradeCatalog.All.Single(upgrade => upgrade.Ability == art && upgrade.Kind == UpgradeKind.Refine);
        while (run.Build.AllocatedPoints < 6 && run.Build.CanChoose(refine, 100)) MarisaGrowthTests.Check(run.Build.TryApply(refine, 100), "Refinement");
        while (run.Build.AllocatedPoints < 6) MarisaGrowthTests.Check(run.Build.TryApply(UpgradeCatalog.Legacy(ArtKind.Power), 100), "Shared power");
        var count = scenario is "close-ring" or "dense-cluster" ? 12 : 1;
        var targets = Enumerable.Range(0, count).Select(index => Target(run, Vector2.Zero)).ToArray();
        var ids = targets.Select(enemy => enemy.Id).ToHashSet();
        var peak = 0;
        for (var tick = 0; tick < 1800; tick++)
        {
            for (var index = 0; index < count; index++)
                targets[index].Position = scenario switch
                {
                    "close-ring" => Geometry.Angle(index * MathF.Tau / count) * 85,
                    "dense-cluster" => new(340 + index % 4 * 28, -42 + index / 4 * 28),
                    "moving-single" => new(400, MathF.Sin(tick * RunState.StepSeconds * 2.2f) * 180),
                    _ => new(400, 0)
                };
            run.Step(new(Vector2.Zero, true));
            run.Enemies.RemoveAll(enemy => !ids.Contains(enemy.Id));
            peak = Math.Max(peak, run.Projectiles.Count);
        }
        MarisaGrowthTests.Check(run.Ticks == 1800, "Arena measurement advanced for the full thirty seconds");
        return new(route, scenario, run.Build.AllocatedPoints, Math.Round(targets.Sum(enemy => enemy.MaxHealth - enemy.Health) / 30, 2), peak);
    }

    private static object Journey(string route, int seed)
    {
        var run = new RunState(HeroKind.Marisa, seed);
        var desired = Nodes(route);
        var choices = new List<string>();
        for (var tick = 0; tick < 25200 && run.Phase is not (RunPhase.Won or RunPhase.Lost); tick++)
        {
            while (run.Phase == RunPhase.Choosing)
            {
                var index = Enumerable.Range(0, run.Choices.Count).OrderByDescending(candidate => Priority(run.Choices[candidate], desired)).First();
                choices.Add(run.Choices[index].Id);
                MarisaGrowthTests.Check(run.Choose(index), "Pilot chooses an offered legal upgrade");
            }
            run.Step(RunPilot.Input(run, tick));
        }
        return new { route, seed, phase = run.Phase.ToString(), time = Math.Round(run.Time, 1), run.Kills, run.Level, run.Health, firstChoices = choices.Take(12).ToArray() };
    }

    private static int Priority(UpgradeDefinition upgrade, string[] desired)
    {
        var preferred = Array.IndexOf(desired, upgrade.Id);
        if (preferred >= 0) return 100 - preferred;
        if (upgrade.Ability == ArtKind.Vitality) return 65;
        if (upgrade.Id == UpgradeCatalog.FinalSpark) return 60;
        if (upgrade.Ability == ArtKind.Power) return 55;
        if (upgrade.Ability == ArtKind.Haste) return 50;
        if (upgrade.Kind == UpgradeKind.Refine && upgrade.Ability == UpgradeCatalog.Get(desired[0]).Ability) return 45;
        if (upgrade.Ability == ArtKind.Flow) return 40;
        if (upgrade.Kind == UpgradeKind.Refine) return 35;
        if (upgrade.Kind == UpgradeKind.Unlock) return 30;
        return 10;
    }
}
