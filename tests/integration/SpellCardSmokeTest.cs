using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Ui.Debug;
using TouhouWuxiaSurvivor.Ui.Stats;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实世界场景验证两张符卡由构筑解锁并自动施放，同时正确更新战斗、HUD 和属性页。
/// </summary>
public partial class SpellCardSmokeTest : Node
{
    /// <summary>
    /// 构造无随机首批敌人的世界，依次验证梦想封印追踪和封魔阵护身流程。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            var demo = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn")
                .Instantiate<WorldDemo>();
            demo.PersistMetaProgression = false;
            demo.GetNode<EnemySpawner>("EnemySpawner").InitialSpawnCount = 0;
            AddChild(demo);

            var player = demo.GetNode<PlayerController>("Player");
            var health = player.GetNode<PlayerHealth>("Health");
            var enemies = demo.GetNode<Node2D>("CombatEntities/Enemies");
            var effects = demo.GetNode<Node2D>("CombatEntities/SpellEffects");
            var progression = demo.GetNode<RunProgressionCoordinator>(
                "RunProgressionCoordinator");
            var spells = demo.GetNode<SpellCardCoordinator>("SpellCardCoordinator");
            PackedScene enemyScene = GD.Load<PackedScene>(
                "res://src/actors/enemies/EnemyActor.tscn");

            Unlock(progression.Build, RunUpgradeKind.NeedleDamage, RunUpgradeKind.FantasySeal);
            EnemyActor[] fantasyTargets = SpawnEnemies(enemyScene, enemies, player, 3, 84.0f);
            spells.Power.SetPower(100);
            Require(spells.TryAutoCast() && spells.Power.CurrentPower == 0 &&
                effects.GetChildCount() == 3,
                "Fantasy Seal did not auto-cast three homing orbs and spend power.");
            VerifyInternalSpellVisuals(effects);
            for (int frame = 0; frame < 20; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            }

            Require(fantasyTargets.All(enemy => !enemy.IsAlive),
                "Fantasy Seal orbs did not defeat their assigned early enemies.");
            VerifyPresentation(demo);

            spells._Process(10.0);
            Unlock(progression.Build,
                RunUpgradeKind.SpiritAttraction,
                RunUpgradeKind.EvilSealingCircle);
            EnemyActor[] circleTargets = SpawnEnemies(enemyScene, enemies, player, 3, 64.0f);
            spells.Power.SetPower(70);
            Require(spells.TryAutoCast() && spells.Power.CurrentPower == 0 &&
                health.IsInvincible && circleTargets.All(enemy => !enemy.IsAlive),
                "Evil-Sealing Circle did not auto-cast, protect Reimu, and damage nearby enemies.");

            GD.Print("Spell card smoke test passed.");
            demo.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GetTree().Paused = false;
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 把指定基础修炼提升到二重，再应用其符卡进阶，模拟玩家完成三选一构筑。
    /// </summary>
    private static void Unlock(
        RunBuildState build,
        RunUpgradeKind prerequisiteKind,
        RunUpgradeKind spellKind)
    {
        RunUpgradeDefinition prerequisite = FindUpgrade(prerequisiteKind);
        RunUpgradeDefinition spell = FindUpgrade(spellKind);
        Require(build.Apply(prerequisite) && build.Apply(prerequisite) && build.Apply(spell),
            $"Could not unlock spell build kind: {spellKind}");
    }

    /// <summary>
    /// 在玩家周围均匀放置指定数量的低耐久敌人，返回实例以便断言正常死亡状态。
    /// </summary>
    private static EnemyActor[] SpawnEnemies(
        PackedScene enemyScene,
        Node2D container,
        Node2D player,
        int count,
        float radius)
    {
        var enemies = new EnemyActor[count];
        for (int index = 0; index < count; index++)
        {
            var enemy = enemyScene.Instantiate<EnemyActor>();
            enemy.Configure(EnemyCatalog.All[0], player);
            container.AddChild(enemy);
            enemy.GlobalPosition = player.GlobalPosition +
                Vector2.FromAngle(Mathf.Tau * index / count) * radius;
            enemies[index] = enemy;
        }

        return enemies;
    }

    /// <summary>
    /// 确认常驻 HUD 标记自动奥义，且 E 面板显示已悟得符卡而不创建滚动区域。
    /// </summary>
    private static void VerifyPresentation(WorldDemo demo)
    {
        var hud = demo.GetNode<WorldDebugHud>("WorldDebugHud");
        Require(hud.StatusText.Contains("奥义自动", StringComparison.Ordinal),
            "HUD did not show automatic spell mode.");
        var stats = demo.GetNode<CharacterStatsOverlay>("CharacterStatsOverlay");
        stats.Open();
        Label spellValue = stats.GetNode<Label>(
            "Root/Panel/Padding/Layout/Sources/SpellValue");
        Require(spellValue.Text.Contains("梦想封印", StringComparison.Ordinal) &&
            stats.FindChildren("*", "ScrollContainer").Count == 0,
            "Stats panel omitted the unlocked spell or introduced scrolling.");
        stats.Close();
    }

    /// <summary>
    /// 确认真实局内施放的三枚梦想封印使用内部弹幕图集，并隐藏公开包才会使用的中文回退。
    /// </summary>
    private static void VerifyInternalSpellVisuals(Node2D effects)
    {
        foreach (Node effect in effects.GetChildren())
        {
            var visual = effect.GetNode<InternalSpellBulletVisual>("Visual");
            var fallback = effect.GetNode<Label>("FallbackLabel");
            Require(visual.Visible && visual.Texture is not null && visual.RegionEnabled &&
                !fallback.Visible,
                "Fantasy Seal did not load the internal bullet atlas in gameplay.");
        }
    }

    /// <summary>
    /// 按稳定效果类型查找升级定义，使场景测试不依赖目录顺序。
    /// </summary>
    private static RunUpgradeDefinition FindUpgrade(RunUpgradeKind kind) =>
        RunUpgradeCatalog.All.Single(definition => definition.Kind == kind);

    /// <summary>
    /// 将场景契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
