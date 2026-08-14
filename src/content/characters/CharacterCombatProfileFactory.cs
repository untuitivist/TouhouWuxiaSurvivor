namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 将战斗定位转换成自机与 Boss 的受控基础属性，使角色差异来自可复用规则而非随机摘要。
/// </summary>
public static class CharacterCombatProfileFactory
{
    /// <summary>
    /// 按定位建立自机属性；六套配置共享接近的总预算，但分别突出伤害、频率、身法、群攻或耐久。
    /// </summary>
    public static PlayableCharacterProfile CreatePlayable(CharacterCombatRole role) => role switch
    {
        CharacterCombatRole.Balanced => new(6.0f, 1.00f, 1.00f, 1.00f, 6.0f, 7),
        CharacterCombatRole.Power => new(5.0f, 0.94f, 1.16f, 1.12f, 6.5f, 6),
        CharacterCombatRole.Rapid => new(5.0f, 1.04f, 0.92f, 0.88f, 5.2f, 6),
        CharacterCombatRole.Swift => new(4.0f, 1.15f, 1.00f, 0.98f, 5.8f, 6),
        CharacterCombatRole.Formation => new(5.0f, 0.96f, 0.96f, 0.99f, 6.4f, 8),
        CharacterCombatRole.Guardian => new(7.0f, 0.90f, 0.96f, 1.02f, 6.6f, 7),
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown character role.")
    };

    /// <summary>
    /// 按同一定位建立 Boss 属性；接触伤害固定在一至二点，避免基础角色被单次接触击倒。
    /// </summary>
    public static BossCharacterProfile CreateBoss(CharacterCombatRole role) => role switch
    {
        CharacterCombatRole.Balanced => new(900.0f, 36.0f, 1.0f, 20.0f),
        CharacterCombatRole.Power => new(1050.0f, 32.0f, 2.0f, 21.0f),
        CharacterCombatRole.Rapid => new(780.0f, 42.0f, 1.0f, 18.0f),
        CharacterCombatRole.Swift => new(720.0f, 46.0f, 1.0f, 17.0f),
        CharacterCombatRole.Formation => new(900.0f, 34.0f, 1.0f, 22.0f),
        CharacterCombatRole.Guardian => new(1200.0f, 28.0f, 2.0f, 23.0f),
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown character role.")
    };
}
