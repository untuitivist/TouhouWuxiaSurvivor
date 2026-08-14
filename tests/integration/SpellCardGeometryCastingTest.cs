using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Tests.Support;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在正式世界执行贯线、扇面和背袭符卡，确认清单几何确实改变生成投射物的空间阵型。
/// </summary>
public partial class SpellCardGeometryCastingTest : Node
{
    /// <summary>建立无随机敌人的正式世界，直接施放三类投射符卡并比较真实子节点位置。</summary>
    public override async void _Ready()
    {
        WorldDemo? demo = null;
        int exitCode = 0;
        try
        {
            string[] packIds = ContentPackCatalog.All
                .Where(pack => pack.Number is 1 or 2 or 11)
                .Select(pack => pack.Id)
                .ToArray();
            ContentPackSelectionService.Apply(new ContentPackSelection(packIds));
            demo = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn")
                .Instantiate<WorldDemo>();
            demo.PersistMetaProgression = false;
            demo.GetNode<EnemySpawner>("EnemySpawner").InitialSpawnCount = 0;
            AddChild(demo);

            Node2D player = demo.GetNode<Node2D>("Player");
            var health = player.GetNode<PlayerHealth>("Health");
            var shooter = player.GetNode<AutoShooter>("AutoShooter");
            var progression = demo.GetNode<RunProgressionCoordinator>(
                "RunProgressionCoordinator");
            var enemies = demo.GetNode<Node2D>("CombatEntities/Enemies");
            var effects = demo.GetNode<Node2D>("CombatEntities/SpellEffects");
            var world = demo.GetNode<EcsCombatWorld>("CombatEntities/EcsCombatWorld");
            var attributes = new SpellCardAttributeProvider(
                shooter,
                progression.Modifiers,
                health,
                CharacterSelectionService.Current.Current.PlayableProfile);
            var caster = new SpellCardEffectCaster(
                player,
                health,
                enemies,
                effects,
                GD.Load<PackedScene>(
                    "res://src/gameplay/spellcards/effects/FantasySealOrb.tscn"),
                GD.Load<PackedScene>(
                    "res://src/gameplay/spellcards/effects/SealingCircleEffect.tscn"),
                attributes,
                world);
            SpawnTargets(world, player.GlobalPosition);

            Vector2[] line = CastAndCapture(caster, effects,
                "th01_sariel_fallen_judgement");
            Vector2[] fan = CastAndCapture(caster, effects, "th02_rika_evil_eye_sigma");
            Vector2[] backstab = CastAndCapture(caster, effects,
                "th11_koishi_subterranean_rose");
            VerifySpatialDifferences(player.GlobalPosition, line, fan, backstab);
            GD.Print("Spell card geometry casting test passed.");
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

    /// <summary>在玩家前方、侧面与后方布置候选点，为三种策略提供同一空间输入。</summary>
    private static void SpawnTargets(EcsCombatWorld world, Vector2 origin)
    {
        var target = new Actors.Enemies.EnemyDefinition(
            Actors.Enemies.EnemyArchetype.Fairy,
            "几何测试靶",
            999,
            0.0f,
            5.0f,
            0.0f,
            0.0f,
            0.0f,
            []);
        foreach (Vector2 offset in new Vector2[]
        {
            new(72, 0), new(86, 24), new(58, -32), new(-54, 12), new(24, 64),
        })
        {
            world.SpawnEnemy(origin + offset, target);
        }
    }

    /// <summary>通过正式施放器施放指定卡，并捕获后立即回收本次生成的全部视觉节点。</summary>
    private static Vector2[] CastAndCapture(
        SpellCardEffectCaster caster,
        Node2D effects,
        string cardId)
    {
        SpellCardDefinition card = SpellCardCatalog.All.Single(item => item.Id == cardId);
        Require(caster.TryCast(card, caster.Resolve(card)), $"Could not cast {cardId}.");
        Vector2[] positions = effects.GetChildren()
            .OfType<FantasySealOrb>()
            .Select(orb => orb.GlobalPosition)
            .ToArray();
        Require(positions.Length >= 2, $"{cardId} did not create a readable formation.");
        foreach (Node child in effects.GetChildren())
        {
            child.Free();
        }
        return positions;
    }

    /// <summary>确认贯线同点蓄势、扇面横列散开、背袭从远端出现，三者具有可观察差异。</summary>
    private static void VerifySpatialDifferences(
        Vector2 origin,
        IReadOnlyList<Vector2> line,
        IReadOnlyList<Vector2> fan,
        IReadOnlyList<Vector2> backstab)
    {
        Require(line.All(position => position.IsEqualApprox(line[0])),
            "Line geometry did not align all projectiles behind one axis.");
        Require(fan.Distinct().Count() == fan.Count,
            "Fan geometry did not spread projectile origins across its front arc.");
        float fanDistance = fan.Average(position => position.DistanceTo(origin));
        float backstabDistance = backstab.Average(position => position.DistanceTo(origin));
        Require(backstabDistance > fanDistance * 2.0f,
            "Backstab geometry did not originate behind distant targets.");
    }

    /// <summary>把真实施放失败转换为包含卡号或阵型语义的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
