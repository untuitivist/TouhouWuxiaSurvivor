using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Gameplay.Encounters;
using TouhouWuxiaSurvivor.Gameplay.Session;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Ui.Completion;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.Ui.Pause;
using TouhouWuxiaSurvivor.Ui.Stats;

namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 协调首次最终Boss后的暂停选择、成功结算与无尽转换，并让时间来源和UI保持可注入。
/// </summary>
public sealed class RunPacingCoordinator : IDisposable
{
    private readonly BossEncounterDirector _bosses;
    private readonly RunCompletionOverlay _completion;
    private readonly WorldMapOverlay _map;
    private readonly PauseMenuOverlay _pauseMenu;
    private readonly CharacterStatsOverlay _stats;
    private readonly RunProgressionCoordinator _progression;
    private readonly Func<double> _elapsedSeconds;
    private readonly Func<RunCombatTelemetry> _combatTelemetry;
    private readonly Func<bool> _isFinalized;
    private readonly Func<RunEndReason, bool> _finalize;
    private readonly AdaptiveRunPacingState _adaptiveState = new();
    private bool _disposed;

    public bool IsEndless { get; private set; }
    public bool IsCompletionPending { get; private set; }

    /// <summary>
    /// 注入Boss事件、只读时钟、模态界面和终局回调；协调器不创建节点或读取全局服务。
    /// </summary>
    public RunPacingCoordinator(
        BossEncounterDirector bosses,
        RunCompletionOverlay completion,
        WorldMapOverlay map,
        PauseMenuOverlay pauseMenu,
        CharacterStatsOverlay stats,
        RunProgressionCoordinator progression,
        Func<double> elapsedSeconds,
        Func<RunCombatTelemetry> combatTelemetry,
        Func<bool> isFinalized,
        Func<RunEndReason, bool> finalize)
    {
        ArgumentNullException.ThrowIfNull(bosses);
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(pauseMenu);
        ArgumentNullException.ThrowIfNull(stats);
        ArgumentNullException.ThrowIfNull(progression);
        ArgumentNullException.ThrowIfNull(elapsedSeconds);
        ArgumentNullException.ThrowIfNull(combatTelemetry);
        ArgumentNullException.ThrowIfNull(isFinalized);
        ArgumentNullException.ThrowIfNull(finalize);
        _bosses = bosses;
        _completion = completion;
        _map = map;
        _pauseMenu = pauseMenu;
        _stats = stats;
        _progression = progression;
        _elapsedSeconds = elapsedSeconds;
        _combatTelemetry = combatTelemetry;
        _isFinalized = isFinalized;
        _finalize = finalize;
        bosses.EncounterDefeated += OnEncounterDefeated;
        completion.SettleRequested += OnSettleRequested;
        completion.ContinueEndlessRequested += OnContinueEndlessRequested;
    }

    /// <summary>
    /// 每帧消费战斗遥测并推进动态阶段；首次进入最终遭遇时只武装一次角色 Boss。
    /// </summary>
    public void Advance()
    {
        if (_disposed || IsEndless)
        {
            return;
        }

        bool wasFinal = _adaptiveState.IsFinalEncounter;
        _adaptiveState.Advance(_elapsedSeconds(), _combatTelemetry());
        if (!wasFinal && _adaptiveState.IsFinalEncounter)
        {
            _bosses.ArmFirstEncounter(_elapsedSeconds());
        }
    }

    /// <summary>以动态阶段状态创建 HUD 快照，真实时间与难度时间保持显式分离。</summary>
    public RunPacingSnapshot CreateSnapshot() =>
        _adaptiveState.CreateSnapshot(_elapsedSeconds(), IsEndless);

    /// <summary>
    /// 解除全部事件连接；重复释放安全返回，防止重开场景后旧协调器继续接收Boss事件。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _bosses.EncounterDefeated -= OnEncounterDefeated;
        _completion.SettleRequested -= OnSettleRequested;
        _completion.ContinueEndlessRequested -= OnContinueEndlessRequested;
    }

    /// <summary>
    /// 仅把四分半后的首次角色Boss视为本体终点；测试或无尽中的普通Boss不会重复打开选择层。
    /// </summary>
    private void OnEncounterDefeated(CharacterDefinition character)
    {
        if (IsEndless || IsCompletionPending || _isFinalized() ||
            !_adaptiveState.IsFinalEncounter)
        {
            return;
        }

        IsCompletionPending = true;
        _progression.SuspendChoicePresentation();
        SetOtherInputBlocked(true);
        _completion.Present(character.DisplayName, _elapsedSeconds());
    }

    /// <summary>
    /// 保持暂停并把成功原因交给统一结算入口；结算失败时恢复选择层以免玩家被困在空界面。
    /// </summary>
    private void OnSettleRequested()
    {
        if (!IsCompletionPending)
        {
            return;
        }

        _completion.CloseForSettlement();
        if (_finalize(RunEndReason.Cleared))
        {
            IsCompletionPending = false;
            return;
        }

        _completion.Present(_bosses.LastSpawnedCharacter?.DisplayName ?? "异变核心",
            _elapsedSeconds());
    }

    /// <summary>
    /// 将本局永久切换为无尽状态，恢复被完成选择临时占用的地图、暂停和属性输入。
    /// </summary>
    private void OnContinueEndlessRequested()
    {
        if (!IsCompletionPending)
        {
            return;
        }

        IsCompletionPending = false;
        IsEndless = true;
        SetOtherInputBlocked(false);
        _completion.CloseAndResume();
        _progression.ResumeChoicePresentation();
    }

    /// <summary>
    /// 同步切换三个可与完成层竞争的模态输入入口，且不改变它们各自的可见状态。
    /// </summary>
    private void SetOtherInputBlocked(bool blocked)
    {
        _map.InputBlocked = blocked;
        _pauseMenu.InputBlocked = blocked;
        _stats.InputBlocked = blocked;
    }
}
