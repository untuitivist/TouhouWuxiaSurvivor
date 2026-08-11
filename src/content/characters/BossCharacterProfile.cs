namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 保存同一角色作为 Boss 时的基础生命、追击速度、接触伤害和碰撞半径。
/// </summary>
public sealed class BossCharacterProfile
{
    public float MaxHealth { get; }
    public float MoveSpeed { get; }
    public float ContactDamage { get; }
    public float CollisionRadius { get; }

    /// <summary>
    /// 构造经过正数校验的 Boss 属性，确保战斗系统获得完整且可计算的数据。
    /// </summary>
    public BossCharacterProfile(
        float maxHealth,
        float moveSpeed,
        float contactDamage,
        float collisionRadius)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHealth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(moveSpeed);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(contactDamage);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(collisionRadius);
        MaxHealth = maxHealth;
        MoveSpeed = moveSpeed;
        ContactDamage = contactDamage;
        CollisionRadius = collisionRadius;
    }
}
