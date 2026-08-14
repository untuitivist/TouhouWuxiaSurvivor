using TouhouWuxiaSurvivor.Content.Characters;

namespace TouhouWuxiaSurvivor.Ui;

/// <summary>
/// 将角色战斗定位投影为统一中文名称与简短玩法说明，避免状态页和构筑页各自维护文案。
/// </summary>
public static class CharacterCombatRoleText
{
    /// <summary>返回适合标题栏和构筑总览的双字定位名称。</summary>
    public static string GetName(CharacterCombatRole role) => role switch
    {
        CharacterCombatRole.Balanced => "均衡",
        CharacterCombatRole.Power => "强攻",
        CharacterCombatRole.Rapid => "速射",
        CharacterCombatRole.Swift => "身法",
        CharacterCombatRole.Formation => "阵法",
        CharacterCombatRole.Guardian => "守御",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    /// <summary>返回定位所强调的基础属性组合，只描述横向差异而不暗示强弱等级。</summary>
    public static string Describe(CharacterCombatRole role) => role switch
    {
        CharacterCombatRole.Balanced => "攻、防、身法均衡",
        CharacterCombatRole.Power => "单击更重，节奏稍缓",
        CharacterCombatRole.Rapid => "射击更密，单击稍轻",
        CharacterCombatRole.Swift => "移动迅捷，耐久偏低",
        CharacterCombatRole.Formation => "范围与奥义承载见长",
        CharacterCombatRole.Guardian => "耐久与护持见长",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}
