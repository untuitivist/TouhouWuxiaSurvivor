using Rebirth.Core;
using Godot;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private string? captureAction;
    private int captureSlot;
    private readonly Dictionary<(string Action, int Slot), Button> bindingButtons = [];

    private void BuildControlSettings(Control panel)
    {
        bindingButtons.Clear();
        for (var index = 0; index < GameControls.Actions.Length; index++)
        {
            var action = GameControls.Actions[index];
            var horizontal = index < 7 ? 36 : 548;
            var vertical = 218 + index % 7 * 39;
            if (index % 7 == 0)
            {
                ui.Label(panel, GameText.Get("操作"), new(horizontal, 190, 154, 25), 14, Palette.Muted);
                ui.Label(panel, GameText.Get("主键"), new(horizontal + 160, 190, 135, 25), 14, Palette.Gold);
                ui.Label(panel, GameText.Get("副键"), new(horizontal + 310, 190, 135, 25), 14, Palette.Gold);
            }
            ui.Label(panel, action.Name, new(horizontal, vertical + 4, 156, 29), 16);
            for (var slot = 0; slot < 2; slot++)
            {
                var capturedSlot = slot;
                var button = ui.Button(panel, GameControls.KeyText((Key)profile.Data.Bindings[action.Id][slot]), new(horizontal + 160 + slot * 150, vertical, 142, 35), () => BeginBindingCapture(action.Id, capturedSlot));
                button.AddThemeFontSizeOverride("font_size", 15);
                button.Name = $"{action.Id}_{slot}";
                bindingButtons[(action.Id, slot)] = button;
            }
        }
        ui.Label(panel, GameText.Get("选择键位后按新键；Esc 取消，Delete 清空此槽，每项至少保留一键。\n冲突会拒绝并说明原因；Tab 与 Delete 保留给界面。Esc 始终可安全返回。"), new(36, 493, 988, 49), 16, Palette.Muted);
    }

    private void BeginBindingCapture(string action, int slot)
    {
        captureAction = action;
        captureSlot = slot;
        foreach (var entry in bindingButtons)
        {
            entry.Value.Disabled = entry.Key != (action, slot);
            entry.Value.Text = entry.Key == (action, slot) ? GameText.Get("请按键…") : GameControls.KeyText((Key)profile.Data.Bindings[entry.Key.Action][entry.Key.Slot]);
        }
        settingsWarning!.Text = GameText.Get("正在等待按键：Esc 取消 · Delete 清空。不接受组合键。");
    }

    private bool CaptureBinding(InputEventKey key)
    {
        if (captureAction == null) return false;
        if (!key.Pressed || key.Echo) return true;
        var code = key.PhysicalKeycode == Key.None ? key.Keycode : key.PhysicalKeycode;
        var action = captureAction;
        var slot = captureSlot;
        if (code == Key.Escape) settingsMessage = GameText.Get("已取消改键。");
        else if (key.CtrlPressed && code != Key.Ctrl || key.AltPressed && code != Key.Alt || key.MetaPressed && code != Key.Meta || key.ShiftPressed && code != Key.Shift)
            settingsMessage = GameText.Get("只支持单个物理键，请勿使用组合键。");
        else settingsMessage = GameControls.SetBinding(profile.Data.Bindings, action, slot, code == Key.Delete ? Key.None : code) ?? GameText.Get("键位已应用。");
        SaveSettings();
        bindingButtons[(action, slot)].GrabFocus();
        return true;
    }
}
