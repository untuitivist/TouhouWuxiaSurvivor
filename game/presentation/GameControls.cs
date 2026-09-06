using Godot;

namespace Rebirth.Presentation;

public static class GameControls
{
    public const string Left = "run_left";
    public const string Right = "run_right";
    public const string Up = "run_up";
    public const string Down = "run_down";
    public const string Focus = "run_focus";
    public const string Dash = "run_dash";
    public const string Pause = "run_pause";
    public const string Inspect = "run_inspect";
    public const string Fullscreen = "run_fullscreen";
    public const string Debug = "run_debug";
    public const string ChoiceOne = "run_choice_one";
    public const string ChoiceTwo = "run_choice_two";
    public const string ChoiceThree = "run_choice_three";
    private static readonly Dictionary<string, (string Primary, string Both)> hints = [];

    public static readonly (string Id, string Name, Key Primary, Key Secondary)[] Actions =
    [
        (Up, "向上移动", Key.W, Key.Up), (Down, "向下移动", Key.S, Key.Down),
        (Left, "向左移动", Key.A, Key.Left), (Right, "向右移动", Key.D, Key.Right),
        (Focus, "慢移 / 判定点", Key.Shift, Key.None), (Dash, "闪身", Key.Space, Key.None),
        (Pause, "暂停 / 返回", Key.Escape, Key.P), (Inspect, "属性与构筑", Key.E, Key.None),
        (Fullscreen, "切换全屏", Key.F11, Key.None),
        (Debug, "诊断信息", Key.F3, Key.None),
        (ChoiceOne, "选择第一项", Key.Key1, Key.Kp1), (ChoiceTwo, "选择第二项", Key.Key2, Key.Kp2), (ChoiceThree, "选择第三项", Key.Key3, Key.Kp3)
    ];

    public static Dictionary<string, long[]> DefaultBindings() => Actions.ToDictionary(action => action.Id, action => new[] { (long)action.Primary, (long)action.Secondary });

    public static bool ValidKey(Key key) => key == Key.None || Enum.IsDefined(key) && key is not (Key.Unknown or Key.Tab or Key.Delete) && (long)key >= 32;

    public static Dictionary<string, long[]> NormalizeBindings(Dictionary<string, long[]>? source)
    {
        var result = new Dictionary<string, long[]>();
        var occupied = new HashSet<long>();
        foreach (var action in Actions)
        {
            var keys = source != null && source.TryGetValue(action.Id, out var saved) && saved is { Length: 2 }
                ? (long[])saved.Clone() : [(long)action.Primary, (long)action.Secondary];
            for (var slot = 0; slot < 2; slot++)
                if (!ValidKey((Key)keys[slot]) || keys[slot] == (long)Key.Escape && action.Id != Pause || keys[slot] != 0 && !occupied.Add(keys[slot])) keys[slot] = 0;
            result[action.Id] = keys;
        }
        foreach (var action in Actions)
        {
            if (result[action.Id].Any(key => key != 0)) continue;
            var fallback = new[] { action.Primary, action.Secondary }.Concat(Enumerable.Range((int)Key.F1, 35).Select(value => (Key)value));
            result[action.Id][0] = (long)fallback.First(key => key != Key.None && !occupied.Contains((long)key));
            occupied.Add(result[action.Id][0]);
        }
        return result;
    }

    public static string? SetBinding(Dictionary<string, long[]> bindings, string action, int slot, Key key)
    {
        if (!bindings.ContainsKey(action) || slot is < 0 or > 1 || !ValidKey(key)) return "此按键保留给界面操作，请选择其他键。";
        if (key == Key.Escape && action != Pause) return "Esc 保留为安全返回键。";
        if (key == Key.None && bindings[action][1 - slot] == 0) return "每个操作至少保留一个按键。";
        if (key != Key.None)
            foreach (var definition in Actions)
                for (var candidate = 0; candidate < 2; candidate++)
                    if ((definition.Id != action || candidate != slot) && bindings[definition.Id][candidate] == (long)key)
                        return $"{KeyText(key)} 已用于「{definition.Name}」，请先修改该绑定。";
        bindings[action][slot] = (long)key;
        Configure(bindings);
        return null;
    }

    public static string KeyText(Key key) => key == Key.None ? "未绑定" : OS.GetKeycodeString(key);
    public static string Hint(string action, bool both = false)
    {
        return hints.TryGetValue(action, out var text) ? both ? text.Both : text.Primary : "未绑定";
    }

    public static void Configure(Dictionary<string, long[]>? bindings = null)
    {
        bindings ??= DefaultBindings();
        foreach (var action in Actions)
        {
            var keys = bindings[action.Id].Where(key => key != 0).Select(key => (Key)key).ToArray();
            Bind(action.Id, keys);
            hints[action.Id] = (KeyText(keys.FirstOrDefault()), string.Join(" / ", keys.Select(KeyText)));
        }
    }

    private static void Bind(string action, params Key[] keys)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action);
        Input.ActionRelease(action);
        InputMap.ActionEraseEvents(action);
        foreach (var key in keys) InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }
}
