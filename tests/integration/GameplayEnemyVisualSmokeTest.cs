using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Ecs.Combat;

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
            AddChild(world);
            var player = world.GetNode<PlayerController>("Player");
            var ecs = world.GetNode<EcsCombatWorld>("CombatEntities/EcsCombatWorld");
            AddVisualFixtures(ecs, player.GlobalPosition);

            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            Require(ecs.MappedEnemyVisualCount >= 2,
                "Formal ECS did not draw both base and TH06 enemies from compendium mappings.");
            Require(ecs.FallbackEnemyVisualCount >= 1,
                "Unmapped DLC enemy did not preserve the Chinese-name fallback.");
            Require(ecs.PickupIconVisualCount >= 3,
                "The three build pickups did not use the Touhou item atlas.");
            Require(ecs.SpiritIconVisualCount >= 1,
                "Spirit experience did not use the Touhou item atlas.");
            Require(ecs.ProjectileIconVisualCount >= 1,
                "Player projectile did not use the original bullet atlas.");
            VerifyTouhouPickupNames();

            GD.Print("Gameplay enemy visual smoke test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 在玩家镜头内布置本体、红魔乡、未映射敌人以及全部道具类型，使一次绘制覆盖所有分支。
    /// </summary>
    private static void AddVisualFixtures(EcsCombatWorld ecs, Vector2 origin)
    {
        ecs.SpawnEnemy(origin + new Vector2(72.0f, 0.0f), FindEnemy("野妖精"));
        ecs.SpawnEnemy(origin + new Vector2(-72.0f, 0.0f), FindEnemy("湖上妖精"));
        EnemyDefinition unmapped = EnemyCatalog.All.First(definition =>
            definition.RequiredContentPack is not null and not "th06_eosd");
        ecs.SpawnEnemy(origin + new Vector2(0.0f, -84.0f), unmapped);
        ecs.SpawnPickup(PickupKind.MoveSpeed, origin + new Vector2(-72.0f, 84.0f));
        ecs.SpawnPickup(PickupKind.RapidFire, origin + new Vector2(0.0f, 84.0f));
        ecs.SpawnPickup(PickupKind.SpiralShot, origin + new Vector2(72.0f, 84.0f));
        ecs.SpawnSpirit(origin + new Vector2(-112.0f, 48.0f), 2);
        ecs.SpawnProjectile(origin + new Vector2(112.0f, 48.0f), Vector2.Right, 0.0f, 1);
    }

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
    /// 按唯一中文名取得敌人定义，使测试不依赖目录排序。
    /// </summary>
    private static EnemyDefinition FindEnemy(string displayName) =>
        EnemyCatalog.All.Single(definition => definition.DisplayName == displayName);

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
