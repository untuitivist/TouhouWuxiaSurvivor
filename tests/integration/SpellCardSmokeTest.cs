using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Tests.Support;
using TouhouWuxiaSurvivor.Ui.Debug;
using TouhouWuxiaSurvivor.Ui.Stats;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 从完整符卡目录抽取两张代表卡，在真实世界验证构筑解锁、自动施放、战斗、HUD 和属性页联动。
/// </summary>
public partial class SpellCardSmokeTest : Node
{
    private static readonly EnemyDefinition SpellTarget = new(
        EnemyArchetype.Fairy, "奥义测试靶", 1, 0.0f, 5.0f,
        0.0f, 0.0f, 0.0f, []);

    /// <summary>
    /// 构造无随机首批敌人的世界，依次验证梦想封印追踪和封魔阵护身流程。
    /// </summary>
    public override async void _Ready()
    {
        WorldDemo? demo = null;
        int exitCode = 0;
        try
        {
            ContentPackDefinition th06 = ContentPackCatalog.All.Single(pack => pack.Number == 6);
            ContentPackSelectionService.Apply(new ContentPackSelection([th06.Id]));
            demo = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn")
                .Instantiate<WorldDemo>();
            demo.PersistMetaProgression = false;
            demo.GetNode<EnemySpawner>("EnemySpawner").InitialSpawnCount = 0;
            AddChild(demo);

            var player = demo.GetNode<PlayerController>("Player");
            var health = player.GetNode<PlayerHealth>("Health");
            var enemies = demo.GetNode<Node2D>("CombatEntities/Enemies");
            var ecsWorld = demo.GetNode<EcsCombatWorld>("CombatEntities/EcsCombatWorld");
            var effects = demo.GetNode<Node2D>("CombatEntities/SpellEffects");
            var progression = demo.GetNode<RunProgressionCoordinator>(
                "RunProgressionCoordinator");
            var spells = demo.GetNode<SpellCardCoordinator>("SpellCardCoordinator");

            SpellCardDefinition fantasy = SpellCardCatalog.All.Single(
                card => card.FullName == "灵符「梦想封印」");
            SpellCardDefinition circle = SpellCardCatalog.All.Single(
                card => card.FullName == "梦符「封魔阵」");
            Unlock(progression.Build, fantasy);
            int fantasyThreshold = ResolveTargetDimension(
                fantasy.Combat.ActivationThresholdScale);
            int fantasyTargets = ResolveTargetDimension(fantasy.Combat.TargetScale);
            SpawnEnemies(ecsWorld, player, fantasyThreshold, 84.0f);
            spells._Process(10.0);
            int expectedOrbs = Math.Min(fantasyThreshold, fantasyTargets);
            Require(effects.GetChildCount() == expectedOrbs,
                "Fantasy Seal did not respond when its cycle and crowd condition were ready.");
            VerifyInternalSpellVisuals(effects);
            for (int frame = 0; frame < 20; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            }

            Require(ecsWorld.AliveEnemyCount < fantasyThreshold,
                "Fantasy Seal orbs did not defeat their assigned early enemies.");
            VerifyPresentation(demo);

            Unlock(progression.Build, circle);
            SpawnEnemies(ecsWorld, player, 3, 64.0f);
            spells._Process(10.0);
            Require(!health.IsInvincible && ecsWorld.AliveEnemyCount == 3,
                "Evil-Sealing Circle cast before receiving its passive damage signal.");
            Require(health.ApplyDamage(1),
                "Could not emit the real player-damage signal for the passive spell test.");
            spells._Process(1.0);
            Require(health.IsInvincible && ecsWorld.AliveEnemyCount == 0,
                "Evil-Sealing Circle did not respond to damage, protect Reimu, and hit enemies.");

            GD.Print("Spell card smoke test passed.");
        }
        catch (Exception exception)
        {
            exitCode = 1;
            GD.PushError(exception.ToString());
        }
        finally
        {
            ContentPackSelectionService.Apply(ContentPackSelection.BaseOnly);
            GetTree().Paused = false;
            if (demo is not null && GodotObject.IsInstanceValid(demo))
            {
                await WorldDemoTestCleanup.FreeAsync(this, demo);
            }

            GetTree().Quit(exitCode);
        }
    }

    /// <summary>
    /// 把指定基础修炼提升到二重，再应用其符卡进阶，模拟玩家完成三选一构筑。
    /// </summary>
    private static void Unlock(RunBuildState build, SpellCardDefinition card)
    {
        RunUpgradeDefinition prerequisite = RunUpgradeCatalog.FindById(
            card.PrerequisiteUpgradeId)!;
        RunUpgradeDefinition spell = RunUpgradeCatalog.FindById(card.UnlockUpgradeId)!;
        for (int rank = 0; rank < card.MinimumRank; rank++)
        {
            Require(build.Apply(prerequisite),
                $"Could not apply prerequisite for spell: {card.Id}");
        }

        Require(build.Apply(spell), $"Could not unlock spell: {card.Id}");
    }

    /// <summary>
    /// 在玩家周围均匀放置指定数量的低耐久敌人，返回实例以便断言正常死亡状态。
    /// </summary>
    private static void SpawnEnemies(
        EcsCombatWorld world,
        Node2D player,
        int count,
        float radius)
    {
        for (int index = 0; index < count; index++)
        {
            Vector2 position = player.GlobalPosition +
                Vector2.FromAngle(Mathf.Tau * index / count) * radius;
            world.SpawnEnemy(position, SpellTarget);
        }
    }

    /// <summary>
    /// 以当前所选角色的奥义目标容量解析正式倍率，使冒烟场景不依赖已经废弃的固定角色常数。
    /// </summary>
    private static int ResolveTargetDimension(float scale)
    {
        int capacity = CharacterSelectionService.Current.Current
            .PlayableProfile.UltimateTargetCapacity;
        return Math.Max(1, (int)MathF.Round(
            capacity * Math.Max(0.0f, scale), MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// 确认常驻 HUD 标记自动奥义，且 E 面板显示已悟得符卡而不创建滚动区域。
    /// </summary>
    private static void VerifyPresentation(WorldDemo demo)
    {
        var hud = demo.GetNode<WorldDebugHud>("WorldDebugHud");
        Require(hud.StatusText.Contains("奥义", StringComparison.Ordinal) &&
            hud.StatusText.Contains("s", StringComparison.Ordinal),
            "HUD did not show the automatic spell countdown.");
        var stats = demo.GetNode<CharacterStatsOverlay>("CharacterStatsOverlay");
        stats.Open();
        CharacterBuildView buildView = stats.GetNode<CharacterBuildView>(
            "Root/Panel/Padding/Layout/Pages/BuildPage");
        buildView.SelectFilter(CharacterBuildFilter.SpellCard);
        Label detail = buildView.GetNode<Label>("Body/DetailsFrame/Details/Name");
        Require(detail.Text.Contains("梦想封印", StringComparison.Ordinal) &&
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
