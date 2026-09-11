using System.Numerics;
using Rebirth.Core;
using static Rebirth.Tests.MarisaGrowthTests;

namespace Rebirth.Tests;

internal static class MarisaHerbTests
{
    public static void Lifecycle()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        MarisaHerbSystem.Step(run);
        MarisaGrowthTests.Check(run.Pickups.Count == 0, "No free remedies before unlocking");
        Learn(run, UpgradeCatalog.HerbsUnlock, UpgradeCatalog.HerbReserve);
        for (var index = 0; index < 10; index++) { run.Marisa.HerbCooldown = 0; MarisaHerbSystem.Step(run); }
        MarisaGrowthTests.Check(run.Pickups.Count == 5 && run.Pickups.All(pickup => pickup.Herbal && pickup.Life == 38), "Reserve has a finite five-remedy cap");
        foreach (ref var pickup in run.Pickups.Active) pickup.Position = run.PlayerPosition;
        PickupSystem.Step(run);
        MarisaGrowthTests.Check(run.Pickups.Count == 5, "Full health preserves remedies even inside pickup radius");
        run.Hurt(20);
        PickupSystem.Step(run);
        MarisaGrowthTests.Check(run.Health <= run.MaxHealth && run.Health > run.MaxHealth - 20, "Injured player can collect real healing");
        MarisaGrowthTests.Check(run.Pickups.Count > 0, "No unnecessary consumption once health is full");
        foreach (ref var pickup in run.Pickups.Active) pickup.Life = RunState.StepSeconds / 2;
        PickupSystem.Step(run);
        MarisaGrowthTests.Check(run.Pickups.Count == 0, "Reserved remedies still expire");
        run.Marisa.HerbCooldown = 0; MarisaHerbSystem.Step(run);
        run.TogglePause(); var life = run.Pickups[0].Life; var cooldown = run.Marisa.HerbCooldown;
        for (var tick = 0; tick < 120; tick++) run.Step(default);
        MarisaGrowthTests.Check(run.Pickups[0].Life == life && run.Marisa.HerbCooldown == cooldown, "Pause freezes remedy timers");
    }

    public static void Budget()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        Learn(run, UpgradeCatalog.HerbsUnlock, UpgradeCatalog.HerbBrew);
        run.Hurt(60);
        var remedy = new Pickup { Herbal = true, Healing = true, Value = 6 };
        run.CollectPickup(remedy);
        MarisaGrowthTests.Check(run.Health == 46 && run.Marisa.PendingHealing == 3, "Remedy gives immediate healing and bounded half-dose reserve");
        var before = run.Health;
        for (var tick = 0; tick < 60; tick++) MarisaHerbSystem.Step(run);
        MarisaGrowthTests.Check(Math.Abs(run.Health - before - 2) < 0.01f, "Slow release is limited to two HP per second");
        for (var index = 0; index < 20; index++) run.CollectPickup(remedy);
        MarisaGrowthTests.Check(run.Health == run.MaxHealth && run.Marisa.PendingHealing == MarisaTuning.BrewLimit, "No over-healing or unbounded recovery queue");
        run.Ranks[(int)ArtKind.Haste] = 3;
        run.Marisa.HerbCooldown = 5;
        for (var tick = 0; tick < 60; tick++) MarisaHerbSystem.Step(run);
        MarisaGrowthTests.Check(Math.Abs(run.Marisa.HerbCooldown - 4) < 0.001f, "Haste cannot accelerate remedy production");
        var full = new RunState(HeroKind.Marisa, 42); Learn(full, UpgradeCatalog.HerbsUnlock);
        for (var index = 0; index < RunState.PickupLimit; index++) full.Pickups.Add(new() { Position = new(500, 0) });
        MarisaHerbSystem.Step(full);
        MarisaGrowthTests.Check(full.Pickups.Count == RunState.PickupLimit, "Herbs respect the shared pickup limit");
    }
}
