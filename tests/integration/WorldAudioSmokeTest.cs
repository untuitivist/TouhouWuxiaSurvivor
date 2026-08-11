using Godot;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Audio.World;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Ecs.Combat;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实世界场景中验证音频总线、原作循环 BGM 和主要战斗事件到东方音效的完整连接。
/// </summary>
public partial class WorldAudioSmokeTest : Node
{
    /// <summary>
    /// 实例化游戏页并依次触发射击、敌人受击死亡、拾取、强化高射速和玩家受伤音效。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            WorldDemo demo = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn")
                .Instantiate<WorldDemo>();
            demo.PersistMetaProgression = false;
            AddChild(demo);
            var audio = demo.GetNode<WorldAudioController>("WorldAudio");
            var player = demo.GetNode<PlayerController>("Player");
            var health = player.GetNode<PlayerHealth>("Health");
            var shooter = player.GetNode<AutoShooter>("AutoShooter");
            var ecsWorld = demo.GetNode<EcsCombatWorld>("CombatEntities/EcsCombatWorld");
            var pickups = demo.GetNode<PickupSpawner>("PickupSpawner");

            VerifyAudioRouting(audio);
            Input.ActionPress("move_right");
            await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
            Require(audio.GetNode<AudioStreamPlayer>("Footstep").Playing,
                "Player movement did not start footstep audio.");
            Input.ActionRelease("move_right");
            await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
            Require(!audio.GetNode<AudioStreamPlayer>("Footstep").Playing,
                "Stopping movement did not stop footstep audio.");

            await WaitForCombatAudio(audio);
            Require(audio.ShotSoundCount > 0, "Automatic firing did not trigger shot audio.");
            Require(audio.EnemyHitSoundCount > 0, "Non-lethal enemy damage did not trigger hit audio.");
            Require(audio.EnemyDeathSoundCount > 0, "Enemy defeat did not trigger death audio.");

            pickups.Spawn(PickupKind.SpiralShot, player.GlobalPosition);
            await WaitForPickup();
            Require(audio.PickupSoundCount == 1, "Collected power pickup did not trigger pickup audio.");
            int projectilesBefore = shooter.ShotsFired;
            int soundsBefore = audio.ShotSoundCount;
            await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
            int projectileDelta = shooter.ShotsFired - projectilesBefore;
            int soundDelta = audio.ShotSoundCount - soundsBefore;
            Require(projectileDelta > soundDelta && soundDelta > 0,
                "High-rate spiral fire was not safely throttled at the audio layer.");

            health.InvincibilityDuration = 0.0f;
            Require(health.ApplyDamage(1), "Player damage setup failed.");
            Require(audio.PlayerHurtSoundCount == 1, "Player damage did not trigger hurt audio.");
            Require(health.ApplyDamage(health.CurrentHealth), "Player death setup failed.");
            Require(audio.PlayerDeathSoundCount == 1 && !audio.IsBgmPlaying,
                "Player death did not stop BGM and trigger death audio.");
            GD.Print("World audio smoke test passed.");
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
    /// 等待拾取碰撞完成两个物理步，使音效断言不依赖当前帧中 Area2D 的处理顺序。
    /// </summary>
    private async Task WaitForPickup()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    /// <summary>
    /// 在三秒上限内等待自动战斗产生射击、受击和击破事件，避免依赖单次固定帧时序。
    /// </summary>
    private async Task WaitForCombatAudio(WorldAudioController audio)
    {
        const int maximumAttempts = 60;
        for (int attempt = 0; attempt < maximumAttempts; attempt++)
        {
            if (audio.ShotSoundCount > 0 &&
                audio.EnemyHitSoundCount > 0 &&
                audio.EnemyDeathSoundCount > 0)
            {
                return;
            }

            await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
        }
    }

    /// <summary>
    /// 检查三层总线和场景播放器路由，并确认游戏页加载后 BGM 已经开始播放。
    /// </summary>
    private static void VerifyAudioRouting(WorldAudioController audio)
    {
        Require(AudioServer.GetBusIndex("Music") >= 0 && AudioServer.GetBusIndex("SFX") >= 0,
            "Music or SFX audio bus is missing.");
        Require(audio.GetNode<AudioStreamPlayer>("Bgm").Bus == "Music" &&
            audio.GetNode<AudioStreamPlayer>("Shot").Bus == "SFX",
            "Audio players are not routed to their category buses.");
        Require(audio.GetNode<AudioStreamPlayer>("Bgm").Stream is AudioStreamOggVorbis
            { Loop: true } bgm && Mathf.IsEqualApprox(bgm.LoopOffset, 32.4946f),
            "Original Reimu BGM did not retain its declared loop start.");
        Require(audio.IsBgmPlaying, "World BGM did not start when the game page loaded.");
    }

    /// <summary>
    /// 将任一音频契约失败转换为带有明确原因的集成测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
