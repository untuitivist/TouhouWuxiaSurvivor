using Godot;
using TouhouWuxiaSurvivor.Ui.Content;
using TouhouWuxiaSurvivor.Ui.Pause;
using TouhouWuxiaSurvivor.Ui.Settings;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实渲染器中覆盖主菜单、设置、内容选择和暂停首页，锁定统一像素主题与紧凑边界。
/// </summary>
public partial class UiShellVisualAcceptanceTest : Node
{
    /// <summary>
    /// 依次驱动四个正式界面状态，验证关键面板和按钮未逃出逻辑视口并保存桌面截图。
    /// </summary>
    public override async void _Ready()
    {
        int exitCode = 0;
        try
        {
            GetWindow().Size = new Vector2I(1280, 720);
            await CaptureMainMenuStates();
            await CapturePauseState();
            GD.Print("UI shell visual acceptance test passed.");
        }
        catch (Exception exception)
        {
            exitCode = 1;
            GD.PushError(exception.ToString());
        }
        finally
        {
            GetTree().Paused = false;
            GetTree().Quit(exitCode);
        }
    }

    /// <summary>
    /// 实例化正式主菜单，使用玩家按钮切换设置与内容页，并在每次容器稳定后截图。
    /// </summary>
    private async Task CaptureMainMenuStates()
    {
        Node menu = GD.Load<PackedScene>("res://src/ui/menu/MainMenu.tscn").Instantiate();
        AddChild(menu);
        await WaitForFrames(2);
        VerifyInsideViewport(menu.GetNode<Control>("Menu/Panel"), "main menu panel");
        VerifyInsideViewport(menu.GetNode<Control>("Menu/RoleBlock"), "character plaque");
        SaveScreenshot("visual-main-menu-1280x720.png");

        menu.GetNode<Button>("Menu/Panel/Padding/Layout/Settings")
            .EmitSignal(BaseButton.SignalName.Pressed);
        await WaitForFrames(2);
        SettingsPanel settings = menu.GetNode<SettingsPanel>("SettingsPanel");
        Require(settings.Visible, "Settings panel did not open for visual capture.");
        VerifyInsideViewport(settings, "settings panel");
        SaveScreenshot("visual-settings-1280x720.png");

        settings.GetNode<Button>("Padding/Layout/Header/Back")
            .EmitSignal(BaseButton.SignalName.Pressed);
        menu.GetNode<Button>("Menu/Panel/Padding/Layout/Start")
            .EmitSignal(BaseButton.SignalName.Pressed);
        await WaitForFrames(2);
        ContentPackSelectionPanel content = menu.GetNode<ContentPackSelectionPanel>(
            "ContentPackSelectionPanel");
        Require(content.Visible, "Content selection did not open for visual capture.");
        VerifyInsideViewport(content.GetNode<Control>("Panel"), "content selection panel");
        SaveScreenshot("visual-content-selection-1280x720.png");

        menu.QueueFree();
        await WaitForFrames(1);
    }

    /// <summary>
    /// 进入正式世界后调用暂停组件公开入口，确保 HUD 背景与暂停漆框能在同一截图中共同验收。
    /// </summary>
    private async Task CapturePauseState()
    {
        Node world = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn").Instantiate();
        AddChild(world);
        await WaitForFrames(2);
        var pause = world.GetNode<PauseMenuOverlay>("PauseMenuOverlay");
        pause.Open();
        await WaitForFrames(2);
        Control panel = pause.GetNode<Control>("Root/PausePanel");
        VerifyInsideViewport(panel, "pause panel");
        Require(panel.Size.X <= 260.5f && panel.Size.Y <= 284.5f,
            $"Pause panel became oversized: {panel.Size}.");
        SaveScreenshot("visual-pause-menu-1280x720.png");
        pause.Close();
        world.QueueFree();
        await WaitForFrames(1);
    }

    /// <summary>
    /// 确认控件全局矩形位于 640×360 逻辑视口，并保留至少四像素安全边距。
    /// </summary>
    private static void VerifyInsideViewport(Control control, string label)
    {
        Rect2 safeViewport = new Rect2(4, 4, 632, 352);
        Require(safeViewport.Encloses(control.GetGlobalRect()),
            $"{label} escaped the compact viewport: {control.GetGlobalRect()}.");
    }

    /// <summary>
    /// 等待指定处理帧，让主题最小尺寸、动态列表与九宫格边距完成最终布局。
    /// </summary>
    private async Task WaitForFrames(int count)
    {
        for (int frame = 0; frame < count; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    /// <summary>
    /// 在桌面渲染器保存最近邻截图；无头环境仍保留全部布局断言并明确跳过文件写入。
    /// </summary>
    private void SaveScreenshot(string fileName)
    {
        if (DisplayServer.GetName() == "headless")
        {
            GD.Print($"Visual screenshot skipped in headless mode: {fileName}");
            return;
        }

        Image image = GetViewport().GetTexture().GetImage();
        if (image.GetSize() != new Vector2I(1280, 720))
        {
            image.Resize(1280, 720, Image.Interpolation.Nearest);
        }

        string path = ProjectSettings.GlobalizePath("user://" + fileName);
        Require(image.SavePng(path) == Error.Ok, $"Could not save screenshot: {path}.");
        GD.Print($"UI shell screenshot: {path}");
    }

    /// <summary>
    /// 将视觉契约失败转为带具体原因的异常，使自动化进程返回可靠的非零退出码。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
