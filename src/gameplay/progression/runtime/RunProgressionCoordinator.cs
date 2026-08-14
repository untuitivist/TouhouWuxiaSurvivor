using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.Ui.Pause;
using TouhouWuxiaSurvivor.Ui.Progression;
using TouhouWuxiaSurvivor.Ui.Stats;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 协调经验状态、构筑、倍率和升级选择，但不负责敌人掉落、玩家移动或武器射击。
/// </summary>
public partial class RunProgressionCoordinator : Node
{
    private readonly RandomNumberGenerator _random = new();
    private readonly RunOfferGenerator _offerGenerator = new();
    private LevelUpOverlay? _overlay;
    private ContentPackSelection _content = ContentPackSelection.BaseOnly;
    private bool _runEndBlocked;
    private bool _choicePresentationSuspended;

    public RunProgressionState State { get; } = new();
    public RunBuildState Build { get; } = new();
    public RunModifierState Modifiers { get; } = new();
    public bool IsChoicePresentationSuspended => _choicePresentationSuspended;

    /// <summary>
    /// 注入升级层及其互斥界面，连接状态与选择事件并初始化独立随机源。
    /// </summary>
    public void Configure(
        LevelUpOverlay overlay,
        WorldMapOverlay map,
        PauseMenuOverlay pauseMenu,
        CharacterStatsOverlay stats,
        ContentPackSelection? content = null)
    {
        _overlay = overlay;
        _overlay.Configure(map, pauseMenu, stats);
        _overlay.ChoiceSelected += OnChoiceSelected;
        State.Changed += OnProgressChanged;
        _content = content ?? ContentPackSelectionService.Current;
        _random.Randomize();
    }

    /// <summary>
    /// 本局结束时阻止后续升级弹出，并让失败总结界面接管当前暂停状态。
    /// </summary>
    public void BlockForRunEnd()
    {
        _runEndBlocked = true;
        _overlay?.CancelForRunEnd();
    }

    /// <summary>
    /// 临时阻止新升级层出现但保留待选次数，供通关选择等更高优先级模态界面安全接管暂停。
    /// </summary>
    public void SuspendChoicePresentation() => _choicePresentationSuspended = true;

    /// <summary>
    /// 解除临时挂起并立即检查积压选择；终局封锁后调用不会重新打开已经结束的玩法界面。
    /// </summary>
    public void ResumeChoicePresentation()
    {
        if (_runEndBlocked)
        {
            return;
        }

        _choicePresentationSuspended = false;
        OnProgressChanged();
    }

    /// <summary>
    /// 经验状态变化后仅在确有待选升级且没有其他升级层时展示下一组三选一。
    /// </summary>
    private void OnProgressChanged()
    {
        if (_runEndBlocked || _choicePresentationSuspended ||
            State.PendingChoices <= 0 || _overlay?.IsOpen == true)
        {
            return;
        }

        PresentNextChoice();
    }

    /// <summary>
    /// 从所有未满重定义中抽取三项；全部满重时自动消耗该次升级并继续处理积压。
    /// </summary>
    private void PresentNextChoice()
    {
        while (State.PendingChoices > 0)
        {
            IReadOnlyList<RunUpgradeChoice> choices =
                _offerGenerator.CreateOffer(_random, Build, _content, State.Level, 3);
            if (choices.Count > 0)
            {
                _overlay!.Present(choices, Build, State.Level);
                return;
            }

            State.ResolveChoice();
        }

        _overlay!.CloseAndRestore();
    }

    /// <summary>
    /// 应用玩家选择、重建倍率并解决一次待选升级；存在积压时直接刷新下一组选项。
    /// </summary>
    private void OnChoiceSelected(RunUpgradeChoice choice)
    {
        if (_runEndBlocked || !Build.Apply(choice, State.Level))
        {
            return;
        }

        Modifiers.Refresh(Build);
        State.ResolveChoice();
        if (State.PendingChoices > 0)
        {
            PresentNextChoice();
        }
        else
        {
            _overlay!.CloseAndRestore();
        }
    }
}
