using Godot;
using TouhouWuxiaSurvivor.Ui.Changelog;
using TouhouWuxiaSurvivor.Ui.Menu;
using TouhouWuxiaSurvivor.Versioning;
using TouhouWuxiaSurvivor.Versioning.Changelog;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证日志解析、主菜单入口、历史选择、固定两栏布局、返回链路和 640×360 视觉边界。
/// </summary>
public partial class ChangelogPanelSmokeTest : Node
{
    /// <summary>实例化真实主菜单并遍历当前与历史版本，窗口模式下同时保存视觉验收截图。</summary>
    public override async void _Ready()
    {
        MainMenu? menu = null;
        int exitCode = 0;
        try
        {
            VerifyCatalog();
            GetWindow().Size = new Vector2I(1280, 720);
            menu = GD.Load<PackedScene>("res://src/ui/menu/MainMenu.tscn").Instantiate<MainMenu>();
            AddChild(menu);
            await WaitFrames(3);
            menu.GetNode<Button>("Menu/Panel/Padding/Layout/Changelog")
                .EmitSignal(Button.SignalName.Pressed);
            await WaitFrames(3);

            ChangelogPanel panel = menu.GetNode<ChangelogPanel>("ChangelogPanel");
            VerifyCurrentEntry(menu, panel);
            VerifyLayout(menu, panel);
            SaveScreenshot("visual-changelog-alpha-0.0.5-1280x720.png");
            VerifyHistoricalSelection(panel);
            panel.GetNode<Button>("Panel/Padding/Layout/Header/Back")
                .EmitSignal(Button.SignalName.Pressed);
            Require(!panel.Visible && menu.GetNode<Control>("Menu").Visible,
                "Returning from the changelog did not restore main-menu commands.");
            GD.Print("Changelog panel smoke test passed.");
        }
        catch (Exception exception)
        {
            exitCode = 1;
            GD.PushError(exception.ToString());
        }
        finally
        {
            menu?.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GetTree().Quit(exitCode);
        }
    }

    /// <summary>直接读取唯一日志源，确认版本顺序、结构和当前版本内容可由解析器完整恢复。</summary>
    private static void VerifyCatalog()
    {
        GameChangelogCatalog catalog = GameChangelogCatalog.LoadDefault();
        Require(catalog.Entries.Count >= 6 && catalog.Entries[0].Version == GameVersion.Current,
            "Changelog catalog does not begin with the current version.");
        Require(catalog.Entries.Select(entry => entry.Version).SequenceEqual(
                new[] { "alpha-0.0.5", "alpha-0.0.4", "alpha-0.0.3", "alpha-0.0.2", "alpha-0.0.1", "alpha-0.0.0" }),
            "Changelog version index is incomplete or not ordered newest first.");
        Require(catalog.Entries[0].Sections.Any(section => section.Heading == "动态难度与敌群") &&
                catalog.Entries[0].ItemCount >= 12,
            "Current changelog entry lost its structured sections or release details.");
    }

    /// <summary>确认主菜单只显示日志页，并正确呈现当前版本及从 Markdown 解析的正文。</summary>
    private static void VerifyCurrentEntry(MainMenu menu, ChangelogPanel panel)
    {
        Require(panel.Visible && !menu.GetNode<Control>("Menu").Visible,
            "Main menu did not switch exclusively to the changelog panel.");
        Require(panel.EntryCount == 6 && panel.SelectedVersion == "alpha-0.0.5",
            "Changelog panel did not select the latest of six versions.");
        Require(panel.CurrentBodyText.Contains("三十秒滑动窗口", StringComparison.Ordinal) &&
                panel.CurrentBodyText.Contains("中心弹幕", StringComparison.Ordinal),
            "Current changelog body is not sourced from the latest Markdown entry.");
    }

    /// <summary>确认面板和两栏都留在逻辑视口内，且不存在可移动分界线或全页滚动容器。</summary>
    private static void VerifyLayout(MainMenu menu, ChangelogPanel panel)
    {
        var viewport = new Rect2(Vector2.Zero, menu.GetViewport().GetVisibleRect().Size);
        Control frame = panel.GetNode<Control>("Panel");
        Control browser = panel.GetNode<Control>("Panel/Padding/Layout/Browser");
        ItemList versions = panel.GetNode<ItemList>("Panel/Padding/Layout/Browser/Index/Versions");
        RichTextLabel body = panel.GetNode<RichTextLabel>(
            "Panel/Padding/Layout/Browser/Detail/Body");
        Require(viewport.Encloses(frame.GetGlobalRect()) &&
                frame.GetGlobalRect().Encloses(browser.GetGlobalRect()) &&
                browser.GetGlobalRect().Encloses(versions.GetGlobalRect()) &&
                browser.GetGlobalRect().Encloses(body.GetGlobalRect()),
            "Changelog layout escaped the 640x360 logical viewport.");
        Require(panel.FindChildren("*", "SplitContainer").Count == 0,
            "Changelog regressed to a draggable divider.");
        Require(panel.FindChildren("*", "ScrollContainer").Count == 0 && body.ScrollActive,
            "Only the changelog body may provide scrolling.");
        Require(versions.CustomMinimumSize.X <= 150.0f,
            "Version index consumes too much horizontal space.");
    }

    /// <summary>按稳定版本身份通过真实 ItemList 信号选择历史条目，并确认标题与正文一起更新。</summary>
    private static void VerifyHistoricalSelection(ChangelogPanel panel)
    {
        ItemList versions = panel.GetNode<ItemList>("Panel/Padding/Layout/Browser/Index/Versions");
        int historicalIndex = Enumerable.Range(0, versions.ItemCount)
            .Single(index => versions.GetItemText(index) == "alpha-0.0.2");
        versions.Select(historicalIndex);
        versions.EmitSignal(ItemList.SignalName.ItemSelected, (long)historicalIndex);
        Require(panel.SelectedVersion == "alpha-0.0.2" &&
                panel.CurrentBodyText.Contains("数值与技能策划", StringComparison.Ordinal),
            "Selecting a historical version did not update the changelog body.");
    }

    /// <summary>等待指定渲染帧，使容器、主题与字体尺寸完成稳定布局。</summary>
    private async Task WaitFrames(int count)
    {
        for (int index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    /// <summary>窗口模式保存最近邻截图；无头回归保留全部行为与布局断言并跳过输出。</summary>
    private void SaveScreenshot(string fileName)
    {
        if (DisplayServer.GetName() == "headless")
        {
            return;
        }

        Image image = GetViewport().GetTexture().GetImage();
        if (image.GetSize() != new Vector2I(1280, 720))
        {
            image.Resize(1280, 720, Image.Interpolation.Nearest);
        }

        string path = ProjectSettings.GlobalizePath("user://" + fileName);
        Require(image.SavePng(path) == Error.Ok, $"Could not save changelog screenshot: {path}.");
        GD.Print($"Changelog screenshot: {path}");
    }

    /// <summary>把日志契约失败转换为包含具体原因的集成测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
