using System.Numerics;
using Rebirth.Core;
using static Rebirth.Tests.MarisaGrowthTests;

namespace Rebirth.Tests;

internal static class MarisaBalance
{
    private static readonly string[] Routes = ["base", "damage", "gravity", "spark", "herbs"];

    private static RunState Build(string route, int seed)
    {
        var run = new RunState(HeroKind.Marisa, seed);
        switch (route)
        {
            case "damage":
                for (var index = 0; index < 4; index++) Learn(run, "marisa.stars.refine");
                break;
            case "gravity":
                Learn(run, UpgradeCatalog.StarMass, UpgradeCatalog.StarSpread, UpgradeCatalog.StarPlanet, UpgradeCatalog.StarLifetime);
                break;
            case "spark":
                Learn(run, UpgradeCatalog.MasterSparkUnlock, UpgradeCatalog.SparkSteer, UpgradeCatalog.SparkWide, UpgradeCatalog.SparkResonance);
                break;
            case "herbs":
                Learn(run, UpgradeCatalog.HerbsUnlock, "marisa.herbs.refine", "marisa.herbs.refine", UpgradeCatalog.HerbBrew);
                break;
        }
        MarisaGrowthTests.Check(run.Build.AllocatedPoints == (route == "base" ? 0 : 4), "Equal four-point comparisons");
        return run;
    }

    private static float Measure(string route, int count, int seed)
    {
        var run = Build(route, seed);
        var targets = new List<Enemy>();
        for (var index = 0; index < count; index++)
        {
            var target = Target(run, new(260 + index % 4 * 40, (index / 4 - 1) * 42));
            target.Kind = EnemyKind.Kedama;
            target.Health = target.MaxHealth = 100000;
            targets.Add(target);
        }
        for (var tick = 0; tick < 600; tick++)
        {
            EnemySystem.Step(run);
            run.World.Grid.Rebuild(run.Enemies);
            MarisaAbilitySystem.Step(run);
            MarisaGrowthTests.Check(run.Stars.Count <= MarisaTuning.StarLimit && run.Stars.All(star => float.IsFinite(star.Mass) && star.Mass > 0), "Entity budget and valid independent masses hold at equal points");
        }
        return targets.Sum(target => target.MaxHealth - target.Health) / 10;
    }

    public static void Guardrails()
    {
        foreach (var seed in new[] { 42, 781, 260906 })
        {
            var baseline = Measure("base", 1, seed);
            var damage = Measure("damage", 1, seed);
            var herbs = Measure("herbs", 1, seed);
            MarisaGrowthTests.Check(baseline > 20 && baseline < 100, "Starting sustained DPS is nonzero and bounded");
            MarisaGrowthTests.Check(damage > baseline * 1.5f && damage < baseline * 2.1f, "Damage ranks have visible, bounded value");
            MarisaGrowthTests.Check(Math.Abs(herbs - baseline) < 0.1f, "Healing does not secretly add damage");
            foreach (var route in Routes)
            {
                var crowd = Measure(route, 12, seed);
                MarisaGrowthTests.Check(float.IsFinite(crowd) && crowd > 20 && crowd < 1800, "Crowd damage remains finite: " + route);
            }
        }
    }

    public static int Run()
    {
        Guardrails();
        foreach (var seed in new[] { 42, 781, 260906 })
        foreach (var route in Routes)
            Console.WriteLine($"MARISA_BALANCE seed={seed} route={route} points={(route == "base" ? 0 : 4)} duelDps={Measure(route, 1, seed):0.00} crowdDps={Measure(route, 12, seed):0.00}");
        Console.WriteLine("MARISA_BALANCE_PASS: deterministic stationary targets; not a substitute for player or mobile testing");
        return 0;
    }
}
