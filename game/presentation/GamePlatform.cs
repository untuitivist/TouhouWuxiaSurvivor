using Godot;

namespace Rebirth.Presentation;

public static class GamePlatform
{
    public static bool IsWeb => OS.HasFeature("web");
    public static bool HasTouch => DisplayServer.IsTouchscreenAvailable();
    public static bool CanResizeWindow => !IsWeb && DisplayServer.GetName() != "headless";
    public static string StorageNotice => IsWeb && !OS.IsUserfsPersistent() ? "浏览器未提供持久存储，关闭页面后可能丢失本次记录。" : "";

    public static bool IsPortrait()
    {
        var size = DisplayServer.WindowGetSize();
        return IsWeb && size.X < size.Y;
    }
}
