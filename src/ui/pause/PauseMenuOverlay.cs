using Godot;
using TouhouWuxiaSurvivor.Settings;
using TouhouWuxiaSurvivor.Ui.Compendium;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.Ui.Settings;

namespace TouhouWuxiaSurvivor.Ui.Pause;

/// <summary>
/// 管理游戏内暂停、设置、离开确认和场景退出流程，并协调全屏地图的互斥输入。
/// </summary>
public partial class PauseMenuOverlay : CanvasLayer
{
    private Control? _root;
    private Control? _pausePanel;
    private SettingsPanel? _settingsPanel;
    private CompendiumPanel? _compendiumPanel;
    private Control? _confirmPanel;
    private Label? _confirmMessage;
    private WorldMapOverlay? _map;
    private Action? _confirmedAction;
    private bool _wasPaused;

    public bool IsOpen => _root?.Visible ?? false;
    public bool InputBlocked { get; set; }
    public event Action? RunAbandonRequested;

    /// <summary>
    /// 缓存菜单节点、连接全部按钮，并让暂停层在场景初始状态下隐藏。
    /// </summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        GameSettingsService.Initialize();
        _root = GetNode<Control>("Root");
        _pausePanel = GetNode<Control>("Root/PausePanel");
        _settingsPanel = GetNode<SettingsPanel>("Root/SettingsPanel");
        _compendiumPanel = GetNode<CompendiumPanel>("Root/CompendiumPanel");
        _confirmPanel = GetNode<Control>("Root/ConfirmPanel");
        _confirmMessage = GetNode<Label>("Root/ConfirmPanel/Padding/Layout/Message");

        GetNode<Button>("Root/PausePanel/Padding/Layout/Continue").Pressed += Close;
        GetNode<Button>("Root/PausePanel/Padding/Layout/Settings").Pressed += ShowSettings;
        GetNode<Button>("Root/PausePanel/Padding/Layout/Compendium").Pressed += ShowCompendium;
        GetNode<Button>("Root/PausePanel/Padding/Layout/MainMenu").Pressed += ConfirmMainMenu;
        GetNode<Button>("Root/PausePanel/Padding/Layout/Desktop").Pressed += ConfirmDesktop;
        GetNode<Button>("Root/ConfirmPanel/Padding/Layout/Buttons/Cancel").Pressed += ShowPausePanel;
        GetNode<Button>("Root/ConfirmPanel/Padding/Layout/Buttons/Confirm").Pressed += ExecuteConfirmedAction;
        _settingsPanel.BackRequested += ShowPausePanel;
        _compendiumPanel.BackRequested += ShowPausePanel;
        _root.Hide();
    }

    /// <summary>
    /// 处理暂停动作：优先关闭地图，其次返回上级面板，最后切换暂停菜单开关。
    /// </summary>
    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (InputBlocked || !inputEvent.IsActionPressed("pause_menu"))
        {
            return;
        }

        if (_settingsPanel?.IsCapturingKey == true)
        {
            return;
        }

        if (_map?.Visible == true)
        {
            _map.Close();
        }
        else if (!IsOpen)
        {
            Open();
        }
        else if (_settingsPanel?.Visible == true || _compendiumPanel?.Visible == true ||
            _confirmPanel?.Visible == true)
        {
            ShowPausePanel();
        }
        else
        {
            Close();
        }

        GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// 注入同场景的地图覆盖层，使两个全屏模态界面共享明确的互斥规则。
    /// </summary>
    public void Configure(WorldMapOverlay map)
    {
        _map = map;
    }

    /// <summary>
    /// 打开暂停菜单、记录此前暂停状态并阻止地图响应快捷键。
    /// </summary>
    public void Open()
    {
        if (IsOpen || _root is null)
        {
            return;
        }

        _wasPaused = GetTree().Paused;
        GetTree().Paused = true;
        _map!.InputBlocked = true;
        _root.Show();
        ShowPausePanel();
    }

    /// <summary>
    /// 关闭暂停菜单并精确恢复打开前的暂停状态，重复调用不会产生副作用。
    /// </summary>
    public void Close()
    {
        if (!IsOpen || _root is null)
        {
            return;
        }

        _root.Hide();
        _map!.InputBlocked = false;
        GetTree().Paused = _wasPaused;
    }

    /// <summary>
    /// 在本局进入失败结算时关闭所有暂停子面板并封锁输入，同时保留当前暂停状态供总结界面接管。
    /// </summary>
    public void BlockForRunEnd()
    {
        InputBlocked = true;
        _confirmedAction = null;
        _pausePanel?.Hide();
        _settingsPanel?.Hide();
        _compendiumPanel?.Hide();
        _confirmPanel?.Hide();
        _root?.Hide();
        if (_map is not null)
        {
            _map.InputBlocked = true;
        }
    }

    /// <summary>
    /// 隐藏暂停和确认面板，仅显示共享设置面板。
    /// </summary>
    private void ShowSettings()
    {
        _pausePanel!.Hide();
        _confirmPanel!.Hide();
        _compendiumPanel!.Hide();
        _settingsPanel!.Show();
    }

    /// <summary>
    /// 隐藏暂停首页并打开复用图鉴，场景树继续保持暂停状态。
    /// </summary>
    private void ShowCompendium()
    {
        _pausePanel!.Hide();
        _settingsPanel!.Hide();
        _confirmPanel!.Hide();
        _compendiumPanel!.Present();
    }

    /// <summary>
    /// 返回暂停菜单首页，并清除尚未执行的确认动作。
    /// </summary>
    private void ShowPausePanel()
    {
        _confirmedAction = null;
        _settingsPanel!.Hide();
        _compendiumPanel!.Hide();
        _confirmPanel!.Hide();
        _pausePanel!.Show();
    }

    /// <summary>
    /// 显示返回主菜单确认提示，并明确说明主动离场会按失败完成本局结算。
    /// </summary>
    private void ConfirmMainMenu() => ShowConfirmation(
        "返回主菜单将按失败结算本局，是否继续？",
        RequestRunAbandon);

    /// <summary>
    /// 显示返回桌面确认提示，并登记确认后的安全退出动作。
    /// </summary>
    private void ConfirmDesktop() => ShowConfirmation(
        "确定要结束游戏并返回桌面吗？",
        QuitToDesktop);

    /// <summary>
    /// 切换到确认面板并保存用户确认后需要调用的动作。
    /// </summary>
    private void ShowConfirmation(string message, Action action)
    {
        _confirmedAction = action;
        _confirmMessage!.Text = message;
        _pausePanel!.Hide();
        _settingsPanel!.Hide();
        _compendiumPanel!.Hide();
        _confirmPanel!.Show();
    }

    /// <summary>
    /// 取出并执行一次确认动作，防止按钮重复触发同一场景操作。
    /// </summary>
    private void ExecuteConfirmedAction()
    {
        Action? action = _confirmedAction;
        _confirmedAction = null;
        action?.Invoke();
    }

    /// <summary>
    /// 广播主动结束本局的意图，由场景根节点完成失败结算和总结展示。
    /// </summary>
    private void RequestRunAbandon() => RunAbandonRequested?.Invoke();

    /// <summary>
    /// 请求 Godot 正常结束进程并返回桌面。
    /// </summary>
    private void QuitToDesktop() => GetTree().Quit();
}
