using Godot;
using TouhouWuxiaSurvivor.Gameplay.Session;
using TouhouWuxiaSurvivor.Ui.Death;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 用接近真实长构筑的结算数据验证总结窗边界，并输出逻辑尺寸与最近邻放大截图供人工复核。
/// </summary>
public partial class DeathSummaryVisualTest : Node
{
    /// <summary>
    /// 展示代表性总结、等待容器完成布局，检查紧凑契约后把两种尺寸的画面保存到用户目录。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            var failure = GD.Load<PackedScene>("res://src/ui/death/DeathScreenOverlay.tscn")
                .Instantiate<DeathScreenOverlay>();
            AddChild(failure);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            failure.Present(CreateRepresentativeSummary());
            failure.ShowSummary();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            VerifyCompactLayout(failure);
            SaveScreenshots();
            GetTree().Paused = false;
            GD.Print("Death summary visual test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GetTree().Paused = false;
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 建立会触发长内容和两行武学文本的代表性快照，覆盖用户截图中的拥挤场景。
    /// </summary>
    private static RunSummary CreateRepresentativeSummary() => new(
        RunEndReason.Abandoned,
        115.0,
        148,
        184,
        -82,
        "魔法森林",
        "幻想乡本体 + 东方红魔乡",
        20260728,
        10,
        194,
        "封魔针法 3重、博丽呼吸法 1重、追魂诀 1重、聚灵诀 2重、" +
        "灵符「梦想封印」、梦符「封魔阵」",
        23,
        23);

    /// <summary>
    /// 检查面板尺寸、视口留白、按钮边界和长武学行数，防止内容重新把总结页撑满屏幕。
    /// </summary>
    private void VerifyCompactLayout(DeathScreenOverlay failure)
    {
        var panel = failure.GetNode<Control>("Root/SummaryPanel");
        Rect2 panelRect = panel.GetGlobalRect();
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Require(panelRect.Size.X <= 460.5f && panelRect.Size.Y <= 260.5f,
            $"Summary panel expanded to {panelRect.Size}.");
        Require(panelRect.Position.X >= 80.0f && panelRect.Position.Y >= 40.0f &&
            panelRect.End.X <= viewportSize.X - 80.0f &&
            panelRect.End.Y <= viewportSize.Y - 40.0f,
            $"Summary panel lost viewport breathing room: {panelRect}.");

        var build = failure.GetNode<Label>(
            "Root/SummaryPanel/Padding/Layout/BuildRow/BuildValue");
        var location = failure.GetNode<Label>(
            "Root/SummaryPanel/Padding/Layout/StatsColumns/RightStats/LocationValue");
        var content = failure.GetNode<Label>(
            "Root/SummaryPanel/Padding/Layout/StatsColumns/RightStats/ContentValue");
        GD.Print($"Wrapped labels: location={location.Size}/{location.GetVisibleLineCount()}, " +
            $"content={content.Size}/{content.GetVisibleLineCount()}, " +
            $"build={build.Size}/{build.GetVisibleLineCount()}.");
        Require(build.MaxLinesVisible == 2 && build.ClipText,
            "Build summary is not constrained to two visible lines.");
        Require(location.GetVisibleLineCount() > 0 && content.GetVisibleLineCount() > 0,
            "A wrapped location or content value has no visible text lines.");
        Require(build.GetVisibleLineCount() == 2,
            $"Representative build uses {build.GetVisibleLineCount()} visible lines instead of two.");
        var buttons = failure.GetNode<Control>(
            "Root/SummaryPanel/Padding/Layout/Buttons");
        Require(buttons.GetGlobalRect().End.Y <= panelRect.End.Y - 8.0f,
            "Summary buttons extend beyond the compact panel.");
    }

    /// <summary>
    /// 保存原始逻辑视口和二倍最近邻画面，便于比较像素缩放后的实际桌面观感。
    /// </summary>
    private void SaveScreenshots()
    {
        if (DisplayServer.GetName() == "headless")
        {
            GD.Print("Headless renderer: screenshot capture skipped after layout assertions.");
            return;
        }

        Image logical = GetViewport().GetTexture().GetImage();
        string logicalPath = ProjectSettings.GlobalizePath(
            "user://death-summary-640x360.png");
        Require(logical.SavePng(logicalPath) == Error.Ok,
            "Could not save the logical summary screenshot.");

        Image desktop = logical.Duplicate() as Image ?? throw new InvalidOperationException(
            "Could not duplicate the summary screenshot.");
        desktop.Resize(1280, 720, Image.Interpolation.Nearest);
        string desktopPath = ProjectSettings.GlobalizePath(
            "user://death-summary-1280x720.png");
        Require(desktop.SavePng(desktopPath) == Error.Ok,
            "Could not save the scaled summary screenshot.");
        GD.Print($"Summary screenshots: {logicalPath} | {desktopPath}");
    }

    /// <summary>
    /// 将布局契约失败转换为包含具体尺寸的测试异常，使无头测试返回明确失败码。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
