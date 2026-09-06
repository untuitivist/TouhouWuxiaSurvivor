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

    public static void Configure()
    {
        Bind(Left, Key.A, Key.Left);
        Bind(Right, Key.D, Key.Right);
        Bind(Up, Key.W, Key.Up);
        Bind(Down, Key.S, Key.Down);
        Bind(Focus, Key.Shift);
        Bind(Dash, Key.Space);
        Bind(Pause, Key.Escape, Key.P);
        Bind(Inspect, Key.E);
    }

    private static void Bind(string action, params Key[] keys)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action);
        InputMap.ActionEraseEvents(action);
        foreach (var key in keys) InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }
}
