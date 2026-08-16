using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Gameplay.Encounters;

/// <summary>
/// 独立安排角色 Boss 遭遇；候选来自启用内容并严格排除当前自机，不会污染普通敌人刷新权重。
/// </summary>
public partial class BossEncounterDirector : Node
{
    private readonly RandomNumberGenerator _random = new();
    private EcsCombatWorld? _world;
    private RunContentContext? _context;
    private Func<Vector2>? _playerPosition;
    private double _nextEncounterSeconds;
    private double _activeEncounterStartedSeconds;
    private bool _encounterActive;

    [Export(PropertyHint.Range, "30,900,1")]
    public double EncounterIntervalSeconds { get; set; } = 180.0;

    public int SpawnedCount { get; private set; }
    public int DefeatedCount { get; private set; }
    public CharacterDefinition? LastSpawnedCharacter { get; private set; }
    public double NextEncounterSeconds => _nextEncounterSeconds;
    public bool IsFirstEncounterArmed { get; private set; }
    public event Action<CharacterDefinition>? EncounterDefeated;

    /// <summary>
    /// 绑定 ECS 世界、冻结的局内内容和玩家位置查询；首次遭遇等待动态阶段导演显式武装。
    /// </summary>
    public void Configure(
        EcsCombatWorld world,
        RunContentContext context,
        Func<Vector2> playerPosition)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(playerPosition);
        Unsubscribe();
        _world = world;
        _context = context;
        _playerPosition = playerPosition;
        _nextEncounterSeconds = double.PositiveInfinity;
        _activeEncounterStartedSeconds = 0.0;
        _encounterActive = false;
        IsFirstEncounterArmed = false;
        _random.Randomize();
        world.ConfigureBossAttacks(new SpellCardBossAttackResolver());
        world.BossDefeated += OnBossDefeated;
    }

    /// <summary>
    /// 由动态阶段导演显式开放首次最终遭遇；重复调用不会延后已经到期的 Boss。
    /// </summary>
    public void ArmFirstEncounter(double elapsedSeconds)
    {
        if (IsFirstEncounterArmed)
        {
            return;
        }

        IsFirstEncounterArmed = true;
        _nextEncounterSeconds = double.IsFinite(elapsedSeconds)
            ? Math.Max(0.0, elapsedSeconds)
            : 0.0;
    }

    /// <summary>每帧只检查到期条件；已有存活 Boss 或候选为空时不会叠加生成。</summary>
    public override void _Process(double delta)
    {
        if (_world is null || _context is null || _playerPosition is null ||
            !IsFirstEncounterArmed || _encounterActive ||
            _world.ElapsedSeconds < _nextEncounterSeconds ||
            _world.AliveBossCount > 0)
        {
            return;
        }

        Vector2 center = _playerPosition();
        float angle = _random.RandfRange(0.0f, Mathf.Tau);
        Vector2 spawnPosition = center + Vector2.FromAngle(angle) * 320.0f;
        if (!TrySpawn(spawnPosition, _world.ElapsedSeconds))
        {
            _nextEncounterSeconds = _world.ElapsedSeconds + 30.0;
        }
    }

    /// <summary>
    /// 尝试从合法候选中生成一个 Boss；测试可指定候选索引，负值则使用局内随机数。
    /// </summary>
    public bool TrySpawn(
        Vector2 position,
        double elapsedSeconds,
        int candidateIndex = -1)
    {
        if (_world is null || _context is null || _encounterActive ||
            _world.AliveBossCount > 0)
        {
            return false;
        }

        IReadOnlyList<CharacterDefinition> candidates = CharacterBossCatalog.GetCandidates(
            _context.ContentSelection, _context.CharacterSelection);
        if (candidates.Count == 0)
        {
            return false;
        }

        int index = candidateIndex >= 0
            ? Math.Clamp(candidateIndex, 0, candidates.Count - 1)
            : _random.RandiRange(0, candidates.Count - 1);
        CharacterDefinition character = candidates[index];
        _world.SpawnBoss(position, BossDefinitionFactory.Create(character));
        LastSpawnedCharacter = character;
        SpawnedCount++;
        _activeEncounterStartedSeconds = Math.Max(0.0, elapsedSeconds);
        _encounterActive = true;
        _nextEncounterSeconds = double.PositiveInfinity;
        return true;
    }

    /// <summary>
    /// 结束当前 Boss 遭遇并从结束时刻安排完整恢复期；该入口同时服务击破事件和场景强制结束，
    /// 重复结束会返回 false 且不重置计时，防止多个清理通知无限延后下一次遭遇。
    /// </summary>
    public bool ResolveActiveEncounter(double elapsedSeconds)
    {
        if (!_encounterActive)
        {
            return false;
        }

        double normalizedSeconds = double.IsFinite(elapsedSeconds)
            ? Math.Max(0.0, elapsedSeconds)
            : _activeEncounterStartedSeconds;
        double resolutionSeconds = Math.Max(_activeEncounterStartedSeconds, normalizedSeconds);
        _nextEncounterSeconds = resolutionSeconds + Math.Max(1.0, EncounterIntervalSeconds);
        _encounterActive = false;
        return true;
    }

    /// <summary>离开场景树时取消世界事件订阅，避免重开一局后旧导演继续累计击破。</summary>
    public override void _ExitTree() => Unsubscribe();

    /// <summary>
    /// 记录角色 Boss 击破，并从战斗结束时重新计算完整恢复期；若测试直接指定了未来生成时间，
    /// 使用生成时间与世界时间中的较大值，避免下一场遭遇被长战斗或测试时钟提前吞掉。
    /// </summary>
    private void OnBossDefeated(Vector2 position, EnemyDefinition definition)
    {
        if (ResolveActiveEncounter(_world?.ElapsedSeconds ?? _activeEncounterStartedSeconds))
        {
            DefeatedCount++;
            if (LastSpawnedCharacter is not null)
            {
                EncounterDefeated?.Invoke(LastSpawnedCharacter);
            }
        }
    }

    /// <summary>安全解除当前世界订阅，并保留配置数据供同一节点随后重新绑定。</summary>
    private void Unsubscribe()
    {
        if (_world is not null)
        {
            _world.BossDefeated -= OnBossDefeated;
        }
    }
}
