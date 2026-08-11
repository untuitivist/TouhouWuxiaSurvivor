using Godot;
using TouhouWuxiaSurvivor.Settings;
using TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;
using TouhouWuxiaSurvivor.Ui.Compendium;
using TouhouWuxiaSurvivor.Ui.Content;
using TouhouWuxiaSurvivor.Ui.Settings;
using TouhouWuxiaSurvivor.Ui.Meta;
using TouhouWuxiaSurvivor.Content.Characters;

namespace TouhouWuxiaSurvivor.Ui.Menu;

/// <summary>
/// 提供进入无限世界、调整共享设置和退出游戏的主菜单入口。
/// </summary>
public partial class MainMenu : Control
{
    private Control? _menu;
    private SettingsPanel? _settings;
    private ContentPackSelectionPanel? _contentSelection;
    private CompendiumPanel? _compendium;
    private CultivationPanel? _cultivation;

    /// <summary>
    /// 初始化持久化设置并连接主菜单按钮与设置返回信号。
    /// </summary>
    public override void _Ready()
    {
        GameSettingsService.Initialize();
        GetTree().Paused = false;
        _menu = GetNode<Control>("Menu");
        _settings = GetNode<SettingsPanel>("SettingsPanel");
        _contentSelection = GetNode<ContentPackSelectionPanel>("ContentPackSelectionPanel");
        _compendium = GetNode<CompendiumPanel>("CompendiumPanel");
        _cultivation = GetNode<CultivationPanel>("CultivationPanel");
        _cultivation.Configure(ProgressionProfileManager.CreateDefault());
        GetNode<Button>("Menu/Panel/Padding/Layout/Start").Pressed += ShowContentSelection;
        GetNode<Button>("Menu/Panel/Padding/Layout/Compendium").Pressed += ShowCompendium;
        GetNode<Button>("Menu/Panel/Padding/Layout/Cultivation").Pressed += ShowCultivation;
        GetNode<Button>("Menu/Panel/Padding/Layout/Settings").Pressed += ShowSettings;
        GetNode<Button>("Menu/Panel/Padding/Layout/Quit").Pressed += Quit;
        _contentSelection.StartRequested += StartGame;
        _contentSelection.BackRequested += HideContentSelection;
        _compendium.BackRequested += HideCompendium;
        _cultivation.BackRequested += HideCultivation;
        _settings.BackRequested += HideSettings;
        _settings.Hide();
        RefreshRoleBlock();
    }

    /// <summary>
    /// 切换到无限世界主场景，新的世界由场景配置中的种子重新生成。
    /// </summary>
    private void StartGame() => GetTree().ChangeSceneToFile("res://src/demo/WorldDemo.tscn");

    /// <summary>
    /// 隐藏主菜单命令并打开由内容包清单驱动的本局选择列表。
    /// </summary>
    private void ShowContentSelection()
    {
        _menu!.Hide();
        _contentSelection!.Present();
    }

    /// <summary>
    /// 从内容选择返回时恢复主菜单命令区域。
    /// </summary>
    private void HideContentSelection() => _menu!.Show();

    /// <summary>
    /// 隐藏主菜单命令并打开由运行目录自动构建的幻想乡图鉴。
    /// </summary>
    private void ShowCompendium()
    {
        _menu!.Hide();
        _compendium!.Present();
    }

    /// <summary>
    /// 从图鉴返回时恢复主菜单命令区域。
    /// </summary>
    private void HideCompendium() => _menu!.Show();

    /// <summary>
    /// 隐藏主菜单命令并打开幻想乡钱财、解锁与博丽神社整备单页。
    /// </summary>
    private void ShowCultivation()
    {
        _menu!.Hide();
        _cultivation!.Present();
    }

    /// <summary>
    /// 从神社整备返回时恢复主菜单命令区域。
    /// </summary>
    private void HideCultivation() => _menu!.Show();

    /// <summary>
    /// 隐藏主菜单命令并显示复用设置面板。
    /// </summary>
    private void ShowSettings()
    {
        _menu!.Hide();
        _settings!.Show();
    }

    /// <summary>
    /// 从设置页返回主菜单命令区域。
    /// </summary>
    private void HideSettings()
    {
        _settings!.Hide();
        _menu!.Show();
    }

    /// <summary>
    /// 请求 Godot 正常结束游戏进程。
    /// </summary>
    private void Quit() => GetTree().Quit();

    /// <summary>
    /// 用共享角色选择刷新主菜单题签和基础倍率，长姓名按字符数缩小以保持单行不挤压山水区域。
    /// </summary>
    private void RefreshRoleBlock()
    {
        CharacterDefinition character = CharacterSelectionService.Current.Current;
        Label name = GetNode<Label>("Menu/RoleBlock/Layout/CharacterName");
        name.Text = character.DisplayName;
        name.AddThemeFontSizeOverride("font_size", character.DisplayName.Length switch
        {
            <= 5 => 26,
            <= 9 => 20,
            _ => 15,
        });
        GetNode<Label>("Menu/RoleBlock/Layout/Role").Text =
            $"生命 {character.PlayableProfile.MaxHealth:0}  ·  " +
            $"身法 ×{character.PlayableProfile.MoveSpeedMultiplier:0.00}  ·  " +
            $"攻势 ×{character.PlayableProfile.AttackMultiplier:0.00}";
    }
}
