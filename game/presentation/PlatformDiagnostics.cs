using Godot;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void TestSharedPlatform()
    {
        Require("博丽灵梦雾雨魔理沙夜境异闻".All(character => canvas.BodyFont.HasChar(character)), "Bundled font covers both heroes and game title");
        var controls = new TouchHud();
        controls.SetContext(true, 1);
        var dashes = 0;
        controls.DashPressed = () => dashes++;
        controls.Handle(new InputEventScreenTouch { Index = 11, Position = TouchHud.StickCenter + new Vector2(75, 0), Pressed = true });
        controls.Handle(new InputEventScreenTouch { Index = 12, Position = TouchHud.FocusCenter, Pressed = true });
        controls.Handle(new InputEventScreenTouch { Index = 13, Position = TouchHud.DashCenter, Pressed = true });
        Require(controls.Movement.X > 0.8f && controls.FocusHeld && dashes == 1, "Three independent fingers move/focus/dash");
        controls.Handle(new InputEventScreenDrag { Index = 13, Position = TouchHud.StickCenter - new Vector2(500, 0) });
        Require(controls.Movement.X > 0.8f, "Other fingers cannot steal the joystick");
        controls.Handle(new InputEventScreenTouch { Index = 12, Pressed = false, Position = Vector2.Zero });
        Require(!controls.FocusHeld && controls.Movement.X > 0.8f, "Releasing focus preserves the moving finger");
        controls.Handle(new InputEventScreenTouch { Index = 11, Canceled = true, Pressed = true });
        Require(controls.Movement == Vector2.Zero, "Canceled touch releases movement");
        controls.Handle(new InputEventScreenTouch { Index = 14, Position = TouchHud.FocusCenter, Pressed = true });
        controls.SetContext(false, 1);
        Require(!controls.Visible && !controls.FocusHeld && controls.Movement == Vector2.Zero, "Menus release every captured finger");
        controls.SetContext(true, 2);
        Require(!controls.Handle(new InputEventScreenTouch { Index = 15, Position = TouchHud.StickCenter, Pressed = true }), "Hidden touch controls cannot consume input");
        controls.Free();
        GD.Print("SHARED_PLATFORM_PASS: bundled Chinese font and multi-pointer ownership/cancel/menu reset");
    }
}
