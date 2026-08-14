using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Pacing;
using TouhouWuxiaSurvivor.Ui.Debug;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Streaming;

namespace TouhouWuxiaSurvivor.Ui.Hud;

/// <summary>
/// 从无限世界与战斗组件采集单帧状态，并把不可变快照交给地图标记和紧凑 HUD。
/// </summary>
public sealed class WorldHudCoordinator
{
    private readonly WorldGenerator _generator;
    private readonly ChunkStreamer _streamer;
    private readonly PlayerController _player;
    private readonly WorldDebugHud _hud;
    private readonly WorldMapOverlay _map;
    private readonly EnemySpawner _enemies;
    private readonly PlayerBuffController _buffs;
    private readonly PlayerHealth _health;
    private readonly RunProgressionCoordinator _progression;
    private readonly ContentPackSelection _content;
    private readonly SpellCardCoordinator _spellCards;
    private readonly RunPacingCoordinator _pacing;
    private readonly EcsCombatWorld? _ecsWorld;

    /// <summary>
    /// 注入所有只读状态来源，使组合根不再承担 HUD 坐标换算与快照拼装职责。
    /// </summary>
    public WorldHudCoordinator(
        WorldGenerator generator,
        ChunkStreamer streamer,
        PlayerController player,
        WorldDebugHud hud,
        WorldMapOverlay map,
        EnemySpawner enemies,
        PlayerBuffController buffs,
        PlayerHealth health,
        RunProgressionCoordinator progression,
        ContentPackSelection content,
        SpellCardCoordinator spellCards,
        RunPacingCoordinator pacing,
        EcsCombatWorld? ecsWorld = null)
    {
        _generator = generator;
        _streamer = streamer;
        _player = player;
        _hud = hud;
        _map = map;
        _enemies = enemies;
        _buffs = buffs;
        _health = health;
        _progression = progression;
        _content = content;
        _spellCards = spellCards;
        _pacing = pacing;
        _ecsWorld = ecsWorld;
    }

    /// <summary>
    /// 换算玩家绝对坐标、更新地图标记，并以同一帧数据刷新生命、战斗和经验显示。
    /// </summary>
    public void Refresh(double deltaSeconds = 1.0 / 60.0)
    {
        (long tileX, long tileY) = GridMath.LocalPositionToAbsoluteTile(
            _player.Position,
            _streamer.OriginChunk);
        var chunk = new ChunkCoordinate(
            GridMath.FloorDiv(tileX, WorldMetrics.ChunkTiles),
            GridMath.FloorDiv(tileY, WorldMetrics.ChunkTiles));
        BiomeId biome = _generator.Biomes.Select(tileX, tileY);
        _map.SetPlayerTile(tileX, tileY);
        _hud.Refresh(new WorldHudSnapshot(
            _generator.Seed,
            tileX,
            tileY,
            chunk,
            biome,
            _streamer.ActiveCount,
            _streamer.PendingCount,
            _ecsWorld?.AliveEnemyCount ?? _enemies.AliveCount,
            _ecsWorld?.DefeatedCount ?? _enemies.DefeatedCount,
            _health.CurrentHealth,
            _health.MaxHealth,
            _ecsWorld?.ElapsedSeconds ?? _enemies.ElapsedSeconds,
            _buffs.DescribeActiveEffects(),
            _content.Describe(),
            _progression.State.Level,
            _progression.State.Experience,
            _progression.State.ExperienceToNext,
            _pacing.CreateSnapshot(),
            _spellCards.CreateSnapshot()), deltaSeconds);
    }
}
