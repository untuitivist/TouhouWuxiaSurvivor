using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Ui.Stats;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实 640×360 逻辑视口验收构筑三列、最长文本和关系图边界，并保存桌面截图。
/// </summary>
public partial class CharacterBuildVisualAcceptanceTest : Node
{
    /// <summary>
    /// 建立含满重、特化、无尽与符卡的高密度构筑，验证固定面板和文字均不越界。
    /// </summary>
    public override async void _Ready()
    {
        CharacterStatsOverlay? overlay = null;
        int exitCode = 0;
        try
        {
            GetWindow().Size = new Vector2I(1280, 720);
            var build = CreateDenseBuild();
            CharacterBuildViewModel model = CharacterBuildViewModelFactory.Create(
                build, new ContentPackSelection(ContentPackCatalog.All.Select(pack => pack.Id)), 24);
            overlay = GD.Load<PackedScene>("res://src/ui/stats/CharacterStatsOverlay.tscn")
                .Instantiate<CharacterStatsOverlay>();
            AddChild(overlay);
            overlay.SetProcessUnhandledInput(false);
            CharacterBuildView view = overlay.GetNode<CharacterBuildView>(
                "Root/Panel/Padding/Layout/Pages/BuildPage");
            view.SetModel(model);
            overlay.GetNode<Control>("Root").Show();
            overlay.ShowPage(CharacterStatsPage.Build);
            CharacterBuildNodeView spellNode = model.LearnedNodes.Single(node =>
                node.Category == RunUpgradeCategory.SpellCard);
            view.Graph.SelectNode(spellNode.Id);
            await WaitFrames(3);
            VerifyLayout(overlay, view);
            SaveScreenshot("visual-character-build-1280x720.png", 1280, 720);
            GD.Print("Character build visual acceptance test passed.");
        }
        catch (Exception exception)
        {
            exitCode = 1;
            GD.PushError(exception.ToString());
        }
        finally
        {
            overlay?.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GetTree().Quit(exitCode);
        }
    }

    /// <summary>
    /// 将六项基础修至三重、选择一项特化并补入无尽与符卡，制造高密度真实关系图。
    /// </summary>
    private static RunBuildState CreateDenseBuild()
    {
        var build = new RunBuildState();
        ContentPackDefinition th06 = ContentPackCatalog.All.Single(pack => pack.Number == 6);
        RunUpgradeDefinition[] foundations = RunUpgradeCatalog.All
            .Where(item => item.RequiredContentPack is null && !item.IsRepeatable)
            .Take(6).ToArray();
        foreach (RunUpgradeDefinition definition in foundations)
        {
            for (int rank = 0; rank < 3; rank++)
            {
                build.Apply(definition);
            }
        }

        build.ApplySpecialization(foundations[0], foundations[0].Specializations[0], 24);
        foreach (RunUpgradeDefinition definition in RunUpgradeCatalog.All
            .Where(item => item.IsRepeatable).Take(1))
        {
            while (build.CanUpgrade(definition))
            {
                build.Apply(definition);
                if (build.GetRank(definition.Id) >= 3)
                {
                    break;
                }
            }
        }

        RunUpgradeDefinition spell = RunUpgradeCatalog.All.First(item =>
            item.RequiredContentPack == th06.Id && item.SpellCardId is not null &&
            build.CanUpgrade(item));
        build.Apply(spell);
        return build;
    }

    /// <summary>
    /// 验证卷轴、图谱和详情栏均被 640×360 安全区包含，且详情标签没有垂直溢出。
    /// </summary>
    private static void VerifyLayout(CharacterStatsOverlay overlay, CharacterBuildView view)
    {
        var viewport = new Rect2(Vector2.Zero, new Vector2(640.0f, 360.0f));
        Control panel = overlay.GetNode<Control>("Root/Panel");
        Control graph = view.GetNode<Control>("Body/GraphFrame/Graph");
        Control details = view.GetNode<Control>("Body/DetailsFrame");
        Require(viewport.Encloses(panel.GetGlobalRect()),
            "Build scroll panel escaped the 640x360 viewport.");
        Require(panel.GetGlobalRect().Encloses(graph.GetGlobalRect()) &&
            panel.GetGlobalRect().Encloses(details.GetGlobalRect()),
            "Build graph or fixed details escaped the panel.");
        foreach (Label label in details.FindChildren("*", "Label").Cast<Label>())
        {
            Require(details.GetGlobalRect().Encloses(label.GetGlobalRect()),
                $"Build detail label escaped its fixed pane: {label.Name}.");
        }
        Require(overlay.FindChildren("*", "ScrollContainer").Count == 0,
            "Visual build layout requires scrolling for core information.");
    }

    /// <summary>等待指定帧数，使主题、容器和字体完成稳定布局。</summary>
    private async Task WaitFrames(int count)
    {
        for (int index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    /// <summary>窗口模式保存最近邻截图，无头回归保留全部布局断言并跳过文件输出。</summary>
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
        GD.Print($"Character build screenshot: {path}");
    }

    /// <summary>将视觉契约失败转换为包含具体原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
