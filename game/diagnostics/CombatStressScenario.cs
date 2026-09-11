using System.Numerics;
using Rebirth.Core;

namespace Rebirth.Diagnostics;

public sealed class CombatStressScenario
{
    private readonly Random random = new(17);
    public int EnemyCount { get; }
    public int ProjectileCount { get; }

    public CombatStressScenario(RunState run, int enemyCount)
    {
        EnemyCount = Math.Clamp(enemyCount, 40, RunState.EnemyLimit);
        run.Timings = new();
        ProjectileCount = EnemyCount == 320 ? 1600 : EnemyCount == 180 ? 600 : 200;
        var nodes = run.Hero == HeroKind.Reimu
            ? new[] { UpgradeCatalog.BoundaryUnlock, UpgradeCatalog.YinYangUnlock, UpgradeCatalog.Homing, UpgradeCatalog.Blast, UpgradeCatalog.Cluster, UpgradeCatalog.Bind, UpgradeCatalog.Clear, UpgradeCatalog.Launch }
            : new[] { UpgradeCatalog.HerbsUnlock, UpgradeCatalog.MasterSparkUnlock, UpgradeCatalog.StarMass, UpgradeCatalog.StarSpread, UpgradeCatalog.HerbBrew, UpgradeCatalog.SparkSteer, UpgradeCatalog.SparkWide, UpgradeCatalog.SparkResonance };
        foreach (var node in nodes)
            run.Build.TryApply(UpgradeCatalog.Get(node), 100);
        run.Enemies.Clear();
        run.Projectiles.Clear();
        for (var index = 0; index < EnemyCount; index++)
            run.Enemies.Add(new() { Id = index + 10000, Kind = EnemyKind.Kedama, Mass = EnemyMassCatalog.Kedama, Position = new(index % 20 * 60 - 580, index / 20 * 40 - 300), Radius = 14, Health = 1000000, MaxHealth = 1000000 });
        for (var index = 0; index < 400; index++)
            run.Pickups.Add(new() { Position = new(index % 40 * 29 - 580, index / 40 * 62 - 280), Value = 0 });
        Refill(run);
    }

    public void Refill(RunState run)
    {
        RunPilot.ResolveChoices(run);
        foreach (var enemy in run.Enemies) enemy.ContactDamage = 0;
        while (run.Projectiles.Count < ProjectileCount)
        {
            var hostile = random.Next(4) != 0;
            run.Projectiles.Add(new() { Position = new(random.Next(-650, 650), random.Next(-350, 350)), Velocity = Geometry.Angle((float)random.NextDouble() * MathF.Tau) * 200,
                Hostile = hostile, Life = 3, Radius = 5, Damage = 0, Art = ArtKind.Ofuda, TurnRate = hostile ? 0 : 5.5f, Pierce = hostile ? 0 : 2 });
        }
        foreach (ref var projectile in run.Projectiles.Active)
            if (projectile.Hostile) projectile.Damage = 0;
    }
}
