using Godot;
using TouhouWuxiaSurvivor.Ui.Compendium;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实主菜单覆盖图鉴五个分类，锁定内部动态图标、紧凑布局和最近邻截图表现。
/// </summary>
public partial class CompendiumVisualTest : Node
{
    /// <summary>
    /// 依次展示本体与 TH06 的场景、敌人、角色和符卡，保存五个代表状态截图。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            Node menu = GD.Load<PackedScene>("res://src/ui/menu/MainMenu.tscn").Instantiate();
            AddChild(menu);
            menu.GetNode<Button>("Menu/Panel/Padding/Layout/Compendium")
                .EmitSignal(BaseButton.SignalName.Pressed);
            var panel = menu.GetNode<CompendiumPanel>("CompendiumPanel");
            var source = panel.GetNode<OptionButton>("Panel/Padding/Layout/Filters/SourceFilter");
            var tabs = panel.GetNode<TabBar>("Panel/Padding/Layout/CategoryTabs");
            var preview = panel.GetNode<CompendiumPreview>(
                "Panel/Padding/Layout/Browser/Details/Layout/Identity/PreviewFrame/Preview");

            await Capture(panel, preview, tabs, CompendiumCategory.Biome,
                "compendium-base-biome-640x360.png");
            SelectSource(source, 7);
            await Capture(panel, preview, tabs, CompendiumCategory.Structure,
                "compendium-th06-structure-640x360.png");
            SelectSource(source, 1);
            await Capture(panel, preview, tabs, CompendiumCategory.Enemy,
                "compendium-base-enemy-640x360.png");
            await CaptureBaseEnemies(panel, preview);
            SelectSource(source, 7);
            await Capture(panel, preview, tabs, CompendiumCategory.Character,
                "compendium-th06-character-640x360.png");
            await CaptureCharacter(panel, preview, "蕾米莉亚·斯卡蕾特",
                "compendium-th06-remilia-640x360.png");
            await CaptureCharacter(panel, preview, "芙兰朵露·斯卡蕾特",
                "compendium-th06-flandre-640x360.png");
            await Capture(panel, preview, tabs, CompendiumCategory.SpellCard,
                "compendium-th06-spell-640x360.png");
            GD.Print("Compendium visual test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 按稳定中文名逐项选择九类本体敌人并截图，避免列表排序只让视觉回归覆盖第一项。
    /// </summary>
    private async Task CaptureBaseEnemies(CompendiumPanel panel, CompendiumPreview preview)
    {
        (string Name, string FileName)[] enemies =
        [
            ("野妖精", "compendium-base-enemy-wild-fairy-640x360.png"),
            ("毛玉", "compendium-base-enemy-kedama-640x360.png"),
            ("妖虫", "compendium-base-enemy-insect-640x360.png"),
            ("阴阳玉", "compendium-base-enemy-yin-yang-orb-640x360.png"),
            ("森林精怪", "compendium-base-enemy-forest-spirit-640x360.png"),
            ("山精", "compendium-base-enemy-mountain-spirit-640x360.png"),
            ("流窜妖怪", "compendium-base-enemy-village-outlaw-640x360.png"),
            ("夜行妖怪", "compendium-base-enemy-wandering-youkai-640x360.png"),
            ("大妖怪", "compendium-base-enemy-great-youkai-640x360.png"),
        ];
        foreach ((string name, string fileName) in enemies)
        {
            await CaptureEntry(panel, preview, CompendiumCategory.Enemy, name, fileName);
        }
    }

    /// <summary>
    /// 切换指定分类、等待布局与绘制稳定，再验证当前预览并保存普通渲染器截图。
    /// </summary>
    private async Task Capture(
        CompendiumPanel panel,
        CompendiumPreview preview,
        TabBar tabs,
        CompendiumCategory category,
        string fileName)
    {
        tabs.CurrentTab = (int)category;
        await WaitForLayout();
        VerifyLayout(panel, preview, category);
        SaveScreenshot(fileName);
    }

    /// <summary>
    /// 选择来源并发出与玩家操作相同的选项变化信号，让列表立即按作品刷新。
    /// </summary>
    private static void SelectSource(OptionButton source, int index)
    {
        source.Select(index);
        source.EmitSignal(OptionButton.SignalName.ItemSelected, (long)index);
    }

    /// <summary>
    /// 按中文全名选择角色并保存其真实详情画面，避免列表排序变化让视觉回归检查错人。
    /// </summary>
    private async Task CaptureCharacter(
        CompendiumPanel panel,
        CompendiumPreview preview,
        string characterName,
        string fileName)
    {
        await CaptureEntry(
            panel, preview, CompendiumCategory.Character, characterName, fileName);
    }

    /// <summary>
    /// 按中文全名选择任意分类条目，等待真实详情刷新，并锁定标题不发生意外换行。
    /// </summary>
    private async Task CaptureEntry(
        CompendiumPanel panel,
        CompendiumPreview preview,
        CompendiumCategory category,
        string entryName,
        string fileName)
    {
        var entries = panel.GetNode<ItemList>("Panel/Padding/Layout/Browser/EntryList");
        int index = Enumerable.Range(0, entries.ItemCount)
            .FirstOrDefault(item => entries.GetItemText(item) == entryName, -1);
        Require(index >= 0, $"Could not find compendium entry: {entryName}.");
        entries.Select(index);
        entries.EmitSignal(ItemList.SignalName.ItemSelected, (long)index);
        await WaitForLayout();
        VerifyLayout(panel, preview, category);
        Label title = panel.GetNode<Label>(
            "Panel/Padding/Layout/Browser/Details/Layout/Identity/Heading/EntryTitle");
        Require(title.Text == entryName && title.GetLineCount() == 1,
            $"Compendium title did not remain on one line: {entryName}.");
        SaveScreenshot(fileName);
    }

    /// <summary>
    /// 等待两个处理帧，让容器重排并让动态预览至少完成一次绘制通知。
    /// </summary>
    private async Task WaitForLayout()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    /// <summary>
    /// 确认声明和浏览区位于 640x360 视口内，且所选分类实际使用内部映射而非文字回退。
    /// </summary>
    private void VerifyLayout(
        CompendiumPanel panel,
        CompendiumPreview preview,
        CompendiumCategory expectedCategory)
    {
        Rect2 viewport = new(Vector2.Zero, GetViewport().GetVisibleRect().Size);
        var notice = panel.GetNode<Label>("Panel/Padding/Layout/InternalAssetNotice");
        var browser = panel.GetNode<Control>("Panel/Padding/Layout/Browser");
        Require(viewport.Encloses(notice.GetGlobalRect()),
            "Internal asset notice extends beyond the design viewport.");
        Require(viewport.Encloses(browser.GetGlobalRect()) && browser.Size.Y >= 190.0f,
            $"Compendium browser became cramped: {browser.GetGlobalRect()}.");
        Require(preview.CurrentCategory == expectedCategory &&
            preview.InternalOriginalAssetsReady && preview.InternalOriginalActive,
            $"Internal preview is inactive for {expectedCategory}.");
    }

    /// <summary>
    /// 在普通渲染器下保存当前画面；headless 环境只执行布局与状态断言。
    /// </summary>
    private void SaveScreenshot(string fileName)
    {
        if (DisplayServer.GetName() == "headless")
        {
            return;
        }

        Image image = GetViewport().GetTexture().GetImage();
        string path = ProjectSettings.GlobalizePath("user://" + fileName);
        Require(image.SavePng(path) == Error.Ok, $"Could not save screenshot: {path}.");
        GD.Print($"Compendium screenshot: {path}");
    }

    /// <summary>
    /// 将视觉契约失败转换为包含具体原因的异常，保证自动测试返回非零退出码。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
