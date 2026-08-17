using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Audio.World;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Diagnostics.Performance;
using TouhouWuxiaSurvivor.Demo.Diagnostics;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Session;
using TouhouWuxiaSurvivor.Gameplay.Encounters;
using TouhouWuxiaSurvivor.Gameplay.Pacing;
using TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Meta.Persistence;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Settings;
using TouhouWuxiaSurvivor.Ui.Debug;
using TouhouWuxiaSurvivor.Ui.Death;
using TouhouWuxiaSurvivor.Ui.Completion;
using TouhouWuxiaSurvivor.Ui.Hud;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.Ui.Pause;
using TouhouWuxiaSurvivor.Ui.Progression;
using TouhouWuxiaSurvivor.Ui.Stats;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Map;
using TouhouWuxiaSurvivor.World.Rendering;
using TouhouWuxiaSurvivor.World.Streaming;

namespace TouhouWuxiaSurvivor.Demo;

/// <summary>
/// 无限世界演示的组合根节点，连接玩家、生成器、流送器、地图和调试界面。
/// </summary>
public partial class WorldDemo : Node2D
{
    private ChunkStreamer? _streamer;
    private WorldGenerator? _generator;
    private PlayerController? _player;
    private WorldHudCoordinator? _hudCoordinator;
    private WorldMapOverlay? _map;
    private PauseMenuOverlay? _pauseMenu;
    private DeathScreenOverlay? _deathScreen;
    private RunCompletionOverlay? _completionScreen;
    private Node2D? _combatEntities;
    private EnemySpawner? _enemySpawner;
    private PickupSpawner? _pickupSpawner;
    private SpiritDropSpawner? _spiritSpawner;
    private RunProgressionCoordinator? _progression;
    private PlayerBuffController? _buffs;
    private PlayerHealth? _health;
    private AutoShooter? _autoShooter;
    private EcsCombatWorld? _ecsCombatWorld;
    private WorldAudioController? _audio;
    private LevelUpOverlay? _levelUp;
    private MetaRunSession? _metaRun;
    private CharacterStatsOverlay? _stats;
    private SpellCardCoordinator? _spellCards;
    private SpellCardEffectAssets? _spellAssets;
    private RunFailureCoordinator? _runFailure;
    private BossEncounterDirector? _bossEncounters;
    private RunPacingCoordinator? _runPacing;
    private WorldDemoMapRuntime? _mapRuntime;
    private WorldOriginRebaseCoordinator? _originRebase;
    private ContentPackSelection _content = ContentPackSelection.BaseOnly;
    private RunContentContext? _runContext;

    public RunContentContext RunContext => _runContext ?? throw new InvalidOperationException(
        "Run content context is not ready.");

    public BossEncounterDirector BossEncounters => _bossEncounters ?? throw new InvalidOperationException(
        "Boss encounter director is not ready.");

    public RunPacingCoordinator RunPacing => _runPacing ?? throw new InvalidOperationException(
        "Run pacing coordinator is not ready.");

    [Export]
    public long WorldSeed { get; set; } = 20260728;

    [Export(PropertyHint.Range, "1,4,1")]
    public int LoadRadius { get; set; } = 2;

    [Export(PropertyHint.Range, "1,8,1")]
    public int ChunksPerFrame { get; set; } = 3;

    [Export]
    public bool PersistMetaProgression { get; set; } = true;

    /// <summary>
    /// 解析场景依赖，构造无限世界服务，并连接刷怪、掉落、索敌和自动武器流程。
    /// </summary>
    public override void _Ready()
    {
        GameSettingsService.Initialize();
        _player = GetNode<PlayerController>("Player");
        var hud = GetNode<WorldDebugHud>("WorldDebugHud");
        _map = GetNode<WorldMapOverlay>("MapLayer/WorldMapOverlay");
        _pauseMenu = GetNode<PauseMenuOverlay>("PauseMenuOverlay");
        _deathScreen = GetNode<DeathScreenOverlay>("DeathScreenOverlay");
        _completionScreen = GetNode<RunCompletionOverlay>("RunCompletionOverlay");
        _combatEntities = GetNode<Node2D>("CombatEntities");
        _enemySpawner = GetNode<EnemySpawner>("EnemySpawner");
        _pickupSpawner = GetNode<PickupSpawner>("PickupSpawner");
        _spiritSpawner = GetNode<SpiritDropSpawner>("SpiritDropSpawner");
        _progression = GetNode<RunProgressionCoordinator>("RunProgressionCoordinator");
        _levelUp = GetNode<LevelUpOverlay>("LevelUpOverlay");
        _stats = GetNode<CharacterStatsOverlay>("CharacterStatsOverlay");
        _spellCards = GetNode<SpellCardCoordinator>("SpellCardCoordinator");
        _spellAssets = GetNode<SpellCardEffectAssets>("SpellCardEffectAssets");
        _buffs = _player.GetNode<PlayerBuffController>("Buffs");
        _health = _player.GetNode<PlayerHealth>("Health");
        _autoShooter = _player.GetNode<AutoShooter>("AutoShooter");
        _audio = GetNode<WorldAudioController>("WorldAudio");
        _metaRun = PersistMetaProgression
            ? new MetaRunSession()
            : new MetaRunSession(new ProgressionProfileManager(
                new VolatileProgressionProfileStore()));
        Node2D enemies = _combatEntities.GetNode<Node2D>("Enemies");
        Node2D projectiles = _combatEntities.GetNode<Node2D>("Projectiles");
        _ecsCombatWorld = _combatEntities.GetNode<EcsCombatWorld>("EcsCombatWorld");
        Node2D pickups = _combatEntities.GetNode<Node2D>("Pickups");
        Node2D spiritDrops = _combatEntities.GetNode<Node2D>("SpiritDrops");
        Node2D spellEffects = _combatEntities.GetNode<Node2D>("SpellEffects");
        _content = ContentPackSelectionService.Current;
        _runContext = new RunContentContext(
            _content, CharacterSelectionService.Current, WorldSeed);
        CharacterDefinition character = _runContext.CharacterSelection.Current;
        _player.GetNode<PlayerVisualController>("Visual").ConfigureCharacter(character);
        _player.MoveSpeed *= character.PlayableProfile.MoveSpeedMultiplier;
        _health.ConfigureCharacterBase((int)MathF.Round(character.PlayableProfile.MaxHealth));
        _autoShooter.CharacterAttackMultiplier = character.PlayableProfile.AttackMultiplier;
        _autoShooter.CharacterAttackIntervalMultiplier =
            character.PlayableProfile.AttackIntervalMultiplier;
        _generator = new WorldGenerator(unchecked((ulong)WorldSeed), _content);
        var renderer = new CompositeChunkRenderer(
            new ChunkTileMapRenderer(GetNode<TileMapLayer>("Ground")),
            new InternalBiomeTileRenderer(
                GetNode<TileMapLayer>("InternalBiomeGround"), _generator.Biomes),
            new InternalStructureRenderer(
                GetNode<Node2D>("InternalStructures"), _generator.StructureLocations));
        _streamer = new ChunkStreamer(_generator, renderer, LoadRadius, ChunksPerFrame);
        _streamer.Prime(_player.Position);
        var discoveredStructures = new DiscoveredStructureStore();
        var mapDiscovery = new WorldMapDiscovery(
            _streamer.ExploredMap,
            discoveredStructures,
            _generator.StructureLocations);
        _mapRuntime = new WorldDemoMapRuntime(
            _streamer, _player, _map, mapDiscovery, discoveredStructures);
        _mapRuntime.Update();
        _pauseMenu.Configure(_map);
        ProfileRunBonuses bonuses = _metaRun.Bonuses;
        _progression.Modifiers.ConfigureBase(
            bonuses.DamageBonus,
            bonuses.MoveSpeedMultiplier,
            bonuses.SpiritAttractionMultiplier);
        _health.ConfigureMaximumHealthBonus(bonuses.MaxHealthBonus);
        _stats.Configure(
            () => CharacterStatsSnapshotFactory.Create(
                _player, _health, _autoShooter, _buffs,
                _spiritSpawner, _progression, bonuses, _spellCards, _content),
            _map,
            _pauseMenu);
        _progression.Configure(_levelUp, _map, _pauseMenu, _stats, _content);
        _player.ConfigureRunModifiers(_progression.Modifiers);
        _pickupSpawner.Configure(pickups);
        _ecsCombatWorld.Configure(_player, _health, _buffs, _progression.Modifiers);
        _originRebase = new WorldOriginRebaseCoordinator(
            _streamer, _player, _combatEntities, _ecsCombatWorld);
        _bossEncounters = new BossEncounterDirector { Name = "BossEncounterDirector" };
        AddChild(_bossEncounters);
        _bossEncounters.Configure(_ecsCombatWorld, _runContext, () => _player.Position);
        _enemySpawner.ConfigureEcs(_ecsCombatWorld);
        _pickupSpawner.ConfigureEcs(_ecsCombatWorld);
        _spiritSpawner.ConfigureEcs(_ecsCombatWorld);
        _spiritSpawner.Configure(
            spiritDrops,
            _player,
            _progression.Modifiers,
            _progression.State);
        if (_player.PassiveSpecializations is not null)
        {
            _spiritSpawner.SpiritCollected +=
                _player.PassiveSpecializations.RegisterSpiritCollected;
        }
        WorldDemoSpellRuntime.Configure(
            _spellCards, _spellAssets, _player, _health, _autoShooter, _progression,
            character.PlayableProfile, enemies, spellEffects, _ecsCombatWorld, _content);
        _audio.Configure(_player, _health, _autoShooter, _enemySpawner, _pickupSpawner);
        _enemySpawner.EnemyDefeated += _pickupSpawner.TrySpawnForEnemy;
        _enemySpawner.EnemyDefeated += _spiritSpawner.SpawnForEnemy;
        _enemySpawner.Configure(_player, enemies, _content, GetBiomeAtLocalPosition);
        _autoShooter.Configure(enemies, projectiles, _buffs, _health, _progression.Modifiers, null, _ecsCombatWorld);
        _runFailure = new RunFailureCoordinator(
            _streamer, _generator, _player, _map, _pauseMenu, _deathScreen,
            _enemySpawner, _progression, _metaRun, _stats, _spellCards, _content, _ecsCombatWorld);
        _runPacing = new RunPacingCoordinator(
            _bossEncounters, _completionScreen, _map, _pauseMenu, _stats, _progression,
            () => _ecsCombatWorld.ElapsedSeconds,
            () => new RunCombatTelemetry(
                _enemySpawner.SpawnedCount,
                _enemySpawner.DefeatedCount),
            () => _runFailure.IsFinalized,
            _runFailure.TryFinalize);
        _enemySpawner.ConfigurePacing(() => _runPacing.CreateSnapshot().DifficultySeconds);
        _progression.ConfigureRepeatableUpgrades(() => _runPacing.IsEndless);
        _hudCoordinator = new WorldHudCoordinator(
            _generator, _streamer, _player, hud, _map, _enemySpawner,
            _buffs, _health, _progression, _content, _spellCards, _runPacing,
            _ecsCombatWorld);
        _health.Died += OnPlayerDied;
        _pauseMenu.RunAbandonRequested += OnRunAbandoned;
        _deathScreen.RestartRequested += RestartRun;
        _deathScreen.MainMenuRequested += ReturnToMainMenu;
        _hudCoordinator.Refresh();
        PerformanceDiagnosticsHost.AttachWorld(this, () =>
            WorldPerformanceDiagnosticsSnapshotFactory.Capture(
                Name, _content, _runContext!, _player!, _streamer!,
                _ecsCombatWorld!, _progression!));
    }

    /// <summary>在固定物理步委托原点重定位，避免渲染帧直接移动玩家碰撞体。</summary>
    public override void _PhysicsProcess(double delta) => _originRebase?.Update();

    /// <summary>每个渲染帧更新区块流送、地图发现和低频 HUD，运动显示由物理插值负责。</summary>
    public override void _Process(double delta)
    {
        if (_streamer is null || _player is null)
        {
            return;
        }

        _runPacing?.Advance();
        _streamer.Update(_player.Position);
        _mapRuntime?.Update();
        _hudCoordinator?.Refresh(delta);
    }

    /// <summary>
    /// 世界离开场景树时解除诊断委托，防止常驻节点在场景切换期间读取已释放节点。
    /// </summary>
    public override void _ExitTree()
    {
        _runPacing?.Dispose();
        PerformanceDiagnosticsHost.DetachWorld(this);
    }

    /// <summary>
    /// 把任意本地像素位置转换成绝对 Tile 并查询群系，供刷怪器选择地区生态而不依赖世界实现。
    /// </summary>
    private BiomeId GetBiomeAtLocalPosition(Vector2 localPosition)
    {
        if (_streamer is null || _generator is null)
        {
            return BiomeId.Common;
        }

        (long tileX, long tileY) = GridMath.LocalPositionToAbsoluteTile(
            localPosition,
            _streamer.OriginChunk);
        return _generator.Biomes.Select(tileX, tileY);
    }

    /// <summary>
    /// 在玩家生命归零时把本局交给共享失败终局入口，避免死亡路径单独维护结算规则。
    /// </summary>
    private void OnPlayerDied() => _runFailure?.TryFinalize(RunEndReason.Defeated);

    /// <summary>
    /// 在暂停菜单确认主动离场时按失败终结本局，而不是直接切换场景绕过结算。
    /// </summary>
    private void OnRunAbandoned() => _runFailure?.TryFinalize(RunEndReason.Abandoned);

    /// <summary>
    /// 清除暂停状态并重新载入游戏场景，以当前内容包选择开始一局全新探索。
    /// </summary>
    private void RestartRun()
        => WorldDemoSceneTransition.Restart(GetTree());

    /// <summary>
    /// 清除死亡暂停状态并切换回主菜单，避免主菜单继承无法处理的暂停状态。
    /// </summary>
    private void ReturnToMainMenu()
        => WorldDemoSceneTransition.ReturnToMainMenu(GetTree());
}
