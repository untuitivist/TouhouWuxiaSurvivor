using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

namespace TouhouWuxiaSurvivor.Gameplay.Encounters;

/// <summary>
/// 把角色 Boss 的耐久、移动、接触伤害和体型投影为符卡基础属性，避免逐卡写死最终弹幕数值。
/// </summary>
public static class BossSpellCardAttributeFactory
{
    /// <summary>
    /// 从同一角色档案和通用 Boss 射击档案建立快照，不在符卡层保存孤立最终数值。
    /// </summary>
    public static SpellCardBaseAttributes Create(CharacterDefinition character)
    {
        ArgumentNullException.ThrowIfNull(character);
        BossCharacterProfile boss = character.BossProfile;
        PlayableCharacterProfile aptitude = character.PlayableProfile;
        EnemyProjectileProfile projectile = EnemyProjectileProfile.Boss;
        float traversalRange = projectile.ProjectileSpeed * projectile.FireInterval;
        return new SpellCardBaseAttributes(
            projectile.Damage,
            projectile.FireInterval,
            traversalRange,
            projectile.ProjectileSpeed,
            projectile.FireInterval,
            aptitude.UltimateIntervalSeconds,
            aptitude.UltimateTargetCapacity,
            boss.CollisionRadius);
    }
}
