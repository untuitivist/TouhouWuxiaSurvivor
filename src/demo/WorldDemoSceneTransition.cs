using Godot;

namespace TouhouWuxiaSurvivor.Demo;

/// <summary>
/// 集中维护一局结束后的场景跳转，并确保任何入口都不会把暂停状态带入下一场景。
/// </summary>
public static class WorldDemoSceneTransition
{
    /// <summary>
    /// 解除暂停并重新载入正式世界，保留当前内容和角色选择服务中的本局配置。
    /// </summary>
    public static void Restart(SceneTree tree)
    {
        tree.Paused = false;
        tree.ReloadCurrentScene();
    }

    /// <summary>
    /// 解除暂停并切回主菜单，避免死亡与结算覆盖层继续持有输入所有权。
    /// </summary>
    public static void ReturnToMainMenu(SceneTree tree)
    {
        tree.Paused = false;
        tree.ChangeSceneToFile("res://src/ui/menu/MainMenu.tscn");
    }
}
