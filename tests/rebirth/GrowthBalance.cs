using System.Numerics;
using System.Text.Json;
using Rebirth.Core;
using Rebirth.Diagnostics;

namespace Rebirth.Tests;

internal static class GrowthBalance
{
    private static readonly string[] Ofuda = [UpgradeCatalog.Homing, UpgradeCatalog.Blast];
    private static readonly string[] Boundary = [UpgradeCatalog.BoundaryUnlock, UpgradeCatalog.Cluster, UpgradeCatalog.Bind];
    private static readonly string[] Orbit = [UpgradeCatalog.YinYangUnlock, UpgradeCatalog.Launch, UpgradeCatalog.Clear];

    public static int Run()
    {
        var arena = new List<object>();
        foreach (var scenario in new[] { "distant-single", "moving-single", "dense-cluster", "close-ring" })
        foreach (var route in new[] { "refine", "ofuda", "boundary", "orbit" })
            arena.Add(MeasureArena(route, scenario));
        var journeys = new List<object>();
        foreach (var route in new[] { "ofuda", "boundary", "orbit" })
        foreach (var seed in new[] { 42, 260906, 781, 108, 999, 2026 })
            journeys.Add(MeasureJourney(route, seed));
        Console.WriteLine(JsonSerializer.Serialize(new { tuning = new { ReimuTuning.BlastMultiplier, ReimuTuning.BlastRadius, ReimuTuning.OrbDamageMultiplier, ReimuTuning.BindDuration }, arena, journeys }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private sealed record ArenaResult(string route, string scenario, int points, double damagePerSecond, float health);

    public static void Guardrails()
    {
        var straight = MeasureArena("refine", "distant-single").damagePerSecond;
        var ofudaCrowd = MeasureArena("ofuda", "dense-cluster").damagePerSecond;
        var fieldCrowd = MeasureArena("boundary", "dense-cluster").damagePerSecond;
        var orbitClose = MeasureArena("orbit", "close-ring").damagePerSecond;
        var ofudaClose = MeasureArena("ofuda", "close-ring").damagePerSecond;
        if (straight <= 50 || ofudaCrowd > fieldCrowd * 1.25 || orbitClose < ofudaClose * 1.1)
            throw new InvalidOperationException($"Route niches regressed: straight={straight}, crowd={ofudaCrowd}/{fieldCrowd}, close={orbitClose}/{ofudaClose}");
    }

    private static ArenaResult MeasureArena(string route, string scenario)
    {
        var run = new RunState(HeroKind.Reimu, 42);
        var nodes = route switch
        {
            "ofuda" => Ofuda,
            "boundary" => Boundary,
            "orbit" => Orbit,
            _ => Array.Empty<string>()
        };
        foreach (var node in nodes) Apply(run, UpgradeCatalog.Get(node));
        var ability = route == "boundary" ? ArtKind.Boundary : route == "orbit" ? ArtKind.YinYang : ArtKind.Ofuda;
        var refine = UpgradeCatalog.All.Single(upgrade => upgrade.Ability == ability && upgrade.Kind == UpgradeKind.Refine);
        while (run.Build.AllocatedPoints < 6 && run.Build.CanChoose(refine, 100)) Apply(run, refine);
        while (run.Build.AllocatedPoints < 6) Apply(run, UpgradeCatalog.Legacy(ArtKind.Power));
        var targets = new List<Enemy>();
        var count = scenario is "close-ring" or "dense-cluster" ? 12 : 1;
        for (var index = 0; index < count; index++)
        {
            var enemy = run.SpawnEnemy(EnemyKind.Elite, Vector2.Zero);
            enemy.Health = enemy.MaxHealth = 1000000;
            enemy.Speed = 0;
            enemy.Timer = 10000;
            targets.Add(enemy);
        }
        var ids = targets.Select(enemy => enemy.Id).ToHashSet();
        for (var tick = 0; tick < 1800; tick++)
        {
            for (var index = 0; index < targets.Count; index++)
                targets[index].Position = scenario switch
                {
                    "close-ring" => Geometry.Angle(index * MathF.Tau / count) * 85,
                    "dense-cluster" => new(340 + index % 4 * 28, -42 + index / 4 * 28),
                    "moving-single" => new(400, MathF.Sin(tick * RunState.StepSeconds * 2.2f) * 180),
                    _ => new(400, 0)
                };
            run.Step(default);
            run.Enemies.RemoveAll(enemy => !ids.Contains(enemy.Id));
        }
        return new(route, scenario, run.Build.AllocatedPoints, Math.Round(targets.Sum(enemy => enemy.MaxHealth - enemy.Health) / 30, 2), run.Health);
    }

    private static object MeasureJourney(string route, int seed)
    {
        var run = new RunState(HeroKind.Reimu, seed);
        var desired = route == "ofuda" ? Ofuda : route == "boundary" ? Boundary : Orbit;
        var snapshots = new List<object>();
        var choices = new List<string>();
        for (var tick = 0; tick < 25200 && run.Phase is not (RunPhase.Won or RunPhase.Lost); tick++)
        {
            while (run.Phase == RunPhase.Choosing)
            {
                var selected = Enumerable.Range(0, run.Choices.Count).OrderByDescending(index => Priority(run.Choices[index], desired)).First();
                choices.Add(run.Choices[selected].Id);
                if (!run.Choose(selected)) throw new InvalidOperationException("Invalid balance choice");
            }
            run.Step(RunPilot.Input(run, tick));
            if (run.Ticks % 3600 == 0) snapshots.Add(new { time = run.Time, run.Kills, run.Level, run.Health });
        }
        return new { route, seed, phase = run.Phase.ToString(), time = Math.Round(run.Time, 1), run.Kills, run.Health, run.Level, snapshots, firstChoices = choices.Take(12).ToArray() };
    }

    private static int Priority(UpgradeDefinition upgrade, string[] desired)
    {
        var preferred = Array.IndexOf(desired, upgrade.Id);
        if (preferred >= 0) return 100 - preferred;
        if (upgrade.Ability == ArtKind.Vitality) return 60;
        if (upgrade.Id == UpgradeCatalog.DreamSeal) return 55;
        if (upgrade.Ability == ArtKind.Power) return 50;
        if (upgrade.Ability == ArtKind.Haste) return 45;
        if (upgrade.Kind == UpgradeKind.Refine && upgrade.Ability == UpgradeCatalog.Get(desired[0]).Ability) return 40;
        if (upgrade.Ability == ArtKind.Flow) return 35;
        if (upgrade.Kind == UpgradeKind.Refine) return 30;
        if (upgrade.Kind == UpgradeKind.Unlock) return 20;
        return 10;
    }

    private static void Apply(RunState run, UpgradeDefinition upgrade)
    {
        if (!run.Build.TryApply(upgrade, 100)) throw new InvalidOperationException("Invalid balance build " + upgrade.Id);
    }
}
