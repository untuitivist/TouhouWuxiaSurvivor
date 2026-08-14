using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Map;
using TouhouWuxiaSurvivor.World.Streaming;

namespace TouhouWuxiaSurvivor.Demo;

/// <summary>
/// 连接正式世界的区块流送与旅行地图发现，使组合根无需维护探索坐标细节。
/// </summary>
public sealed class WorldDemoMapRuntime
{
    private readonly ChunkStreamer _streamer;
    private readonly PlayerController _player;
    private readonly WorldMapDiscovery _discovery;

    /// <summary>
    /// 创建地图运行时，并把探索存储与已发现地标注入正式地图界面。
    /// </summary>
    public WorldDemoMapRuntime(
        ChunkStreamer streamer,
        PlayerController player,
        WorldMapOverlay map,
        WorldMapDiscovery discovery,
        DiscoveredStructureStore structures)
    {
        _streamer = streamer;
        _player = player;
        _discovery = discovery;
        map.Configure(streamer.ExploredMap, structures);
    }

    /// <summary>
    /// 按当前玩家绝对 Tile 更新圆形探索视野与地标发现。
    /// </summary>
    public void Update()
    {
        (long tileX, long tileY) = GridMath.LocalPositionToAbsoluteTile(
            _player.Position,
            _streamer.OriginChunk);
        _discovery.Update(tileX, tileY);
    }
}
