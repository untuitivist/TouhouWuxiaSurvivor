using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Diagnostics.Performance;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.World.Streaming;

namespace TouhouWuxiaSurvivor.Demo.Diagnostics;

/// <summary>
/// 把正式世界的已有计数投影为诊断快照，使 WorldDemo 组合根不承担日志字段拼装职责。
/// </summary>
public static class WorldPerformanceDiagnosticsSnapshotFactory
{
    /// <summary>
    /// 创建一秒采样所需的聚合记录；只读取池、流送器和成长状态，不遍历任何单体实体。
    /// </summary>
    public static PerformanceDiagnosticsRuntimeSnapshot Capture(
        string sceneName,
        ContentPackSelection content,
        RunContentContext runContext,
        PlayerController player,
        ChunkStreamer streamer,
        EcsCombatWorld combatWorld,
        RunProgressionCoordinator progression)
    {
        CharacterDefinition character = runContext.CharacterSelection.Current;
        return new PerformanceDiagnosticsRuntimeSnapshot(
            sceneName, content.Describe(), character.CharacterId, character.DisplayName,
            combatWorld.ElapsedSeconds, player.Position.X, player.Position.Y,
            streamer.ActiveCount, streamer.PendingCount,
            combatWorld.AliveEnemyCount, combatWorld.EnemyCount, combatWorld.AliveBossCount,
            combatWorld.DefeatedCount, combatWorld.ProjectileCount,
            combatWorld.EnemyProjectileCount, combatWorld.ProjectileCapacity,
            combatWorld.PickupCount, combatWorld.SpiritCount,
            progression.State.Level,
            combatWorld.MappedEnemyVisualCount + combatWorld.MappedBossVisualCount,
            combatWorld.FallbackEnemyVisualCount + combatWorld.FallbackBossVisualCount,
            combatWorld.ProjectileCollisionCandidateChecks,
            player.GetTree().Paused,
            FindActiveModal(player));
    }

    /// <summary>
    /// 返回当前可见且会暂停玩法的顶层界面名称，帮助区分低帧与升级、地图或暂停状态。
    /// </summary>
    private static string FindActiveModal(Node context)
    {
        Node? scene = context.GetTree().CurrentScene;
        if (scene is null)
        {
            return "scene-transition";
        }

        foreach (string path in new[]
        {
            "LevelUpOverlay",
            "PauseMenuOverlay",
            "DeathScreenOverlay",
            "CharacterStatsOverlay",
            "MapLayer/WorldMapOverlay",
        })
        {
            Node? node = scene.GetNodeOrNull(path);
            if (node is CanvasItem { Visible: true })
            {
                return node.Name;
            }
        }

        return "none";
    }
}
