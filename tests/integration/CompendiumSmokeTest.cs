using Godot;
using TouhouWuxiaSurvivor.Ui.Compendium;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证主菜单图鉴的数据覆盖、来源筛选、分类切换、详情和返回流程。
/// </summary>
public partial class CompendiumSmokeTest : Node
{
    /// <summary>
    /// 打开真实主菜单图鉴，逐步检查全部地区、敌人、红魔乡角色和返回状态。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            Node menu = GD.Load<PackedScene>("res://src/ui/menu/MainMenu.tscn").Instantiate();
            AddChild(menu);
            VerifyMainMenuLayout(menu);
            menu.GetNode<Button>("Menu/Panel/Padding/Layout/Compendium")
                .EmitSignal(BaseButton.SignalName.Pressed);
            var panel = menu.GetNode<CompendiumPanel>("CompendiumPanel");
            Require(panel.Visible && !menu.GetNode<Control>("Menu").Visible,
                "Compendium did not replace the main menu commands.");
            Label internalNotice = panel.GetNode<Label>("Panel/Padding/Layout/InternalAssetNotice");
            Require(internalNotice.Text.Contains("仅供内部验证", StringComparison.Ordinal) &&
                internalNotice.Text.Contains("公开前", StringComparison.Ordinal),
                "Compendium does not declare the internal original-asset replacement boundary.");
            Require(panel.SourceOptionCount == 22 && panel.CategoryCount == 6,
                "Compendium filters do not cover base plus TH01-TH20 and six categories.");
            Require(panel.VisibleEntryCount == 65,
                "All-source biome page must contain five base and sixty official biomes.");
            VerifyCompendiumDensity(panel);
            var preview = panel.GetNode<CompendiumPreview>(
                "Panel/Padding/Layout/Browser/Details/Layout/Identity/PreviewFrame/Preview");
            Require(preview.AssetsReady && preview.CurrentCategory == CompendiumCategory.Biome,
                "Biome daily-life preview did not load its pixel assets.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(preview.InternalOriginalActive,
                "Base biome preview did not activate its mapped internal scene.");
            double animationStart = preview.AnimationTime;
            preview._Process(0.25);
            Require(preview.AnimationTime > animationStart,
                "Compendium preview did not advance its live animation.");

            OptionButton source = panel.GetNode<OptionButton>(
                "Panel/Padding/Layout/Filters/SourceFilter");
            TabBar tabs = panel.GetNode<TabBar>("Panel/Padding/Layout/CategoryTabs");
            source.Select(1);
            source.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
            tabs.CurrentTab = (int)CompendiumCategory.Build;
            Require(panel.VisibleEntryCount == 24 &&
                (panel.CurrentDetailsText.Contains("等级上限", StringComparison.Ordinal) ||
                    panel.CurrentDetailsText.Contains("解锁境界", StringComparison.Ordinal)) &&
                panel.CurrentDetailsText.Contains("候选规则", StringComparison.Ordinal),
                "Base compendium did not expose the runtime build and specialization catalog.");
            Require(preview.CurrentCategory == CompendiumCategory.Build &&
                !preview.InternalOriginalActive,
                "Build preview did not use its rule-graph presentation.");
            tabs.CurrentTab = (int)CompendiumCategory.SpellCard;
            Require(panel.VisibleEntryCount == 6,
                "Base compendium did not expose the complete permanent spell-card pool.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(preview.InternalOriginalActive,
                "Base spell-card preview did not activate its mapped bullet atlas.");
            source.Select(0);
            source.EmitSignal(OptionButton.SignalName.ItemSelected, 0L);

            tabs.CurrentTab = (int)CompendiumCategory.Enemy;
            Require(panel.VisibleEntryCount == 69,
                "Enemy page must contain nine base and sixty official enemies.");
            Require(preview.CurrentCategory == CompendiumCategory.Enemy,
                "Enemy page did not switch the preview into movement mode.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            VBoxContainer facts = panel.GetNode<VBoxContainer>(
                "Panel/Padding/Layout/Browser/Details/Layout/EntryFacts");
            Require(facts.GetChildCount() == 8,
                "Enemy attributes were not arranged as eight complete runtime rows.");
            Require(((HBoxContainer)facts.GetChild(0)).GetChildCount() == 1,
                "Variable-length habitat did not receive a full-width row.");
            var widePair = (HBoxContainer)((HBoxContainer)facts.GetChild(0)).GetChild(0);
            var regularPair = (HBoxContainer)((HBoxContainer)facts.GetChild(1)).GetChild(0);
            Require(widePair.Size.X >= regularPair.Size.X * 1.8f,
                "Variable-length habitat did not receive the full detail width.");
            Vector2 factMinimum = facts.GetCombinedMinimumSize();
            Require(factMinimum.Y <= facts.Size.Y + 1.0f,
                $"Enemy attributes do not fit: minimum {factMinimum}, actual {facts.Size}.");

            source.Select(7);
            source.EmitSignal(OptionButton.SignalName.ItemSelected, 7L);
            Require(panel.VisibleEntryCount == 3,
                "TH06 enemy filter must expose all three regional enemies.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(preview.InternalOriginalAssetsReady && preview.InternalOriginalActive,
                "TH06 enemy preview did not activate the isolated internal atlas.");
            tabs.CurrentTab = (int)CompendiumCategory.Character;
            Require(panel.VisibleEntryCount == 7,
                "TH06 character catalog must expose all seven declared characters.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(preview.InternalOriginalActive,
                "TH06 character preview did not activate its internal portrait and Chinese caption.");

            string details = panel.CurrentDetailsText;
            Require(details.Contains("可选自机", StringComparison.Ordinal) &&
                details.Contains("可作为角色 Boss", StringComparison.Ordinal) &&
                details.Contains("本局 Boss 候选", StringComparison.Ordinal),
                "Character detail omitted playable, Boss, or self-exclusion state.");
            tabs.CurrentTab = (int)CompendiumCategory.SpellCard;
            Require(panel.VisibleEntryCount == 7 &&
                preview.CurrentCategory == CompendiumCategory.SpellCard,
                "TH06 spell-card page must expose every declared character's representative card.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(preview.InternalOriginalActive,
                "TH06 spell-card preview did not activate the internal bullet atlas.");
            details = panel.CurrentDetailsText;
            Require(details.Contains("所属角色：", StringComparison.Ordinal) &&
                new[] { "露米娅", "琪露诺", "红美铃", "帕秋莉·诺蕾姬", "十六夜咲夜",
                    "蕾米莉亚·斯卡蕾特", "芙兰朵露·斯卡蕾特" }.Any(owner =>
                        details.Contains(owner, StringComparison.Ordinal)) &&
                details.Contains("原作依据：原作正式符卡", StringComparison.Ordinal) &&
                details.Contains("TH06 SC", StringComparison.Ordinal) &&
                details.Contains("原作演出", StringComparison.Ordinal) &&
                details.Contains("前置构筑", StringComparison.Ordinal) &&
                details.Contains("自动触发", StringComparison.Ordinal) &&
                details.Contains("定位与弹型", StringComparison.Ordinal) &&
                details.Contains("弹型", StringComparison.Ordinal) &&
                details.Contains("姿态", StringComparison.Ordinal) &&
                details.Contains("本内容包原生同语义素材", StringComparison.Ordinal) &&
                details.Contains("周天换算", StringComparison.Ordinal) &&
                details.Contains("攻势换算", StringComparison.Ordinal),
                "Spell-card details omitted canon, build, timing, or scaling fields.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Vector2 spellFactMinimum = facts.GetCombinedMinimumSize();
            Require(facts.GetChildCount() == 9 &&
                spellFactMinimum.Y <= facts.Size.Y + 1.0f,
                $"Spell-card attributes do not fit the fixed no-scroll detail panel: " +
                $"rows {facts.GetChildCount()}, minimum {spellFactMinimum}, actual {facts.Size}.");
            panel.GetNode<Button>("Panel/Padding/Layout/Header/Back")
                .EmitSignal(BaseButton.SignalName.Pressed);
            Require(!panel.Visible && menu.GetNode<Control>("Menu").Visible,
                "Returning from compendium did not restore the main menu.");
            menu.QueueFree();
            GD.Print("Compendium smoke test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 在 640x360 设计视口确认命令区、角色题签和标题带互不遮挡且没有越出画面。
    /// </summary>
    private static void VerifyMainMenuLayout(Node menu)
    {
        Rect2 commands = menu.GetNode<Control>("Menu/Panel").GetGlobalRect();
        Rect2 role = menu.GetNode<Control>("Menu/RoleBlock").GetGlobalRect();
        Rect2 title = menu.GetNode<Control>("TitleBand").GetGlobalRect();
        Rect2 viewport = new(Vector2.Zero, menu.GetViewport().GetVisibleRect().Size);
        Require(!commands.Intersects(role), "Main menu commands overlap the character inscription.");
        Require(!commands.Intersects(title) && !role.Intersects(title),
            "Main menu content overlaps the title band.");
        Require(viewport.Encloses(commands) && viewport.Encloses(role),
            "Main menu content extends beyond the design viewport.");
        Require(((Control)menu).Theme.DefaultFontSize <= 12,
            "Menu body type is too large for the 640x360 design viewport.");
        Require(menu.GetNode<Button>("Menu/Panel/Padding/Layout/Start")
            .CustomMinimumSize.Y <= 32.0f,
            "Main menu commands regressed to oversized touch-style rows.");
    }

    /// <summary>
    /// 锁定图鉴列表、命令和动态窗的紧凑上限，避免内容扩充后再次挤压详情区域。
    /// </summary>
    private static void VerifyCompendiumDensity(CompendiumPanel panel)
    {
        Require(panel.GetNode<Button>("Panel/Padding/Layout/Header/Back")
            .CustomMinimumSize.Y <= 28.0f,
            "Compendium header command is too tall.");
        Require(panel.GetNode<ItemList>("Panel/Padding/Layout/Browser/EntryList")
            .CustomMinimumSize.X <= 160.0f,
            "Compendium list consumes too much horizontal space.");
        Require(panel.GetNode<NinePatchRect>(
            "Panel/Padding/Layout/Browser/Details/Layout/Identity/PreviewFrame")
            .CustomMinimumSize == new Vector2(120.0f, 84.0f),
            "Compendium animation preview no longer follows the compact ratio.");
        Require(panel.GetNodeOrNull<RichTextLabel>(
            "Panel/Padding/Layout/Browser/Details/Layout/EntryDetails") is null,
            "Compendium attributes regressed to a mouse-wheel text box.");
        Require(panel.GetNode<Control>("Panel/Padding/Layout/Browser") is not HSplitContainer,
            "Compendium browser regressed to a draggable divider.");
        VBoxContainer facts = panel.GetNode<VBoxContainer>(
            "Panel/Padding/Layout/Browser/Details/Layout/EntryFacts");
        var firstRow = (HBoxContainer)facts.GetChild(0);
        var firstPair = (HBoxContainer)firstRow.GetChild(0);
        Label firstKey = (Label)firstPair.GetChild(0);
        Require(!firstKey.Text.EndsWith('：') &&
            firstKey.AutowrapMode == TextServer.AutowrapMode.Off,
            "Compendium fact keys can wrap their punctuation onto a separate line.");
    }

    /// <summary>
    /// 将图鉴契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
