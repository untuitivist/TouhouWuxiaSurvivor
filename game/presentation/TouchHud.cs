using Rebirth.Core;
using Godot;

namespace Rebirth.Presentation;

public partial class TouchHud : Control
{
    public static readonly Vector2 StickCenter = new(160, 535);
    public static readonly Vector2 DashCenter = new(1135, 548);
    public static readonly Vector2 FocusCenter = new(988, 598);
    public Vector2 Movement { get; private set; }
    public bool FocusHeld => focusPointer >= 0;
    public Action? DashPressed;
    public Action? PausePressed;
    public Action? InspectPressed;
    public Font? BodyFont;
    private int movePointer = -1;
    private int focusPointer = -1;
    private int dashPointer = -1;
    private bool playing;
    private bool seenTouch;
    private int mode;

    public TouchHud()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        Size = new(1280, 720);
        Visible = false;
    }

    public void SetContext(bool inCombat, int preference)
    {
        playing = inCombat;
        mode = preference;
        var enabled = playing && mode != 2 && (mode == 1 || seenTouch || GamePlatform.HasTouch);
        if (!enabled) ResetPointers();
        Visible = enabled;
    }

    public void ResetPointers()
    {
        movePointer = focusPointer = dashPointer = -1;
        Movement = Vector2.Zero;
        QueueRedraw();
    }

    public bool Handle(InputEvent input)
    {
        if (input is InputEventScreenTouch touch)
        {
            seenTouch = true;
            SetContext(playing, mode);
            if (!Visible) return false;
            if (!touch.Pressed || touch.Canceled)
            {
                var owned = touch.Index == movePointer || touch.Index == focusPointer || touch.Index == dashPointer;
                if (touch.Index == movePointer) { movePointer = -1; Movement = Vector2.Zero; }
                if (touch.Index == focusPointer) focusPointer = -1;
                if (touch.Index == dashPointer) dashPointer = -1;
                QueueRedraw();
                return owned;
            }
            if (touch.Position.DistanceTo(StickCenter) <= 112 && movePointer < 0)
            {
                movePointer = touch.Index;
                UpdateStick(touch.Position);
            }
            else if (touch.Position.DistanceTo(DashCenter) <= 68 && dashPointer < 0)
            {
                dashPointer = touch.Index;
                DashPressed?.Invoke();
            }
            else if (touch.Position.DistanceTo(FocusCenter) <= 57 && focusPointer < 0) focusPointer = touch.Index;
            else if (HudLayout.PauseButton.HasPoint(touch.Position)) PausePressed?.Invoke();
            else if (HudLayout.InspectButton.HasPoint(touch.Position)) InspectPressed?.Invoke();
            else return false;
            QueueRedraw();
            return true;
        }
        if (input is InputEventScreenDrag drag && Visible && drag.Index == movePointer)
        {
            UpdateStick(drag.Position);
            return true;
        }
        return false;
    }

    private void UpdateStick(Vector2 position)
    {
        var offset = (position - StickCenter) / 82;
        Movement = offset.Length() < 0.16f ? Vector2.Zero : offset.LimitLength();
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (BodyFont == null) return;
        Ring(StickCenter, 90, new Color("8ac2af"));
        DrawTextureRect(PixelSkin.Artwork("touch-grip"), new(StickCenter + Movement * 62 - Vector2.One * 23, Vector2.One * 46), false);
        Ring(DashCenter, 64, dashPointer >= 0 ? Palette.Red : Palette.Gold);
        Ring(FocusCenter, 53, FocusHeld ? Palette.Jade : Palette.Paper);
        LabelAt(GameText.Get("闪身"), DashCenter, 24);
        LabelAt(GameText.Get("慢移"), FocusCenter, 23);
        DrawStyleBox(PixelSkin.Frame("panel"), HudLayout.PauseButton);
        DrawStyleBox(PixelSkin.Frame("panel"), HudLayout.InspectButton);
        LabelAt(GameText.Get("暂停"), HudLayout.PauseButton.GetCenter(), 23, Palette.Ink);
        LabelAt(GameText.Get("构筑"), HudLayout.InspectButton.GetCenter(), 23, Palette.Ink);
    }

    private void Ring(Vector2 center, float radius, Color color)
    {
        DrawTextureRect(PixelSkin.Artwork("touch-disc"), new(center - Vector2.One * radius, Vector2.One * radius * 2), false, color);
    }

    private void LabelAt(string text, Vector2 center, int size, Color? color = null)
    {
        text = GameText.Get(text);
        var width = BodyFont!.GetStringSize(text, fontSize: size).X;
        DrawString(BodyFont, center + new Vector2(-width / 2, size / 3), text, fontSize: size, modulate: color ?? Palette.Paper);
    }
}
