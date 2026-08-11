namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 保存角色成为自机时的基础生命、移动速度和攻击倍率，供世界组合根统一应用。
/// </summary>
public sealed class PlayableCharacterProfile
{
    public float MaxHealth { get; }
    public float MoveSpeedMultiplier { get; }
    public float AttackMultiplier { get; }

    /// <summary>
    /// 构造经过边界校验的自机属性，防止内容数据产生无法游玩的零值或负值角色。
    /// </summary>
    public PlayableCharacterProfile(
        float maxHealth,
        float moveSpeedMultiplier,
        float attackMultiplier)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHealth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(moveSpeedMultiplier);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attackMultiplier);
        MaxHealth = maxHealth;
        MoveSpeedMultiplier = moveSpeedMultiplier;
        AttackMultiplier = attackMultiplier;
    }
}
