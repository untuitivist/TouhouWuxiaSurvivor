using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void PrepareGrowthCombatPreview()
    {
        StartRun(HeroKind.Reimu, 260906);
        foreach (var id in new[] { UpgradeCatalog.YinYangUnlock, UpgradeCatalog.BoundaryUnlock, UpgradeCatalog.Launch, UpgradeCatalog.Clear, UpgradeCatalog.Cluster, UpgradeCatalog.Bind, UpgradeCatalog.Homing, UpgradeCatalog.Blast })
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

    private void PrepareGrowthPreview()
    {
        StartRun(HeroKind.Reimu, 260906);
        run!.Build.TryApply(UpgradeCatalog.Get(UpgradeCatalog.YinYangUnlock), 1);
        run.Build.TryApply(UpgradeCatalog.Get(UpgradeCatalog.BoundaryUnlock), 1);
        run.AddExperience(50);
        run.Step(default);
        run.Choices.Clear();
        foreach (var id in new[] { UpgradeCatalog.Blast, UpgradeCatalog.Bind, UpgradeCatalog.Launch })
            run.Choices.Add(UpgradeCatalog.Get(id));
        canvas.ResetView();
        RefreshRunScreen();
    }
}
