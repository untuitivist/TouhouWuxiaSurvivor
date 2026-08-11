using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Tests.Support;
using TouhouWuxiaSurvivor.Ui.Content;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 通过普通 Windows 渲染器保存角色选择和正式战斗画面，供人工检查排版、裁切、最近邻和 HUD 遮挡。
/// </summary>
public partial class CharacterWorldVisualAcceptanceTest : Node
{
    private const string RemiliaName = "蕾米莉亚·斯卡蕾特";
    private const string FlandreName = "芙兰朵露·斯卡蕾特";

    /// <summary>
    /// 先捕获 640x360 主菜单选择层，再装配 1280x720 正式世界并布置可重复的战斗验收场景。
    /// </summary>
    public override async void _Ready()
    {
        WorldDemo? world = null;
        int exitCode = 0;
        try
        {
            ContentPackDefinition th06 = ContentPackCatalog.All.Single(pack => pack.Number == 6);
            var content = new ContentPackSelection([th06.Id]);
            ContentPackSelectionService.Apply(content);
            CharacterSelectionService.ResetToDefault();
            await CaptureCharacterSelection(content);

            CharacterDefinition remilia = CharacterCatalog.GetRequiredByDisplayName(RemiliaName);
            CharacterSelectionService.Apply(remilia.CharacterId, content);
            world = await CreateWorld();
            PopulateCombatAcceptanceScene(world, remilia.CharacterId);
            await WaitForFrames(3);
            VerifyRenderedCombat(world);
            SaveScreenshot("visual-world-character-boss-1280x720.png", 1280, 720);
            GD.Print("Character and world visual acceptance test passed.");
        }
        catch (Exception exception)
        {
            exitCode = 1;
            GD.PushError(exception.ToString());
        }
        finally
        {
            if (world is not null && GodotObject.IsInstanceValid(world))
            {
                await WorldDemoTestCleanup.FreeAsync(this, world);
            }

            ContentPackSelectionService.Apply(ContentPackSelection.BaseOnly);
            CharacterSelectionService.ResetToDefault();
            GetTree().Quit(exitCode);
        }
    }

    /// <summary>
    /// 打开真实主菜单的内容层、选择蕾米莉亚并等待布局稳定，然后保存逻辑分辨率截图。
    /// </summary>
    private async Task CaptureCharacterSelection(ContentPackSelection content)
    {
        GetWindow().Size = new Vector2I(640, 360);
        Node menu = GD.Load<PackedScene>("res://src/ui/menu/MainMenu.tscn").Instantiate();
        AddChild(menu);
        menu.GetNode<Button>("Menu/Panel/Padding/Layout/Start")
            .EmitSignal(BaseButton.SignalName.Pressed);
        var panel = menu.GetNode<ContentPackSelectionPanel>("ContentPackSelectionPanel");
        var choice = panel.GetNode<OptionButton>(
            "Panel/Padding/Layout/CharacterSelection/CharacterChoice");
        int index = Enumerable.Range(0, choice.ItemCount).Single(
            item => choice.GetItemText(item) == RemiliaName);
        choice.Select(index);
        string th06Id = ContentPackCatalog.All.Single(pack => pack.Number == 6).Id;
        Require(content.IsEnabled(th06Id) && panel.GetPackRow(th06Id).IsSelected,
            "TH06 row was not selected in the visual acceptance menu.");
        await WaitForFrames(3);
        SaveScreenshot("visual-content-character-selection-640x360.png", 640, 360);
        menu.QueueFree();
        await WaitForFrames(1);
    }

    /// <summary>
    /// 把窗口切换到桌面验收尺寸并实例化完整世界，关闭初始批量刷怪以保持截图布置确定。
    /// </summary>
    private async Task<WorldDemo> CreateWorld()
    {
        GetWindow().Size = new Vector2I(1280, 720);
        var world = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn")
            .Instantiate<WorldDemo>();
        world.PersistMetaProgression = false;
        world.GetNode<EnemySpawner>("EnemySpawner").InitialSpawnCount = 0;
        AddChild(world);
        await WaitForFrames(2);
        return world;
    }

    /// <summary>
    /// 在镜头内布置六类普通敌人、蕾米莉亚自机、芙兰朵露 Boss，以及两阵营静止弹幕用于裁切检查。
    /// </summary>
    private static void PopulateCombatAcceptanceScene(WorldDemo world, string playerCharacterId)
    {
        var player = world.GetNode<PlayerController>("Player");
        var ecs = world.GetNode<EcsCombatWorld>("CombatEntities/EcsCombatWorld");
        Vector2 center = player.GlobalPosition;
        EnemyDefinition[] enemies = EnemyCatalog.All
            .Where(definition => definition.RequiredContentPack is null)
            .Take(6)
            .ToArray();
        Vector2[] offsets =
        [
            new(-165.0f, -85.0f), new(-92.0f, -122.0f), new(-18.0f, -138.0f),
            new(62.0f, -124.0f), new(138.0f, -82.0f), new(-155.0f, 38.0f),
        ];
        for (int index = 0; index < enemies.Length; index++)
        {
            ecs.SpawnEnemy(center + offsets[index], enemies[index]);
        }

        IReadOnlyList<CharacterDefinition> candidates = CharacterBossCatalog.GetCandidates(
            world.RunContext.ContentSelection, playerCharacterId);
        int flandreIndex = Enumerable.Range(0, candidates.Count).Single(
            index => candidates[index].DisplayName == FlandreName);
        Vector2 bossPosition = center + new Vector2(175.0f, 8.0f);
        Require(world.BossEncounters.TrySpawn(bossPosition, ecs.ElapsedSeconds, flandreIndex),
            "Could not spawn Flandre through the formal Boss director.");

        for (int index = 0; index < 10; index++)
        {
            Vector2 direction = Vector2.Right.Rotated(Mathf.Tau * index / 10.0f);
            ecs.SpawnProjectile(center + direction * 50.0f, direction, 0.0f, 1);
        }

        for (int index = 0; index < 18; index++)
        {
            Vector2 direction = Vector2.Right.Rotated(Mathf.Tau * index / 18.0f);
            ecs.SpawnEnemyProjectile(
                bossPosition + direction * 54.0f, Vector2.Zero, 0.0f, 1, index % 4);
        }

        ecs.QueueRedraw();
    }

    /// <summary>
    /// 读取正式绘制统计和节点边界，保证截图确实覆盖原作敌人、完整角色 Boss、两阵营弹幕及紧凑状态栏。
    /// </summary>
    private static void VerifyRenderedCombat(WorldDemo world)
    {
        var playerVisual = world.GetNode<PlayerVisualController>("Player/Visual");
        var playerSprite = playerVisual.GetNode<Sprite2D>("Sprite");
        var ecs = world.GetNode<EcsCombatWorld>("CombatEntities/EcsCombatWorld");
        var status = world.GetNode<Control>("WorldDebugHud/StatusMargin");
        Rect2 logicalViewport = new(Vector2.Zero, new Vector2(640.0f, 360.0f));
        Require(playerVisual.DisplayName == RemiliaName && playerVisual.UsesSprite &&
            playerSprite.TextureFilter == CanvasItem.TextureFilterEnum.Nearest,
            "Remilia player visual is missing or not using nearest-neighbor sampling.");
        Require(ecs.MappedEnemyVisualCount >= 6 && ecs.MappedBossVisualCount == 1 &&
            ecs.FallbackBossVisualCount == 0,
            "Formal ECS screenshot is missing mapped ordinary enemies or the mapped Flandre Boss.");
        Require(ecs.ProjectileIconVisualCount >= 28 && ecs.EnemyProjectileIconVisualCount >= 18,
            "Formal ECS screenshot is missing player or enemy bullet atlas visuals.");
        Require(logicalViewport.Encloses(status.GetGlobalRect()) && status.Size.Y <= 48.0f,
            "World status bar extends beyond the logical viewport or became oversized.");
    }

    /// <summary>
    /// 等待指定数量处理帧，使窗口缩放、容器布局、ECS 绘制和像素动画都完成一次稳定提交。
    /// </summary>
    private async Task WaitForFrames(int count)
    {
        for (int frame = 0; frame < count; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    /// <summary>
    /// 从真实视口读取画面并保存 PNG；无窗口回归保留布局与素材断言，仅明确跳过像素捕获。
    /// </summary>
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
        GD.Print($"Visual acceptance screenshot: {path} ({width}x{height})");
    }

    /// <summary>把视觉验收失败转换为带有明确原因的异常，使渲染测试以非零状态退出。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
