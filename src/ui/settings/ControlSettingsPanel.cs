using Godot;
using TouhouWuxiaSurvivor.Settings;

namespace TouhouWuxiaSurvivor.Ui.Settings;

/// <summary>
/// 根据输入动作目录生成双键绑定列表，并在捕获状态下把下一次键盘输入写入选定槽位。
/// </summary>
public partial class ControlSettingsPanel : VBoxContainer
{
    private readonly Dictionary<(string Action, int Slot), Button> _buttons = [];
    private VBoxContainer? _rows;
    private Label? _status;
    private string? _captureAction;
    private int _captureSlot;

    public bool IsCapturing => _captureAction is not null;

    /// <summary>
    /// 获取容器并按目录顺序创建每个操作的名称、主键和副键按钮。
    /// </summary>
    public override void _Ready()
    {
        GameSettingsService.Initialize();
        _rows = GetNode<VBoxContainer>("Scroll/Rows");
        _status = GetNode<Label>("Header/Status");
        GetNode<Button>("Header/Reset").Pressed += ResetBindings;
        BuildRows();
        RefreshButtons();
    }

    /// <summary>
    /// 捕获选键状态下的下一次非重复按键，更新绑定并消费事件，防止菜单同时响应。
    /// </summary>
    public override void _Input(InputEvent inputEvent)
    {
        if (_captureAction is null || inputEvent is not InputEventKey keyEvent ||
            !keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        Key key = keyEvent.PhysicalKeycode != Key.None
            ? keyEvent.PhysicalKeycode
            : keyEvent.Keycode;
        bool changed = GameSettingsService.SetBinding(_captureAction, _captureSlot, key);
        if (_status is not null)
        {
            _status.Text = changed
                ? "按键已保存"
                : "同一操作的两个键不能重复";
        }

        _captureAction = null;
        RefreshButtons();
        GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// 为每个输入动作构建固定高度行；两个按钮分别捕获槽位 0 和槽位 1。
    /// </summary>
    private void BuildRows()
    {
        if (_rows is null)
        {
            return;
        }

        foreach (InputActionDefinition definition in InputActionCatalog.All)
        {
            var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 30) };
            var label = new Label
            {
                Text = definition.DisplayName,
                CustomMinimumSize = new Vector2(140, 0),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            row.AddChild(label);

            for (int slot = 0; slot < 2; slot++)
            {
                int capturedSlot = slot;
                var button = new Button { CustomMinimumSize = new Vector2(105, 26) };
                button.Pressed += () => BeginCapture(definition.Id, capturedSlot);
                row.AddChild(button);
                _buttons[(definition.Id, slot)] = button;
            }

            _rows.AddChild(row);
        }
    }

    /// <summary>
    /// 进入指定动作槽位的捕获状态，并在对应按钮与状态栏提示玩家输入。
    /// </summary>
    private void BeginCapture(string actionId, int slot)
    {
        _captureAction = actionId;
        _captureSlot = slot;
        RefreshButtons();
        _buttons[(actionId, slot)].Text = "请按键...";
        if (_status is not null)
        {
            _status.Text = "按下新的物理按键；Esc 也可以作为绑定";
        }
    }

    /// <summary>
    /// 取消当前捕获并恢复全部默认键位，再刷新按钮和状态提示。
    /// </summary>
    private void ResetBindings()
    {
        _captureAction = null;
        GameSettingsService.ResetBindings();
        RefreshButtons();
        if (_status is not null)
        {
            _status.Text = "已恢复默认按键";
        }
    }

    /// <summary>
    /// 从设置快照刷新全部按钮文本，并恢复非捕获按钮的可用状态。
    /// </summary>
    private void RefreshButtons()
    {
        foreach (InputActionDefinition definition in InputActionCatalog.All)
        {
            long[] bindings = GameSettingsService.Current.Bindings[definition.Id];
            for (int slot = 0; slot < 2; slot++)
            {
                Button button = _buttons[(definition.Id, slot)];
                button.Text = GetKeyText(bindings[slot]);
                button.Disabled = IsCapturing &&
                    (_captureAction != definition.Id || _captureSlot != slot);
            }
        }
    }

    /// <summary>
    /// 使用 Godot 的物理键格式化规则生成适合按钮显示的本地化键名。
    /// </summary>
    private static string GetKeyText(long keyValue)
    {
        if ((Key)keyValue == Key.None)
        {
            return "未绑定";
        }

        var keyEvent = new InputEventKey { PhysicalKeycode = (Key)keyValue };
        return keyEvent.AsTextPhysicalKeycode();
    }
}
