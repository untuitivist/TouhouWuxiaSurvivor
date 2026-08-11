using Godot;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Spawning;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实游戏场景中验证屏外刷怪、最近目标自动射击、击破和掉落拾取闭环。
/// </summary>
public partial class CombatLoopSmokeTest : Node
{
    private const string WorldScenePath = "res://src/demo/WorldDemo.tscn";
    private static readonly string[] LegacyScenePaths =
    [
        "res://src/combat/projectiles/PlayerProjectile.tscn",
        "res://src/actors/enemies/EnemyActor.tscn",
        "res://src/actors/pickups/PickupActor.tscn",
        "res://src/actors/spirit/SpiritDropActor.tscn",
    ];

    /// <summary>
    /// 启动正式 ECS 游戏页，验证场景依赖、自动击破、灵息和三种强化物都不创建旧 Actor 节点。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            RequireLegacyScenesNotCached("before loading WorldDemo");
            PackedScene worldScene = GD.Load<PackedScene>(WorldScenePath);
            Node demo = worldScene.Instantiate();
            AddChild(demo);
            RequireLegacyScenesNotCached("after instantiating WorldDemo");
            var player = demo.GetNode<PlayerController>("Player");
            var buffs = player.GetNode<PlayerBuffController>("Buffs");
            var shooter = player.GetNode<AutoShooter>("AutoShooter");
            var ecsWorld = demo.GetNode<EcsCombatWorld>("CombatEntities/EcsCombatWorld");
            var spawner = demo.GetNode<EnemySpawner>("EnemySpawner");
            var pickupSpawner = demo.GetNode<PickupSpawner>("PickupSpawner");
            var spiritSpawner = demo.GetNode<SpiritDropSpawner>("SpiritDropSpawner");

            VerifyFormalSceneDependencies(shooter, spawner, pickupSpawner, spiritSpawner);
            RequireLegacyContainersEmpty(demo);
            Require(spawner.AliveCount > 0 && ecsWorld.EnemyCount == spawner.AliveCount,
                "Enemy spawner did not create the initial wave in the ECS pool.");
            await ToSignal(GetTree().CreateTimer(2.4), SceneTreeTimer.SignalName.Timeout);
            Require(shooter.ShotsFired > 0, "Auto shooter did not fire at the nearest enemy.");
            Require(ecsWorld.TotalProjectilesSpawned == shooter.ShotsFired &&
                ecsWorld.TotalProjectilesSpawned > 0,
                $"Automatic shooting did not enter the ECS projectile runtime: shots={shooter.ShotsFired}, ecs={ecsWorld.TotalProjectilesSpawned}.");
            Require(demo.GetNode<Node2D>("CombatEntities/Projectiles").GetChildCount() == 0,
                "ECS projectile mode must not create per-projectile scene nodes.");
            Require(spawner.DefeatedCount > 0 && ecsWorld.DefeatedCount > 0,
                "Automatic projectiles did not defeat an ECS enemy.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(spiritSpawner.SpawnedCount > 0,
                "Defeated ECS enemies did not enter the ECS spirit-drop flow.");
            int spiritsBeforeManualSpawn = ecsWorld.SpiritCount;
            spiritSpawner.Spawn(player.GlobalPosition + new Vector2(180.0f, 0.0f), 2);
            Require(ecsWorld.SpiritCount == spiritsBeforeManualSpawn + 1,
                "Spirit spawner did not add the requested spirit to the ECS pool.");
            RequireLegacyContainersEmpty(demo);

            int pickupsBeforeRapid = ecsWorld.PickupCount;
            pickupSpawner.Spawn(PickupKind.RapidFire, player.GlobalPosition);
            Require(ecsWorld.PickupCount == pickupsBeforeRapid + 1,
                "Rapid-fire pickup did not enter the ECS pool.");
            RequireLegacyContainersEmpty(demo);
            await WaitForPickup();
            Require(Mathf.IsEqualApprox(buffs.FireRateMultiplier, 2.0f),
                "Rapid-fire pickup must provide exactly double fire rate.");

            int pickupsBeforeMove = ecsWorld.PickupCount;
            pickupSpawner.Spawn(PickupKind.MoveSpeed, player.GlobalPosition);
            Require(ecsWorld.PickupCount == pickupsBeforeMove + 1,
                "Move-speed pickup did not enter the ECS pool.");
            RequireLegacyContainersEmpty(demo);
            await WaitForPickup();
            Require(Mathf.IsEqualApprox(buffs.SpeedMultiplier, 1.5f),
                "Move-speed pickup must provide exactly 1.5x movement speed.");

            int shotsBeforeSpiral = shooter.ShotsFired;
            int pickupsBeforeSpiral = ecsWorld.PickupCount;
            pickupSpawner.Spawn(PickupKind.SpiralShot, player.GlobalPosition);
            Require(ecsWorld.PickupCount == pickupsBeforeSpiral + 1,
                "Spiral pickup did not enter the ECS pool.");
            RequireLegacyContainersEmpty(demo);
            await WaitForPickup();
            Require(buffs.IsSpiralActive && Mathf.IsEqualApprox(buffs.FireRateMultiplier, 20.0f),
                "Power pickup must activate the 20x spiral state.");
            var visual = player.GetNode<PlayerVisualController>("Visual");
            Require(visual.DisplayName == "博丽灵梦" && visual.IsArmedVisible,
                "Power pickup did not preserve the player name and enable the armed text marker.");
            await ToSignal(GetTree().CreateTimer(0.08), SceneTreeTimer.SignalName.Timeout);
            Require(shooter.ShotsFired >= shotsBeforeSpiral + 2,
                "Spiral state did not fire the required opposite projectile pair.");

            Require(pickupSpawner.SpawnedCount >= 3,
                "Formal pickup spawner did not route all requested pickups into ECS.");
            RequireLegacyContainersEmpty(demo);
            VerifyPickupDefinitions();

            GD.Print("Combat loop smoke test passed.");
            demo.QueueFree();
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
    /// 核对正式场景既没有给生成器赋旧 PackedScene，也没有把四个旧实体场景登记为直接资源依赖。
    /// </summary>
    private static void VerifyFormalSceneDependencies(
        AutoShooter shooter,
        EnemySpawner enemySpawner,
        PickupSpawner pickupSpawner,
        SpiritDropSpawner spiritSpawner)
    {
        Require(shooter.ProjectileScene is null && enemySpawner.EnemyScene is null &&
            pickupSpawner.PickupScene is null && spiritSpawner.SpiritScene is null,
            "Formal WorldDemo still assigns a legacy actor PackedScene.");

        foreach (string dependency in ResourceLoader.GetDependencies(WorldScenePath))
        {
            string fallbackPath = GetDependencyFallbackPath(dependency);
            foreach (string forbidden in LegacyScenePaths)
            {
                Require(!string.Equals(fallbackPath, forbidden, StringComparison.Ordinal),
                    $"Formal WorldDemo still loads legacy dependency: {forbidden}");
            }
        }
    }

    /// <summary>
    /// 在正式世界载入前后检查 Godot 资源缓存，拒绝直接依赖之外的间接或动态旧实体场景加载。
    /// </summary>
    private static void RequireLegacyScenesNotCached(string phase)
    {
        foreach (string path in LegacyScenePaths)
        {
            Require(!ResourceLoader.HasCached(path),
                $"Legacy scene was cached {phase}: {path}");
        }
    }

    /// <summary>
    /// 从 Godot 可能携带 UID 的依赖描述中提取最终回退路径，使禁止清单按完整资源路径精确比较。
    /// </summary>
    private static string GetDependencyFallbackPath(string dependency)
    {
        int separator = dependency.LastIndexOf("::", StringComparison.Ordinal);
        return separator >= 0 ? dependency[(separator + 2)..] : dependency;
    }

    /// <summary>
    /// 确认保留用于场景组织和兼容调试的四个节点容器始终为空，正式实体只存在于 ECS 数据池。
    /// </summary>
    private static void RequireLegacyContainersEmpty(Node demo)
    {
        string[] containerPaths =
        [
            "CombatEntities/Enemies",
            "CombatEntities/Projectiles",
            "CombatEntities/Pickups",
            "CombatEntities/SpiritDrops",
        ];
        foreach (string path in containerPaths)
        {
            Require(demo.GetNode<Node>(path).GetChildCount() == 0,
                $"Formal ECS runtime created legacy scene nodes under {path}.");
        }
    }

    /// <summary>
    /// 等待拾取区域完成两次物理步，使刚生成在玩家脚下的道具稳定触发收集信号。
    /// </summary>
    private async Task WaitForPickup()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    /// <summary>
    /// 校验三种基础道具都严格沿用参考游戏的五秒持续时间及对应倍率和特殊能力。
    /// </summary>
    private static void VerifyPickupDefinitions()
    {
        PickupDefinition speed = PickupCatalog.Get(PickupKind.MoveSpeed);
        PickupDefinition rapid = PickupCatalog.Get(PickupKind.RapidFire);
        PickupDefinition spiral = PickupCatalog.Get(PickupKind.SpiralShot);
        Require(Mathf.IsEqualApprox(speed.Duration, 5.0f) &&
            Mathf.IsEqualApprox(speed.MoveSpeedMultiplier, 1.5f),
            "Move-speed definition differs from the reference game.");
        Require(Mathf.IsEqualApprox(rapid.Duration, 5.0f) &&
            Mathf.IsEqualApprox(rapid.FireRateMultiplier, 2.0f),
            "Rapid-fire definition differs from the reference game.");
        Require(Mathf.IsEqualApprox(spiral.Duration, 5.0f) &&
            Mathf.IsEqualApprox(spiral.FireRateMultiplier, 20.0f) && spiral.EnablesSpiral,
            "Power definition differs from the reference game.");
    }

    /// <summary>
    /// 将战斗闭环中的失败条件转换为带有明确原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
