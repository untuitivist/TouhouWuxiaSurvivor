using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Tests.Support;
using TouhouWuxiaSurvivor.Ui.Content;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证内容面板选定角色后，正式世界会应用同一身份、视觉和自机属性，并从本局 Boss 池排除自身。
/// </summary>
public partial class CharacterRuntimeSmokeTest : Node
{
    /// <summary>
    /// 走过真实内容面板提交和 WorldDemo 装配链，任何失败都在清理音频与全局选择后明确退出。
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
            string selectedId = await SelectCharacterThroughPanel("蕾米莉亚·斯卡蕾特");
            CharacterDefinition selected = CharacterCatalog.GetRequired(selectedId);

            world = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn")
                .Instantiate<WorldDemo>();
            world.PersistMetaProgression = false;
            world.GetNode<EnemySpawner>("EnemySpawner").InitialSpawnCount = 0;
            AddChild(world);
            VerifyRuntime(world, selected, content);
            GD.Print("Character runtime smoke test passed.");
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
    /// 在真实选择面板找到目标姓名、提交按钮并返回服务保存的稳定角色 ID。
    /// </summary>
    private async Task<string> SelectCharacterThroughPanel(string displayName)
    {
        var panel = GD.Load<PackedScene>(
            "res://src/ui/content/ContentPackSelectionPanel.tscn")
            .Instantiate<ContentPackSelectionPanel>();
        var designSurface = new Control
        {
            Name = "CharacterSelectionDesignSurface",
            Size = new Vector2(640.0f, 360.0f),
        };
        AddChild(designSurface);
        designSurface.AddChild(panel);
        panel.Present();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        VerifySelectionLayout(panel);
        var choice = panel.GetNode<OptionButton>(
            "Panel/Padding/Layout/CharacterSelection/CharacterChoice");
        int index = Enumerable.Range(0, choice.ItemCount).Single(
            item => choice.GetItemText(item) == displayName);
        string expectedId = CharacterCatalog.GetRequiredByDisplayName(displayName).CharacterId;
        string metadataId = choice.GetItemMetadata(index).AsString();
        Require(metadataId == expectedId && metadataId != displayName &&
            metadataId.StartsWith("character_", StringComparison.Ordinal),
            "Character option metadata does not carry the catalog stable ID.");
        choice.Select(index);
        bool submitted = false;
        panel.StartRequested += () => submitted = true;
        panel.GetNode<Button>("Panel/Padding/Layout/Commands/Start")
            .EmitSignal(BaseButton.SignalName.Pressed);
        Require(submitted && CharacterSelectionService.Current.CharacterId == expectedId &&
            CharacterSelectionService.Current.Current.DisplayName == displayName,
            "Content panel did not commit the selected character.");
        string characterId = CharacterSelectionService.Current.CharacterId;
        designSurface.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        return characterId;
    }

    /// <summary>
    /// 核对角色定义在正式玩家壳中的全部投影，以及同一稳定身份不会进入本局 Boss 候选。
    /// </summary>
    private static void VerifyRuntime(
        WorldDemo world,
        CharacterDefinition expected,
        ContentPackSelection content)
    {
        var player = world.GetNode<PlayerController>("Player");
        var visual = player.GetNode<PlayerVisualController>("Visual");
        var health = player.GetNode<PlayerHealth>("Health");
        var shooter = player.GetNode<AutoShooter>("AutoShooter");
        Require(world.RunContext.CharacterSelection.CharacterId == expected.CharacterId &&
            visual.DisplayName == expected.DisplayName && visual.UsesSprite,
            "World player identity or mapped original visual is incorrect.");
        Require(health.MaxHealth == (int)MathF.Round(expected.PlayableProfile.MaxHealth) &&
            Mathf.IsEqualApprox(player.MoveSpeed,
                120.0f * expected.PlayableProfile.MoveSpeedMultiplier) &&
            shooter.Damage == Math.Max(1, (int)MathF.Round(
                expected.PlayableProfile.AttackMultiplier)),
            "Playable character profile was not applied to runtime stats.");
        Require(CharacterBossCatalog.GetCandidates(content, expected.CharacterId).All(
                candidate => candidate.CharacterId != expected.CharacterId),
            "Selected player leaked into the current Boss candidate pool.");

        var ecsWorld = world.GetNode<EcsCombatWorld>("CombatEntities/EcsCombatWorld");
        Require(world.BossEncounters.TrySpawn(player.Position + new Vector2(240.0f, 0.0f),
                ecsWorld.ElapsedSeconds, candidateIndex: 0),
            "Formal world Boss encounter director could not spawn an eligible character.");
        Require(world.BossEncounters.LastSpawnedCharacter is not null &&
            world.BossEncounters.LastSpawnedCharacter.CharacterId != expected.CharacterId &&
            ecsWorld.AliveBossCount == 1,
            "Formal Boss runtime reused the player identity or failed to register one live Boss.");
    }

    /// <summary>
    /// 在项目 640x360 逻辑视口内锁定选择卷轴、角色行、滚动列表和命令行的边界，防止扩充角色后溢出。
    /// </summary>
    private static void VerifySelectionLayout(ContentPackSelectionPanel selection)
    {
        var panel = selection.GetNode<Control>("Panel");
        var character = selection.GetNode<Control>(
            "Panel/Padding/Layout/CharacterSelection");
        var scroll = selection.GetNode<Control>("Panel/Padding/Layout/Scroll");
        var commands = selection.GetNode<Control>("Panel/Padding/Layout/Commands");
        int designWidth = ProjectSettings.GetSetting(
            "display/window/size/viewport_width").AsInt32();
        int designHeight = ProjectSettings.GetSetting(
            "display/window/size/viewport_height").AsInt32();
        Rect2 viewport = new(Vector2.Zero, new Vector2(designWidth, designHeight));
        Rect2 panelRect = panel.GetGlobalRect();
        Require(designWidth == 640 && designHeight == 360 && viewport.Encloses(panelRect),
            $"Content selection panel is not enclosed by the 640x360 design viewport: " +
            $"design={designWidth}x{designHeight}, panel={panelRect}.");
        Require(panel.GetCombinedMinimumSize().Y <= panel.Size.Y + 1.0f,
            "Content selection controls exceed the fixed scroll panel height.");
        Require(panelRect.Encloses(character.GetGlobalRect()) &&
            panelRect.Encloses(scroll.GetGlobalRect()) &&
            panelRect.Encloses(commands.GetGlobalRect()),
            "Character, content list, or command row extends beyond the selection panel.");
        Require(scroll.Size.Y >= 150.0f &&
            !character.GetGlobalRect().Intersects(commands.GetGlobalRect()),
            "Character selection compressed the content list or overlaps the commands.");
    }

    /// <summary>
    /// 将链路契约失败转换为包含具体原因的异常，便于无头测试定位角色配置回归。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
