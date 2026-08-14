using Godot;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Ui.Progression;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 把正式目录中显示文本最长的三项候选放入真实升级层，验收横向亲和、特化和探索提示的最坏排版。
/// </summary>
public partial class LevelUpVisualAcceptanceTest : Node
{
    /// <summary>
    /// 建立 640×360 逻辑界面、展示最长候选、验证每行宽度与控件边界并在窗口模式保存截图。
    /// </summary>
    public override async void _Ready()
    {
        int exitCode = 0;
        LevelUpOverlay? overlay = null;
        try
        {
            GetWindow().Size = new Vector2I(1280, 720);
            var build = new RunBuildState();
            RunUpgradeChoice[] choices = CreateLongestChoices(build);
            overlay = GD.Load<PackedScene>("res://src/ui/progression/LevelUpOverlay.tscn")
                .Instantiate<LevelUpOverlay>();
            AddChild(overlay);
            overlay.Present(choices, build, 8);
            await WaitForFrames(2);
            VerifyLayout(overlay);
            SaveScreenshot("visual-level-up-longest-1280x720.png", 1280, 720);
            GD.Print("Level-up visual acceptance test passed.");
        }
        catch (Exception exception)
        {
            exitCode = 1;
            GD.PushError(exception.ToString());
        }
        finally
        {
            GetTree().Paused = false;
            overlay?.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GetTree().Quit(exitCode);
        }
    }

    /// <summary>
    /// 同时枚举普通升重与特化，全部标为探索项后按正式格式长度选出不重复的前三项。
    /// </summary>
    private static RunUpgradeChoice[] CreateLongestChoices(RunBuildState build) =>
        RunUpgradeCatalog.All.SelectMany(definition =>
                new[] { new RunUpgradeChoice(definition, isExploration: true) }
                    .Concat(definition.Specializations.Select(specialization =>
                        new RunUpgradeChoice(definition, specialization, true))))
            .OrderByDescending(choice =>
                RunUpgradeChoiceTextFormatter.Format(choice, build).Length)
            .Take(3)
            .ToArray();

    /// <summary>
    /// 检查卷轴与三个按钮均留在逻辑视口，且每行文字宽度小于按钮可用宽度，防止静默裁切。
    /// </summary>
    private static void VerifyLayout(LevelUpOverlay overlay)
    {
        var viewport = new Rect2(Vector2.Zero, new Vector2(640.0f, 360.0f));
        Control panel = overlay.GetNode<Control>("Root/Panel");
        Rect2 panelRect = panel.GetGlobalRect();
        float widestLine = 0.0f;
        string widestText = string.Empty;
        for (int index = 0; index < 3; index++)
        {
            Button measured = overlay.GetNode<Button>(
                $"Root/Panel/Padding/Layout/Choices/Choice{index}");
            Font font = measured.GetThemeFont("font");
            int fontSize = measured.GetThemeFontSize("font_size");
            foreach (string line in measured.Text.Split('\n'))
            {
                float width = font.GetStringSize(
                    line, HorizontalAlignment.Left, -1, fontSize).X;
                if (width > widestLine)
                {
                    widestLine = width;
                    widestText = line;
                }
            }
        }
        Require(viewport.Encloses(panelRect),
            $"Level-up panel {panelRect} escaped 640x360; widest line " +
            $"{widestLine:0.#}px: {widestText}");
        for (int index = 0; index < 3; index++)
        {
            Button button = overlay.GetNode<Button>(
                $"Root/Panel/Padding/Layout/Choices/Choice{index}");
            Require(button.Visible && panel.GetGlobalRect().Encloses(button.GetGlobalRect()),
                $"Level-up choice {index} escaped the scroll panel.");
            Font font = button.GetThemeFont("font");
            int fontSize = button.GetThemeFontSize("font_size");
            float availableWidth = button.Size.X - 20.0f;
            foreach (string line in button.Text.Split('\n'))
            {
                Require(font.GetStringSize(line, HorizontalAlignment.Left, -1, fontSize).X <=
                    availableWidth, $"Level-up choice {index} clipped its line: {line}");
            }
        }
    }

    /// <summary>等待指定处理帧，使主题最小尺寸和按钮文本完成布局。</summary>
    private async Task WaitForFrames(int count)
    {
        for (int frame = 0; frame < count; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    /// <summary>保存桌面尺寸的最近邻 PNG；无头回归保留全部布局断言并明确跳过截图。</summary>
    private void SaveScreenshot(string fileName, int width, int height)
    {
        if (DisplayServer.GetName() == "headless")
        {
            GD.Print($"Visual screenshot skipped in headless mode: {fileName}");
            return;
        }

        Image image = GetViewport().GetTexture().GetImage();
        if (image.GetWidth() != width || image.GetHeight() != height)
        {
            image.Resize(width, height, Image.Interpolation.Nearest);
        }

        string path = ProjectSettings.GlobalizePath("user://" + fileName);
        Require(image.SavePng(path) == Error.Ok, $"Could not save screenshot: {path}.");
        GD.Print($"Level-up visual acceptance screenshot: {path} ({width}x{height})");
    }

    /// <summary>将视觉排版失败转换为含具体文本的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
