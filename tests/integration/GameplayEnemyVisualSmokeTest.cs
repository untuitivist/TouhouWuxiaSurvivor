using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在正式世界中验证 ECS 敌人、强化、灵息和弹幕全部走共享东方原作素材链。
/// </summary>
public partial class GameplayEnemyVisualSmokeTest : Node
{
    /// <summary>
    /// 实例化正式世界并加入映射与回退实体，等待真实绘制帧后检查每种批量视觉计数。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            Node world = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn").Instantiate();
            world.GetNode<EnemySpawner>("EnemySpawner").InitialSpawnCount = 0;
            world.GetNode<AutoShooter>("Player/AutoShooter").SetProcess(false);
            AddChild(world);
            var player = world.GetNode<PlayerController>("Player");
            var ecs = world.GetNode<EcsCombatWorld>("CombatEntities/EcsCombatWorld");
            var visuals = new InternalVisualCatalog();
            EnemyDefinition[] formalEnemies = VerifyFormalEnemyMappings(visuals);
            AddVisualFixtures(ecs, player.GlobalPosition, formalEnemies, visuals);
            Require(ecs.EnemyCount == formalEnemies.Length + 1,
                "Visual fixture count differs from all formal enemies plus one synthetic unknown.");

            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            Require(ecs.MappedEnemyVisualCount == formalEnemies.Length,
                "Formal ECS did not draw every catalog enemy through its mapped texture branch.");
            Require(ecs.FallbackEnemyVisualCount == 1,
                "Synthetic unknown enemy was not the only Chinese-name fallback.");
            Require(ecs.PickupIconVisualCount >= 3,
                "The three build pickups did not use the Touhou item atlas.");
            Require(ecs.SpiritIconVisualCount >= 1,
                "Spirit experience did not use the Touhou item atlas.");
            Require(ecs.ProjectileIconVisualCount >= 1,
                "Player projectile did not use the original bullet atlas.");
            VerifyTouhouPickupNames();

            GD.Print("Gameplay enemy visual smoke test passed.");
            world.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 在正式世界中布置全部目录敌人、一个合成未知身份和全部道具类型，使一次绘制精确覆盖映射与回退分支。
    /// </summary>
    private static void AddVisualFixtures(
        EcsCombatWorld ecs,
        Vector2 origin,
        IReadOnlyList<EnemyDefinition> formalEnemies,
        InternalVisualCatalog visuals)
    {
        for (int index = 0; index < formalEnemies.Count; index++)
        {
            var offset = new Vector2(
                720.0f + index % 12 * 32.0f,
                480.0f + index / 12 * 32.0f);
            ecs.SpawnEnemy(origin + offset, formalEnemies[index]);
        }

        Require(!visuals.TryGet("test_unknown", InternalVisualCategory.Enemy,
                "测试未知敌人", out _),
            "Synthetic unknown enemy unexpectedly entered the formal visual catalog.");
        ecs.SpawnEnemy(origin + new Vector2(0.0f, -84.0f), CreateUnknownEnemy());
        ecs.SpawnPickup(PickupKind.MoveSpeed, origin + new Vector2(-72.0f, 84.0f));
        ecs.SpawnPickup(PickupKind.RapidFire, origin + new Vector2(0.0f, 84.0f));
        ecs.SpawnPickup(PickupKind.SpiralShot, origin + new Vector2(72.0f, 84.0f));
        ecs.SpawnSpirit(origin + new Vector2(-112.0f, 48.0f), 2);
        ecs.SpawnProjectile(origin + new Vector2(112.0f, 48.0f), Vector2.Right, 0.0f, 1);
    }

    /// <summary>
    /// 逐项核对本体与二十个正式内容包的全部敌人稳定键、ActorStrip 类型和实际纹理均可由共享目录加载。
    /// </summary>
    private static EnemyDefinition[] VerifyFormalEnemyMappings(InternalVisualCatalog visuals)
    {
        EnemyDefinition[] formalEnemies = EnemyCatalog.All.ToArray();
        foreach (EnemyDefinition enemy in formalEnemies)
        {
            string sourceId = enemy.RequiredContentPack ?? "base";
            Require(visuals.TryGet(sourceId, InternalVisualCategory.Enemy,
                    enemy.DisplayName, out InternalVisualDefinition visual),
                $"Formal enemy mapping is missing: {sourceId}/{enemy.DisplayName}");
            Require(visual.Kind == InternalVisualKind.ActorStrip,
                $"Formal enemy mapping is not an ActorStrip: {sourceId}/{enemy.DisplayName}");
            Require(visuals.TryGetTexture(visual, out Texture2D texture) &&
                    texture.GetWidth() >= 4 && texture.GetWidth() % 4 == 0 &&
                    texture.GetHeight() > 0,
                $"Formal enemy texture could not be loaded as a four-frame strip: {sourceId}/{enemy.DisplayName}");
        }

        return formalEnemies;
    }

    /// <summary>
    /// 构造一个不属于任何正式目录的高耐久静止敌人，稳定覆盖中文名回退且不会误把真实内容标为缺图。
    /// </summary>
    private static EnemyDefinition CreateUnknownEnemy() => new(
        EnemyArchetype.Fairy,
        "测试未知敌人",
        9999,
        0.0f,
        6.0f,
        0.0f,
        0.0f,
        0.0f,
        [],
        requiredContentPack: "test_unknown");

    /// <summary>
    /// 确认正式显示名已脱离参考游戏语义，并和共享视觉映射的稳定键保持一致。
    /// </summary>
    private static void VerifyTouhouPickupNames()
    {
        Require(PickupCatalog.Get(PickupKind.MoveSpeed).DisplayName == "高速点" &&
            PickupCatalog.Get(PickupKind.RapidFire).DisplayName == "火力点" &&
            PickupCatalog.Get(PickupKind.SpiralShot).DisplayName == "全力点",
            "Pickup display names still expose borrowed demo-game terminology.");
    }

    /// <summary>
    /// 将视觉契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
