namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 标识局内选择自然形成的五类构筑亲和；亲和只由玩家已选内容累积，不读取角色、地区或内容包。
/// </summary>
public enum RunUpgradeAffinity
{
    Force,
    Precision,
    Swiftness,
    Formation,
    Guard,
}
