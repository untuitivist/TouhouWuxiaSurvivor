using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Ui.Death;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.Ui.Pause;
using TouhouWuxiaSurvivor.Ui.Stats;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Streaming;

namespace TouhouWuxiaSurvivor.Gameplay.Session;

/// <summary>
/// 统一处理失败终局的输入封锁、世界快照、幂等奖励结算和总结展示，使场景根只负责依赖装配。
/// </summary>
public sealed class RunFailureCoordinator
{
    private readonly ChunkStreamer _streamer;
    private readonly WorldGenerator _generator;
    private readonly PlayerController _player;
    private readonly WorldMapOverlay _map;
    private readonly PauseMenuOverlay _pauseMenu;
    private readonly DeathScreenOverlay _failureScreen;
    private readonly EnemySpawner _enemySpawner;
    private readonly RunProgressionCoordinator _progression;
    private readonly MetaRunSession _metaRun;
    private readonly CharacterStatsOverlay _stats;
    private readonly SpellCardCoordinator? _spellCards;
    private readonly ContentPackSelection _content;
    private readonly EcsCombatWorld? _ecsWorld;

    public bool IsFinalized { get; private set; }

    /// <summary>
    /// 接收当前单局所需的稳定依赖；协调器不创建节点或服务，也不拥有主菜单导航权限。
    /// </summary>
    public RunFailureCoordinator(
        ChunkStreamer streamer,
        WorldGenerator generator,
        PlayerController player,
        WorldMapOverlay map,
        PauseMenuOverlay pauseMenu,
        DeathScreenOverlay failureScreen,
        EnemySpawner enemySpawner,
        RunProgressionCoordinator progression,
        MetaRunSession metaRun,
        CharacterStatsOverlay stats,
        SpellCardCoordinator? spellCards,
        ContentPackSelection content,
        EcsCombatWorld? ecsWorld = null)
    {
        _streamer = streamer;
        _generator = generator;
        _player = player;
        _map = map;
        _pauseMenu = pauseMenu;
        _failureScreen = failureScreen;
        _enemySpawner = enemySpawner;
        _progression = progression;
        _metaRun = metaRun;
        _stats = stats;
        _spellCards = spellCards;
        _content = content;
        _ecsWorld = ecsWorld;
    }

    /// <summary>
    /// 首次调用时冻结所有局内交互、捕获最终统计并完成结算；重复终局信号不再生成快照或界面。
    /// </summary>
    public bool TryFinalize(RunEndReason endReason)
    {
        if (IsFinalized)
        {
            return false;
        }

        IsFinalized = true;
        (long tileX, long tileY) = GridMath.LocalPositionToAbsoluteTile(
            _player.Position,
            _streamer.OriginChunk);
        BiomeId biome = _generator.Biomes.Select(tileX, tileY);
        _map.InputBlocked = true;
        _pauseMenu.BlockForRunEnd();
        _stats.InputBlocked = true;
        _stats.CancelForRunEnd();
        _progression.BlockForRunEnd();
        _spellCards?.BlockForRunEnd();

        RunSettlementResult settlement = _metaRun.Settle(
            _ecsWorld?.ElapsedSeconds ?? _enemySpawner.ElapsedSeconds,
            _ecsWorld?.DefeatedCount ?? _enemySpawner.DefeatedCount,
            _progression.State.Level);
        _failureScreen.Present(new RunSummary(
            endReason,
            _enemySpawner.ElapsedSeconds,
            _ecsWorld?.DefeatedCount ?? _enemySpawner.DefeatedCount,
            tileX,
            tileY,
            BiomeNames.GetChinese(biome),
            _content.Describe(),
            _generator.Seed,
            _progression.State.Level,
            _progression.State.TotalExperience,
            _progression.Build.Describe(),
            settlement.Reward,
            settlement.Balance));
        return true;
    }
}
