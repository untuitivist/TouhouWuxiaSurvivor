using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void PrepareGrowthCombatPreview(HeroKind hero = HeroKind.Reimu)
    {
        StartRun(hero, 260906);
        var nodes = hero == HeroKind.Reimu
            ? new[] { UpgradeCatalog.YinYangUnlock, UpgradeCatalog.BoundaryUnlock, UpgradeCatalog.Launch, UpgradeCatalog.Clear, UpgradeCatalog.Cluster, UpgradeCatalog.Bind, UpgradeCatalog.Homing, UpgradeCatalog.Blast }
            : new[] { UpgradeCatalog.StardustUnlock, UpgradeCatalog.MasterSparkUnlock, UpgradeCatalog.StarPierce, UpgradeCatalog.StarSplit, UpgradeCatalog.StardustRecall, UpgradeCatalog.StardustEcho, UpgradeCatalog.SparkSweep, UpgradeCatalog.SparkClear };
        foreach (var id in nodes)
            run!.Build.TryApply(UpgradeCatalog.Get(id), 100);
        foreach (var position in new[] { new System.Numerics.Vector2(400, -90), new System.Numerics.Vector2(420, -70), new System.Numerics.Vector2(-300, 90) })
        {
            var enemy = run!.SpawnEnemy(EnemyKind.Elite, position);
            enemy.Health = enemy.MaxHealth = 10000;
            enemy.Speed = 0;
            enemy.Timer = 100;
        }
        for (var tick = 0; tick < 56; tick++) run!.Step(default);
        canvas.Clock = run!.Time;
        canvas.ResetView();
        canvas.ReceiveEvents();
        RefreshRunScreen();
    }

    private void PrepareGrowthPreview(HeroKind hero = HeroKind.Reimu)
    {
        StartRun(hero, 260906);
        run!.Build.TryApply(UpgradeCatalog.Get(hero == HeroKind.Reimu ? UpgradeCatalog.YinYangUnlock : UpgradeCatalog.StardustUnlock), 1);
        run.Build.TryApply(UpgradeCatalog.Get(hero == HeroKind.Reimu ? UpgradeCatalog.BoundaryUnlock : UpgradeCatalog.MasterSparkUnlock), 1);
        run.AddExperience(50);
        run.Step(default);
        run.Choices.Clear();
        var choices = hero == HeroKind.Reimu ? new[] { UpgradeCatalog.Blast, UpgradeCatalog.Bind, UpgradeCatalog.Launch }
            : new[] { UpgradeCatalog.StarPierce, UpgradeCatalog.StardustEcho, UpgradeCatalog.SparkSweep };
        foreach (var id in choices)
            run.Choices.Add(UpgradeCatalog.Get(id));
        canvas.ResetView();
        RefreshRunScreen();
    }

    private bool PrepareMarisaGrowthFixture(string mode)
    {
        if (mode == "marisa-growth-choices") PrepareGrowthPreview(HeroKind.Marisa);
        else if (mode is "marisa-growth-combat" or "marisa-growth-build")
        {
            PrepareGrowthCombatPreview(HeroKind.Marisa);
            if (mode == "marisa-growth-build") OpenBuild();
        }
        else return false;
        return true;
    }
}
