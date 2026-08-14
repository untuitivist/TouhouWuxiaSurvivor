using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Ui.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Structures;
using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证正作选择列表、分类增量说明以及本体和正作内容的生成隔离边界。
/// </summary>
public partial class ContentPackSmokeTest : Node
{
    private const ulong Seed = 20260728;

    /// <summary>
    /// 打开主菜单选择页并依次检查清单数量、全部完成状态、群系、结构和敌人隔离。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            ContentPackSelectionService.Apply(ContentPackSelection.BaseOnly);
            VerifyBaseCatalog();
            VerifySelectionMenu();
            VerifyWorldIsolation();
            VerifyEnemyIsolation();
            VerifyBaseEnemyEcology();
            GD.Print("Content pack smoke test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 确认本体清单标记完成，并准确列出五个地区、六类结构、九类敌人和博丽灵梦。
    /// </summary>
    private static void VerifyBaseCatalog()
    {
        ContentPackDefinition definition = ContentPackCatalog.Base;
        Require(definition.Status == "complete", "Base content manifest is not complete.");
        Require(definition.Additions.Count(item => item.Category == "地区") == 5,
            "Base manifest must list five biomes.");
        Require(definition.Additions.Count(item => item.Category == "结构") == 6,
            "Base manifest must list six structures.");
        Require(definition.Additions.Count(item => item.Category == "敌人") == 9,
            "Base manifest must list nine enemies.");
        Require(definition.Additions.Any(item => item.Category == "角色" && item.Name == "博丽灵梦"),
            "Base manifest must identify the playable character.");
    }

    /// <summary>
    /// 实例化主菜单并确认旧作过滤、默认折叠、独立展开和全部二十个正作的增量详情。
    /// </summary>
    private void VerifySelectionMenu()
    {
        Node menu = GD.Load<PackedScene>("res://src/ui/menu/MainMenu.tscn").Instantiate();
        AddChild(menu);
        menu.GetNode<Button>("Menu/Panel/Padding/Layout/Start").EmitSignal(BaseButton.SignalName.Pressed);
        var panel = menu.GetNode<ContentPackSelectionPanel>("ContentPackSelectionPanel");
        Require(panel.Visible, "Start did not open the content selection panel.");
        Require(panel.ListedPackCount == 20, "Selection panel did not list TH01 through TH20.");
        Require(!panel.ShowOldWorks && panel.VisibleOfficialPackCount == 15,
            "Old works must default hidden while TH06-TH20 remain visible.");

        VBoxContainer packList = panel.GetNode<VBoxContainer>("Panel/Padding/Layout/Scroll/PackList");
        var baseRow = packList.GetChild<ContentPackSelectionRow>(0);
        Require(baseRow.HeaderText.Contains(ContentPackCatalog.Base.DisplayName,
                StringComparison.Ordinal) && !baseRow.DetailsVisible,
            "Base row did not default to a compact name-only state.");
        foreach (ContentPackDefinition definition in ContentPackCatalog.All)
        {
            ContentPackSelectionRow row = panel.GetPackRow(definition.Id);
            Require(!row.IsExpanded && !row.DetailsVisible &&
                !row.HeaderText.Contains("已完成", StringComparison.Ordinal),
                $"Package did not default to a name-only collapsed row: {definition.Id}");
            Require(row.DetailsText.Contains("状态：已完成", StringComparison.Ordinal) &&
                row.DetailsText.Contains("地区：", StringComparison.Ordinal) &&
                row.DetailsText.Contains("结构：", StringComparison.Ordinal) &&
                row.DetailsText.Contains("敌人：", StringComparison.Ordinal) &&
                row.DetailsText.Contains("角色：", StringComparison.Ordinal) &&
                row.DetailsText.Contains("符卡：", StringComparison.Ordinal),
                $"Package summary is incomplete: {definition.Id}");
            Require(row.Visible == (definition.Number >= 6),
                $"Old-work default visibility is incorrect: {definition.Id}");
        }

        ContentPackSelectionRow eosdRow = panel.GetPackRow(
            ContentPackIds.EmbodimentOfScarletDevil);
        ContentPackSelectionRow inRow = panel.GetPackRow(ContentPackIds.ImperishableNight);
        eosdRow.ToggleExpanded();
        Require(eosdRow.IsExpanded && eosdRow.DetailsVisible && !inRow.IsExpanded,
            "Expanding TH06 also expanded an unrelated official work.");
        Require(eosdRow.DetailsText.Contains("雾之湖", StringComparison.Ordinal) &&
            eosdRow.DetailsText.Contains("红魔馆领地", StringComparison.Ordinal) &&
            eosdRow.DetailsText.Contains("巴瓦鲁魔法图书馆", StringComparison.Ordinal) &&
            eosdRow.DetailsText.Contains("雾湖小岛", StringComparison.Ordinal) &&
            eosdRow.DetailsText.Contains("红魔馆", StringComparison.Ordinal) &&
            eosdRow.DetailsText.Contains("大图书馆", StringComparison.Ordinal) &&
            eosdRow.DetailsText.Contains("湖上妖精", StringComparison.Ordinal) &&
            eosdRow.DetailsText.Contains("红雾妖虫", StringComparison.Ordinal) &&
            eosdRow.DetailsText.Contains("使魔书灵", StringComparison.Ordinal),
            "TH06 did not list all three region content groups.");
        eosdRow.ToggleExpanded();
        Require(!eosdRow.IsExpanded && !eosdRow.DetailsVisible,
            "Second TH06 title click did not collapse its details.");

        inRow.ToggleExpanded();
        Require(inRow.DetailsText.Contains("地区：迷途竹林", StringComparison.Ordinal) &&
            inRow.DetailsText.Contains("结构：竹林古道", StringComparison.Ordinal) &&
            inRow.DetailsText.Contains("敌人：竹叶妖", StringComparison.Ordinal),
            "TH08 boundary is missing from its completed package row.");

        ContentPackDefinition firstOldDefinition = ContentPackCatalog.All.Single(
            definition => definition.Number == 1);
        ContentPackSelectionRow firstOld = panel.GetPackRow(firstOldDefinition.Id);
        firstOld.SetSelected(true);
        CheckButton showOldWorks = panel.GetNode<CheckButton>(
            "Panel/Padding/Layout/VisibilityFilters/ShowOldWorks");
        showOldWorks.ButtonPressed = true;
        showOldWorks.EmitSignal(BaseButton.SignalName.Toggled, true);
        Require(panel.ShowOldWorks && panel.VisibleOfficialPackCount == 20 && firstOld.Visible,
            "Show-old-works toggle did not reveal TH01-TH05.");
        firstOld.ToggleExpanded();
        Require(firstOld.IsExpanded && firstOld.DetailsVisible,
            "Old works do not support independent detail expansion.");
        showOldWorks.ButtonPressed = false;
        showOldWorks.EmitSignal(BaseButton.SignalName.Toggled, false);
        Require(!firstOld.Visible && firstOld.IsSelected,
            "Hiding old works cleared an existing run selection.");
        menu.QueueFree();
    }

    /// <summary>
    /// 检查本体敌人数量、后期解锁和三个地区专属生态，防止正作敌人混入本体。
    /// </summary>
    private static void VerifyBaseEnemyEcology()
    {
        EnemyDefinition[] baseEnemies = EnemyCatalog.All
            .Where(enemy => enemy.RequiredContentPack is null)
            .ToArray();
        Require(baseEnemies.Length == 9, "Base enemy catalog must contain nine enemies.");
        Require(baseEnemies.Any(enemy => enemy.Archetype == EnemyArchetype.GreatYoukai &&
            enemy.UnlockTime >= RunPacingTimeline.CrisisSeconds),
            "Base enemy catalog is missing its late-game elite.");
        EnemyArchetype[] regional =
        [
            EnemyArchetype.ForestSpirit,
            EnemyArchetype.MountainSpirit,
            EnemyArchetype.VillageOutlaw,
        ];
        Require(regional.All(archetype => baseEnemies.Any(enemy => enemy.Archetype == archetype &&
            enemy.AllowedBiomes.Count > 0)), "Base enemy catalog is missing regional ecology.");
    }

    /// <summary>
    /// 在相同种子和采样坐标下确认本体排除红魔乡内容，而启用后能生成湖泊与红魔馆。
    /// </summary>
    private static void VerifyWorldIsolation()
    {
        var baseBiomes = new BiomeSelector(Seed, ContentPackSelection.BaseOnly);
        var eosdSelection = new ContentPackSelection([ContentPackIds.EmbodimentOfScarletDevil]);
        var eosdBiomes = new BiomeSelector(Seed, eosdSelection);
        bool foundMistyLake = false;
        for (long y = -2200; y <= 2200; y += 47)
        {
            for (long x = -2200; x <= 2200; x += 47)
            {
                Require(baseBiomes.Select(x, y) != BiomeId.MistyLake,
                    "Base-only biome selection leaked Misty Lake.");
                foundMistyLake |= eosdBiomes.Select(x, y) == BiomeId.MistyLake;
            }
        }

        Require(foundMistyLake, "TH06 selection did not enable Misty Lake.");
        var baseStructures = new StructureLocator(Seed, baseBiomes);
        var eosdStructures = new StructureLocator(Seed, eosdBiomes);
        Require(!baseStructures.FindInBounds(-4000, -4000, 4000, 4000)
            .Any(item => item.Id == StructureId.ScarletDevilMansion),
            "Base-only structure selection leaked Scarlet Devil Mansion.");
        Require(eosdStructures.FindInBounds(-4000, -4000, 4000, 4000)
            .Any(item => item.Id == StructureId.ScarletDevilMansion),
            "TH06 selection did not enable Scarlet Devil Mansion.");
    }

    /// <summary>
    /// 重复抽取后确认纯本体不出现红雾妖虫，而红魔乡内容池包含该专属敌人。
    /// </summary>
    private static void VerifyEnemyIsolation()
    {
        var random = new RandomNumberGenerator { Seed = 42 };
        bool foundEosdEnemy = false;
        bool foundInEnemy = false;
        var eosd = new ContentPackSelection([ContentPackIds.EmbodimentOfScarletDevil]);
        var imperishableNight = new ContentPackSelection([ContentPackIds.ImperishableNight]);
        for (int index = 0; index < 500; index++)
        {
            Require(EnemyCatalog.Choose(
                random, 120.0, BiomeId.MistyLake, ContentPackSelection.BaseOnly)
                .RequiredContentPack is null, "Base-only enemy pool leaked package enemy.");
            foundEosdEnemy |= EnemyCatalog.Choose(
                random, 120.0, BiomeId.MistyLake, eosd).RequiredContentPack ==
                ContentPackIds.EmbodimentOfScarletDevil;
            foundInEnemy |= EnemyCatalog.Choose(
                random, 120.0, BiomeId.BambooForest, imperishableNight).RequiredContentPack ==
                ContentPackIds.ImperishableNight;
        }

        Require(foundEosdEnemy, "TH06 enemy pool did not include its package enemy.");
        Require(foundInEnemy, "TH08 enemy pool did not include its package enemy.");
    }

    /// <summary>
    /// 将内容包契约失败转换为带有明确原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
