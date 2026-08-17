namespace TouhouWuxiaSurvivor.Content;

/// <summary>
/// 区分内容资料库存、正在验收的开发内容和真正完成的内容包，避免把目录覆盖误报为玩法完成。
/// </summary>
public enum ContentPackStatus
{
    Inventory,
    Development,
    Complete,
}
