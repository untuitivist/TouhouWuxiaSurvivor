using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Spawning;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实游戏场景中验证屏外刷怪、最近目标自动射击、击破和掉落拾取闭环。
/// </summary>
public partial class CombatLoopSmokeTest : Node
{
    /// <summary>
    /// 启动游戏页、完成一次自动击破，并逐项验证三种原版拾取物的数值和强化视觉、弹幕行为。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            Node demo = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn").Instantiate();
            AddChild(demo);
            var player = demo.GetNode<PlayerController>("Player");
            var buffs = player.GetNode<PlayerBuffController>("Buffs");
            var shooter = player.GetNode<AutoShooter>("AutoShooter");
            var ecsWorld = demo.GetNode<EcsCombatWorld>("CombatEntities/EcsCombatWorld");
            var spawner = demo.GetNode<EnemySpawner>("EnemySpawner");
            var pickupSpawner = demo.GetNode<PickupSpawner>("PickupSpawner");

            Require(spawner.AliveCount > 0, "Enemy spawner did not create the initial wave.");
            await ToSignal(GetTree().CreateTimer(2.4), SceneTreeTimer.SignalName.Timeout);
            Require(shooter.ShotsFired > 0, "Auto shooter did not fire at the nearest enemy.");
            Require(ecsWorld.TotalProjectilesSpawned == shooter.ShotsFired &&
                ecsWorld.TotalProjectilesSpawned > 0,
                $"Automatic shooting did not enter the ECS projectile runtime: shots={shooter.ShotsFired}, ecs={ecsWorld.TotalProjectilesSpawned}.");
            Require(demo.GetNode<Node2D>("CombatEntities/Projectiles").GetChildCount() == 0,
                "ECS projectile mode must not create per-projectile scene nodes.");
            Require(spawner.DefeatedCount > 0 && ecsWorld.DefeatedCount > 0,
                "Automatic projectiles did not defeat an ECS enemy.");

            pickupSpawner.Spawn(PickupKind.RapidFire, player.GlobalPosition);
            await WaitForPickup();
            Require(Mathf.IsEqualApprox(buffs.FireRateMultiplier, 2.0f),
                "Rapid-fire pickup must provide exactly double fire rate.");

            pickupSpawner.Spawn(PickupKind.MoveSpeed, player.GlobalPosition);
            await WaitForPickup();
            Require(Mathf.IsEqualApprox(buffs.SpeedMultiplier, 1.5f),
                "Move-speed pickup must provide exactly 1.5x movement speed.");

            int shotsBeforeSpiral = shooter.ShotsFired;
            pickupSpawner.Spawn(PickupKind.SpiralShot, player.GlobalPosition);
            await WaitForPickup();
            Require(buffs.IsSpiralActive && Mathf.IsEqualApprox(buffs.FireRateMultiplier, 20.0f),
                "Power pickup must activate the 20x spiral state.");
            var visual = player.GetNode<PlayerVisualController>("Visual");
            Require(visual.DisplayName == "博丽灵梦" && visual.IsArmedVisible,
                "Power pickup did not preserve the player name and enable the armed text marker.");
            await ToSignal(GetTree().CreateTimer(0.08), SceneTreeTimer.SignalName.Timeout);
            Require(shooter.ShotsFired >= shotsBeforeSpiral + 2,
                "Spiral state did not fire the required opposite projectile pair.");

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
