using System.Numerics;
using Rebirth.Core;
using static Rebirth.Tests.MarisaGrowthTests;

namespace Rebirth.Tests;

internal static class MarisaBalance
{
    private static readonly int[] Investments = [0, 4, 8, 12];
    private static readonly int[] Seeds = [42, 781, 260906];

    internal static RunState Build(HeroKind hero, int points, int seed, bool areaRoute = false)
    {
        var run = new RunState(hero, seed);
        var refine = hero == HeroKind.Reimu ? "reimu.ofuda.refine" : "marisa.stars.refine";
        var earlyArea = areaRoute && hero == HeroKind.Reimu && points >= 4;
        for (var index = 0; index < Math.Min(points, earlyArea ? 2 : 4); index++) Learn(run, refine);
        if (earlyArea) Learn(run, UpgradeCatalog.Homing, UpgradeCatalog.Blast);
        if (points >= 8)
            Learn(run, hero == HeroKind.Reimu
                ? [earlyArea ? refine : UpgradeCatalog.Homing, earlyArea ? refine : UpgradeCatalog.Blast, "base.power", "base.power"]
                : [UpgradeCatalog.StarMass, UpgradeCatalog.StarMass, UpgradeCatalog.StarSpread, UpgradeCatalog.StarLifetime]);
        if (points >= 12)
            Learn(run, hero == HeroKind.Reimu
                ? [UpgradeCatalog.BoundaryUnlock, UpgradeCatalog.Cluster, "reimu.boundary.refine", "reimu.boundary.refine"]
                : [UpgradeCatalog.MasterSparkUnlock, UpgradeCatalog.SparkSteer, UpgradeCatalog.SparkWide, UpgradeCatalog.SparkResonance]);
        MarisaGrowthTests.Check(run.Build.AllocatedPoints == points, "Matched offensive investment");
        return run;
    }

    private static float Measure(HeroKind hero, int points, int seed, bool crowd, float targetDistance = 300, float targetAngularSpeed = 0, bool areaRoute = false)
    {
        var run = Build(hero, points, seed, areaRoute);
        var targets = new List<Enemy>();
        var positions = new List<Vector2>();
        for (var index = 0; index < (crowd ? 12 : 1); index++)
        {
            var position = crowd ? new Vector2(260 + index % 4 * 35, (index / 4 - 1) * 35) : new Vector2(targetDistance, 0);
            var target = Target(run, position, crowd ? EnemyKind.Fairy : EnemyKind.Boss);
            target.Health = target.MaxHealth = 100000;
            targets.Add(target); positions.Add(position);
        }
        var identities = targets.Select(target => target.Id).ToHashSet();
        var warmupDamage = 0f;
        for (var tick = 0; tick < 1800; tick++)
        {
            for (var index = 0; index < targets.Count; index++)
                targets[index].Position = targetAngularSpeed == 0 ? positions[index]
                    : Geometry.Angle(tick * RunState.StepSeconds * targetAngularSpeed) * targetDistance;
            run.Step(default);
            run.Enemies.RemoveAll(enemy => !identities.Contains(enemy.Id));
            MarisaGrowthTests.Check(run.Stars.Count <= MarisaTuning.StarLimit, "Bounded star storage");
            if (tick == 359) warmupDamage = targets.Sum(target => target.MaxHealth - target.Health);
        }
        return (targets.Sum(target => target.MaxHealth - target.Health) - warmupDamage) / 24;
    }

    public static void Guardrails()
    {
        foreach (var points in Investments)
        {
            var reimu = Seeds.Average(seed => Measure(HeroKind.Reimu, points, seed, false));
            var marisa = Seeds.Average(seed => Measure(HeroKind.Marisa, points, seed, false));
            var ratio = marisa / reimu;
            MarisaGrowthTests.Check(float.IsFinite(ratio) && ratio is >= 0.8f and <= 1.2f,
                $"Matched sustained duel DPS: points={points}, Reimu={reimu:0.00}, Marisa={marisa:0.00}, ratio={ratio:0.000}");
        }
    }

    public static int Run()
    {
        foreach (var points in Investments)
        foreach (var hero in Enum.GetValues<HeroKind>())
        {
            var duel = Seeds.Average(seed => Measure(hero, points, seed, false));
            var crowd = Seeds.Average(seed => Measure(hero, points, seed, true));
            Console.WriteLine($"MARISA_BALANCE hero={hero} points={points} duelDps={duel:0.00} crowdDps={crowd:0.00}");
        }
        foreach (var points in Investments)
        foreach (var hero in Enum.GetValues<HeroKind>())
        {
            var near = Seeds.Average(seed => Measure(hero, points, seed, false, targetDistance: 150));
            var far = Seeds.Average(seed => Measure(hero, points, seed, false, targetDistance: 450));
            var moving = Seeds.Average(seed => Measure(hero, points, seed, false, targetAngularSpeed: 0.3f));
            var area = Seeds.Average(seed => Measure(hero, points, seed, true, areaRoute: true));
            Console.WriteLine($"MARISA_CONTEXT hero={hero} points={points} nearDps={near:0.00} farDps={far:0.00} movingDps={moving:0.00} areaRouteDps={area:0.00}");
        }
        Guardrails();
        Console.WriteLine("MARISA_BALANCE_PASS: three-seed, equal-investment sustained DPS; mass-bearing tethered targets, six-second warmup and 24-second sample, not human or mobile validation");
        return 0;
    }
}
