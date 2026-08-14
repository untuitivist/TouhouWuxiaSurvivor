using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Streaming;

namespace TouhouWuxiaSurvivor.Demo;

/// <summary>
/// 在固定物理步统一重定位玩家、场景实体、ECS 数据和区块原点，保持无限世界数值稳定。
/// </summary>
public sealed class WorldOriginRebaseCoordinator
{
    private readonly ChunkStreamer _streamer;
    private readonly PlayerController _player;
    private readonly Node2D _combatEntities;
    private readonly EcsCombatWorld _ecsWorld;

    /// <summary>绑定一次本局不变的世界与战斗对象，避免组合根逐帧查找场景节点。</summary>
    public WorldOriginRebaseCoordinator(
        ChunkStreamer streamer,
        PlayerController player,
        Node2D combatEntities,
        EcsCombatWorld ecsWorld)
    {
        _streamer = streamer;
        _player = player;
        _combatEntities = combatEntities;
        _ecsWorld = ecsWorld;
    }

    /// <summary>
    /// 到达安全阈值时执行一次完整重定位；未越界时不遍历战斗容器并返回 false。
    /// </summary>
    public bool Update()
    {
        ChunkCoordinate localChunk = GridMath.LocalPositionToChunk(_player.Position);
        if (Math.Abs(localChunk.X) < WorldMetrics.RebaseDistanceChunks &&
            Math.Abs(localChunk.Y) < WorldMetrics.RebaseDistanceChunks)
        {
            return false;
        }

        var offset = new Vector2(
            localChunk.X * WorldMetrics.ChunkPixels,
            localChunk.Y * WorldMetrics.ChunkPixels);
        _player.Position -= offset;
        _player.ResetPhysicsInterpolation();
        RebaseSceneEntities(offset);
        _ecsWorld.Rebase(offset);
        _streamer.Rebase(localChunk);
        return true;
    }

    /// <summary>平移仍保留的低数量场景实体，并清除 Godot 自动插值中的旧世界坐标。</summary>
    private void RebaseSceneEntities(Vector2 offset)
    {
        foreach (Node category in _combatEntities.GetChildren())
        {
            if (category == _ecsWorld)
            {
                continue;
            }

            foreach (Node child in category.GetChildren())
            {
                if (child is not Node2D entity)
                {
                    continue;
                }

                entity.Position -= offset;
                entity.ResetPhysicsInterpolation();
            }
        }
    }
}
